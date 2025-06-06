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
  public abstract class Connection : NotifyObject
  {

    public ObservableCollection<TransferUnitVM> TransferUnits { get; set; } = new ObservableCollection<TransferUnitVM>();
    public CanvasVM Canvas { get; set; }
    public double MarginStrength = 125;
    public double OffsetInput = 2.5;
    public double OffsetOutput = 3;
    public double spacing = 23;

    private Connectable _Start;
    public Connectable Start
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

    public void moveEnd(Point delta)
    {
      endPos = new Point(endPos.X + delta.X, endPos.Y + delta.Y);
    }

    public void moveStart(Point delta)
    {
      startPos = new Point(startPos.X + delta.X, startPos.Y + delta.Y);
    }


    private Connectable _End;
    public Connectable End
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

    double InnerMarginStrength = 10;
    public void updateTransferUnit(TransferUnitVM transferUnit)
    {
      int index = TransferUnits.IndexOf(transferUnit);

      if (index != -1)
      {

        double PositionX = transferUnit.Position.X;
        double PositionY = getNewYPosition(transferUnit, transferUnit.Connections);

        //if (CurvePoints.Count <= index + 5)
        //{
        //  Debug.Assert(false, "CurvePoints.Count <= index + 5");
        //  return;
        //}

        BezierSegment p = BezierFromIntersection(startPos, new Point(PositionX, PositionY), new Point(PositionX + transferUnit.Width, PositionY), endPos);
        CurvePoints[0] = p.Point1;
        CurvePoints[1] = p.Point2;
        CurvePoints[2] = p.Point3;

      }
    }

    // linear equation solver utility for ai + bj = c and di + ej = f
    public void solvexy(double a, double b, double c, double d, double e, double f, out double i, out double j)
    {
      j = (c - a / d * f) / (b - a * e / d);
      i = (c - (b * j)) / a;
    }

    // basis functions
    public double b0(double t) { return Math.Pow(1 - t, 3); }
    public double b1(double t) { return t * (1 - t) * (1 - t) * 3; }
    public double b2(double t) { return (1 - t) * t * t * 3; }
    public double b3(double t) { return Math.Pow(t, 3); }

    public void bez4pts1(double x0, double y0, double x4, double y4, double x5, double y5, double x3, double y3, out double x1, out double y1, out double x2, out double y2)
    {
      // find chord lengths
      double c1 = Math.Sqrt((x4 - x0) * (x4 - x0) + (y4 - y0) * (y4 - y0));
      double c2 = Math.Sqrt((x5 - x4) * (x5 - x4) + (y5 - y4) * (y5 - y4));
      double c3 = Math.Sqrt((x3 - x5) * (x3 - x5) + (y3 - y5) * (y3 - y5));
      // guess "best" t
      double t1 = c1 / (c1 + c2 + c3);
      double t2 = (c1 + c2) / (c1 + c2 + c3);
      // transform x1 and x2
      solvexy(b1(t1), b2(t1), x4 - (x0 * b0(t1)) - (x3 * b3(t1)), b1(t2), b2(t2), x5 - (x0 * b0(t2)) - (x3 * b3(t2)), out x1, out x2);
      // transform y1 and y2
      solvexy(b1(t1), b2(t1), y4 - (y0 * b0(t1)) - (y3 * b3(t1)), b1(t2), b2(t2), y5 - (y0 * b0(t2)) - (y3 * b3(t2)), out y1, out y2);
    }

    public BezierSegment BezierFromIntersection(Point startPt, Point int1, Point int2, Point endPt)
    {
      double x1, y1, x2, y2;
      bez4pts1(startPt.X, startPt.Y, int1.X, int1.Y, int2.X, int2.Y, endPt.X, endPt.Y, out x1, out y1, out x2, out y2);
      PathFigure p = new PathFigure { StartPoint = startPt };
      return new BezierSegment { Point1 = new Point(x1, y1), Point2 = new Point(x2, y2), Point3 = endPt };
    }

    public void addTransferUnit(TransferUnitVM transferUnit)
    {
      TransferUnits.Add(transferUnit);
      transferUnit.Connections.Add(this);

      CurvePoints.Clear();
      startPos = new Point(startPos.X, startPos.Y);

      foreach (TransferUnitVM currentTransferUnit in TransferUnits)
      {
        CurvePoints.Add(new Point());
        CurvePoints.Add(new Point());
      }

      foreach (TransferUnitVM currentTransferUnit in TransferUnits)
      {
        updateTransferUnit(currentTransferUnit);
      }
      forceRerenderConnection();
    }

    public Point AbsoluteStart
    {
      get => Get<Point>();
      set { Set(value); Start.updateOutputs(); Start.updateInputs(); }
    }

    public Point AbsoluteEnd
    {
      get => Get<Point>();
      set { Set(value); End.updateInputs(); End.updateOutputs(); }
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
    /// <param name="canvasItem">The canvas item that needs to be updated</param>
    /// <param name="connectionList">Either Inputs or Outputs of the given canvas item</param>
    public double getNewYPosition(CanvasItem canvasItem, ObservableCollection<Connection> connectionList)
    {
      int index = connectionList.IndexOf(this) + 1;
      double middlePoint = (double)(connectionList.Count() + 1) / 2;
      int modifier = middlePoint > index ? -1 : +1;
      double stepCount = Math.Abs(middlePoint - index);
      double newPosition = (double)(spacing * modifier) * stepCount;
      return newPosition + canvasItem.Position.Y + (double)(canvasItem.Height / 2);

    }

    public void updateInput()
    {
      double PositionInputX = AbsoluteEnd.X + End.Position.X;
      double PositionInputY = AbsoluteEnd.Y + End.Position.Y;
      endPos = new Point(PositionInputX, PositionInputY);
    }

    public void forceRerenderConnection()
    {
      Debug.Assert(Start != null);
      if (Start == null)
        return;

      Point _startPos = startPos;
      startPos = new Point();
      startPos = _startPos;
    }

    public void updateOutput()
    {
      Debug.Assert(Start != null);
      if (Start == null)
        return;

      double PositionOutputX = AbsoluteStart.X + Start.Position.X;
      double PositionOutputY = AbsoluteStart.Y + Start.Position.Y;
      startPos = new Point(PositionOutputX, PositionOutputY);
    }

    public Connection(Connectable output, Connectable input, Connection oldConnection = null)
    {
      Canvas = CanvasVM.currentCanvas;
      Debug.Assert(output != null);

      CurvePoints = new PointCollection();
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());

      End = input;

      if (oldConnection == null)
      {
        Start = output;
        return;
      }

      _Start = output;
      Canvas.Connections.Remove(oldConnection as ConnectionVM);
      oldConnection.End.Inputs.Remove(oldConnection);
      for (int i = 0; i < oldConnection.Start.Outputs.Count; i++)
      {
        if (oldConnection.Start.Outputs[i] == oldConnection)
        {
          oldConnection.Start.Outputs[i] = this;
          break;
        }
      }


    }

    public abstract void Delete();



  }
}
