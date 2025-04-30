using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MyGraph.ViewModels;

namespace MyGraph.Views
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      DataContext = new MainWindowVM();
      InitializeComponent();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
      ((MainWindowVM)DataContext).MouseWheelZoom(e.Delta);
    }


    private void ZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      ((MainWindowVM)DataContext).MouseWheelZoom(e.Delta);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
      ((MainWindowVM)DataContext).MouseDown(e);
    }
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
      ((MainWindowVM)DataContext).MouseUp(e);
    }




    protected override void OnMouseMove(MouseEventArgs e)
    {
      ((MainWindowVM)DataContext).MouseMove(e.GetPosition(this));
    }

  }
}