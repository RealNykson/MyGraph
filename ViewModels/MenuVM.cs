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
using System.Windows.Media;
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
      set
      {
        Set(value);
        SearchedNodes = new ObservableCollection<NodeVM>(Canvas.Nodes.OrderBy(n => n.Name));
        if (!value)
        {
          Canvas.SearchedNode = null;
          SearchText = "";
        }
      }
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

    public bool EditMode
    {
      get => Get<bool>();
      set => Set(value);
    }

    public bool AreSelectedNodesLocked
    {
      get
      {
        if (Canvas?.SelectedNodes == null || !Canvas.SelectedNodes.Any())
          return false;

        return Canvas.SelectedNodes.Where(n => n.IsLocked).FirstOrDefault() != null;
      }
    }

    public string SearchText
    {
      get => Get<string>();
      set
      {

        if (SearchText == "" && value != "")
        {
          scaleBeforeSearch = Canvas.Scale;
          positionBeforeSearch = new Point(Canvas.CanvasTransformMatrix.Matrix.OffsetX / Canvas.Scale, Canvas.CanvasTransformMatrix.Matrix.OffsetY / Canvas.Scale);
        }

        Set(value);

        if (value == "")
        {
          if (Canvas.SearchedNode != null)
          {
            backToSearchStart();
          }
          SearchedNodes = new ObservableCollection<NodeVM>(Canvas.Nodes.OrderBy(n => n.Name));
          return;
        }



        SearchedNodes = new ObservableCollection<NodeVM>(Canvas.Nodes.Where(m => m.Name.ToLower().Contains(value.ToLower())).ToList().OrderBy(n => n.Name));
        if (SearchedNodes.Count() == 0)
        {
          backToSearchStart();
        }
        if (SearchedNodes.Count() != 0 && SearchedNodes[0] != Canvas.SearchedNode)
          searchNode(SearchedNodes[0]);

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
    public IRelayCommand SortNodesCommand { get; private set; }
    public IRelayCommand SwitchModeCommand { get; private set; }
    public IRelayCommand AddNewNodeCommand { get; private set; }

    private void createCommands()
    {
      DeleteNodesCommand = new RelayCommand(deleteNodes);
      LockNodesCommand = new RelayCommand(lockNodes);
      ConnectNodesCommand = new RelayCommand(connectNodes);
      OpenSearchCommand = new RelayCommand(openSearch);
      SearchNodeCommand = new RelayCommand<NodeVM>(searchNode);
      OpenSettingsCommand = new RelayCommand(openSettings);
      ChangeThemeCommand = new RelayCommand(changeTheme);
      SortNodesCommand = new RelayCommand(sortNodes);
      SwitchModeCommand = new RelayCommand(switchMode);
      AddNewNodeCommand = new RelayCommand(addNewNode);
    }

    private void addNewNode()
    {
      new NodeVM();
    }

    private void sortNodes()
    {
      Canvas.sortNodes();
    }

    private Point positionBeforeSearch;
    private double scaleBeforeSearch;
    private void backToSearchStart()
    {
      if (positionBeforeSearch != null)
      {
        Matrix matrix = Canvas.CanvasTransformMatrix.Matrix;
        matrix.OffsetX = positionBeforeSearch.X * Canvas.Scale;
        matrix.OffsetY = positionBeforeSearch.Y * Canvas.Scale;
        Canvas.CanvasTransformMatrix.Matrix = matrix;
        Canvas.SearchedNode = null;
      }

    }

    public void change()
    {
      App.Current.Resources.MergedDictionaries.Add(currentTheme);
    }
    public void changeTheme()
    {

      if (lightTheme == null)
      {
        lightTheme = new ResourceDictionary() { Source = new Uri("/Resources/Colors/LightMode.xaml", UriKind.Relative) };
        darkTheme = new ResourceDictionary() { Source = new Uri("/Resources/Colors/DarkMode.xaml", UriKind.Relative) };
      }

      if (currentTheme != null)
      {
        App.Current.Resources.MergedDictionaries.Remove(currentTheme);
      }

      currentTheme = DarkMode ? lightTheme : darkTheme;
      App.Current.Resources.MergedDictionaries.Add(currentTheme);

      DarkMode = !DarkMode;
    }

    public void searchNode(NodeVM node)
    {
      Canvas.SearchedNode = node;

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
      bool select = AreSelectedNodesLocked;

      foreach (var node in Canvas.SelectedNodes)
      {
        node.IsLocked = !select;
      }

      OnPropertyChanged(nameof(AreSelectedNodesLocked));
    }
    private void connectNodes()
    {
    }
    private void deleteNodes()
    {
      Canvas.CurrentAction = Action.None;

      for (int i = Canvas.SelectedNodes.Count - 1; i >= 0; i--)
      {
        Canvas.SelectedNodes.ElementAt(i).Delete();
      }

    }

    public void switchMode()
    {
      EditMode = !EditMode;
      // Add any additional logic needed when switching modes
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
      EditMode = true;

      createCommands();
      Canvas = CanvasVM.currentCanvas;
      SearchedNodes = new ObservableCollection<NodeVM>();
      SearchText = "";
      IsSearching = false;
    }
    #endregion




  }
}
