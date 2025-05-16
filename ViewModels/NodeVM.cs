using MyGraph.Models;
using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    public int Id { get; set; }

    public bool IsTransfer { 
      get => Get<bool>(); 
      set => Set(value); 
    }


    #region Properties
    public string Name
    {
      get => Get<string>();
      set => Set(value.Trim());
    }
    public Point Position
    {
      get => Get<Point>();
      set { Set(value); updateInputs(); updateOutputs(); }
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
      set
      {
        if (value > MinHeight)
        {
          Set(value);
          return;
        }
        Set(MinHeight);
      }
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

    public bool IsLocked
    {
      get => Get<bool>();
      set { Set(value); Canvas.SelectedNodes_Changed(this, null); }
    }

    public bool IsDragging
    {
      get => Get<bool>();
      set => Set(value);
    }

    public ObservableCollection<Connection> Inputs
    {
      get => Get<ObservableCollection<Connection>>();
      set { Set(value); value.CollectionChanged += Inputs_CollectionChanged; }
    }

    public ObservableCollection<Connection> Outputs
    {
      get => Get<ObservableCollection<Connection>>();
      set { Set(value); value.CollectionChanged += Outputs_CollectionChanged; }
    }

    #endregion 

    #region Commands 

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

    #endregion Commands

    #region Methods

    /// <summary>
    /// Gets all nodes connected after this node including itself
    /// </summary>
    /// <returns></returns>
    public List<NodeVM> getAllConnectedNodes()
    {
      List<NodeVM> connected = new List<NodeVM>();
      connected.Add(this);
      foreach (ConnectionVM node in Outputs)
      {
        connected.AddRange(node.End.getAllConnectedNodes());
      }
      return connected;
    }

    public void move(double deltaX, double deltaY)
    {
      if (IsLocked)
      {
        return;
      }

      Position = new Point(Position.X + deltaX, Position.Y + deltaY);


      foreach (Connection connection in Inputs)
      {
        connection.Start.orderConnections();
      }

      foreach (Connection connection in Outputs)
      {
        connection.End.orderConnections();
      }

    }

    public void moveAbsolute(double newPosX, double newPosY)
    {
      Position = new Point(newPosX, newPosY);

      foreach (Connection connection in Inputs)
      {

        connection.End.orderConnections();
        connection.updateInput();
      }

      foreach (Connection connection in Outputs)
      {

        connection.Start.orderConnections();
        connection.updateOutput();
      }
    }

    public void connectNode(NodeVM node)
    {
      Debug.Assert(node != null);
      //Debug.Assert(node != this);
      if (Outputs.Where(n => n.End == node).FirstOrDefault() != null) {
        return;
      }
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

    public void orderConnections()
    {
      List<Connection> orderedOutputs = Outputs.OrderBy(c => c.End.Position.Y).ToList();

      if (!Outputs.SequenceEqual(orderedOutputs))
      {
        Outputs = new ObservableCollection<Connection>(orderedOutputs);
        updateOutputs();
      }

      List<Connection> orderedInputs = Inputs.OrderBy(c => c.Start.Position.Y).ToList();
      if (!Outputs.SequenceEqual(orderedInputs))
      {
        Inputs = new ObservableCollection<Connection>(orderedInputs);
        updateInputs();
      }

    }

    double spacingHorizontal = 250;
    double spacingVertical = 75;
    public void orderAllChildrenRelativeToSelf(List<NodeVM> allreadyOrderedList)
    {

      double startX = Position.X;
      double startY = Position.Y;
      double currentX = startX;
      double currentY = startY;
      allreadyOrderedList.Add(this);

      List<Connection> connections = new List<Connection>();

      foreach (Connection con in Outputs)
      {

        if (allreadyOrderedList.Contains(con.End))
          continue;
        con.End.moveAbsolute(currentX + Width + spacingHorizontal, startY);
        startY += con.End.Height + spacingVertical;
      }

      foreach (Connection con in Outputs)
      {
        if (allreadyOrderedList.Contains(con.End))
          continue;
        con.End.orderAllChildrenRelativeToSelf(allreadyOrderedList);
      }



    }

    public void updateOutputs()
    {

      if (Outputs == null)
        return;
      foreach (Connection connection in Outputs)
      {
        connection.updateOutput();
      }
    }

    public void updateInputs()
    {

      if (Inputs == null)
        return;

      foreach (Connection connection in Inputs)
      {
        connection.updateInput();
      }
    }

    public bool isAllreadyConnectedTo(NodeVM input)
    {
      return Canvas.Connections.Where(c => c.Start == this && c.End == input).Count() != 0;
    }

    #endregion

    #region Events

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

     if (!IsSelected)
      {
        justSet = true;
      }

      bool before = IsSelected;
      IsSelected = true;

 
      if (!Keyboard.IsKeyDown(Key.LeftShift) && !before)
      {
        foreach (NodeVM node in Canvas.Nodes.Where(n => n != this))
        {
          node.IsSelected = false;
        }
      }



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

    private void Inputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      ObservableCollection<Connection> newInputs = (ObservableCollection<Connection>)sender;
      switch (e.Action)
      {
        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
          if (newInputs.Count() > Outputs.Count() && newInputs.Count() != 1)
          {
            Height += 15;
            updateOutputs();
          }
          break;
        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
          if (newInputs.Count() >= Outputs.Count() && newInputs.Count() != 0)
          {
            Height -= 15;
            updateOutputs();
          }
          break;
        default:
          break;
      }

      updateInputs();
      Canvas.SelectedNodes_Changed(this, null);

    }
    private void Outputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {

      ObservableCollection<Connection> newOutputs = (ObservableCollection<Connection>)sender;
      switch (e.Action)
      {
        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
          if (newOutputs.Count() > Inputs.Count() && newOutputs.Count() != 1)
          {
            Height += 23;
            updateInputs();
          }
          break;
        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
          if (newOutputs.Count() >= Inputs.Count() && newOutputs.Count() != 0)
          {
            Height -= 23;
            updateInputs();
          }
          break;
        default:
          break;
      }

      updateOutputs();
      Canvas.SelectedNodes_Changed(this, null);

    }

    #endregion

    #region Constructor 

    public NodeVM(string name = "", int id = -1)
    {
      Id = id;

      MinWidth = 350;
      MinHeight = 70;
      Name = name;
      Position = Canvas.findNextFreeArea(MinWidth, MinHeight);
      Outputs = new ObservableCollection<Connection>();
      Inputs = new ObservableCollection<Connection>();
      createCommands();
      Canvas.Nodes.Add(this);

    }

    #endregion


  }
}
