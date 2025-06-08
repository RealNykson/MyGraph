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
  /// <summary>
  /// Base class for all connection describes a belzier curve between two points.
  /// </summary>
  public abstract class Connection : NotifyObject
  {

    public CanvasVM Canvas { get; set; }

    public double MarginStrength = 125;

    public virtual Point AbsoluteStart
    {
      get => Get<Point>();
      set { Set(value); }
    }

    public virtual Point AbsoluteEnd
    {
      get => Get<Point>();
      set { Set(value); }
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

    public void moveEnd(Point delta)
    {
      endPos = new Point(endPos.X + delta.X, endPos.Y + delta.Y);
    }

    public void moveStart(Point delta)
    {
      startPos = new Point(startPos.X + delta.X, startPos.Y + delta.Y);
    }

    public void forceRerenderConnection()
    {
      Point _startPos = startPos;
      startPos = new Point();
      startPos = _startPos;
    }

    public Connection()
    {
      Canvas = CanvasVM.currentCanvas;

      CurvePoints = new PointCollection();
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());
    }

    public abstract void Delete();



  }
}
