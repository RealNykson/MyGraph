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
  public class NodeVM : Connectable
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






    #endregion

    #region Constructor 

    public NodeVM(string name = "", int id = -1)
    {
      Id = id;
      Name = name;
      createCommands();
      Canvas.Nodes.Add(this);

    }

    #endregion


  }
}
