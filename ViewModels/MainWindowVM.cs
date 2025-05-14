using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MyGraph.ViewModels
{
  public class MainWindowVM : NotifyObject
  {
    #region Properties
    private DatabaseConnection _dbConnection;

    public ObservableCollection<ProcessUnit> ProcessUnits
    {
      get => Get<ObservableCollection<ProcessUnit>>();
      set => Set(value);
    }

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

    #region Commands
    public IRelayCommand MinimizeWindowCommand { get; private set; }
    public IRelayCommand MaximizeWindowCommand { get; private set; }
    public IRelayCommand CloseWindowCommand { get; private set; }

    private void createCommands()
    {
      MinimizeWindowCommand = new RelayCommand(minimizeWindow);
      MaximizeWindowCommand = new RelayCommand(maximizeWindow);
      CloseWindowCommand = new RelayCommand(closeWindow);
    }

    private void minimizeWindow()
    {
      Application.Current.MainWindow.WindowState = WindowState.Minimized;
    }

    private void maximizeWindow()
    {
      Application.Current.MainWindow.WindowState =
        Application.Current.MainWindow.WindowState == WindowState.Maximized ?
        WindowState.Normal : WindowState.Maximized;
    }

    private void closeWindow()
    {
      Application.Current.MainWindow.Close();
    }
    #endregion Commands

    #region Constructor
    public MainWindowVM()
    {
      Width = 1000;
      Height = 600;
      createCommands();

    }
    #endregion


  }
}
