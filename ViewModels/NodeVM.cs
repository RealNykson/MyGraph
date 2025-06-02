using MyGraph.Models;
using MyGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace MyGraph.ViewModels
{
  class NodeVM : Connectable
  {
    public int Id { get; set; }

    public bool IsTransfer
    {
      get => Get<bool>();
      set => Set(value);
    }


    #region Properties
    public string Name
    {
      get => Get<string>();
      set => Set(value.Trim());
    }


    public double MinWidth
    {
      get => Get<double>();
      set
      {
        Set(value);
        if (value > Width)
          Width = value;
      }
    }

    public double MinHeight
    {
      get => Get<double>();
      set
      {
        Set(value);
        if (value > Height)
          Height = value;
      }
    }

    public override void handleConnection()
    {
      if (Canvas.GhostConnection.Start != this
        && Canvas.GhostConnection.Start != null
        && !Canvas.GhostConnection.Start.isAllreadyConnectedTo(this))
      {
        Connectable start = Canvas.GhostConnection.Start;
        Canvas.GhostConnection.Delete();
        start.connect(this);
        Canvas.CurrentAction = ViewModels.Action.None;
      }
    }

    public bool IsDragging
    {
      get => Get<bool>();
      set => Set(value);
    }


    #endregion 

    #region Commands 

    public IRelayCommand AddGhostOutputCommand { get; private set; }

    public void createCommands()
    {
      AddGhostOutputCommand = new RelayCommand(addGhostOutput);
    }

    public void addGhostOutput()
    {
      Canvas.CurrentAction = Action.ConnectingOutput;
      new PreviewConnectionVM(this);
    }

    #endregion Commands

    #region Methods

    public void Delete()
    {
      RemoveConnections();
      Canvas.SelectedNodes.Remove(this);
      Canvas.Nodes.Remove(this);
    }




    #endregion

    #region Events


    public void MouseEnter()
    {
      if (Canvas.CurrentAction == Action.ConnectingOutput
               && Canvas.GhostConnection.Start != this
               && !Canvas.GhostConnection.Start.isAllreadyConnectedTo(this))
      {
        Canvas.GhostConnection.End = this;
        ZIndex = Canvas.Nodes.Max(n => n.ZIndex) + 1;
      }

    }

    public void MouseLeave()
    {
      if (Canvas.CurrentAction == Action.ConnectingOutput
        && Canvas.GhostConnection.End == this)
      {
        Canvas.GhostConnection.End = null;
        Inputs.Remove(Canvas.GhostConnection);
        Canvas.GhostConnection.moveEndToMouse();
      }

    }



    #endregion

    #region Constructor 

    public NodeVM(string name = "", int id = -1)
    {
      Id = id;
      Width = 350;
      Height = 70;
      Name = name;
      //Position = Canvas.findNextFreeArea(MinWidth, MinHeight);
      Outputs = new ObservableCollection<Connection>();
      Inputs = new ObservableCollection<Connection>();
      createCommands();
      Canvas.Nodes.Add(this);

    }

    #endregion


  }
}
