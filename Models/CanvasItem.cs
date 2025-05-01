using MyGraph.Utilities;
using MyGraph.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGraph.Models
{
  abstract class CanvasItem : NotifyObject 
  {
    public Point Position
    {
      get => Get<Point>();
      set => Set(value);
    }

    public CanvasVM Canvas
    {
      get => Get<CanvasVM>();
      set => Set(value);
    }
    public CanvasItem()
    {
      //Canvas = MainWindowVM.Canvas;
    }


    //public Canvas Canvas { get; set; }


  }
}
