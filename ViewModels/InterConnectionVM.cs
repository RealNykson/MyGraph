using MyGraph.Models;
using System.Windows;

namespace MyGraph.ViewModels
{
    public class InterConnectionVM : Connection
    {
        public ConnectableConnection previousConnection { get; set; }
        public ConnectableConnection nextConnection { get; set; }


        public InterConnectionVM(ConnectableConnection previous, ConnectableConnection next)
        {
            previousConnection = previous;
            nextConnection = next;
            startPos = previous.AbsoluteEnd;
            endPos = next.AbsoluteStart;

            previous.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConnectableConnection.AbsoluteEnd))
                    updateStartPos();
            };
            next.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConnectableConnection.AbsoluteStart))
                    updateEndPos();
            };

        }
        public void updateStartPos()
        {
            startPos = previousConnection.AbsoluteEnd;
        }
        public void updateEndPos()
        {
            endPos = nextConnection.AbsoluteStart;
        }

        public override void Delete()
        {
            return;
        }
    }
}