using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
  public partial class Canvas : UserControl
  {
    public Canvas()
    {
      DataContext = new CanvasVM();
      InitializeComponent();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
      ((ViewModels.CanvasVM)DataContext).MouseWheelZoom(e.Delta);
    }


    private void ZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      ((ViewModels.CanvasVM)DataContext).MouseWheelZoom(e.Delta);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
      ((ViewModels.CanvasVM)DataContext).MouseDown(e);
    }
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
      ((ViewModels.CanvasVM)DataContext).MouseUp(e);
    }




    protected override void OnMouseMove(MouseEventArgs e)
    {
      ((ViewModels.CanvasVM)DataContext).MouseMove(e.GetPosition(this));
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {

      ((ViewModels.CanvasVM)DataContext).MousePositionOnCanvas = e.GetPosition((IInputElement)sender);
    }

    private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
    {

      Grid grid = (Grid)sender;
      ((ViewModels.CanvasVM)DataContext).GridWidth = grid.ActualWidth;
      ((ViewModels.CanvasVM)DataContext).GridHeight = grid.ActualHeight;

    }

    private void Canvas_MouseLeave(object sender, MouseEventArgs e)
    {

      ((ViewModels.CanvasVM)DataContext).MouseLeave();

    }
  }
}