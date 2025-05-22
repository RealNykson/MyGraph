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
using System.Windows.Controls;
using System.Windows.Media;

namespace MyGraph.Models
{
  abstract class Connection : NotifyObject
  {
    public CanvasVM Canvas { get; set; }
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

    public void updateTransferUnit(TransferUnitVM transferUnit)
    {
      int index = TransferUnits.IndexOf(transferUnit);
      if (index != -1)
      {
        //CurvePoints[index + 1] = new Point(transferUnit.Position.X, transferUnit.Position.Y);
        //CurvePoints[index + 2] = new Point(transferUnit.Position.X + 100, transferUnit.Position.Y);
        forceRerenderConnection();
      }
    }

    public void addTransferUnit(TransferUnitVM transferUnit)
    {
      TransferUnits.Add(transferUnit);
      CurvePoints.Clear();
      CurvePoints.Add(new Point());
      startPos = new Point(startPos.X, startPos.Y);

      foreach (TransferUnitVM currentTransferUnit in TransferUnits)
      {
        CurvePoints.Add(new Point(currentTransferUnit.Position.X, currentTransferUnit.Position.Y));
        CurvePoints.Add(new Point(currentTransferUnit.Position.X + 100, currentTransferUnit.Position.Y));
      }

      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());
      endPos = new Point(endPos.X, endPos.Y);
    }



    public Point startPos
    {
      get => Get<Point>();
      set
      {
        Set(value);
        if (CurvePoints.Count > 0)
          CurvePoints[0] = new Point(value.X + MarginStrength, value.Y);
        else
          CurvePoints.Add(new Point(value.X + MarginStrength, value.Y));
      }
    }

    public Point endPos
    {
      get => Get<Point>();
      set
      {
        Set(value);
        if (CurvePoints.Count >= 3)
        {
          CurvePoints[CurvePoints.Count - 2] = new Point(value.X - MarginStrength, value.Y);
          CurvePoints[CurvePoints.Count - 1] = new Point(value.X, value.Y);
        }
        else
        {
          CurvePoints.Add(new Point(value.X - MarginStrength, value.Y));
          CurvePoints.Add(new Point(value.X, value.Y));
        }
        forceRerenderConnection();
      }
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
      Debug.Assert(CurvePoints.Count >= 3);

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
      endPos = new Point(PositionX, PositionY);

    }
    public void forceRerenderConnection()
    {
      Point _startPos = startPos;
      startPos = new Point();
      startPos = _startPos;
    }
    public void updateOutput()
    {

      double PositionOutputX = Start.Position.X + Start.Width + OffsetOutput;
      double PositionOutputY = getNewYPosition(Start, Start.Outputs);

      startPos = new Point(PositionOutputX, PositionOutputY);
    }

    public Connection(NodeVM output, NodeVM input)
    {
      Canvas = CanvasVM.currentCanvas;
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
