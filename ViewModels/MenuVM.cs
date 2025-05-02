using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGraph.ViewModels
{
  class MenuVM : NotifyObject
  {
    public CanvasVM Canvas { get => Get<CanvasVM>(); set => Set(value); }
    public MenuVM()
    {
      Canvas = CanvasVM.currentCanvas;
    }




  }
}
