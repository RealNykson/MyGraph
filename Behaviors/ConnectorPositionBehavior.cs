using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyGraph.Views;
using System.Windows.Threading;
using MyGraph.ViewModels;
using System.Collections.ObjectModel;
using Action = System.Action;
using MyGraph.Models;
using System.Diagnostics;

namespace MyGraph.Behaviors
{
    public enum ConnectorRole
    {
        Output,
        Input
    }

    public class ConnectorPositionBehavior
    {
        // Track monitored collections to avoid duplicate subscriptions
        private static readonly Dictionary<INotifyCollectionChanged, HashSet<ItemsControl>> MonitoredCollections =
            new Dictionary<INotifyCollectionChanged, HashSet<ItemsControl>>();

        public static readonly DependencyProperty ConnectionProperty =
            DependencyProperty.RegisterAttached(
                "Connection",
                typeof(MyGraph.Models.Connection),
                typeof(ConnectorPositionBehavior),
                new PropertyMetadata(null, OnConnectionChanged));

        public static MyGraph.Models.Connection GetConnection(DependencyObject obj) => (MyGraph.Models.Connection)obj.GetValue(ConnectionProperty);
        public static void SetConnection(DependencyObject obj, MyGraph.Models.Connection value) => obj.SetValue(ConnectionProperty, value);

        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.RegisterAttached(
                "Role",
                typeof(ConnectorRole),
                typeof(ConnectorPositionBehavior),
                new PropertyMetadata(ConnectorRole.Output));

        public static ConnectorRole GetRole(DependencyObject obj) => (ConnectorRole)obj.GetValue(RoleProperty);
        public static void SetRole(DependencyObject obj, ConnectorRole value) => obj.SetValue(RoleProperty, value);

        private static void OnConnectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement element)) return;

            if (e.NewValue is MyGraph.Models.Connection newConnection)
            {
                // Defer the setup until after the current WPF layout/render pass.
                // This ensures that all properties (like 'Role') have been set from XAML.
                element.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Check if the connection is still the same, in case it changed again
                    // before this delegate was executed.
                    if (GetConnection(element) != newConnection)
                        return;

                    ConnectorRole role = GetRole(element);
                    if (role == ConnectorRole.Output)
                    {
                        newConnection.Start.Outputs.CollectionChanged += (s, ee) => Element_Loaded_Or_LayoutUpdated(element, ee);
                        newConnection.Start.Inputs.CollectionChanged += (s, ee) => Element_Loaded_Or_LayoutUpdated(element, ee);
                    }
                    else // Input
                    {
                        newConnection.End.Inputs.CollectionChanged += (s, ee) => Element_Loaded_Or_LayoutUpdated(element, ee);
                        newConnection.End.Outputs.CollectionChanged += (s, ee) => Element_Loaded_Or_LayoutUpdated(element, ee);
                    }

                    element.Loaded += Element_Loaded_Or_LayoutUpdated;
                    element.LayoutUpdated += Element_Loaded_Or_LayoutUpdated;

                    if (element.IsLoaded && element.IsVisible)
                    {
                        UpdatePosition(element, newConnection);
                    }

                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }



        // Shared handler for Loaded and LayoutUpdated events
        private static void Element_Loaded_Or_LayoutUpdated(object sender, EventArgs e)
        {
            if (!(sender is FrameworkElement element))
            {
                return;
            }

            var connection = GetConnection(element);
            if (connection != null && element.IsLoaded && element.IsVisible)
            {

                UpdatePosition(element, connection);
            }
        }

        private static void UpdatePosition(FrameworkElement element, MyGraph.Models.Connection connection)
        {
            var connectableView = FindAncestor<UserControl>(element);
            if (connectableView != null)
            {
                try
                {
                    if (element.ActualWidth == 0 && element.ActualHeight == 0 && element.IsVisible)
                    {
                        return;
                    }

                    Point connectorCenter = new Point(element.ActualWidth / 2, element.ActualHeight / 2);
                    ConnectorRole role = GetRole(element);

                    // ******************* DO NOT MOVE THIS CODE ***************************************
                    // If the call is from ObservableCollection.CollectionChanged event we manually need to 
                    // force a layoutUpdate for the itemscontrol so that the collection change is visible and 
                    // the calculation will yield the right point. For performance/latency reasons we need to 
                    // update the layout at the last possible moment before calculating the position.
                    // Updating the itemsControl layout too early results in visible flickering. 
                    // ***************************************************************************************
                    var itemsControl = FindAncestor<ItemsControl>(element);
                    if (itemsControl != null)
                    {
                        itemsControl?.UpdateLayout();
                    }
                    Point absolutePositionInNode = element.TranslatePoint(connectorCenter, connectableView);
                    // ***************************************************************************************

                    if (role == ConnectorRole.Output)
                    {
                        if (connection.AbsoluteStart != absolutePositionInNode)
                        {

                            itemsControl?.UpdateLayout();
                            connection.AbsoluteStart = absolutePositionInNode;
                        }
                    }
                    else
                    {
                        if (connection.AbsoluteEnd != absolutePositionInNode)
                        {
                            connection.AbsoluteEnd = absolutePositionInNode;
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    element.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var currentConnection = GetConnection(element);
                        if (element.IsLoaded && element.IsVisible && currentConnection == connection)
                        {
                            UpdatePosition(element, connection);
                        }
                    }), DispatcherPriority.ContextIdle);
                    System.Diagnostics.Debug.WriteLine($"Error calculating position for connector ({element.Name}): {ex.Message}. Will retry via Dispatcher.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Could not find Node ancestor for position calculation.");
            }
        }

        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}