using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Navigation;
using MyGraph.Models;

namespace MyGraph.ViewModels
{
  class TransferUnitVM : CanvasItem
  {
    public int Id { get => Get<int>(); set => Set(value); }
    public string Name { get => Get<string>(); set => Set(value); }
    public override Point Position
    {
      get => Get<Point>();
      set { Set(value); foreach (ConnectionVM connection in Connections) { connection.updateTransferUnit(this); } }
    }

    public ObservableCollection<ConnectionVM> Connections { get; set; } = new ObservableCollection<ConnectionVM>();

    public TransferUnitVM(string name, int id = -1)
    {

      Width = 100;
      Height = 100;
      Canvas.TransferUnits.Add(this);
      Name = name;
      Id = id;
    }

    public override void handleConnection()
    {

      return;
    }
  }
}
