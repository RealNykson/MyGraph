using MyGraph.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyGraph.Views
{
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      CoreCompatibilityPreferences.EnableMultiMonitorDisplayClipping = true;
      DataContext = new MainWindowVM();
      InitializeComponent();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (this.WindowState == WindowState.Maximized)
      {
        this.BorderThickness = new System.Windows.Thickness(8);
      }
      else
      {
        this.BorderThickness = new System.Windows.Thickness(0);
      }

    }
  }

}
