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
    public abstract class Connectable : CanvasItem
    {
        public Connectable()
        {
            Outputs = new ObservableCollection<ConnectableConnection>();
            Inputs = new ObservableCollection<ConnectableConnection>();
        }


        public ObservableCollection<ConnectableConnection> Inputs
        {
            get => Get<ObservableCollection<ConnectableConnection>>();
            //This Collection should never be set outside of the constructor because 
            //each Connector listens to the CollectionChanged event in ConnectorPositionBehavior
            //and setting it will cause the listener to be removed
            private set => Set(value);
        }

        public ObservableCollection<ConnectableConnection> Outputs
        {
            get => Get<ObservableCollection<ConnectableConnection>>();
            //This Collection should never be set outside of the constructor because 
            //each Connector listens to the CollectionChanged event in ConnectorPositionBehavior
            //and setting it will cause the listener to be removed
            private set => Set(value);
        }

        #region Methods
        public void connect(Connectable connectable, ConnectableConnection oldConnection = null, Connectable nextDestination = null)
        {


            Debug.Assert(connectable != null);
            if (connectable == null)
            {
                return;
            }


            if (connectable is TransferUnitVM transferUnit)
            {
                Debug.Assert(nextDestination != null);
                if (nextDestination == null)
                {
                    return;
                }

                ConnectableConnection connectionVM = this.Outputs.Where(n => n.End == connectable).FirstOrDefault();
                if (connectionVM == null)
                {
                    connectionVM = new ConnectionVM(this, connectable);
                }


                ConnectableConnection nextConnection = transferUnit.Outputs.Where(n => n.End == nextDestination).FirstOrDefault();
                if (nextConnection == null)
                {
                    nextConnection = new ConnectionVM(transferUnit, nextDestination);
                }

                transferUnit.addInternConnection(connectionVM, nextConnection);
            }
            else
            {

                ConnectionVM connectionVM = new ConnectionVM(this, connectable);
                ConnectableConnection connection = Outputs.Where(n => n.End == connectable).FirstOrDefault();
                Debug.Assert(Canvas.Connections.Where(c => c.End == connectable && c.Start == this).Count() == 0);
                if (connection != null && connection != Canvas.GhostConnection as ConnectableConnection)
                {
                    return;
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

                updateOutputs();
                updateInputs();


                /*
                * This orders the connections but the performance is really bad so currently disabled
                */

                //foreach (ConnectionVM connection in Outputs)
                //{
                //    connection.End.orderConnections();
                //}
                //foreach (ConnectionVM connection in Inputs)
                //{
                //    connection.Start.orderConnections();
                //}
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


            List<ConnectableConnection> orderedOutputs = Outputs.Where(c => c.End != null).OrderBy(c => c.End.Position.Y).ToList();

            if (!Outputs.SequenceEqual(orderedOutputs))
            {
                Outputs.Clear();
                foreach (ConnectableConnection connection in orderedOutputs)
                {
                    Outputs.Add(connection);
                }
                updateOutputs();
            }

            List<ConnectableConnection> orderedInputs = Inputs.OrderBy(c => c.Start.Position.Y).ToList();
            if (!Inputs.SequenceEqual(orderedInputs))
            {
                Inputs.Clear();
                foreach (ConnectableConnection connection in orderedInputs)
                {
                    Inputs.Add(connection);
                }
                updateInputs();
            }

        }

        public void moveOutputs(Point delta)
        {
            foreach (Connection connection in Outputs)
            {
                connection.moveStart(delta);
            }
        }

        public void moveInputs(Point delta)
        {
            foreach (Connection connection in Inputs)
            {
                connection.moveEnd(delta);
            }
        }

        public void updateOutputs()
        {

            Debug.Assert(Outputs != null);
            if (Outputs == null)
                return;
            foreach (ConnectableConnection connection in Outputs)
            {
                connection.updateOutput();
            }
        }

        public void updateInputs()
        {
            Debug.Assert(Inputs != null);
            if (Inputs == null)
                return;

            foreach (ConnectableConnection connection in Inputs)
            {
                connection.updateInput();
            }
        }

        #endregion
        //Can be overridden by subclasses to implement custom connection logic
        public virtual void customConnectionLogic(ConnectableConnection connection)
        {
            //Default connection logic
            Connectable start = Canvas.GhostConnection.Start;
            start.connect(this, Canvas.GhostConnection);
            Canvas.GhostConnection = null;
            Canvas.CurrentAction = ViewModels.Action.None;
        }

        public void handleConnection()
        {
            if (Canvas.GhostConnection.Start != this
              && Canvas.GhostConnection.Start != null
              && !Canvas.GhostConnection.Start.isAllreadyConnectedTo(this))
            {
                customConnectionLogic(Canvas.GhostConnection);
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
                this.orderConnections();
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

        #endregion

    }
}