using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using MyGraph.Utilities;
using MyGraph.ViewModels;

namespace MyGraph.Models
{
    abstract class Connectable : CanvasItem
    {
        private List<Type> allowConnectableTypes;

        public ObservableCollection<Connection> Inputs
        {
            get => Get<ObservableCollection<Connection>>();
            set { Set(value); value.CollectionChanged += Inputs_CollectionChanged; }
        }

        public ObservableCollection<Connection> Outputs
        {
            get => Get<ObservableCollection<Connection>>();
            set { Set(value); value.CollectionChanged += Outputs_CollectionChanged; }
        }

        #region Methods
        public void connect(Connectable connectable, List<TransferUnitVM> transferUnits = null)
        {
            Debug.Assert(connectable != null);
            if (connectable == null)
            {
                return;
            }
            //Debug.Assert(node != this);
            if (connectable == this)
            {
                return;
            }

            if (Outputs.Where(n => n.End == connectable).FirstOrDefault() != null)
            {
                return;
            }
            Debug.Assert(Canvas.Connections.Where(c => c.End == connectable && c.Start == this).Count() == 0);


            ConnectionVM connectionVM = new ConnectionVM(this, connectable);
            if (transferUnits != null)
            {
                foreach (TransferUnitVM transferUnit in transferUnits)
                {
                    connectionVM.addTransferUnit(transferUnit);
                    transferUnit.Connections.Add(connectionVM);
                }
            }
            connectable.orderConnections();
            this.orderConnections();
        }
        public bool isAllreadyConnectedTo(Connectable input)
        {
            return Canvas.Connections.Where(c => c.Start == this && c.End == input).Count() != 0;
        }

        public override Point Position
        {
            get => Get<Point>();
            set
            {
                Set(value);

                updateInputs();
                updateOutputs();
                foreach (ConnectionVM connection in Outputs)
                {
                    connection.End.orderConnections();
                }
                foreach (ConnectionVM connection in Inputs)
                {
                    connection.Start.orderConnections();
                }
            }
        }



        public void disconnect(Connectable connectable)
        {
            Debug.Assert(connectable != null);
            if (connectable == null) return;

            Debug.Assert(connectable != this);
            if (connectable == this) return;

            Debug.Assert(Outputs.Where(n => n.End == connectable).Count() == 1);
            if (Outputs.Where(n => n.End == connectable).Count() != 1) return;

            Debug.Assert(connectable.Inputs.Where(n => n.Start == this).Count() == 1);
            if (connectable.Inputs.Where(n => n.Start == this).Count() != 1) return;

            Debug.Assert(Canvas.Connections.Where(c => c.End == connectable && c.Start == this).Count() == 1);
            if (Canvas.Connections.Where(c => c.End == connectable && c.Start == this).Count() != 1) return;
            connectable.Inputs.Where(n => n.Start == this).FirstOrDefault().Delete();

        }

        public void RemoveConnections()
        {
            for (int i = Outputs.Count - 1; i >= 0; i--)
            {
                Outputs.ElementAt(i).Delete();
            }

            for (int i = Inputs.Count - 1; i >= 0; i--)
            {
                Inputs.ElementAt(i).Delete();
            }
        }

        public void orderConnections()
        {
            List<Connection> orderedOutputs = Outputs.OrderBy(c => c.End.Position.Y).ToList();

            if (!Outputs.SequenceEqual(orderedOutputs))
            {
                Outputs = new ObservableCollection<Connection>(orderedOutputs);
                updateOutputs();
            }

            List<Connection> orderedInputs = Inputs.OrderBy(c => c.Start.Position.Y).ToList();
            if (!Outputs.SequenceEqual(orderedInputs))
            {
                Inputs = new ObservableCollection<Connection>(orderedInputs);
                updateInputs();
            }

        }


        public void updateOutputs()
        {

            if (Outputs == null)
                return;
            foreach (Connection connection in Outputs)
            {
                connection.updateOutput();
            }
        }

        public void updateInputs()
        {
            if (Inputs == null)
                return;

            foreach (Connection connection in Inputs)
            {
                connection.updateInput();
            }
        }

        #endregion

        public override void handleConnection()
        {
            if (Canvas.GhostConnection.Start != this
              && Canvas.GhostConnection.Start != null
              && !Canvas.GhostConnection.Start.isAllreadyConnectedTo(this))
            {
                Connectable start = Canvas.GhostConnection.Start;
                Canvas.GhostConnection.Delete();
                start.connect(this);
                Canvas.CurrentAction = ViewModels.Action.None;
            }
        }

        #region Events
        public void MouseEnter()
        {
            if (Canvas.CurrentAction == ViewModels.Action.ConnectingOutput
                     && Canvas.GhostConnection.Start != this
                     && !Canvas.GhostConnection.Start.isAllreadyConnectedTo(this))
            {
                Canvas.GhostConnection.End = this;
                ZIndex = Canvas.Nodes.Max(n => n.ZIndex) + 1;
            }

        }

        public void MouseLeave()
        {
            if (Canvas.CurrentAction == ViewModels.Action.ConnectingOutput
              && Canvas.GhostConnection.End == this)
            {
                Canvas.GhostConnection.End = null;
                Inputs.Remove(Canvas.GhostConnection);
                Canvas.GhostConnection.moveEndToMouse();
            }

        }

        private void Inputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ObservableCollection<Connection> newInputs = (ObservableCollection<Connection>)sender;
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (newInputs.Count() > Outputs.Count() && newInputs.Count() != 1)
                    {
                        Height += 15;
                        updateOutputs();
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    if (newInputs.Count() >= Outputs.Count() && newInputs.Count() != 0)
                    {
                        Height -= 15;
                        updateOutputs();
                    }
                    break;
                default:
                    break;
            }

            updateInputs();
            Canvas.OnPropertyChanged(nameof(Canvas.SelectedNodesInputs));
        }

        private void Outputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            ObservableCollection<Connection> newOutputs = (ObservableCollection<Connection>)sender;
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (newOutputs.Count() > Inputs.Count() && newOutputs.Count() != 1)
                    {
                        Height += 23;
                        updateInputs();
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    if (newOutputs.Count() >= Inputs.Count() && newOutputs.Count() != 0)
                    {
                        Height -= 23;
                        updateInputs();
                    }
                    break;
                default:
                    break;
            }

            updateOutputs();
            Canvas.OnPropertyChanged(nameof(Canvas.SelectedNodesOutputs));
        }

        #endregion

    }
}