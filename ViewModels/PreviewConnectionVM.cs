using MyGraph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MyGraph.ViewModels
{
  public class PreviewConnectionVM : Connection
  {
    public PreviewConnectionVM(Connectable output, Connection oldConnection = null) : base(output, null, oldConnection)
    {

      CurvePoints[1] = Canvas.MousePositionOnCanvas;
      CurvePoints[2] = Canvas.MousePositionOnCanvas;
      Start.updateOutputs();
      

      if (Canvas.GhostConnection != null)
      {
        Canvas.GhostConnection.Delete();
      }

      Canvas.GhostConnection = this;
    }

    public override void Delete()
    {
      Start = null;
      End = null;
      Canvas.GhostConnection = null;
    }
    public void moveEndToMouse()
    {
      CurvePoints[1] = Canvas.MousePositionOnCanvas;
      CurvePoints[2] = Canvas.MousePositionOnCanvas;

      //Easy way to trigger WPF render refresh because PointCollection dont recognize change
      Point _startPos = startPos;
      startPos = new Point();
      startPos = _startPos;
    }



  }
}
