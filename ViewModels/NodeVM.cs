using MyGraph.Models;
using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
    public string Name
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
        {
          if (Canvas.SelectedNodes.IndexOf(this) == -1)
          {
            Canvas.SelectedNodes.Add(this);
          }
          ZIndex = Canvas.Nodes.Max(n => n.ZIndex) + 1;
        }
        else
        {
          Canvas.SelectedNodes.Remove(this);
        }

        Set(value);

      }
    }


    public ObservableCollection<Connection> Inputs
    {
      get => Get<ObservableCollection<Connection>>();
      set => Set(value);
    }
    public ObservableCollection<Connection> Outputs
    {
      get => Get<ObservableCollection<Connection>>();
      set => Set(value);
    }

    public void move(double deltaX, double deltaY)
    {
      if (IsLocked)
        return;
      Position = new Point(Position.X + deltaX, Position.Y + deltaY);

      foreach (Connection connection in Inputs)
      {
        connection.moveEnd(deltaX, deltaY);
      }

      foreach (Connection connection in Outputs)
      {
        connection.moveStart(deltaX, deltaY);
      }
    }

    public void connectNode(NodeVM node)
    {
      Debug.Assert(node != null);
      Debug.Assert(node != this);
      Debug.Assert(Outputs.Where(n => n.End == node).FirstOrDefault() == null);
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
      new PreviewConnectionVM(this);
    }
    public bool IsLocked
    {
      get => Get<bool>();
      set => Set(value);
    }


    public bool IsDragging
    {
      get => Get<bool>();
      set => Set(value);
    }

    public void updateOutputs()
    {
      foreach (Connection connection in Outputs)
      {
        connection.updateOutput();
      }
    }

    public void updateInputs()
    {
      foreach (Connection connection in Inputs)
      {
        connection.updateInput();
      }
    }
    public bool isAllreadyConnectedTo(NodeVM input)
    {
      return Canvas.Connections.Where(c => c.Start == this && c.End == input).Count() != 0;
    }

    bool justSet = false;
    public void MouseDown(MouseButtonEventArgs ev)
    {

      if (ev.LeftButton != MouseButtonState.Pressed)
        return;

      if (Canvas.CurrentAction == Action.ConnectingOutput
        && Canvas.GhostConnection.Start != this
        && !Canvas.GhostConnection.Start.isAllreadyConnectedTo(this))
      {
        NodeVM start = Canvas.GhostConnection.Start;
        Canvas.GhostConnection.Delete();
        start.connectNode(this);
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
      if (!IsSelected)
      {
        justSet = true;
      }
      IsSelected = true;


      if (!IsLocked)
      {
        startDragPosition = Canvas.LastMousePosition;
        Canvas.CurrentAction = Action.Dragging;
      }
    }
    public void MouseRightDown(MouseButtonEventArgs ev)
    {
      IsLocked = !IsLocked;
    }


    private Point startDragPosition;
    public void MouseUp(MouseButtonEventArgs ev)
    {
      if (!justSet && Canvas.LastMousePosition == startDragPosition)
      {
        IsSelected = false;
      }
      justSet = false;

      Canvas.CurrentAction = Action.None;
    }
    public void MouseEnter()
    {
      if (Canvas.CurrentAction == Action.ConnectingOutput
               && Canvas.GhostConnection.Start != this
               && !Canvas.GhostConnection.Start.isAllreadyConnectedTo(this))
      {
        Canvas.GhostConnection.End = this;
        Canvas.GhostConnection.updateInput();
        ZIndex = Canvas.Nodes.Max(n => n.ZIndex) + 1;

      }

    }
    public void MouseLeave()
    {
      if (Canvas.CurrentAction == Action.ConnectingOutput
        && Canvas.GhostConnection.End == this)
      {
        Canvas.GhostConnection.End = null;

        Inputs.Remove(Canvas.GhostConnection);

        Canvas.GhostConnection.moveEndToMouse();

      }


    }
    public void Delete()
    {
      for (int i = Outputs.Count - 1; i >= 0; i--)
      {
        Outputs.ElementAt(i).Delete();
      }

      for (int i = Inputs.Count - 1; i >= 0; i--)
      {
        Inputs.ElementAt(i).Delete();
      }

      Canvas.SelectedNodes.Remove(this);
      Canvas.Nodes.Remove(this);

    }


    public NodeVM()
    {

      MinWidth = 150;
      MinHeight = 60;
      Position = new Point(2500, 2500);
      Outputs = new ObservableCollection<Connection>();
      Inputs = new ObservableCollection<Connection>();
      Inputs.CollectionChanged += Inputs_CollectionChanged;
      Outputs.CollectionChanged += Outputs_CollectionChanged;
      createCommands();

      Canvas.Nodes.Add(this);

    }

    private void Inputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      foreach (Connection c in sender as ObservableCollection<Connection>)
      {
        c.updateInput();
      }
    }

    private void Outputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      foreach (Connection c in sender as ObservableCollection<Connection>)
      {
        c.updateOutput();
      }
    }
  }
}
