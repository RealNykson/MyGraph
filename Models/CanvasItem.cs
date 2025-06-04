using MyGraph.Utilities;
using MyGraph.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MyGraph.Models
{
  public abstract class CanvasItem : NotifyObject
  {
    public int ZIndex
    {
      get => Get<int>();
      set => Set(value);
    }

    public bool IsSelected
    {
      get => Get<bool>();
      set
      {
        if (value)
        {
          ZIndex = Canvas.CanvasItems.Max(n => n.ZIndex) + 1;
        }

        Canvas.OnPropertyChanged(nameof(Canvas.SelectedCanvasItems));
        Canvas.OnPropertyChanged(nameof(Canvas.SelectedNodes));
        Canvas.OnPropertyChanged(nameof(Canvas.SelectedNodesInputs));
        Canvas.OnPropertyChanged(nameof(Canvas.SelectedNodesOutputs));
        Canvas.OnPropertyChanged(nameof(Canvas.IsOneSelectedItemLocked));

        Set(value);
      }
    }

    public bool IsLocked
    {
      get => Get<bool>();
      set
      {
        Set(value);
        Canvas.OnPropertyChanged(nameof(Canvas.IsOneSelectedItemLocked));
      }
    }

    public abstract void handleConnection();

    bool justSet = false;
    public void MouseDown(MouseButtonEventArgs ev)
    {

      if (ev.LeftButton != MouseButtonState.Pressed)
      {
        return;
      }

      if (Canvas.CurrentAction == ViewModels.Action.ConnectingOutput)
      {
        handleConnection();
        return;
      }

      if (!IsSelected)
      {
        justSet = true;
      }

      bool before = IsSelected;
      IsSelected = true;

      if (!Keyboard.IsKeyDown(Key.LeftShift) && !before)
      {
        foreach (CanvasItem item in Canvas.CanvasItems.Where(n => n != this))
        {
          item.IsSelected = false;
        }
      }

      if (!IsLocked)
      {
        startDragPosition = Canvas.LastMousePosition;
        Canvas.CurrentAction = ViewModels.Action.Dragging;
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

      Canvas.CurrentAction = ViewModels.Action.None;
    }
    public double Width { get => Get<double>(); set => Set(value); }
    public double Height { get => Get<double>(); set => Set(value); }

    public CanvasVM Canvas
    {
      get => Get<CanvasVM>();
      set => Set(value);
    }

    public virtual Point Position { get => Get<Point>(); set => Set(value); }

    public void moveAbsolute(double newPosX, double newPosY)
    {
      if (IsLocked)
      {
        return;
      }
      Position = new Point(newPosX, newPosY);
    }

    public CanvasItem()
    {
      Debug.Assert(CanvasVM.currentCanvas != null);
      Canvas = CanvasVM.currentCanvas;
    }
    public void move(double deltaX, double deltaY)
    {
      if (IsLocked)
      {
        return;
      }

      Position = new Point(Position.X + deltaX, Position.Y + deltaY);

    }

  }
}
