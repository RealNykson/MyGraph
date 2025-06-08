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


      if (oldConnection == null)
      {
        Start = output;
        End = input;
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
      End = input;


    }

    public abstract void Delete();



  }
}
