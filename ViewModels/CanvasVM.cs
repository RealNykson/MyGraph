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
  class CanvasVM : NotifyObject
  {
    public MatrixTransform CanvasTransformMatrix
    {
      get { return Get<MatrixTransform>(); }
      set { Set(value); }
    }

    public ObservableCollection<ConnectionVM> Connections
    {
      get { return Get<ObservableCollection<ConnectionVM>>(); }
      set { Set(value); g_Connections = value; }
    }
    public static ObservableCollection<ConnectionVM> g_Connections;
    public enum Action
    {
      None,
      Dragging,
      Panning,
      ConnectingOutput
    }
    public static Action g_currentAction
    {
      get { return _g_currentAction; }
      set
      {
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
        _g_currentAction = value;
      }

    }
    private static Action _g_currentAction = Action.None;

    public double Scale
    {
      get { return Get<double>(); }
      set { Set(value); g_Scale = value; }
    }
    public static double g_Scale = 1;

    public double OffsetX
    {
      get { return Get<double>(); }
      set { Set(value); }
    }

    public double OffsetY
    {
      get { return Get<double>(); }
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
    public double TranslateX
    {
      get { return Get<double>(); }
      set { Set(value); }
    }

    public double TranslateY
    {
      get { return Get<double>(); }
      set { Set(value); }
    }




    public ObservableCollection<int> Dots
    {
      get { return Get<ObservableCollection<int>>(); }
      set { Set(value); }
    }

    public IRelayCommand ChangeThemeCommand { get; private set; }


    public Point MousePos
    {
      get { return _lastPosition; }
    }
    public static ObservableCollection<NodeVM> g_Nodes;
    public ObservableCollection<NodeVM> Nodes
    {
      get { return Get<ObservableCollection<NodeVM>>(); }
      set { Set(value); g_Nodes = Nodes; }
    }

    public static ConnectionVM g_GhostConnection = null;

    NodeVM test123;
    NodeVM newNode;

    public CanvasVM()
    {
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

      test123 = new NodeVM() { name = "new" };
      newNode = new NodeVM() { name = "new2" };
      NodeVM test = new NodeVM() { name = "test" };

      test123.move(500, 200);

      //test123.connectNode(newNode);
      Scale = 1;
    }



    //public const double ScaleRate = 0.025;
    public const double ScaleRate = 1.1;
    public const double MinScale = 0.1;
    public const double MaxScale = 3;

    public void MouseWheelZoom(double delta)
    {
      Point pos1 = _lastPosition;


      double deltaScale = delta > 0 ? 1.1 : 1 / 1.1;
      Matrix mat = CanvasTransformMatrix.Matrix;
      mat.ScaleAt(deltaScale, deltaScale, pos1.X, pos1.Y);
      CanvasTransformMatrix.Matrix = mat;
      Scale *= deltaScale;

    }
    public static Point g_lastMousePosition;

    public Point _lastPosition
    {
      get => Get<Point>();
      set { Set(value); g_lastMousePosition = value; }
    }

    public string StartPoint
    {
      get => Get<string>();
      set => Set(value);
    }

    public string EndPoints
    {
      get => Get<string>();
      set => Set(value);
    }

    public void updateDraggingNode(Point delta)
    {
      if (System.Windows.Input.Mouse.LeftButton != MouseButtonState.Pressed)
      {
        g_currentAction = Action.None;
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
        g_currentAction = Action.None;
        return;
      }

      var mat = CanvasTransformMatrix.Matrix;
      mat.OffsetX += delta.X;
      mat.OffsetY += delta.Y;
      CanvasTransformMatrix.Matrix = mat;
    }
    public void updateGhostConnection(Point delta)
    {

      if (g_GhostConnection == null)
      {
        return;
      }

      g_GhostConnection.updateEnd(delta.X / Scale, delta.Y / Scale);

    }


    public void MouseMove(Point currentPosition)
    {

      //In case cursor leaves captured area(Window) and comes back in


      Vector delta = currentPosition - _lastPosition;
      switch (g_currentAction)
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

      _lastPosition = currentPosition;


    }

    public void MouseDown(MouseButtonEventArgs ev)
    {

      if(g_currentAction == Action.ConnectingOutput)
      {
        g_GhostConnection.Delete();
        g_GhostConnection = null;
      }

      g_currentAction = Action.Panning;
      foreach (NodeVM node in Nodes)
      {
        node.IsSelected = false;
      }
    }
    public void MouseUp(MouseButtonEventArgs ev)
    {
      g_currentAction = Action.None;
    }


    private bool darkMode = true;
    public void ChangeTheme()
    {
      NodeVM testNode = new NodeVM();
      test123.connectNode(testNode);

      ResourceDictionary Theme = new ResourceDictionary() { Source = new Uri(darkMode ? "/Resources/Colors/LightMode.xaml" : "/Resources/Colors/DarkMode.xaml", UriKind.Relative) };
      App.Current.Resources.Clear();
      App.Current.Resources.MergedDictionaries.Add(Theme);
      darkMode = !darkMode;
    }



  }
}
