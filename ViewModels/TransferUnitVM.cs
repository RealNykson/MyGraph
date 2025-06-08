using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Navigation;
using MyGraph.Models;

namespace MyGraph.ViewModels
{
  public class TransferUnitVM : Connectable
  {
    public int Id { get => Get<int>(); set => Set(value); }
    public string Name { get => Get<string>(); set => Set(value); }

    /// <summary>
    /// This is a dictionary of connections that display what connection end in what connection 
    /// The key is a Input Connection of the transfer Unit to who one wants to look up the connections that it need to connect to.
    /// This CAN be multiple connections.
    /// E.g. Node ---Connection 1--> TransferUnit1 ---Connection 2--> Node1
    /// E.g. Node ---Connection 1--> TransferUnit1 ---Connection 3--> Node2
    /// ConnectionToConnection[Connection 1] = [Connection 2, Connection 3]
    /// </summary>
    public Dictionary<Connection, List<Connection>> ConnectionToConnection { get => Get<Dictionary<Connection, List<Connection>>>(); set => Set(value); }

    public void customConnectionLogic(Connection connection)
    {
      return;
    }



    public TransferUnitVM(string name, int id = -1)
    {
      Width = 300;
      Height = 100;
      Canvas.TransferUnits.Add(this);
      Name = name;
      Id = id;
    }

  }
}
