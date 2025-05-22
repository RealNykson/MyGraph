using MyGraph.Utilities;
using MyGraph.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MyGraph.Models
{
  abstract class Connection : CanvasItem
  {
    public double MarginStrength = 125;
    public double OffsetInput = 2.5;
    public double OffsetOutput = 3;
    public double spacing = 23;
    public ObservableCollection<TransferUnitVM> TransferUnits { get; set; } = new ObservableCollection<TransferUnitVM>();

    private NodeVM _Start;
    public NodeVM Start
    {
      get
      {
        return _Start;
      }
      set
      {
        if (_Start != null)
        {
          _Start.Outputs.Remove(this);
        }

        _Start = value;
        if (value != null)
        {
          value.Outputs.Add(this);
        }

      }
    }


    private NodeVM _End;
    public NodeVM End
    {
      get
      {
        return _End;
      }
      set
      {
        if (_End != null)
        {
          _End.Inputs.Remove(this);
        }

        _End = value;
        if (value != null)
        {
          value.Inputs.Add(this);
          return;
        }

      }
    }

    public Point startPos
    {
      get => Get<Point>();
      set { Set(value); }
    }

    public PointCollection CurvePoints
    {

      get => Get<PointCollection>();
      set => Set(value);
    }




    /// <summary>
    /// Updates the position of the connection when a new input/output is added
    /// </summary>
    /// <param name="node">The node that needs to be updated</param>
    /// <param name="connectionList">Either Inputs or Outputs of the given node</param>
    public double getNewYPosition(NodeVM node, ObservableCollection<Connection> connectionList)
    {
      int index = connectionList.IndexOf(this) + 1;
      Debug.Assert(CurvePoints.Count == 3);

      double middlePoint = (double)(connectionList.Count() + 1) / 2;
      int modifier = middlePoint > index ? -1 : +1;
      double stepCount = Math.Abs(middlePoint - index);

      double newPosition = (double)(spacing * modifier) * stepCount;
      double maxPositionInsideNode = (double)(node.Height / 2) - spacing;

      return newPosition + node.Position.Y + (double)(node.Height / 2);

    }

    public void updateInput()
    {

      double PositionX = End.Position.X - OffsetInput;
      double PositionY = getNewYPosition(End, End.Inputs);
      CurvePoints[1] = new Point(PositionX - MarginStrength, PositionY);
      CurvePoints[2] = new Point(PositionX, PositionY);

      Point _startPos = startPos;
      startPos = new Point();
      startPos = _startPos;

    }
    public void updateOutput()
    {

      double PositionOutputX = Start.Position.X + Start.Width + OffsetOutput;
      double PositionOutputY = getNewYPosition(Start, Start.Outputs);

      startPos = new Point(PositionOutputX, PositionOutputY);
      CurvePoints[0] = new Point(PositionOutputX + MarginStrength, PositionOutputY);
    }

    public Connection(NodeVM output, NodeVM input)
    {
      Debug.Assert(output != null);

      CurvePoints = new PointCollection();
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());

      Start = output;
      End = input;

    }

    public abstract void Delete();



  }
}
