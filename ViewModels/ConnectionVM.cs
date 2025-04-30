using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
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

    private void forceRenderRefresh()
    {

      Point _startPos = startPos;
      startPos = new Point();
      startPos = _startPos;

    }
    double spacing = 15;

    public void updateNew(NodeVM node, ObservableCollection<ConnectionVM> connectionList)
    {
      int index = connectionList.IndexOf(this) + 1;
      Debug.Assert(index != -1);
      Debug.Assert(CurvePoints.Count == 3);

      double middlePoint = (double)(connectionList.Count() + 1) / 2;
      int modifier = middlePoint > index ? -1 : +1;
      double stepCount = Math.Abs(middlePoint - index);

      double newPosition = (double)(spacing * modifier) * stepCount;
      double maxPositionInsideNode = (double)(node.Height / 2) - spacing;

      if (newPosition > maxPositionInsideNode)
      {
        node.Height += spacing;
        foreach (ConnectionVM connection in node.Inputs)
        {
          updateNew(node, node.Inputs);
        }
        foreach (ConnectionVM connection in node.Outputs)
        {
          updateNew(node, node.Outputs);
        }
      }


      double PositionX = node.XPos;
      double PositionY = ((double)(spacing * modifier) * stepCount) + node.YPos + (double)(node.Height / 2);
      CurvePoints[1] = new Point(PositionX - MarginStrength, PositionY);
      CurvePoints[2] = new Point(PositionY + OffsetInner, PositionY);
      forceRenderRefresh();
    }
   

    public ConnectionVM(NodeVM output, NodeVM input)
    {
      Debug.Assert(output != null);
      Debug.Assert(input != null);

      CurvePoints = new PointCollection();
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());
      CurvePoints.Add(new Point());

      Output = output;
      Input = input;
      updateNew(output, output.Outputs);
      updateNew(input, input.Outputs);

    }
    public void MouseUp()
    {
    }
    public void MouseDown()
    {
      Output.disconnectNode(Input);
    }
  }
}
