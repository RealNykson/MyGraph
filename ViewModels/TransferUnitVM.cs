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

    public override bool IsDraggable { get => true; }
    public override bool IsSelectable { get => true; }

    public string Name { get => Get<string>(); set => Set(value); }

    /// <summary>
    /// This is a dictionary of connections that display what connection end in what connection 
    /// The key is a Input Connection of the transfer Unit to who one wants to look up the connections that it need to connect to.
    /// This CAN be multiple connections.
    /// E.g. Node ---Connection 1--> TransferUnit1 ---Connection 2--> Node1
    /// E.g. Node ---Connection 1--> TransferUnit1 ---Connection 3--> Node2
    /// ConnectionToConnection[Connection 1] = [Connection 2, Connection 3]
    /// </summary>
    public ObservableCollection<InterConnectionVM> InternConnections { get => Get<ObservableCollection<InterConnectionVM>>(); set => Set(value); }

    public ObservableCollection<Connection> Connections { get => Get<ObservableCollection<Connection>>(); set => Set(value); }
    public void addInternConnection(ConnectableConnection connection, ConnectableConnection nextDestination)
    {
      InternConnections.Add(new InterConnectionVM(connection, nextDestination));
    }

    public override void customConnectionLogic(ConnectableConnection connection)
    {

      return;
    }



    public TransferUnitVM(string name, int id = -1)
    {
      InternConnections = new ObservableCollection<InterConnectionVM>();
      Outputs.CollectionChanged += Outputs_CollectionChanged;
      Inputs.CollectionChanged += Inputs_CollectionChanged;
      Canvas.TransferUnits.Add(this);
      Name = name;
      Id = id;
    }

    private void Inputs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {

      if (e.Action == NotifyCollectionChangedAction.Remove)
      {
        foreach (ConnectableConnection removedConnection in e.OldItems)
        {
          var connectionsToRemove = InternConnections.Where(ic => ic.previousConnection == removedConnection).ToList();
          foreach (var connection in connectionsToRemove)
          {
            InternConnections.Remove(connection);
          }
        }
      }

    }

    private void Outputs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      if (e.Action == NotifyCollectionChangedAction.Replace)
      {
        foreach (ConnectableConnection newConnection in e.NewItems)
        {
          foreach (ConnectableConnection oldConnection in e.OldItems)
          {
            var connectionsToUpdate = InternConnections.Where(ic => ic.nextConnection == oldConnection).ToList();
            foreach (var connection in connectionsToUpdate)
            {
              connection.nextConnection = newConnection;
            }
          }
        }
      }
      if (e.Action == NotifyCollectionChangedAction.Remove)
      {
        foreach (ConnectableConnection removedConnection in e.OldItems)
        {
          var connectionsToRemove = InternConnections.Where(ic => ic.nextConnection == removedConnection).ToList();
          foreach (var connection in connectionsToRemove)
          {
            InternConnections.Remove(connection);
          }
        }
      }
    }
  }
}
