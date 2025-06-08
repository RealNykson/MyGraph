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
  public class ConnectionVM : ConnectableConnection
  {



    public ConnectionVM(Connectable output, Connectable input, ConnectableConnection oldConnection = null) : base(output, input, oldConnection)
    {
      Canvas.Connections.Add(this);
    }


    public override void Delete()
    {
      Connectable start = Start;
      Connectable end = End;
      End = null;
      Start = null;
      Canvas.Connections.Remove(this);

      start.updateOutputs();
      end.updateInputs();

    }


    public void MouseDown()
    {
      new PreviewConnectionVM(Start, this);
      Canvas.CurrentAction = Action.ConnectingOutput;
    }
  }
}
