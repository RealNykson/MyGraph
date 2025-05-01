using MyGraph.Models;
using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MyGraph.ViewModels
{
  class ConnectionVM : Connection
  {

    public ConnectionVM(NodeVM output, NodeVM input) : base(output, input)
    {
      Canvas.Connections.Add(this);
    }

    public override void Delete()
    {
      NodeVM start = Start;
      NodeVM end = End;
      End = null;
      Start = null;
      Canvas.Connections.Remove(this);

      start.updateOutputs();
      end.updateInputs();

    }

    public void MouseDown()
    {
      NodeVM start = Start;
      Start.disconnectNode(End);
      new PreviewConnectionVM(start);

      Canvas.CurrentAction = Action.ConnectingOutput;
    }
  }
}
