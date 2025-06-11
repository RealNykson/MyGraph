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
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace MyGraph.ViewModels
{
  /*
    If any action is added to this enum, make sure to add the Action in CurrentAction to define behavior for that action.
  */
  public enum Action
  {
    None,
    Dragging,
    Panning,
    ConnectingOutput,
    DrawingSelect
  }

  public class CanvasVM : NotifyObject
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
          SelectorSelectedItems.Clear();
        }

        if (CurrentAction == Action.Dragging && value != Action.Dragging)
        {
          foreach (NodeVM node in Nodes)
          {
            node.orderConnections();
          }
        }

        switch (value)
        {
          case Action.None:
            Mouse.OverrideCursor = Cursors.Arrow;
            break;
          case Action.Dragging:
            Mouse.OverrideCursor = Cursors.Arrow;
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
      set
      {
        Set(value);
      }
    }

    public double SelectRangeWidth
    {
      get => Get<double>();
      set { if (value >= 0) Set(value); }
    }

    public double SelectRangeHeight
    {
      get => Get<double>();
      set { if (value >= 0) Set(value); }
    }

    public Point MousePositionOnCanvas
    {
      get => Get<Point>();
      set => Set(value);

    }

    public bool IsOneSelectedNodeLocked
    {
      get => SelectedCanvasItems.Where(c => c.IsLocked).Count() != 0;
    }

    public bool EnableConnectionAnimations
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

    public ObservableCollection<TransferUnitVM> TransferUnits
    {
      get { return Get<ObservableCollection<TransferUnitVM>>(); }
      set { Set(value); }
    }

    public ObservableCollection<CanvasItem> CanvasItems { get; }
    public ObservableCollection<CanvasItem> SelectedCanvasItems { get; }
    public ObservableCollection<NodeVM> SelectedNodes { get; }
    public ObservableCollection<Connectable> SelectedNodesOutputs { get; }
    public ObservableCollection<Connectable> SelectedNodesInputs { get; }

    public bool IsOneSelectedItemLocked
    {
      get => SelectedCanvasItems.Where(c => c.IsLocked).Count() != 0;
    }

    #endregion

    #endregion

    #region Methods

    #region Selection Handling

    private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      if (e.OldItems != null)
      {
        foreach (CanvasItem item in e.OldItems)
        {
          item.PropertyChanged -= CanvasItem_PropertyChanged;
          CanvasItems.Remove(item);
          if (item.IsSelected)
          {
            SelectedCanvasItems.Remove(item);
            if (item is NodeVM node)
            {
              SelectedNodes.Remove(node);
            }
          }
        }
      }
      if (e.NewItems != null)
      {
        foreach (CanvasItem item in e.NewItems)
        {
          item.PropertyChanged += CanvasItem_PropertyChanged;
          CanvasItems.Add(item);
          if (item.IsSelected)
          {
            if (!SelectedCanvasItems.Contains(item))
            {
              SelectedCanvasItems.Add(item);
            }
            if (item is NodeVM node && !SelectedNodes.Contains(node))
            {
              SelectedNodes.Add(node);
            }
          }
        }
      }
    }

    private void CanvasItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName != "IsSelected") return;

      var item = (CanvasItem)sender;
      if (item.IsSelected)
      {
        if (!SelectedCanvasItems.Contains(item))
        {
          SelectedCanvasItems.Add(item);
          if (item is NodeVM node && !SelectedNodes.Contains(node))
          {
            SelectedNodes.Add(node);
          }
        }
      }
      else
      {
        SelectedCanvasItems.Remove(item);
        if (item is NodeVM node)
        {
          SelectedNodes.Remove(node);
        }
      }
    }

    private void SelectedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      if (e.NewItems != null)
      {
        foreach (NodeVM node in e.NewItems)
        {
          foreach (ConnectableConnection output in node.Outputs)
          {
            SelectedNodesOutputs.Add(output.End);
          }
          foreach (ConnectableConnection input in node.Inputs)
          {
            SelectedNodesInputs.Add(input.Start);
          }

          if (node.Outputs is INotifyCollectionChanged outputs)
          {
            outputs.CollectionChanged += OnNodeConnectionsChanged;
          }
          if (node.Inputs is INotifyCollectionChanged inputs)
          {
            inputs.CollectionChanged += OnNodeConnectionsChanged;
          }
        }
      }

      if (e.OldItems != null)
      {
        foreach (NodeVM node in e.OldItems)
        {
          foreach (ConnectableConnection output in node.Outputs)
          {
            SelectedNodesOutputs.Remove(output.End);
          }

          foreach (ConnectableConnection input in node.Inputs)
          {
            SelectedNodesInputs.Remove(input.Start);
          }

          if (node.Outputs is INotifyCollectionChanged outputs)
          {
            outputs.CollectionChanged -= OnNodeConnectionsChanged;
          }
          if (node.Inputs is INotifyCollectionChanged inputs)
          {
            inputs.CollectionChanged -= OnNodeConnectionsChanged;
          }
        }
      }
    }

    private void OnNodeConnectionsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      SelectedNodesOutputs.Clear();
      SelectedNodesInputs.Clear();

      foreach (var node in SelectedNodes)
      {
        foreach (ConnectableConnection output in node.Outputs)
        {
          SelectedNodesOutputs.Add(output.End);
        }

        foreach (ConnectableConnection input in node.Inputs)
        {
          SelectedNodesInputs.Add(input.Start);
        }
      }
    }

    #endregion

    #region Visibility
    public void UpdateItemVisibility(CanvasItem item)
    {
      if (item == null || CanvasTransformMatrix == null)
      {
        return;
      }

      Rect viewRect = new Rect(+100, +100, GridWidth - 200, GridHeight - 200);
      //Rect viewRect = new Rect(-100, -100, GridWidth + 100, GridHeight + 100);
      var matrix = CanvasTransformMatrix.Matrix;
      Point itemPosInView = new Point(matrix.Transform(item.Position).X, matrix.Transform(item.Position).Y);
      Rect itemRectInView = new Rect(itemPosInView, new Size(item.Width * Scale, item.Height * Scale));
      item.IsVisible = viewRect.IntersectsWith(itemRectInView);
    }

    public void UpdateAllItemsVisibility()
    {
      foreach (var item in CanvasItems)
      {
        UpdateItemVisibility(item);
      }
    }

    #endregion

    private DispatcherTimer currentPanTimer;

    private void loadObjectsFromDatabase()
    {

      DatabaseConnection _dbConnection = new DatabaseConnection();

      try
      {
        _dbConnection.Connect();

        var processUnitsList = _dbConnection.GetProcessUnits(39);

        //foreach (ProcessUnit pc in processUnitsList)
        //{
        //  new NodeVM(pc.UnitName, pc.UnitId * -1);
        //}

        var connectionsList = _dbConnection.GetConnections(39);

        foreach (ConnectionDB connection in connectionsList)
        {
          ProcessUnit sourceUnit = processUnitsList.FirstOrDefault(n => n.UnitId == connection.SourceUnitId);
          ProcessUnit transfer = processUnitsList.FirstOrDefault(n => n.UnitId == connection.TransferUnitId);
          ProcessUnit destinationUnit = processUnitsList.FirstOrDefault(n => n.UnitId == connection.DestinationUnitId);
          TransferUnitVM transferReal = TransferUnits.FirstOrDefault(n => n.Id == transfer.UnitId);
          if (transferReal == null)
          {
            transferReal = new TransferUnitVM(transfer.UnitName, transfer.UnitId);
          }

          NodeVM sourceNode = Nodes.FirstOrDefault(n => n.Id == sourceUnit.UnitId);
          if (sourceNode == null)
          {
            sourceNode = new NodeVM(sourceUnit.UnitName, sourceUnit.UnitId);
          }

          NodeVM destinationNode = Nodes.FirstOrDefault(n => n.Id == destinationUnit.UnitId);
          if (destinationNode == null)
          {
            destinationNode = new NodeVM(destinationUnit.UnitName, destinationUnit.UnitId);
          }

          sourceNode.connect(transferReal, null, destinationNode);
        }

        //foreach (ConnectionDB connection in connectionsList)
        //{
        //  var sourceUnit = Nodes.FirstOrDefault(n => n.Id == -connection.SourceUnitId);
        //  var transfer = Nodes.FirstOrDefault(n => n.Id == -connection.TransferUnitId);
        //  var destinationUnit = Nodes.FirstOrDefault(n => n.Id == -connection.DestinationUnitId);
        //  if (sourceUnit != null && destinationUnit != null && transfer != null)
        //  {
        //    transfer.IsTransfer = true;
        //    sourceUnit.connect(transfer);
        //    transfer.connect(destinationUnit);
        //  }
        //}

        foreach (NodeVM node in new ObservableCollection<NodeVM>(Nodes))
        {
          if (node.Inputs.Count() == 0 && node.Outputs.Count() == 0)
          {
            node.Delete();
          }
        }



      }

      catch (Exception ex)
      {
        MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }

    }

    /// <summary>
    /// Method is used to convert coordinates from a CanvasItem to Matrix coordinates.
    /// </summary>
    /// <param name="point">The point to convert.</param>
    /// <returns>The converted point.</returns>
    public Point ConvertCanvasToMatrixCoordinates(Point point)
    {
      return new Point(-point.X * Scale, -point.Y * Scale);
    }

    public void panToNode(NodeVM node)
    {
      if (node == null)
        return;

      if (currentPanTimer != null)
      {
        currentPanTimer.Stop();
      }

      var timer = new System.Windows.Threading.DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(8)
      };

      timer.Tick += (sender, e) =>
      {
        Point nodePosition = ConvertCanvasToMatrixCoordinates(node.Position);
        double targetOffsetX = nodePosition.X + (GridWidth / 2) - (node.Width / 2);
        double targetOffsetY = nodePosition.Y + (GridHeight / 2) - (node.Height / 2);

        Matrix currentMatrix = CanvasTransformMatrix.Matrix;
        double currentOffsetX = currentMatrix.OffsetX;
        double currentOffsetY = currentMatrix.OffsetY;

        double distance = Math.Sqrt(Math.Pow(targetOffsetX - currentOffsetX, 2) + Math.Pow(targetOffsetY - currentOffsetY, 2));

        if (distance < 1.0)
        {
          currentMatrix.OffsetX = targetOffsetX;
          currentMatrix.OffsetY = targetOffsetY;
          CanvasTransformMatrix.Matrix = currentMatrix;
          timer.Stop();
          if (currentPanTimer == timer)
          {
            currentPanTimer = null;
          }
          UpdateAllItemsVisibility();
          return;
        }

        double easingFactor = 0.30;
        currentMatrix.OffsetX += (targetOffsetX - currentOffsetX) * easingFactor;
        currentMatrix.OffsetY += (targetOffsetY - currentOffsetY) * easingFactor;

        CanvasTransformMatrix.Matrix = currentMatrix;
        UpdateAllItemsVisibility();
      };

      currentPanTimer = timer;
      currentPanTimer.Start();
    }

    public void updateDraggingItems(Point delta)
    {

      if (System.Windows.Input.Mouse.LeftButton != MouseButtonState.Pressed)
      {
        CurrentAction = Action.None;
        return;
      }

      foreach (CanvasItem item in SelectedCanvasItems.Where(i => i.IsDraggable))
      {
        item.move(delta.X / Scale, delta.Y / Scale);
        UpdateItemVisibility(item);
      }
    }

    public Point findNextFreeArea(double widthNeeded, double heightNeeded)
    {
      Point currentCheckingPosition = new Point(CanvasTransformMatrix.Matrix.OffsetX / Scale * -1.1, CanvasTransformMatrix.Matrix.OffsetY / Scale * -1.1);

      while (true)
      {
        bool collisionFound = false;

        foreach (NodeVM node in Nodes)
        {
          Rect neededArea = new Rect(currentCheckingPosition.X, currentCheckingPosition.Y, widthNeeded, heightNeeded);

          Rect nodeRect = new Rect(node.Position.X, node.Position.Y, node.Width, node.Height);

          if (neededArea.IntersectsWith(nodeRect))
          {
            currentCheckingPosition.X += 10;
            collisionFound = true;
            break;
          }
        }

        if (!collisionFound)
        {
          return currentCheckingPosition;
        }

        if (currentCheckingPosition.X > CanvasWidth)
        {
          currentCheckingPosition.X = CanvasTransformMatrix.Matrix.OffsetX;
          currentCheckingPosition.Y += 10;
        }
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
      UpdateAllItemsVisibility();
    }


    public List<CanvasItem> SelectorSelectedItems = new List<CanvasItem>();

    private List<CanvasItem> NotSelectedDown;
    private List<CanvasItem> NotSelectedUp;
    private List<CanvasItem> NotSelectedLeft;
    private List<CanvasItem> NotSelectedRight;

    public void addSelectorSelectedItems()
    {
      Rect selectionRect = new Rect(StartSelectRangePosition.X, StartSelectRangePosition.Y, SelectRangeWidth, SelectRangeHeight);

      // Handle deselection
      for (int i = SelectorSelectedItems.Count - 1; i >= 0; i--)
      {
        var item = SelectorSelectedItems[i];
        Rect itemRect = new Rect(item.Position.X, item.Position.Y, item.Width, item.Height);
        if (!selectionRect.IntersectsWith(itemRect))
        {
          item.IsSelected = false;
          SelectorSelectedItems.RemoveAt(i);
        }
      }

      var newlySelected = new HashSet<CanvasItem>();

      // Check against sorted lists. Using NotSelectedLeft (sorted by X) is a good primary check.
      if (NotSelectedLeft != null)
      {
        foreach (CanvasItem item in NotSelectedLeft)
        {
          if (item.Position.X > selectionRect.Right)
          {
            break;
          }

          if (item.IsSelected)
          {
            continue;
          }

          Rect itemRect = new Rect(item.Position.X, item.Position.Y, item.Width, item.Height);
          if (selectionRect.IntersectsWith(itemRect))
          {
            newlySelected.Add(item);
          }
        }
      }

      // Also check against Y-sorted list to catch items that might be missed
      if (NotSelectedDown != null)
      {
        foreach (CanvasItem item in NotSelectedDown)
        {
          if (item.Position.Y > selectionRect.Bottom)
          {
            break;
          }
          if (item.IsSelected)
          {
            continue;
          }
          if (newlySelected.Contains(item)) { continue; }

          Rect itemRect = new Rect(item.Position.X, item.Position.Y, item.Width, item.Height);
          if (selectionRect.IntersectsWith(itemRect))
          {
            newlySelected.Add(item);
          }
        }
      }

      foreach (var item in newlySelected)
      {
        item.IsSelected = true;
        SelectorSelectedItems.Add(item);
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
        addSelectorSelectedItems();
        return;
      }

      SelectRangeHeight += delta.Y / Scale;
      addSelectorSelectedItems();

    }

    public void MouseMove(Point currentPosition)
    {
      Vector delta = currentPosition - LastMousePosition;
      switch (CurrentAction)
      {
        case Action.Dragging:
          updateDraggingItems((Point)delta);
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

    /// <summary>
    /// Arranges nodes in the canvas for clarity, minimizing connection overlaps and grouping connected components.
    /// This method implements a version of the Sugiyama layout algorithm to achieve a hierarchical, left-to-right arrangement.
    /// It handles complex graphs, including those with cycles, to produce a clean and readable layout.
    /// Blame: Felix Baumueller.
    /// </summary>
    public void sortConnectables()
    {
      var allConnectables = Nodes.Cast<Connectable>().Concat(TransferUnits).ToList();
      if (!allConnectables.Any()) return;

      List<List<Connectable>> components = findConnectedComponents(allConnectables);

      double horizontalSpacing = 400;
      double verticalSpacing = 50;
      double groupVerticalSpacing = 200;
      double initialStartX = 50;
      double currentY = 50;

      foreach (List<Connectable> component in components)
      {
        if (!component.Any()) continue;

        // 1. Assign layers (or levels) to each node in the component.
        var layers = assignLayers(component);

        // 2. Order nodes within each layer to reduce crossings and assign initial Y positions.
        var yPositions = positionNodesVertically(layers, verticalSpacing);

        // 3. Assign final X and Y coordinates.
        double maxComponentY = assignCoordinates(layers, yPositions, initialStartX, currentY, horizontalSpacing, verticalSpacing);

        currentY = maxComponentY + groupVerticalSpacing;
      }

      foreach (Connectable node in allConnectables)
      {
        node.orderConnections();
      }
    }

    private List<List<Connectable>> findConnectedComponents(List<Connectable> allConnectables)
    {
      var components = new List<List<Connectable>>();
      var visited = new HashSet<Connectable>();

      foreach (var connectable in allConnectables)
      {
        if (!visited.Contains(connectable))
        {
          var component = new List<Connectable>();
          var queue = new Queue<Connectable>();

          queue.Enqueue(connectable);
          visited.Add(connectable);

          while (queue.Count > 0)
          {
            var current = queue.Dequeue();
            component.Add(current);

            var neighbors = current.Inputs.Select(c => c.Start).Concat(current.Outputs.Select(c => c.End));
            foreach (var neighbor in neighbors)
            {
              if (neighbor != null && allConnectables.Contains(neighbor) && !visited.Contains(neighbor))
              {
                visited.Add(neighbor);
                queue.Enqueue(neighbor);
              }
            }
          }
          components.Add(component);
        }
      }
      return components;
    }

    private Dictionary<int, List<Connectable>> assignLayers(List<Connectable> component)
    {
      var levels = new Dictionary<Connectable, int>();
      foreach (var node in component)
      {
        levels[node] = 0;
      }

      // Using an iterative approach that is guaranteed to terminate, even with cycles.
      // This is a simplified longest-path layering.
      for (int i = 0; i < component.Count + 1; i++)
      {
        bool changed = false;
        foreach (var node in component)
        {
          int currentLevel = levels[node];
          foreach (var connection in node.Outputs)
          {
            var targetNode = connection.End;
            if (targetNode != null && component.Contains(targetNode) && levels[targetNode] <= currentLevel)
            {
              levels[targetNode] = currentLevel + 1;
              changed = true;
            }
          }
        }
        if (!changed) break;
      }

      var layers = levels.GroupBy(kv => kv.Value)
                         .OrderBy(g => g.Key)
                         .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

      // Re-index layers to be contiguous from 0
      var finalLayers = new Dictionary<int, List<Connectable>>();
      int layerIndex = 0;
      foreach (var key in layers.Keys.OrderBy(k => k))
      {
        finalLayers[layerIndex++] = layers[key];
      }

      return finalLayers;
    }

    private Dictionary<Connectable, double> positionNodesVertically(Dictionary<int, List<Connectable>> layers, double verticalSpacing)
    {
      var yPositions = new Dictionary<Connectable, double>();

      // Initial placement: center each node between its sources if possible
      foreach (var layerIndex in layers.Keys.OrderBy(k => k))
      {
        var layer = layers[layerIndex];

        // For the first layer (nodes with no inputs), distribute them evenly
        if (layerIndex == 0)
        {
          double currentY = 0;
          foreach (var node in layer)
          {
            yPositions[node] = currentY;
            currentY += node.Height + verticalSpacing;
          }
        }
        else
        {
          // For subsequent layers, center between input nodes
          foreach (var node in layer)
          {
            var inputNodes = node.Inputs.Select(c => c.Start).Where(n => n != null && yPositions.ContainsKey(n)).ToList();
            if (inputNodes.Count > 0)
            {
              // Center between sources using average
              var sourceCenters = inputNodes.Select(n => yPositions[n] + n.Height / 2).ToList();
              double average = sourceCenters.Average();
              yPositions[node] = average - node.Height / 2;
            }
            else
            {
              // If no sources, stack as before
              double currentY = yPositions.Values.DefaultIfEmpty(0).Max();
              yPositions[node] = currentY;
            }
          }

          // After initial placement, resolve overlaps in this layer
          var ordered = layer.OrderBy(n => yPositions[n]).ToList();
          for (int i = 1; i < ordered.Count; i++)
          {
            var prev = ordered[i - 1];
            var curr = ordered[i];
            double minY = yPositions[prev] + prev.Height + verticalSpacing;
            if (yPositions[curr] < minY)
              yPositions[curr] = minY;
          }
        }
      }

      // Iteratively improve positions using barycenter heuristic (up and down passes)
      for (int i = 0; i < 15; i++)
      {
        // Downward pass using median for robustness
        for (int l = 1; l < layers.Count; l++)
        {
          updateLayerPositions(layers[l], yPositions, true);
        }

        // Upward pass using median
        for (int l = layers.Count - 2; l >= 0; l--)
        {
          updateLayerPositions(layers[l], yPositions, false);
        }
      }

      return yPositions;
    }

    private void updateLayerPositions(List<Connectable> layer, Dictionary<Connectable, double> yPositions, bool isDownwardPass)
    {
      foreach (var node in layer)
      {
        var connectedNodes = isDownwardPass
            ? node.Inputs.Select(c => c.Start)
            : node.Outputs.Select(c => c.End);

        var validConnections = connectedNodes.Where(p => p != null && yPositions.ContainsKey(p)).ToList();

        if (validConnections.Any())
        {
          var centerPositions = validConnections.Select(p => yPositions[p] + p.Height / 2).ToList();
          double averagePosition = centerPositions.Average();
          yPositions[node] = averagePosition - node.Height / 2;
        }
      }
    }

    private double assignCoordinates(Dictionary<int, List<Connectable>> layers, Dictionary<Connectable, double> yPositions,
                                   double startX, double componentStartY, double horizontalSpacing, double verticalSpacing)
    {
      double currentX = startX;
      double maxOverallY = componentStartY;

      var levelWidths = new Dictionary<int, double>();
      foreach (var layer in layers)
      {
        levelWidths[layer.Key] = layer.Value.Any() ? layer.Value.Max(n => n.Width) : 0;
      }

      // Create a mutable copy for adjustments.
      var finalYPositions = new Dictionary<Connectable, double>(yPositions);

      // Resolve overlaps layer by layer with a robust method.
      foreach (var layer in layers.Values)
      {
        if (layer.Count < 2) continue;

        var orderedNodes = layer.OrderBy(n => finalYPositions[n]).ToList();

        for (int i = 0; i < orderedNodes.Count - 1; i++)
        {
          var current = orderedNodes[i];
          var next = orderedNodes[i + 1];

          double currentBottom = finalYPositions[current] + current.Height;
          double nextTop = finalYPositions[next];

          if (nextTop < currentBottom + verticalSpacing)
          {
            double offset = (currentBottom + verticalSpacing) - nextTop;
            // Apply offset to all subsequent nodes in the layer to push them down.
            for (int j = i + 1; j < orderedNodes.Count; j++)
            {
              finalYPositions[orderedNodes[j]] += offset;
            }
          }
        }
      }

      // Find overall min Y to shift entire component to startY
      double minComponentY = 0;
      if (layers.Values.SelectMany(l => l).Any())
      {
        minComponentY = layers.Values.SelectMany(l => l).Min(n => finalYPositions.ContainsKey(n) ? finalYPositions[n] : 0);
      }


      foreach (var layer in layers.OrderBy(l => l.Key))
      {
        double layerWidth = levelWidths[layer.Key];
        foreach (var node in layer.Value)
        {
          double finalY = componentStartY + (finalYPositions.ContainsKey(node) ? finalYPositions[node] : 0) - minComponentY;
          node.moveAbsolute(currentX, finalY);
          maxOverallY = Math.Max(maxOverallY, finalY + node.Height);
        }
        currentX += layerWidth + horizontalSpacing;
      }

      UpdateAllItemsVisibility();
      return maxOverallY;
    }

    #endregion Methods

    #region Events

    public void MouseDown(MouseButtonEventArgs ev)
    {
      if (Keyboard.IsKeyDown(Key.LeftCtrl))
      {
        CurrentAction = Action.Panning;
        return;
      }
      if (!Keyboard.IsKeyDown(Key.LeftShift))
      {
        foreach (CanvasItem item in CanvasItems)
        {
          item.IsSelected = false;
        }
      }
      StartSelectRangePosition = MousePositionOnCanvas;
      CurrentAction = Action.DrawingSelect;

      var unselected = CanvasItems.Where(i => !i.IsSelected).ToList();
      NotSelectedDown = unselected.OrderBy(c => c.Position.Y).ToList();
      NotSelectedUp = unselected.OrderByDescending(c => c.Position.Y).ToList();
      NotSelectedLeft = unselected.OrderBy(c => c.Position.X).ToList();
      NotSelectedRight = unselected.OrderByDescending(c => c.Position.X).ToList();
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
          NotSelectedDown = null;
          NotSelectedUp = null;
          NotSelectedLeft = null;
          NotSelectedRight = null;
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
      if (delta > 0 && CleanScale >= 150)
      {
        return;
      }
      if (delta < 0 && CleanScale <= 10)
      {
        return;
      }

      double oldScale = Scale;
      CleanScale += delta < 0 ? -5 : +5;

      double newScale = Math.Pow(ScaleRate, (CleanScale - 100.0) / 5.0);

      if (oldScale <= 0)
      {
        return;
      }
      double scaleCorrectionFactor = newScale / oldScale;

      Matrix mat = CanvasTransformMatrix.Matrix;
      mat.ScaleAt(scaleCorrectionFactor, scaleCorrectionFactor, pos1.X, pos1.Y);
      CanvasTransformMatrix.Matrix = mat;
      Scale = newScale;
      UpdateAllItemsVisibility();
    }

    #endregion

    #region Constructor 



    public CanvasVM()
    {

      currentCanvas = this;
      CleanScale = 100;
      GridHeight = 650;
      GridWidth = 1000;
      CanvasHeight = 1000;
      CanvasWidth = 1000;
      Scale = 1;


      Nodes = new ObservableCollection<NodeVM>();
      Connections = new ObservableCollection<ConnectionVM>();
      TransferUnits = new ObservableCollection<TransferUnitVM>();

      CanvasItems = new ObservableCollection<CanvasItem>();
      SelectedCanvasItems = new ObservableCollection<CanvasItem>();
      SelectedNodes = new ObservableCollection<NodeVM>();
      SelectedNodesOutputs = new ObservableCollection<Connectable>();
      SelectedNodesInputs = new ObservableCollection<Connectable>();

      Nodes.CollectionChanged += Items_CollectionChanged;
      TransferUnits.CollectionChanged += Items_CollectionChanged;
      SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;

      EnableConnectionAnimations = false;


      NodeVM node1 = new NodeVM("Node 1", 1);
      NodeVM node2 = new NodeVM("Node 2", 2);
      NodeVM node3 = new NodeVM("Node 3", 3);
      TransferUnitVM transferUnit1 = new TransferUnitVM("Transfer Unit 1", 1);
      TransferUnitVM transferUnit2 = new TransferUnitVM("Transfer Unit 2", 2);
      TransferUnitVM transferUnit3 = new TransferUnitVM("Transfer Unit 3", 3);

      CanvasTransformMatrix = new MatrixTransform();

      var mat = CanvasTransformMatrix.Matrix;
      mat.OffsetX += CanvasWidth / -2;
      mat.OffsetY += CanvasHeight / -2;
      CanvasTransformMatrix.Matrix = mat;

      loadObjectsFromDatabase();

    }


    #endregion


  }
}
