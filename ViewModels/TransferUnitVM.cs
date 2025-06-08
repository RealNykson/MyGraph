using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using MyGraph.Models;

namespace MyGraph.ViewModels
{
  public class TransferUnitVM : Connectable
  {
    public struct internConnection
    {
      public ConnectableConnection connection;
      public ConnectableConnection nextConnections;
    }
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
    public ObservableCollection<internConnection> InternConnections { get => Get<ObservableCollection<internConnection>>(); set => Set(value); }

    public ObservableCollection<Connection> Connections { get => Get<ObservableCollection<Connection>>(); set => Set(value); }
    public void addInternConnection(ConnectableConnection connection, ConnectableConnection nextDestination)
    {
      InternConnections.Add(new internConnection() { connection = connection, nextConnections = nextDestination });
    }

    public override void customConnectionLogic(ConnectableConnection connection)
    {
      return;
    }



    public TransferUnitVM(string name, int id = -1)
    {
      InternConnections = new ObservableCollection<internConnection>();
      Outputs.CollectionChanged += Outputs_CollectionChanged;
      Inputs.CollectionChanged += Inputs_CollectionChanged;
      //Minimal Width and Height
      Width = 300;
      Height = 100;
      Canvas.TransferUnits.Add(this);
      Name = name;
      Id = id;
    }

    private void Inputs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      //switch (e.Action)
      //{
      //  case NotifyCollectionChangedAction.Remove:
      //    if (e.OldItems == null) return;
      //    foreach (ConnectableConnection removedInput in e.OldItems)
      //    {
      //      var itemsToRemove = InternConnections.Where(ic => ic.connection == removedInput).ToList();
      //      foreach (var item in itemsToRemove)
      //      {
      //        InternConnections.Remove(item);
      //      }
      //    }
      //    break;
      //}
    }

    private void Outputs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      //throw new NotImplementedException();
    }
  }
}
