using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace MyGraph.ViewModels
{
  class MenuVM : NotifyObject
  {
    public CanvasVM Canvas
    {
      get => Get<CanvasVM>();
      set => Set(value);
    }
    #region Commands
    public IRelayCommand DeleteNodesCommand { get; private set; }
    public IRelayCommand LockNodesCommand { get; private set; }
    public IRelayCommand ConnectNodesCommand { get; private set; }

    private void createCommands()
    {
      DeleteNodesCommand = new RelayCommand(deleteNodes);
      LockNodesCommand = new RelayCommand(lockNodes);
      ConnectNodesCommand = new RelayCommand(connectNodes);

    }

    #endregion
    private void lockNodes()
    {
      for (int i = Canvas.SelectedNodes.Count - 1; i >= 0; i--)
      {
        Canvas.SelectedNodes.ElementAt(i).IsLocked = !Canvas.SelectedNodes.ElementAt(i).IsLocked;
      }

    }
    private void connectNodes()
    {
      new NodeVM();
    }
    private void deleteNodes()
    {

      for (int i = Canvas.SelectedNodes.Count - 1; i >= 0; i--)
      {
        Canvas.SelectedNodes.ElementAt(i).Delete();
      }

    }

    public MenuVM()
    {
      createCommands();
      Canvas = CanvasVM.currentCanvas;
    }




  }
}
