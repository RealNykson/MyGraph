using System.Windows.Controls;
using System.Windows.Input;
using MyGraph.Models;
using MyGraph.ViewModels;

namespace MyGraph.Views
{
  /// <summary>
  /// Interaction logic for Node.xaml
  /// </summary>
  public partial class TransferUnit : UserControl
  {
    public TransferUnit()
    {
      InitializeComponent();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
      ((CanvasItem)DataContext).MouseUp(e);
      e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
      ((CanvasItem)DataContext).MouseDown(e);
      e.Handled = true;
    }

    private void MouseEnter(object sender, MouseEventArgs e)
    {
      ((Connectable)DataContext).MouseEnter();
      e.Handled = true;
    }

    private void MouseLeave(object sender, MouseEventArgs e)
    {

      ((Connectable)DataContext).MouseLeave();
      e.Handled = true;

    }







  }
}
