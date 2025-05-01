using MyGraph.Models;
using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace MyGraph.ViewModels
{
  class NodeVM : CanvasItem
  {
    public string name
    {
      get => Get<string>();
      set => Set(value);
    }

    public double MinWidth
    {
      get => Get<double>();
      set
      {
        Set(value);
        if (value > Width)
          Width = value;
      }
    }
    public double MinHeight
    {
      get => Get<double>();
      set
      {
        Set(value);
        if (value > Height)
          Height = value;
      }
    }


    public double Width
    {
      get => Get<double>();
      set => Set(value);
    }
    public double Height
    {
      get => Get<double>();
      set => Set(value);
    }


    public bool IsSelected
    {
      get => Get<bool>();
      set
      {
        if (value)
          ZIndex = Canvas.Nodes.Max(n => n.ZIndex) + 1;
        Set(value);

      }
    }


    public ObservableCollection<ConnectionVM> Inputs
    {
      get => Get<ObservableCollection<ConnectionVM>>();
      set => Set(value);
    }
    public ObservableCollection<ConnectionVM> Outputs
    {
      get => Get<ObservableCollection<ConnectionVM>>();
      set => Set(value);
    }
    public void move(double deltaX, double deltaY)
    {
      Position = new Point(Position.X + deltaX, Position.Y + deltaY);

      foreach (ConnectionVM connection in Inputs)
      {
        connection.moveEnd(deltaX, deltaY);
      }

      foreach (ConnectionVM connection in Outputs)
      {
        connection.moveStart(deltaX, deltaY);
      }
    }

    public void connectNode(NodeVM node)
    {
      Debug.Assert(node != null);
      Debug.Assert(node != this);
      Debug.Assert(Outputs.Where(n => n.End == node).FirstOrDefault() == null);
      Debug.Assert(node.Inputs.Where(n => n.Start == this).FirstOrDefault() == null);
      Debug.Assert(Canvas.Connections.Where(c => c.End == node && c.Start == this).Count() == 0);

      ConnectionVM connectionVM = new ConnectionVM(this, node);

    }
    public void disconnectNode(NodeVM node)
    {
      Debug.Assert(node != null);
      Debug.Assert(node != this);
      Debug.Assert(Outputs.Where(n => n.End == node).Count() == 1);
      Debug.Assert(node.Inputs.Where(n => n.Start == this).Count() == 1);
      Debug.Assert(Canvas.Connections.Where(c => c.End == node && c.Start == this).Count() == 1);

      node.Inputs.Where(n => n.Start == this).FirstOrDefault().Delete();

    }


    public IRelayCommand AddGhostOutputCommand { get; private set; }

    public void createCommands()
    {
      AddGhostOutputCommand = new RelayCommand(addGhostOutput);
    }
    public void addGhostOutput()
    {
      Canvas.CurrentAction = Action.ConnectingOutput;
      new ConnectionVM(this, null);
    }

    public bool IsDragging
    {
      get => Get<bool>();
      set => Set(value);
    }

    public bool isAllreadyConnectedTo(NodeVM input)
    {
      return Canvas.Connections.Where(c => c.Start == this && c.End == input).Count() != 0;
    }

    public void MouseDown(MouseButtonEventArgs ev)
    {

      if (ev.LeftButton != MouseButtonState.Pressed)
        return;

      if (Canvas.CurrentAction == Action.ConnectingOutput
        && Canvas.GhostConnection.Start != this
        && !Canvas.GhostConnection.Start.isAllreadyConnectedTo(this))
      {
        Canvas.GhostConnection.Start.connectNode(this);
        Canvas.CurrentAction = Action.None;
        return;
      }

      if (!Keyboard.IsKeyDown(Key.LeftShift) && !IsSelected)
      {
        foreach (NodeVM node in Canvas.Nodes)
        {
          node.IsSelected = false;
        }
      }
      IsSelected = !IsSelected;
      Canvas.CurrentAction = Action.Dragging;
    }

    public void MouseUp(MouseButtonEventArgs ev)
    {
      if (ev.LeftButton != MouseButtonState.Released)
        return;
      Canvas.CurrentAction = Action.None;
    }
    public void MouseEnter()
    {
      if (Canvas.CurrentAction == Action.ConnectingOutput)
      {
        Canvas.GhostConnection.End = this;
        Inputs.Add(Canvas.GhostConnection);
        Canvas.GhostConnection.updateInput();
      }

    }
    public void MouseLeave()
    {

      if (Canvas.CurrentAction == Action.ConnectingOutput)
      {
        Inputs.Remove(Canvas.GhostConnection);
        Canvas.GhostConnection.End = null;
      }

    }


    public NodeVM()
    {
      MinWidth = 200;
      MinHeight = 100;
      Outputs = new ObservableCollection<ConnectionVM>();
      Inputs = new ObservableCollection<ConnectionVM>();
      createCommands();

      Canvas.Nodes.Add(this);

    }

  }
}
