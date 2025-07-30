using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using MyGraph.ViewModels;

namespace MyGraph.Models
{
    public abstract class ConnectableConnection : Connection
    {

        #region Constructor

        public ConnectableConnection(Connectable output, Connectable input, ConnectableConnection oldConnection = null)
        {
            if (oldConnection == null)
            {
                Start = output;
                End = input;
                return;
            }

            _Start = output;
            Canvas.Connections.Remove(oldConnection as ConnectionVM);
            oldConnection.End.Inputs.Remove(oldConnection);
            for (int i = 0; i < oldConnection.Start.Outputs.Count; i++)
            {
                if (oldConnection.Start.Outputs[i] == oldConnection)
                {
                    oldConnection.Start.Outputs[i] = this;
                    break;
                }
            }
            End = input;

        }
        #endregion

        #region Properties

        private Connectable _Start;
        public Connectable Start
        {
            get
            {
                return _Start;
            }
            set
            {
                if (_Start != null)
                {
                    _Start.Outputs.Remove(this);
                }

                _Start = value;
                if (value != null)
                {
                    value.Outputs.Add(this);
                }

            }
        }

        private Connectable _End;
        public Connectable End
        {
            get
            {
                return _End;
            }
            set
            {
                if (_End != null)
                {
                    _End.Inputs.Remove(this);
                }

                _End = value;
                if (value != null)
                {
                    value.Inputs.Add(this);
                    return;
                }

            }
        }
        public Point AbsoluteStart
        {
            get => Get<Point>();
            set { Set(value); Start.updateOutputs(); Start.updateInputs(); }
        }

        public Point AbsoluteEnd
        {
            get => Get<Point>();
            set { Set(value); End.updateInputs(); End.updateOutputs(); }
        }

        public void updateInput()
        {
            double PositionInputX = AbsoluteEnd.X + End.Position.X;
            double PositionInputY = AbsoluteEnd.Y + End.Position.Y;
            endPos = new Point(PositionInputX, PositionInputY);
        }

        public void updateOutput()
        {
            Debug.Assert(Start != null);
            if (Start == null)
                return;

            double PositionOutputX = AbsoluteStart.X + Start.Position.X;
            double PositionOutputY = AbsoluteStart.Y + Start.Position.Y;
            startPos = new Point(PositionOutputX, PositionOutputY);
        }

        #endregion
    }
}
