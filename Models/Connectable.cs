using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MyGraph.Utilities;
using MyGraph.ViewModels;

namespace MyGraph.Models
{
    public enum VisableState
    {
        Normal, // This is the default state which does not affect the Connectable
        PartOfSelection, // This state is used to indicate that the Connectable is part of the connection that is currently selected 
        PartOfHover, // This state is used to indicate that the Connectable is part of the connection that is currently hovered over
        Faded // This state is used to indicate that the is not part of Hover/Selection and thus should be faded out 
    }
    public abstract class Connectable : CanvasItem
    {



        public Connectable()
        {
            Outputs = new ObservableCollection<ConnectableConnection>();
            Inputs = new ObservableCollection<ConnectableConnection>();
        }

        public VisableState VisableState { get => Get<VisableState>(); set => Set(value); }

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

        /// <summary>
        /// Marks the whole reachable connection path starting from this Connectable.
        /// Two traversals are executed:
        ///  1. Forward  – following only outputs (or <c>nextConnection</c> for transfer units).
        ///  2. Backward – following only inputs  (or <c>previousConnection</c> for transfer units).
        /// The union of both reachable sets (including the starting element) is shown while every
        /// other connectable is faded.
        /// </summary>
        public void markWholeConnectionPath()
        {
            // All connectables that exist on the canvas right now.
            var allConnectables = Canvas.CanvasItems.OfType<Connectable>().ToList();

            var visited = new HashSet<Connectable>();

            // Traverse forward (outputs → … → outputs)
            TraverseDirectional(this, GetForwardNeighbours, visited);
            // Traverse backward (inputs  → … → inputs)
            TraverseDirectional(this, GetBackwardNeighbours, visited);

            // Ensure the start element itself is always part of the visible set.
            visited.Add(this);

            // Apply visibility.
            foreach (var c in allConnectables)
            {
                c.VisableState = visited.Contains(c) ? VisableState.Normal : VisableState.Faded;
            }

            // Apply visibility to connections as well.
            foreach (var connection in Canvas.Connections)
            {
                // Default: fade all connections
                connection.VisableState = VisableState.Faded;

                // If both endpoints are within the visible set, mark as normal
                if (connection.Start != null && connection.End != null &&
                    visited.Contains(connection.Start) && visited.Contains(connection.End))
                {
                    connection.VisableState = VisableState.Normal;
                }
            }
        }

        /// <summary>
        /// Generic breadth-first traversal where the <paramref name="neighbourSelector"/> defines
        /// the one-way direction (forward or backward).
        /// </summary>
        private static void TraverseDirectional(Connectable start,
                                               Func<Connectable, IEnumerable<Connectable>> neighbourSelector,
                                               HashSet<Connectable> visited)
        {
            if (!visited.Contains(start))
                visited.Add(start);

            var queue = new Queue<Connectable>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in neighbourSelector(current))
                {
                    if (next != null && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }
        }

        /// <summary>Immediate neighbours when traversing *forward* (outputs only).</summary>
        private static IEnumerable<Connectable> GetForwardNeighbours(Connectable c)
        {
            if (c is TransferUnitVM tu)
            {
                foreach (var ic in tu.InternConnections)
                {
                    var target = ic?.nextConnection?.End;
                    if (target != null)
                        yield return target;
                }
            }
            else
            {
                foreach (var conn in c.Outputs)
                {
                    var target = conn?.End;
                    if (target != null)
                        yield return target;
                }
            }
        }

        /// <summary>Immediate neighbours when traversing *backward* (inputs only).</summary>
        private static IEnumerable<Connectable> GetBackwardNeighbours(Connectable c)
        {
            if (c is TransferUnitVM tu)
            {
                foreach (var ic in tu.InternConnections)
                {
                    var source = ic?.previousConnection?.Start;
                    if (source != null)
                        yield return source;
                }
            }
            else
            {
                foreach (var conn in c.Inputs)
                {
                    var source = conn?.Start;
                    if (source != null)
                        yield return source;
                }
            }
        }

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
            markWholeConnectionPath();
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
            foreach (var item in Canvas.CanvasItems.Where(s => typeof(Connectable).IsAssignableFrom(s.GetType())))
            {
                if (item is Connectable connectable)
                {
                    connectable.VisableState = VisableState.Normal;
                }
            }
            foreach (var item in Canvas.Connections)
            {
                item.VisableState = VisableState.Normal;
            }


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