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

    public int Id { get; set; }

    public int ZIndex
    {
      get => Get<int>();
      set => Set(value);
    }

    public abstract bool IsDraggable { get; }

    public abstract bool IsSelectable { get; }

    public bool IsInView
    {
      get => Get<bool>();
      set => Set(value);
    }

    public bool IsSelected
    {
      get => Get<bool>();
      set
      {
        if (!IsSelectable)
        {
          return;
        }
        if (value)
        {
          ZIndex = Canvas.CanvasItems.Max(n => n.ZIndex) + 1;
        }

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

    bool justSet = false;
    public void MouseDown(MouseButtonEventArgs ev)
    {

      if (ev.LeftButton != MouseButtonState.Pressed)
      {
        return;
      }

      if (Canvas.CurrentAction == ViewModels.Action.ConnectingOutput && this is Connectable connectable)
      {
        connectable.handleConnection();
        return;
      }

      if (!IsLocked && IsDraggable)
      {
        startDragPosition = Canvas.LastMousePosition;
        Canvas.CurrentAction = ViewModels.Action.Dragging;
      }

      if (!IsSelected)
      {
        justSet = true;
      }

      bool before = IsSelected;
      IsSelected = true;

      if (!Keyboard.IsKeyDown(Key.LeftShift) && !before)
      {
        foreach (CanvasItem item in Canvas.CanvasItems.Where(n => n != this && n.IsSelected))
        {
          item.IsSelected = false;
        }
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

    public double Width
    {
      get => Get<double>();
      set => Set(value);
    }
    public double Height
    {
      get { return Get<double>(); }
      set { Set(value); }
    }

    /// <summary>
    /// This property is true if the TransforMatrix can look at the item.
    /// In other words: It is false if it is out of the view of the user.
    /// We collapse all items that are not visible for performance reasons.
    /// </summary>
    public bool IsVisible
    {
      get => Get<bool>();
      set => Set(value);
    }

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
      IsVisible = true;
      Debug.Assert(CanvasVM.currentCanvas != null);
      Canvas = CanvasVM.currentCanvas;
    }

    public void move(double deltaX, double deltaY)
    {
      if (IsLocked || !IsDraggable)
      {
        return;
      }

      Position = new Point(Position.X + deltaX, Position.Y + deltaY);

    }

  }
}
