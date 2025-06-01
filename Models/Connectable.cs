using System;
using System.Collections.Generic;
using MyGraph.Utilities;
using MyGraph.ViewModels;

namespace MyGraph.Models
{
    abstract class Connectable : CanvasItem
    {
        List<Type> allowConnectableTypes = new List<Type>();


        public Connectable()
        {
            allowConnectableTypes.Add(typeof(NodeVM));
        }




    }
}