using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace MyGraph.ViewModels
{
  class MenuVM : NotifyObject
  {
    #region Properties
    public CanvasVM Canvas
    {
      get => Get<CanvasVM>();
      set => Set(value);
    }
    public bool IsSearching
    {
      get => Get<bool>();
      set => Set(value);
    }
    public string SearchText
    {
      get => Get<string>();
      set
      {
        if (value == "")
        {
          SearchedNodes = new ObservableCollection<NodeVM>(Canvas.Nodes.OrderBy(n => n.Name));
        }
        else
        {
          SearchedNodes = new ObservableCollection<NodeVM>(Canvas.Nodes.Where(m => m.Name.ToLower().Contains(value.ToLower())).ToList().OrderBy(n => n.Name));
        }
        Set(value);
      }
    }

    public ObservableCollection<NodeVM> SearchedNodes
    {
      get => Get<ObservableCollection<NodeVM>>();
      set => Set(value);
    }

    #endregion

    #region Commands
    public IRelayCommand DeleteNodesCommand { get; private set; }
    public IRelayCommand LockNodesCommand { get; private set; }
    public IRelayCommand ConnectNodesCommand { get; private set; }
    public IRelayCommand OpenSearchCommand { get; private set; }
    public IRelayCommand SearchNodeCommand { get; private set; }

    private void createCommands()
    {
      DeleteNodesCommand = new RelayCommand(deleteNodes);
      LockNodesCommand = new RelayCommand(lockNodes);
      ConnectNodesCommand = new RelayCommand(connectNodes);
      OpenSearchCommand = new RelayCommand(openSearch);
      SearchNodeCommand = new RelayCommand<NodeVM>(searchNode);
    }

    public void searchNode(NodeVM node)
    {
      Debug.Assert(node != null);

      Canvas.panToNode(node);

    }
    public void openSearch()
    {
      IsSearching = !IsSearching;
    }

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

    #endregion Commands

    #region Constructor
    public MenuVM()
    {
      createCommands();
      Canvas = CanvasVM.currentCanvas;
      SearchedNodes = new ObservableCollection<NodeVM>();
      SearchText = "";
      IsSearching = false;

    }
    #endregion




  }
}
