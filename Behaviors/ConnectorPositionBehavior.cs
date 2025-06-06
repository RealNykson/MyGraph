using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyGraph.Views;
using System.Windows.Threading;

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

            // Handle detachment from old connection
            if (e.OldValue is MyGraph.Models.Connection)
            {
                element.Loaded -= Element_Loaded_Or_LayoutUpdated;
                element.LayoutUpdated -= Element_Loaded_Or_LayoutUpdated;
                //UnsubscribeFromCollection(element);
            }

            // Handle attachment to new connection
            if (e.NewValue is MyGraph.Models.Connection newConnection)
            {
                element.Loaded += Element_Loaded_Or_LayoutUpdated;
                element.LayoutUpdated += Element_Loaded_Or_LayoutUpdated;
                ConnectorRole role = GetRole(element);
                if (role == ConnectorRole.Output)
                {
                    newConnection.Start.Outputs.CollectionChanged += (s, ee) => Element_Loaded_Or_LayoutUpdated(element, ee);
                    newConnection.Start.Inputs.CollectionChanged += (s, ee) => Element_Loaded_Or_LayoutUpdated(element, ee);
                }
                else
                {
                    newConnection.End.Inputs.CollectionChanged += (s, ee) => Element_Loaded_Or_LayoutUpdated(element, ee);
                    newConnection.End.Outputs.CollectionChanged += (s, ee) => Element_Loaded_Or_LayoutUpdated(element, ee);
                }

                if (element.IsLoaded && element.IsVisible)
                {
                    UpdatePosition(element, newConnection);
                }
            }
        }

        private static void SubscribeToCollection(FrameworkElement element)
        {
            var itemsControl = FindAncestor<ItemsControl>(element);
            if (itemsControl?.ItemsSource is INotifyCollectionChanged collection)
            {
                if (!MonitoredCollections.ContainsKey(collection))
                {
                    MonitoredCollections[collection] = new HashSet<ItemsControl>();
                    collection.CollectionChanged += Collection_CollectionChanged;
                }
                MonitoredCollections[collection].Add(itemsControl);
            }
        }

        private static void UnsubscribeFromCollection(FrameworkElement element)
        {
            var itemsControl = FindAncestor<ItemsControl>(element);
            if (itemsControl?.ItemsSource is INotifyCollectionChanged collection)
            {
                if (MonitoredCollections.ContainsKey(collection))
                {
                    MonitoredCollections[collection].Remove(itemsControl);

                    // If no more ItemsControls are using this collection, unsubscribe completely
                    if (MonitoredCollections[collection].Count == 0)
                    {
                        collection.CollectionChanged -= Collection_CollectionChanged;
                        MonitoredCollections.Remove(collection);
                    }
                }
            }
        }

        private static void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!(sender is INotifyCollectionChanged collection)) return;

            if (MonitoredCollections.ContainsKey(collection))
            {
                // Update all connector elements in all ItemsControls using this collection
                foreach (var itemsControl in MonitoredCollections[collection].ToList())
                {
                    UpdateAllConnectorsInItemsControl(itemsControl);
                }
            }
        }

        private static void UpdateAllConnectorsInItemsControl(ItemsControl itemsControl)
        {
            if (itemsControl == null) return;

            // Use Dispatcher to ensure UI operations happen on the UI thread
            itemsControl.Dispatcher.BeginInvoke(new Action(() =>
            {
                var connectorElements = FindConnectorElements(itemsControl);
                foreach (var element in connectorElements)
                {
                    var connection = GetConnection(element);
                    if (connection != null && element.IsLoaded && element.IsVisible)
                    {
                        UpdatePosition(element, connection);
                    }
                }
            }), DispatcherPriority.Loaded); // Use Loaded priority to ensure layout is complete
        }

        private static List<FrameworkElement> FindConnectorElements(ItemsControl itemsControl)
        {
            var connectorElements = new List<FrameworkElement>();

            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null)
                {
                    // Find all elements with our behavior attached within this container
                    FindConnectorElementsRecursive(container, connectorElements);
                }
            }

            return connectorElements;
        }

        private static void FindConnectorElementsRecursive(DependencyObject parent, List<FrameworkElement> connectorElements)
        {
            if (parent is FrameworkElement element && GetConnection(element) != null)
            {
                connectorElements.Add(element);
            }

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                FindConnectorElementsRecursive(child, connectorElements);
            }
        }

        // Shared handler for Loaded and LayoutUpdated events
        private static void Element_Loaded_Or_LayoutUpdated(object sender, EventArgs e)
        {
            if (!(sender is FrameworkElement element)) return;

            var connection = GetConnection(element);
            // Ensure the element is loaded, visible and still has a connection attached
            if (connection != null && element.IsLoaded && element.IsVisible)
            {
                UpdatePosition(element, connection);
            }
        }

        private static void UpdatePosition(FrameworkElement element, MyGraph.Models.Connection connection)
        {
            var nodeView = FindAncestor<Node>(element);
            if (nodeView != null)
            {
                try
                {
                    // Ensure ActualWidth/Height are available; LayoutUpdated should typically guarantee this.
                    if (element.ActualWidth == 0 && element.ActualHeight == 0 && element.IsVisible)
                    {
                        // If still zero, TranslatePoint might use (0,0) or fail. Defer if necessary or log.
                        // System.Diagnostics.Debug.WriteLine($"Warning: ActualWidth/Height are 0 for {element} in UpdatePosition.");
                        // The existing InvalidOperationException catch might handle failures.
                    }

                    Point connectorCenter = new Point(element.ActualWidth / 2, element.ActualHeight / 2);
                    Point absolutePositionInNode = element.TranslatePoint(connectorCenter, nodeView);

                    ConnectorRole role = GetRole(element);

                    if (role == ConnectorRole.Output)
                    {
                        if (connection.AbsoluteStart != absolutePositionInNode)
                        {
                            connection.AbsoluteStart = absolutePositionInNode;
                        }
                    }
                    else // ConnectorRole.Input
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