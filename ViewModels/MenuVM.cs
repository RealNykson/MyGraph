using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace MyGraph.ViewModels
{
  class MenuVM : NotifyObject
  {
    #region Properties
    private static ResourceDictionary lightTheme;
    private static ResourceDictionary darkTheme;
    private ResourceDictionary currentTheme;

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
    public bool SettingsOpen
    {
      get => Get<bool>();
      set => Set(value);
    }
    public bool DarkMode
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
    public IRelayCommand OpenSettingsCommand { get; private set; }
    public IRelayCommand SearchNodeCommand { get; private set; }
    public IRelayCommand ChangeThemeCommand { get; private set; }

    private void createCommands()
    {
      DeleteNodesCommand = new RelayCommand(deleteNodes);
      LockNodesCommand = new RelayCommand(lockNodes);
      ConnectNodesCommand = new RelayCommand(connectNodes);
      OpenSearchCommand = new RelayCommand(openSearch);
      SearchNodeCommand = new RelayCommand<NodeVM>(searchNode);
      OpenSettingsCommand = new RelayCommand(openSettings);
      ChangeThemeCommand = new RelayCommand(changeTheme);
    }

    public void changeTheme()
    {
        // Initialize themes if not already done
        if (lightTheme == null)
        {
            lightTheme = new ResourceDictionary() { Source = new Uri("/Resources/Colors/LightMode.xaml", UriKind.Relative) };
            darkTheme = new ResourceDictionary() { Source = new Uri("/Resources/Colors/DarkMode.xaml", UriKind.Relative) };
        }

        // Remove current theme
        if (currentTheme != null)
        {
            App.Current.Resources.MergedDictionaries.Remove(currentTheme);
        }

        // Add new theme
        currentTheme = DarkMode ? lightTheme : darkTheme;
        App.Current.Resources.MergedDictionaries.Add(currentTheme);
        
        // Toggle the mode
        DarkMode = !DarkMode;
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
    public void openSettings()
    {
      SettingsOpen = !SettingsOpen;
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
      Canvas.CurrentAction = Action.None;

      for (int i = Canvas.SelectedNodes.Count - 1; i >= 0; i--)
      {
        Canvas.SelectedNodes.ElementAt(i).Delete();
      }

    }

    #endregion Commands

    #region Constructor
    public MenuVM()
    {
        // Initialize themes if not already done
        if (lightTheme == null)
        {
            lightTheme = new ResourceDictionary() { Source = new Uri("/Resources/Colors/LightMode.xaml", UriKind.Relative) };
            darkTheme = new ResourceDictionary() { Source = new Uri("/Resources/Colors/DarkMode.xaml", UriKind.Relative) };
        }

        currentTheme = lightTheme;
        App.Current.Resources.MergedDictionaries.Add(currentTheme);
        DarkMode = false;

        createCommands();
        Canvas = CanvasVM.currentCanvas;
        SearchedNodes = new ObservableCollection<NodeVM>();
        SearchText = "";
        IsSearching = false;
    }
    #endregion




  }
}
