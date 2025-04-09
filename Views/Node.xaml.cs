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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyGraph.Views
{
  /// <summary>
  /// Interaction logic for Node.xaml
  /// </summary>
  public partial class Node : UserControl
  {
    public Node()
    {
      InitializeComponent();
    }
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
      ((NodeVM)DataContext).MouseDown(e);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
      ((NodeVM)DataContext).MouseReleased(e);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
     // ((NodeVM)DataContext).MouseMove(e.GetPosition(this));
    }





  }
}
