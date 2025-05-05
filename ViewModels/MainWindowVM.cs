using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGraph.ViewModels
{
  public class MainWindowVM : NotifyObject
  {
    #region Properties
    public double Height
    {
      get => Get<double>();
      set => Set(value);
    }
    public double Width
    {
      get => Get<double>();
      set => Set(value);
    }

    #endregion

    #region Constructor
    public MainWindowVM()
    {
      Width = 1000;
      Height = 600;
    }
    #endregion



  }
}
