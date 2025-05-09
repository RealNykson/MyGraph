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
using System.Threading;
using System.Windows.Media.Animation;
using System.Diagnostics.Eventing.Reader;
using MyGraph.Models;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace MyGraph.ViewModels
{
  public enum Action
  {
    None,
    Dragging,
    Panning,
    ConnectingOutput,
    DrawingSelect
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

    public int CleanScale
    {
      get => Get<int>();
      set => Set(value);
    }

    public Action CurrentAction
    {
      get { return Get<Action>(); }
      set
      {

        if (CurrentAction == Action.ConnectingOutput
          && value != Action.ConnectingOutput
          && GhostConnection != null)
        {
          GhostConnection.Delete();
        }
        if (CurrentAction == Action.DrawingSelect && value != Action.DrawingSelect)
        {
          SelectRangeHeight = 0;
          SelectRangeWidth = 0;
          SelectorSelectedNodes.Clear();
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
          case Action.DrawingSelect:
            Mouse.OverrideCursor = Cursors.Arrow;
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
      set { Set(value); }
    }

    public double GridWidth
    {
      get { return Get<double>(); }
      set { Set(value); }
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

    public PreviewConnectionVM GhostConnection
    {
      get => Get<PreviewConnectionVM>();
      set => Set(value);
    }

    public NodeVM SearchedNode
    {
      get => Get<NodeVM>();
      set
      {
        Set(value);
        panToNode(value);
      }
    }

    public Point StartSelectRangePosition
    {
      get => Get<Point>();
      set => Set(value);
    }

    public double SelectRangeWidth
    {
      get => Get<double>();
      set => Set(value);
    }

    public double SelectRangeHeight
    {
      get => Get<double>();
      set => Set(value);
    }

    public Point MousePositionOnCanvas
    {
      get => Get<Point>();
      set => Set(value);

    }

    public bool IsOneSelectedNodeLocked
    {
      get => Get<bool>();
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

    public ObservableCollection<NodeVM> SelectedNodes
    {
      get { return Get<ObservableCollection<NodeVM>>(); }
      set { Set(value); value.CollectionChanged += SelectedNodes_Changed; }
    }

    public ObservableCollection<NodeVM> SelectedNodesOutputs
    {
      get { return Get<ObservableCollection<NodeVM>>(); }
      set { Set(value); }
    }

    public ObservableCollection<NodeVM> SelectedNodesInputs
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

    #region Methods

    public void panToNode(NodeVM node)
    {
      if (node == null)
        return;

      double targetPanX = -node.Position.X * Scale;
      double targetPanY = -node.Position.Y * Scale;

      double offsetX = GridWidth / 2 - (node.Width / 2);
      double offsetY = GridHeight / 2 - (node.Height / 2);

      Matrix currentMatrix = CanvasTransformMatrix.Matrix;
      double startOffsetX = currentMatrix.OffsetX;
      double startOffsetY = currentMatrix.OffsetY;
      double endOffsetX = targetPanX + offsetX;
      double endOffsetY = targetPanY + offsetY;

      Duration duration = new Duration(TimeSpan.FromSeconds(0.2));
      
      Storyboard storyboard = new Storyboard();
      
      System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
      timer.Interval = TimeSpan.FromMilliseconds(10); 
      
      int totalSteps = 20; 
      int currentStep = 0;
      
      timer.Tick += (sender, e) => 
      {
          currentStep++;
          if (currentStep > totalSteps)
          {
              Matrix finalMatrix = currentMatrix;
              finalMatrix.OffsetX = endOffsetX;
              finalMatrix.OffsetY = endOffsetY;
              CanvasTransformMatrix.Matrix = finalMatrix;
              
              timer.Stop();
              return;
          }
          
          double progress = (double)currentStep / totalSteps;
          double easedProgress = EaseOutQuad(progress);
          
          Matrix animatedMatrix = currentMatrix;
          animatedMatrix.OffsetX = startOffsetX + (endOffsetX - startOffsetX) * easedProgress;
          animatedMatrix.OffsetY = startOffsetY + (endOffsetY - startOffsetY) * easedProgress;
          
          CanvasTransformMatrix.Matrix = animatedMatrix;
      };
      
      timer.Start();
    }
    
    private double EaseOutQuad(double t)
    {
        return t * (2 - t);
    }

    public void updateDraggingNode(Point delta)
    {
      if (System.Windows.Input.Mouse.LeftButton != MouseButtonState.Pressed)
      {
        CurrentAction = Action.None;
        return;
      }
      foreach (NodeVM node in SelectedNodes)
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


    public List<NodeVM> SelectorSelectedNodes = new List<NodeVM>();

    public void addSelectorSelectedNodes()
    {
      foreach (NodeVM node in Nodes)
      {
        if (node.Position.X >= StartSelectRangePosition.X
          && node.Position.X <= StartSelectRangePosition.X + SelectRangeWidth
          && node.Position.Y >= StartSelectRangePosition.Y
          && node.Position.Y <= StartSelectRangePosition.Y + SelectRangeHeight)
        {
          if (!node.IsSelected)
          {
            SelectorSelectedNodes.Add(node);
            node.IsSelected = true;
          }
          continue;
        }
        else if (SelectorSelectedNodes.Contains(node))
        {
          node.IsSelected = false;
          SelectorSelectedNodes.Remove(node);
        }

      }

    }



    public void updateGhostConnection(Point delta)
    {
      if (GhostConnection == null || GhostConnection.End != null)
      {
        return;
      }
      GhostConnection.moveEndToMouse();
    }
    public void updateDrawSelect(Point delta)
    {
      if (System.Windows.Input.Mouse.LeftButton != MouseButtonState.Pressed)
      {
        CurrentAction = Action.None;
        return;
      }

      if (MousePositionOnCanvas.X < StartSelectRangePosition.X
          || (MousePositionOnCanvas.X < (StartSelectRangePosition.X + SelectRangeWidth)
          && delta.X > 0))
      {

        SelectRangeWidth += (-1 * delta.X) / Scale;
        StartSelectRangePosition = new Point(StartSelectRangePosition.X + delta.X / Scale, StartSelectRangePosition.Y);
      }
      else
      {
        SelectRangeWidth += delta.X / Scale;
      }

      if (MousePositionOnCanvas.Y < StartSelectRangePosition.Y
          || (MousePositionOnCanvas.Y < StartSelectRangePosition.Y + SelectRangeHeight && delta.Y > 0))
      {
        SelectRangeHeight += (-delta.Y) / Scale;
        StartSelectRangePosition = new Point(StartSelectRangePosition.X, StartSelectRangePosition.Y + delta.Y / Scale);
        addSelectorSelectedNodes();
        return;
      }

      SelectRangeHeight += delta.Y / Scale;
      addSelectorSelectedNodes();



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
        case Action.DrawingSelect:
          updateDrawSelect((Point)delta);
          break;
        default:
          break;
      }

      LastMousePosition = currentPosition;
    }
    public List<int>[] ConstructAdj(int V, int[,] edges)
    {
      List<int>[] adj = new List<int>[V];

      for (int i = 0; i < V; i++)
      {
        adj[i] = new List<int>();
      }

      for (int i = 0; i < edges.GetLength(0); i++)
      {
        adj[edges[i, 0]].Add(edges[i, 1]);
      }

      return adj;
    }

    public int[] TopologicalSort(int V, int[,] edges)
    {
      List<int>[] adj = ConstructAdj(V, edges);
      int[] indegree = new int[V];

      for (int i = 0; i < V; i++)
      {
        foreach (var neighbor in adj[i])
        {
          indegree[neighbor]++;
        }
      }

      Queue<int> q = new Queue<int>();
      for (int i = 0; i < V; i++)
      {
        if (indegree[i] == 0)
        {
          q.Enqueue(i);
        }
      }

      int[] result = new int[V];
      int index = 0;

      while (q.Count > 0)
      {
        int node = q.Dequeue();
        result[index++] = node;

        foreach (var neighbor in adj[node])
        {
          indegree[neighbor]--;
          if (indegree[neighbor] == 0)
          {
            q.Enqueue(neighbor);
          }
        }
      }

      // Check for cycle
      if (index != V)
      {
        Console.WriteLine("Graph contains a cycle!");
        return new int[0];
      }

      return result;
    }

    public void sortNodes()
    {

      List<NodeVM> startNodes = new List<NodeVM>();

      startNodes = Nodes.Where(n => n.Inputs.Count() == 0).ToList();

      foreach (NodeVM node in startNodes)
      {
        List<NodeVM> orderList = new List<NodeVM>();
        node.orderAllChildrenRelativeToSelf(orderList);
      }

    }

    #endregion Methods

    #region Events

    public void SelectedNodes_Changed(object sender, NotifyCollectionChangedEventArgs e)
    {
      if (e != null && e.Action == NotifyCollectionChangedAction.Reset)
      {
        SelectedNodesOutputs.Clear();
        SelectedNodesInputs.Clear();
        return;
      }

      IsOneSelectedNodeLocked = SelectedNodes.Where(n => n.IsLocked).Count() != 0;

      // This can be improved significantly by only removing/added outputs but this is the lazy way 
      SelectedNodesOutputs.Clear();
      SelectedNodesInputs.Clear();

      foreach (NodeVM node in SelectedNodes)
      {
        foreach (Connection output in node.Outputs)
        {
          if (!SelectedNodesOutputs.Contains(output.End))
          {
            SelectedNodesOutputs.Add(output.End);
          }
        }

        foreach (Connection inputs in node.Inputs)
        {
          if (inputs.End != null && !SelectedNodesInputs.Contains(inputs.Start))
          {
            SelectedNodesInputs.Add(inputs.Start);
          }
        }
      }

      //

    }
    public void MouseDown(MouseButtonEventArgs ev)
    {

      if (Keyboard.IsKeyDown(Key.LeftCtrl))
      {
        CurrentAction = Action.Panning;
        return;
      }
      if (!Keyboard.IsKeyDown(Key.LeftShift))
      {
        foreach (NodeVM node in Nodes)
        {
          node.IsSelected = false;
        }
      }
      StartSelectRangePosition = MousePositionOnCanvas;
      CurrentAction = Action.DrawingSelect;

    }

    public void MouseLeave()
    {
      CurrentAction = Action.None;
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
        case Action.DrawingSelect:
          CurrentAction = Action.None;
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
      if (delta > 0 && CleanScale >= 150)
      {
        return;
      }
      if (delta < 0 && CleanScale <= 10)
      {
        return;
      }
      Matrix mat = CanvasTransformMatrix.Matrix;
      mat.ScaleAt(deltaScale, deltaScale, pos1.X, pos1.Y);
      CanvasTransformMatrix.Matrix = mat;
      Scale *= deltaScale;
      CleanScale += delta < 0 ? -5 : +5;



    }

    #endregion

    #region Constructor 
    public CanvasVM()
    {
      currentCanvas = this;
      CleanScale = 100;
      GridHeight = 650;
      GridWidth = 1000;
      CanvasHeight = 5000;
      CanvasWidth = 5000;
      Scale = 1;

      Nodes = new ObservableCollection<NodeVM>();
      SelectedNodes = new ObservableCollection<NodeVM>();
      SelectedNodesOutputs = new ObservableCollection<NodeVM>();
      SelectedNodesInputs = new ObservableCollection<NodeVM>();
      Dots = new ObservableCollection<int>();
      Connections = new ObservableCollection<ConnectionVM>();

      CanvasTransformMatrix = new MatrixTransform();

      for (int i = 0; i < 10000; i++)
      {
        Dots.Add(i);
      }

      // Gruppe A (Cluster 1)
      NodeVM a1 = new NodeVM() { Name = "Alpha" };
      NodeVM a2 = new NodeVM() { Name = "Beta" };
      NodeVM a3 = new NodeVM() { Name = "Gamma" };
      NodeVM a4 = new NodeVM() { Name = "Delta" };
      NodeVM a5 = new NodeVM() { Name = "Epsilon" };

      a1.move(100, 100);
      a2.move(300, 100);
      a3.move(500, 100);
      a4.move(200, 250);
      a5.move(400, 250);

      a1.connectNode(a2);
      a2.connectNode(a3);
      a2.connectNode(a4);
      a4.connectNode(a5);
      a3.connectNode(a5);

      // Gruppe C (Tree-Struktur)
      NodeVM c1 = new NodeVM() { Name = "Root" };
      NodeVM c2 = new NodeVM() { Name = "Leaf1" };
      NodeVM c3 = new NodeVM() { Name = "Leaf2" };
      NodeVM c4 = new NodeVM() { Name = "Leaf3" };
      NodeVM c5 = new NodeVM() { Name = "Leaf4" };

      c1.move(250, 500);
      c2.move(50, 650);
      c3.move(200, 650);
      c4.move(400, 650);
      c5.move(200, 800);

      c1.connectNode(c2);
      c1.connectNode(c3);
      c1.connectNode(c4);
      c3.connectNode(c5);


      var mat = CanvasTransformMatrix.Matrix;
      mat.OffsetX += -2500;
      mat.OffsetY += -2500;
      CanvasTransformMatrix.Matrix = mat;

    }

    #endregion


  }
}
