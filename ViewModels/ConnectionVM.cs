using MyGraph.Models;
using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MyGraph.ViewModels
{
  class ConnectionVM : CanvasItem
  {

    public double MarginStrength = 150;
    public double OffsetInner = 0;
    double spacing = 15;

    public NodeVM Start { get; private set; }
    public NodeVM End { get; set; }

    public Point startPos
    {
      get => Get<Point>();
      set => Set(value);
    }

    public PointCollection CurvePoints
    {

      get => Get<PointCollection>();
      set => Set(value);
    }

    public void moveStart(double deltaX, double deltaY)
    {
      Debug.Assert(CurvePoints.Count == 3);
      startPos = new Point(startPos.X + deltaX, startPos.Y + deltaY);
      CurvePoints[0] = new Point(CurvePoints[0].X + deltaX, CurvePoints[0].Y + deltaY);
    }

    public void moveEnd(double deltaX, double deltaY)
    {

      Debug.Assert(CurvePoints.Count == 3);
      CurvePoints[1] = new Point(CurvePoints[1].X + deltaX, CurvePoints[1].Y + deltaY);
      CurvePoints[2] = new Point(CurvePoints[2].X + deltaX, CurvePoints[2].Y + deltaY);

      //Easy way to trigger WPF render refresh because PointCollection dont recognize change
      Point _startPos = startPos;
      startPos = new Point();
      startPos = _startPos;
    }
    private void updateAllConnection()
    {
      foreach (ConnectionVM connection in End.Inputs)
      {
        connection.updateInput();
      }

      foreach (ConnectionVM connection in Start.Outputs)
      {
        connection.updateOutput();
      }

    }

    /// <summary>
    /// Updates the position of the connection when a new input/output is added
    /// </summary>
    /// <param name="node">The node that needs to be updated</param>
    /// <param name="connectionList">Either Inputs or Outputs of the given node</param>
    public double getNewYPosition(NodeVM node, ObservableCollection<ConnectionVM> connectionList)
    {
      int index = connectionList.IndexOf(this) + 1;
      Debug.Assert(CurvePoints.Count == 3);

      double middlePoint = (double)(connectionList.Count() + 1) / 2;
      int modifier = middlePoint > index ? -1 : +1;
      double stepCount = Math.Abs(middlePoint - index);

      double newPosition = (double)(spacing * modifier) * stepCount;
      double maxPositionInsideNode = (double)(node.Height / 2) - spacing;

      if (newPosition > maxPositionInsideNode)
      {
        node.Height += spacing;
        updateAllConnection();
      }

      return newPosition + node.Position.Y + (double)(node.Height / 2);

    }

    public void updateInput()
    {
      double PositionX = End.Position.X;
      double PositionY = getNewYPosition(End, End.Inputs);
      CurvePoints[1] = new Point(PositionX - MarginStrength, PositionY);
      CurvePoints[2] = new Point(PositionX, PositionY);

      Point _startPos = startPos;
      startPos = new Point();
      startPos = _startPos;

    }
    public void updateOutput()
    {
      double PositionOutputX = Start.Position.X + Start.Width;
      double PositionOutputY = getNewYPosition(Start, Start.Outputs);

      startPos = new Point(PositionOutputX, PositionOutputY);
      CurvePoints[0] = new Point(PositionOutputX + MarginStrength, PositionOutputY);
    }

    public ConnectionVM(NodeVM output, NodeVM input)
    {
      Debug.Assert(output != null);

      CurvePoints = new PointCollection();
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());

      Start = output;
      End = input;

      Start.Outputs.Add(this);

      if (End == null)
      {
        foreach (ConnectionVM connection in Start.Outputs)
        {
          connection.updateOutput();
        }
        CurvePoints[1] = new Point(Canvas.LastMousePosition.X, Canvas.LastMousePosition.Y);
        CurvePoints[2] = new Point(Canvas.LastMousePosition.X, Canvas.LastMousePosition.Y);

        Canvas.GhostConnection = this;
        ZIndex = Canvas.Nodes.Max(c => c.ZIndex) + 1;
        return;
      }

      Canvas.Connections.Add(this);

      //Not yet connected thus drawing to mouse position until connected
      End = input;

      End.Inputs.Add(this);
      updateAllConnection();


    }

    public void Delete()
    {
      Start.Outputs.Remove(this);
      Canvas.Connections.Remove(this);

      if (Start.Height > Start.MinHeight)
        Start.Height -= spacing;

      if (End != null)
      {
        End.Inputs.Remove(this);
        if (End.Height > End.MinHeight)
          End.Height -= spacing;
        updateAllConnection();
        return;
      }

      Canvas.GhostConnection = null;
      foreach (ConnectionVM connection in Start.Outputs)
      {
        connection.updateOutput();
      }

    }

    public void MouseDown()
    {
      if (End != null)
      {
        Start.disconnectNode(End);
        new ConnectionVM(Start, null);
        Canvas.CurrentAction = Action.ConnectingOutput;
      }

    }
  }
}
