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
          SelectorSelectedItems.Clear();
        }

        switch (value)
        {
          case Action.None:
            Mouse.OverrideCursor = Cursors.Arrow;
            break;
          case Action.Dragging:
            Mouse.OverrideCursor = Cursors.Arrow;
            //Mouse.OverrideCursor = Cursors.Hand;
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

    public double GlobalAnimationOffset
    {
      get => Get<double>();
      set => Set(value);
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

    public ObservableCollection<CanvasItem> CanvasItems
    {
      get
      {
        return new ObservableCollection<CanvasItem>(
        Nodes.Cast<CanvasItem>().Concat(
        TransferUnits.Cast<CanvasItem>()
        ));
      }
    }


    public ObservableCollection<CanvasItem> SelectedCanvasItems
    {
      get { return new ObservableCollection<CanvasItem>(CanvasItems.Where(c => c.IsSelected).ToList()); }
    }

    public ObservableCollection<NodeVM> SelectedNodes
    {
      get { return new ObservableCollection<NodeVM>(Nodes.Where(n => n.IsSelected).ToList()); }
    }

    public ObservableCollection<Connectable> SelectedNodesOutputs
    {
      get { return new ObservableCollection<Connectable>(SelectedNodes.SelectMany(n => n.Outputs.Select(o => o.End)).ToList()); }
    }

    public ObservableCollection<Connectable> SelectedNodesInputs
    {
      get { return new ObservableCollection<Connectable>(SelectedNodes.SelectMany(n => n.Inputs.Select(i => i.Start)).ToList()); }
    }

    public bool IsOneSelectedItemLocked
    {
      get => SelectedCanvasItems.Where(c => c.IsLocked).Count() != 0;
    }

    #endregion

    #endregion

    #region Methods

    private DispatcherTimer currentPanTimer;
    private DateTime _lastPanRequest = DateTime.MinValue;
    private NodeVM _targetNode = null;

    // Global animation timer for connection dash offset animation
    private DispatcherTimer globalAnimationTimer;
    private DateTime animationStartTime;

    private void loadObjectsFromDatabase()
    {

      DatabaseConnection _dbConnection = new DatabaseConnection();

      try
      {
        _dbConnection.Connect();

        var processUnitsList = _dbConnection.GetProcessUnits(39);

        foreach (ProcessUnit pc in processUnitsList)
        {
          new NodeVM(pc.UnitName, pc.UnitId);
        }

        var connectionsList = _dbConnection.GetConnections(39);

        foreach (ConnectionDB connection in connectionsList)
        {
          var sourceUnit = Nodes.FirstOrDefault(n => n.Id == connection.SourceUnitId);
          var transfer = Nodes.FirstOrDefault(n => n.Id == connection.TransferUnitId);
          var destinationUnit = Nodes.FirstOrDefault(n => n.Id == connection.DestinationUnitId);
          if (sourceUnit != null && destinationUnit != null && transfer != null)
          {
            sourceUnit.connect(transfer);
            transfer.IsTransfer = true;
            transfer.connect(destinationUnit);
          }
        }

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

    public void panToNode(NodeVM node)
    {
      if (node == null)
        return;

      // Debounce rapid requests (ignore calls within 100ms of each other)
      DateTime now = DateTime.Now;
      if ((now - _lastPanRequest).TotalMilliseconds < 100)
      {
        // Store the target node for deferred processing
        _targetNode = node;
        return;
      }

      _lastPanRequest = now;
      _targetNode = null;

      // Always stop any existing animation immediately
      if (currentPanTimer != null && currentPanTimer.IsEnabled)
      {
        currentPanTimer.Stop();
        currentPanTimer = null;
      }

      double targetPanX = -node.Position.X * Scale;
      double targetPanY = -node.Position.Y * Scale;

      double offsetX = GridWidth / 2 - (node.Width / 2);
      double offsetY = GridHeight / 2 - (node.Height / 2);

      Matrix currentMatrix = CanvasTransformMatrix.Matrix;
      double startOffsetX = currentMatrix.OffsetX;
      double startOffsetY = currentMatrix.OffsetY;
      double endOffsetX = targetPanX + offsetX;
      double endOffsetY = targetPanY + offsetY;

      // Calculate distance to determine animation speed
      double distance = Math.Sqrt(Math.Pow(endOffsetX - startOffsetX, 2) + Math.Pow(endOffsetY - startOffsetY, 2));

      // Skip animation for very small distances
      if (distance < 10)
      {
        Matrix finalMatrix = currentMatrix;
        finalMatrix.OffsetX = endOffsetX;
        finalMatrix.OffsetY = endOffsetY;
        CanvasTransformMatrix.Matrix = finalMatrix;
        return;
      }

      // Adjust animation speed based on distance (faster for longer distances)
      int totalSteps = Math.Min(20, Math.Max(5, (int)(distance / 40)));
      double intervalMs = Math.Min(20, Math.Max(10, distance / 100));

      System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
      timer.Interval = TimeSpan.FromMilliseconds(intervalMs);
      currentPanTimer = timer;

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
          currentPanTimer = null;

          // Process deferred pan request if one exists
          if (_targetNode != null)
          {
            NodeVM nextNode = _targetNode;
            _targetNode = null;
            panToNode(nextNode);
          }
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

      foreach (CanvasItem item in SelectedCanvasItems)
      {
        item.move(delta.X / Scale, delta.Y / Scale);
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
    }


    public List<CanvasItem> SelectorSelectedItems = new List<CanvasItem>();

    public void addSelectorSelectedItems()
    {
      foreach (CanvasItem item in CanvasItems)
      {
        if (item.Position.X >= StartSelectRangePosition.X
          && item.Position.X <= StartSelectRangePosition.X + SelectRangeWidth
          && item.Position.Y >= StartSelectRangePosition.Y
          && item.Position.Y <= StartSelectRangePosition.Y + SelectRangeHeight)
        {
          if (!item.IsSelected)
          {
            SelectorSelectedItems.Add(item);
            item.IsSelected = true;
          }
          continue;
        }
        else if (SelectorSelectedItems.Contains(item))
        {
          item.IsSelected = false;
          SelectorSelectedItems.Remove(item);
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

    /// <summary>
    /// This functions tries to find a approximation of an optimal placmenent of the nodes to have the least overlaps.
    /// Since topological sorting is a NP-hard problem, this function uses a heuristic approach to find a good solution.
    /// Be careful when modifying this function, since most aspects are dependent on each other and thus can break easily.
    /// Searching for someone to blame? => Felix Baumueller
    /// </summary>
    public void sortNodes()
    {
      List<List<Connectable>> sortedGroups = getTopologicallySortedGroups();

      double initialStartX = 50;
      double startX = initialStartX;
      double startY = 50;
      double horizontalSpacing = 400;
      double verticalSpacing = 50;

      double groupVerticalSpacing = 200;

      foreach (List<Connectable> group in sortedGroups)
      {
        if (group.Count == 0)
          continue;

        Dictionary<Connectable, int> nodeLevels = new Dictionary<Connectable, int>();
        CalculateLevels(group, nodeLevels);

        var nodesByLevel = group.GroupBy(node => nodeLevels[node])
                               .OrderBy(g => g.Key)
                               .ToDictionary(g => g.Key, g => g.ToList());

        // Two-pass algorithm for better vertical alignment
        // First pass: Determine positions with a bottom-up approach
        Dictionary<Connectable, double> nodeYPositions = new Dictionary<Connectable, double>();
        Dictionary<int, double> levelWidths = new Dictionary<int, double>();
        Dictionary<int, double> levelHeights = new Dictionary<int, double>();
        Dictionary<Connectable, List<Connectable>> parentToChildren = new Dictionary<Connectable, List<Connectable>>();
        Dictionary<Connectable, List<Connectable>> childToParents = new Dictionary<Connectable, List<Connectable>>();

        // Build parent-child relationships
        foreach (var levelGroup in nodesByLevel)
        {
          int level = levelGroup.Key;
          if (level > 0 && nodesByLevel.ContainsKey(level - 1))
          {
            foreach (Connectable parent in nodesByLevel[level - 1])
            {
              foreach (Connection output in parent.Outputs)
              {
                Connectable child = output.End;
                if (nodeLevels.ContainsKey(child) && nodeLevels[child] == level)
                {
                  // Add to parent->children mapping
                  if (!parentToChildren.ContainsKey(parent))
                    parentToChildren[parent] = new List<Connectable>();
                  parentToChildren[parent].Add(child);

                  // Add to child->parents mapping
                  if (!childToParents.ContainsKey(child))
                    childToParents[child] = new List<Connectable>();
                  childToParents[child].Add(parent);
                }
              }
            }
          }
        }

        // Calculate width and height for each level
        foreach (var levelGroup in nodesByLevel)
        {
          int level = levelGroup.Key;
          List<Connectable> levelNodes = levelGroup.Value;

          double levelWidth = levelNodes.Max(node => node.Width);
          levelWidths[level] = levelWidth;

          double levelHeight = levelNodes.Sum(node => node.Height) +
                              (levelNodes.Count - 1) * verticalSpacing;
          levelHeights[level] = levelHeight;
        }

        double currentLevelX = startX;
        double maxY = startY;

        // Initial placement for level 0
        if (nodesByLevel.ContainsKey(0))
        {
          double currentY = startY;

          // First pass: Position nodes with children
          foreach (NodeVM node in nodesByLevel[0])
          {
            // If node has children, place it based on where those children will likely be
            if (parentToChildren.ContainsKey(node) && parentToChildren[node].Count > 0)
            {
              // We'll position it later after we know where the children are
              continue;
            }

            // Otherwise place sequentially
            nodeYPositions[node] = currentY;
            currentY += node.Height + verticalSpacing;
            maxY = Math.Max(maxY, nodeYPositions[node] + node.Height);
          }
        }

        // For each subsequent level, place nodes based on their parents
        int maxLevel = nodesByLevel.Keys.Count > 0 ? nodesByLevel.Keys.Max() : 0;
        for (int level = 1; level <= maxLevel; level++)
        {
          if (!nodesByLevel.ContainsKey(level)) continue;

          foreach (NodeVM node in nodesByLevel[level])
          {
            if (childToParents.ContainsKey(node) && childToParents[node].Count > 0)
            {
              // Calculate position based on parents
              var positionedParents = childToParents[node]
                .Where(p => nodeYPositions.ContainsKey(p))
                .ToList();

              if (positionedParents.Any())
              {
                double avgParentCenter = positionedParents
                  .Average(p => nodeYPositions[p] + p.Height / 2);

                nodeYPositions[node] = avgParentCenter - node.Height / 2;
              }
            }
          }

          // Then, place nodes without parents at this level
          double currentY = startY;
          foreach (NodeVM node in nodesByLevel[level])
          {
            if (!nodeYPositions.ContainsKey(node))
            {
              nodeYPositions[node] = currentY;
              currentY += node.Height + verticalSpacing;
            }
          }

          ResolveOverlaps(nodesByLevel[level], nodeYPositions, verticalSpacing);
        }

        // Second pass - adjust parents based on children's positions
        for (int level = maxLevel - 1; level >= 0; level--)
        {
          if (!nodesByLevel.ContainsKey(level)) continue;

          foreach (NodeVM parent in nodesByLevel[level])
          {
            if (parentToChildren.ContainsKey(parent) && parentToChildren[parent].Count > 0)
            {
              // Calculate position based on children
              var positionedChildren = parentToChildren[parent]
                .Where(c => nodeYPositions.ContainsKey(c))
                .ToList();

              if (positionedChildren.Any())
              {
                double minChildY = positionedChildren.Min(c => nodeYPositions[c]);
                double maxChildY = positionedChildren.Max(c => nodeYPositions[c] + c.Height);

                double childrenCenter = (minChildY + maxChildY) / 2;
                nodeYPositions[parent] = childrenCenter - parent.Height / 2;
              }
            }

            // Handle level 0 nodes that didn't get positioned in the first pass
            else if (level == 0 && !nodeYPositions.ContainsKey(parent))
            {
              double currentY = startY;
              while (nodesByLevel[0].Any(n => nodeYPositions.ContainsKey(n) &&
                                       nodeYPositions[n] <= currentY &&
                                       nodeYPositions[n] + n.Height + verticalSpacing > currentY))
              {
                currentY += verticalSpacing;
              }
              nodeYPositions[parent] = currentY;
              maxY = Math.Max(maxY, currentY + parent.Height);
            }
          }

          ResolveOverlaps(nodesByLevel[level], nodeYPositions, verticalSpacing);
        }

        // Place the nodes at their final positions
        for (int level = 0; level <= maxLevel; level++)
        {
          if (!nodesByLevel.ContainsKey(level)) continue;

          double levelWidth = levelWidths[level];
          currentLevelX = startX + level * (horizontalSpacing + levelWidth);

          foreach (NodeVM node in nodesByLevel[level])
          {
            double nodeY = nodeYPositions[node];
            node.moveAbsolute(currentLevelX, nodeY);
            maxY = Math.Max(maxY, nodeY + node.Height);
          }
        }

        // Reset X position for next group and move Y position down
        startX = initialStartX;
        startY = maxY + groupVerticalSpacing;
      }
    }

    private void ResolveOverlaps(List<Connectable> nodes, Dictionary<Connectable, double> positions, double spacing)
    {
      var orderedNodes = nodes.OrderBy(n => positions[n]).ToList();

      for (int i = 0; i < orderedNodes.Count - 1; i++)
      {
        Connectable current = orderedNodes[i];
        Connectable next = orderedNodes[i + 1];

        double currentBottom = positions[current] + current.Height;
        double nextTop = positions[next];

        if (nextTop < currentBottom + spacing)
        {
          double offset = currentBottom + spacing - nextTop;

          // Push all subsequent nodes down
          for (int j = i + 1; j < orderedNodes.Count; j++)
          {
            positions[orderedNodes[j]] += offset;
          }
        }
      }
    }

    private void CalculateLevels(List<Connectable> nodes, Dictionary<Connectable, int> levels)
    {
      foreach (var node in nodes)
      {
        levels[node] = 0;
      }

      bool changed = true;
      while (changed)
      {
        changed = false;
        foreach (var node in nodes)
        {
          int currentLevel = levels[node];
          foreach (var connection in node.Outputs)
          {
            var targetNode = connection.End;
            if (nodes.Contains(targetNode) && levels[targetNode] <= currentLevel)
            {
              levels[targetNode] = currentLevel + 1;
              changed = true;
            }
          }
        }
      }
    }

    private List<List<Connectable>> findConnectedComponents()
    {
      HashSet<Connectable> visited = new HashSet<Connectable>();
      List<List<Connectable>> components = new List<List<Connectable>>();

      foreach (NodeVM node in Nodes)
      {
        if (!visited.Contains(node))
        {
          List<Connectable> component = new List<Connectable>();
          Queue<Connectable> queue = new Queue<Connectable>();
          queue.Enqueue(node);
          visited.Add(node);

          while (queue.Count > 0)
          {
            Connectable current = queue.Dequeue();
            component.Add(current);

            // Add all unvisited neighbors (both incoming and outgoing connections)
            foreach (Connection output in current.Outputs)
            {
              if (!visited.Contains(output.End))
              {
                visited.Add(output.End);
                queue.Enqueue(output.End);
              }
            }
            foreach (Connection input in current.Inputs)
            {
              if (!visited.Contains(input.Start))
              {
                visited.Add(input.Start);
                queue.Enqueue(input.Start);
              }
            }
          }
          components.Add(component);
        }
      }
      return components;
    }

    private List<Connectable> topologicalSortComponent(List<Connectable> component)
    {
      // Calculate in-degrees for each node
      Dictionary<Connectable, int> inDegree = new Dictionary<Connectable, int>();
      foreach (Connectable node in component)
      {
        inDegree[node] = node.Inputs.Count;
      }

      // Find nodes with no incoming edges
      Queue<Connectable> queue = new Queue<Connectable>();
      foreach (Connectable node in component)
      {
        if (inDegree[node] == 0)
        {
          queue.Enqueue(node);
        }
      }

      List<Connectable> sortedNodes = new List<Connectable>();
      int visitedCount = 0;

      while (queue.Count > 0)
      {
        Connectable current = queue.Dequeue();
        sortedNodes.Add(current);
        visitedCount++;

        foreach (Connection output in current.Outputs)
        {
          Connectable neighbor = output.End;
          inDegree[neighbor]--;
          if (inDegree[neighbor] == 0)
          {
            queue.Enqueue(neighbor);
          }
        }
      }

      // If we couldn't visit all nodes, there's a cycle
      if (visitedCount != component.Count)
      {
        return null; // Indicates a cycle was found
      }

      return sortedNodes;
    }

    public List<List<Connectable>> getTopologicallySortedGroups()
    {
      List<List<Connectable>> result = new List<List<Connectable>>();
      List<List<Connectable>> components = findConnectedComponents();

      foreach (List<Connectable> component in components)
      {
        List<Connectable> sortedComponent = topologicalSortComponent(component);
        if (sortedComponent != null) // Only add if no cycle was found
        {
          result.Add(sortedComponent);
        }
      }

      return result;
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

    double newOffset = 0.25;
    Timer timer;
    public void AnimateConnections()
    {
      if (!EnableConnectionAnimations)
        return;

      timer = new Timer((o) =>
      {
        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
        {
          GlobalAnimationOffset += -newOffset;
        }));
      }, null, 0, 30);

    }


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

      EnableConnectionAnimations = true;
      AnimateConnections();

      NodeVM node1 = new NodeVM("Node 1", 1);
      NodeVM node2 = new NodeVM("Node 2", 2);
      NodeVM node3 = new NodeVM("Node 3", 3);
      TransferUnitVM transferUnit1 = new TransferUnitVM("Transfer Unit 1", 1);
      TransferUnitVM transferUnit2 = new TransferUnitVM("Transfer Unit 2", 2);
      TransferUnitVM transferUnit3 = new TransferUnitVM("Transfer Unit 3", 3);

      node1.connect(node2, new List<TransferUnitVM> { transferUnit3 });



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
