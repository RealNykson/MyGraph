using System;
using MyGraph.ViewModels;
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
  /// Interaction logic for Conncetion.xaml
  /// </summary>
  public partial class Connection : UserControl
  {
    public Connection()
    {
      InitializeComponent();
    }
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
      ((ConnectionVM)DataContext).MouseDown();
      e.Handled = true;
    }

  }
}
