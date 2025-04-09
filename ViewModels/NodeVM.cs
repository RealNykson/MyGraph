using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
      set => Set(value);
    }

    public double YPos
    {
      get => Get<double>();
      set => Set(value);
    }
    public bool IsSelected
    {
      get => Get<bool>();
      set => Set(value);
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

      Mouse.OverrideCursor = Cursors.Hand;
      IsSelected = true;
      IsDragging = true;
    }

    public void MouseReleased(MouseButtonEventArgs ev)
    {
      if (ev.LeftButton != MouseButtonState.Released)
        return;

      Mouse.OverrideCursor = Cursors.Arrow;
      IsDragging = false;
    }

    public NodeVM()
    {

    }

  }
}
