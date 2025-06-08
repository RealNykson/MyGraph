using System.Collections.ObjectModel;

namespace MyGraph.Models
{
    public abstract class ConnectableConnection : Connection
    {
        public ConnectableConnection(Connectable output, Connectable input, Connection oldConnection = null) : base(output, input, oldConnection)
        {

        }
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
    }
}
