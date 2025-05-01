using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Collections.ObjectModel;
using MyGraph.ViewModels;
using System.Windows.Media;
using System.Data;
using System.Windows.Controls;
using System.Dynamic;

namespace MyGraph.ViewModels
{
  public enum Action
  {
    None,
    Dragging,
    Panning,
    ConnectingOutput
  }

  class CanvasVM : NotifyObject
  {

    public static CanvasVM currentCanvas = null;

    #region Properties

    #region CanvasData

    public double Scale
    {
      get { return Get<double>(); }
      set { Set(value); }
    }

    public Action CurrentAction
    {
      get { return Get<Action>(); }
      set
      {

        if (CurrentAction == Action.ConnectingOutput && value != Action.ConnectingOutput && GhostConnection != null)
        {
          GhostConnection.Delete();
        }

        switch (value)
        {
          case Action.None:
            Mouse.OverrideCursor = Cursors.Arrow;
            break;
          case Action.Dragging:
            Mouse.OverrideCursor = Cursors.Hand;
            break;
          case Action.Panning:
            Mouse.OverrideCursor = Cursors.ScrollAll;
            break;
          case Action.ConnectingOutput:
            Mouse.OverrideCursor = Cursors.Cross;
            break;
          default:
            Mouse.OverrideCursor = Cursors.Arrow;
            break;
        }
        Set(value);
      }
    }

    public MatrixTransform CanvasTransformMatrix
    {
      get { return Get<MatrixTransform>(); }
      set { Set(value); }
    }

    public double GridHeight
    {
      get { return Get<double>(); }
      set { Set(value); CanvasHeight = value; }
    }

    public double GridWidth
    {
      get { return Get<double>(); }
      set { Set(value); CanvasWidth = value; }
    }

    public double CanvasHeight
    {
      get { return Get<double>(); }
      set { Set(value); }
    }

    public double CanvasWidth
    {
      get { return Get<double>(); }
      set { Set(value); }
    }

    public Point LastMousePosition
    {
      get => Get<Point>();
      set => Set(value);
    }

    public ConnectionVM GhostConnection
    {
      get => Get<ConnectionVM>();
      set => Set(value);
    }

    #endregion

    #region Lists

    public ObservableCollection<ConnectionVM> Connections
    {
      get { return Get<ObservableCollection<ConnectionVM>>(); }
      set { Set(value); }
    }

    public ObservableCollection<NodeVM> Nodes
    {
      get { return Get<ObservableCollection<NodeVM>>(); }
      set { Set(value); }
    }
    public ObservableCollection<int> Dots
    {
      get { return Get<ObservableCollection<int>>(); }
      set { Set(value); }
    }

    #endregion


    #endregion

    #region Commands

    private bool darkMode = true;
    public IRelayCommand ChangeThemeCommand { get; private set; }
    public void ChangeTheme()
    {
      NodeVM testNode = new NodeVM();

      ResourceDictionary Theme = new ResourceDictionary() { Source = new Uri(darkMode ? "/Resources/Colors/LightMode.xaml" : "/Resources/Colors/DarkMode.xaml", UriKind.Relative) };
      App.Current.Resources.Clear();
      App.Current.Resources.MergedDictionaries.Add(Theme);
      darkMode = !darkMode;
    }

    #endregion
    #region Constructor 
    public CanvasVM()
    {
      currentCanvas = this;
      GridHeight = 450;
      GridWidth = 800;
      CanvasHeight = 5000;
      CanvasWidth = 5000;
      ResourceDictionary Theme = new ResourceDictionary() { Source = new Uri("/Resources/Colors/DarkMode.xaml", UriKind.Relative) };
      App.Current.Resources.MergedDictionaries.Add(Theme);

      ChangeThemeCommand = new RelayCommand(ChangeTheme);
      Nodes = new ObservableCollection<NodeVM>();
      Dots = new ObservableCollection<int>();
      Connections = new ObservableCollection<ConnectionVM>();
      CanvasTransformMatrix = new MatrixTransform();

      for (int i = 0; i < 10000; i++)
      {
        Dots.Add(i);
      }

      NodeVM test123 = new NodeVM() { name = "new" };
      NodeVM newNode = new NodeVM() { name = "new2" };
      NodeVM test = new NodeVM() { name = "test" };

      test123.move(500, 200);

      test123.connectNode(newNode);
      Scale = 1;
    }

    #endregion

    #region Methods
    public void updateDraggingNode(Point delta)
    {
      if (System.Windows.Input.Mouse.LeftButton != MouseButtonState.Pressed)
      {
        CurrentAction = Action.None;
        return;
      }
      foreach (NodeVM node in Nodes.Where(n => n.IsSelected == true))
      {
        node.move(delta.X / Scale, delta.Y / Scale);
      }
    }

    public void updatePanningCanvas(Point delta)
    {
      if (System.Windows.Input.Mouse.LeftButton != MouseButtonState.Pressed)
      {
        CurrentAction = Action.None;
        return;
      }

      var mat = CanvasTransformMatrix.Matrix;
      mat.OffsetX += delta.X;
      mat.OffsetY += delta.Y;
      CanvasTransformMatrix.Matrix = mat;
    }

    public void updateGhostConnection(Point delta)
    {
      if (GhostConnection == null)
      {
        return;
      }
      GhostConnection.moveEnd(delta.X / Scale, delta.Y / Scale);
    }

    public void MouseMove(Point currentPosition)
    {
      Vector delta = currentPosition - LastMousePosition;
      switch (CurrentAction)
      {
        case Action.Dragging:
          updateDraggingNode((Point)delta);
          break;
        case Action.Panning:
          updatePanningCanvas((Point)delta);
          break;
        case Action.ConnectingOutput:
          updateGhostConnection((Point)delta);
          break;
        default: break;
      }

      LastMousePosition = currentPosition;
    }

    #endregion Methods

    #region Events

    public void MouseDown(MouseButtonEventArgs ev)
    {

      if (CurrentAction == Action.ConnectingOutput)
      {
        GhostConnection.Delete();
      }

      CurrentAction = Action.Panning;
    }

    public void MouseUp(MouseButtonEventArgs ev)
    {
      switch (CurrentAction)
      {
        case Action.None:
          break;
        case Action.Panning:
          CurrentAction = Action.None;
          break;
        case Action.Dragging:
          CurrentAction = Action.None;
          break;
        case Action.ConnectingOutput:
          break;
        default:
          CurrentAction = Action.None;
          break;
        
      }

    }

    public const double ScaleRate = 1.1;
    public const double MinScale = 0.1;
    public const double MaxScale = 3;
    public void MouseWheelZoom(double delta)
    {
      Point pos1 = LastMousePosition;


      double deltaScale = delta > 0 ? 1.1 : 1 / 1.1;
      Matrix mat = CanvasTransformMatrix.Matrix;
      mat.ScaleAt(deltaScale, deltaScale, pos1.X, pos1.Y);
      CanvasTransformMatrix.Matrix = mat;
      Scale *= deltaScale;

    }

    #endregion





  }
}
