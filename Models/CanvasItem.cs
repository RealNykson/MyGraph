using MyGraph.Utilities;
using MyGraph.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MyGraph.Models
{
  abstract class CanvasItem : NotifyObject
  {
    public int ZIndex
    {
      get => Get<int>();
      set => Set(value);
    }

    public CanvasVM Canvas
    {
      get => Get<CanvasVM>();
      set => Set(value);
    }
    public CanvasItem()
    {
      Debug.Assert(CanvasVM.currentCanvas != null);
      Canvas = CanvasVM.currentCanvas;
    }


    //public Canvas Canvas { get; set; }


  }
}
