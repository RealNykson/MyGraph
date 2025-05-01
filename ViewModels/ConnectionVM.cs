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
  class ConnectionVM : NotifyObject
  {

    public double MarginStrength = 150;
    public double OffsetInner = 0;
    public NodeVM Output { get; private set; }
    public NodeVM Input { get; private set; }

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

    public void updateStart(double deltaX, double deltaY)
    {
      Debug.Assert(CurvePoints.Count == 3);
      startPos = new Point(startPos.X + deltaX, startPos.Y + deltaY);
      CurvePoints[0] = new Point(CurvePoints[0].X + deltaX, CurvePoints[0].Y + deltaY);
    }


    public void updateEnd(double deltaX, double deltaY)
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
      foreach (ConnectionVM connection in Input.Inputs)
      {
        connection.updateInput();
      }

      foreach (ConnectionVM connection in Output.Outputs)
      {
        connection.updateOutput();
      }

    }
    double spacing = 15;

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

      return newPosition + node.YPos + (double)(node.Height / 2);

    }

    public void updateInput()
    {
      double PositionX = Input.XPos;
      double PositionY = getNewYPosition(Input, Input.Inputs);
      CurvePoints[1] = new Point(PositionX - MarginStrength, PositionY);
      CurvePoints[2] = new Point(PositionX, PositionY);

      Point _startPos = startPos;
      startPos = new Point();
      startPos = _startPos;

    }
    public void updateOutput()
    {
      double PositionOutputX = Output.XPos + Output.Width;
      double PositionOutputY = getNewYPosition(Output, Output.Outputs);

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


      Output = output;
      Input = input;

      Output.Outputs.Add(this);
      CanvasVM.g_Connections.Add(this);

      if (Input == null)
      {
        CurvePoints[1] = CanvasVM.g_lastMousePosition;
        CurvePoints[2] = CanvasVM.g_lastMousePosition;

        foreach (ConnectionVM connection in Output.Outputs)
        {
          connection.updateOutput();
        }

        return;
      }

      //Not yet connected thus drawing to mouse position until connected
      Input = input;

      Input.Inputs.Add(this);
      updateAllConnection();


    }
    public void Delete()
    {
      Output.Outputs.Remove(this);
      CanvasVM.g_Connections.Remove(this);

      if (Output.Height > Output.MinHeight)
        Output.Height -= spacing;

      if (Input != null)
      {
        Input.Inputs.Remove(this);
        if (Input.Height > Input.MinHeight)
          Input.Height -= spacing;
        updateAllConnection();
        return;
      }
      else
      {
        CanvasVM.g_GhostConnection = null;
      }
      foreach (ConnectionVM connection in Output.Outputs)
      {
        connection.updateOutput();
      }


    }

    public void MouseUp()
    {
    }
    public void MouseDown()
    {
      if (Input != null)
        Output.disconnectNode(Input);
    }
  }
}
