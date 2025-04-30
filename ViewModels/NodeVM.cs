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
  class NodeVM : NotifyObject
  {
    public string name
    {
      get => Get<string>();
      set => Set(value);
    }

    public double XPos
    {
      get => Get<double>();
      private set => Set(value);
    }
    public double YPos
    {
      get => Get<double>();
      private set => Set(value);
    }


    public int ZIndex
    {
      get => Get<int>();
      set => Set(value);
    }

    public double MinWidth
    {
      get => Get<double>();
      set {
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
          ZIndex = MainWindowVM.g_Nodes.Max(n => n.ZIndex) + 1;
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
      XPos += deltaX;
      YPos += deltaY;

      foreach (ConnectionVM connection in Inputs)
      {
        connection.updateEnd(deltaX, deltaY);
      }

      foreach (ConnectionVM connection in Outputs)
      {
        connection.updateStart(deltaX, deltaY);
      }
    }

    public void connectNode(NodeVM node)
    {
      Debug.Assert(node != null);
      Debug.Assert(node != this);
      Debug.Assert(Outputs.Where(n => n.Input == node).FirstOrDefault() == null);
      Debug.Assert(node.Inputs.Where(n => n.Output == this).FirstOrDefault() == null);
      Debug.Assert(MainWindowVM.g_Connections.Where(c => c.Input == node && c.Output == this).Count() == 0);

      ConnectionVM connectionVM = new ConnectionVM(this, node);

      node.Inputs.Add(connectionVM);
      Outputs.Add(connectionVM);

      foreach (ConnectionVM connection in Outputs)
      {
        connection.updateNew(this, Outputs);
      }
      foreach (ConnectionVM connection in node.Inputs)
      {
        connection.updateNew(node, node.Inputs);
      }


      MainWindowVM.g_Connections.Add(connectionVM);

    }
    public void disconnectNode(NodeVM node)
    {
      Debug.Assert(node != null);
      Debug.Assert(node != this);
      Debug.Assert(Outputs.Where(n => n.Input == node).Count() == 1);
      Debug.Assert(node.Inputs.Where(n => n.Output == this).Count() == 1);
      Debug.Assert(MainWindowVM.g_Connections.Where(c => c.Input == node && c.Output == this).Count() == 1);

      node.Inputs.Remove(node.Inputs.Where(n => n.Output == this).FirstOrDefault());
      Outputs.Remove(Outputs.Where(n => n.Input == node).FirstOrDefault());

      foreach (ConnectionVM connection in Outputs)
      {
        connection.updateNew(this, Outputs);
      }
      foreach (ConnectionVM connection in node.Inputs)
      {
        connection.updateNew(node, node.Inputs);
      }

      MainWindowVM.g_Connections.Remove(MainWindowVM.g_Connections.Where(c => c.Input == node && c.Output == this).FirstOrDefault());

    }

    public IRelayCommand SelectChange { get; private set; }

    public void createCommands()
    {

    }

    public bool IsDragging
    {
      get => Get<bool>();
      set => Set(value);
    }


    public void MouseDown(MouseButtonEventArgs ev)
    {

      if (ev.LeftButton != MouseButtonState.Pressed)
        return;


      if (!Keyboard.IsKeyDown(Key.LeftShift) && !IsSelected)
      {
        foreach (NodeVM node in MainWindowVM.g_Nodes)
        {
          node.IsSelected = false;
        }
      }
      IsSelected = true;
      MainWindowVM.g_currentAction = MainWindowVM.Action.Dragging;
    }

    public void MouseUp(MouseButtonEventArgs ev)
    {
      if (ev.LeftButton != MouseButtonState.Released)
        return;
      MainWindowVM.g_currentAction = MainWindowVM.Action.None;
    }

    public NodeVM()
    {
      MinWidth = 200;
      MinHeight = 100;
      Outputs = new ObservableCollection<ConnectionVM>();
      Inputs = new ObservableCollection<ConnectionVM>();
      MainWindowVM.g_Nodes.Add(this);
    }

  }
}
