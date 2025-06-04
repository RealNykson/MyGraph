using System;
using System.Windows;
using System.Windows.Media;
// No `using MyGraph.Models;` to avoid ambiguity, qualify directly
using MyGraph.Views;  // Assuming your Node view is here
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
        public static readonly DependencyProperty ConnectionProperty =
            DependencyProperty.RegisterAttached(
                "Connection",
                typeof(MyGraph.Models.Connection), // Explicitly qualify
                typeof(ConnectorPositionBehavior),
                new PropertyMetadata(null, OnConnectionChanged));

        public static MyGraph.Models.Connection GetConnection(DependencyObject obj) => (MyGraph.Models.Connection)obj.GetValue(ConnectionProperty);
        public static void SetConnection(DependencyObject obj, MyGraph.Models.Connection value) => obj.SetValue(ConnectionProperty, value);

        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.RegisterAttached(
                "Role",
                typeof(ConnectorRole),
                typeof(ConnectorPositionBehavior),
                new PropertyMetadata(ConnectorRole.Output)); // Default to Output

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
            }

            // Handle attachment to new connection
            if (e.NewValue is MyGraph.Models.Connection newConnection)
            {
                element.Loaded += Element_Loaded_Or_LayoutUpdated;
                element.LayoutUpdated += Element_Loaded_Or_LayoutUpdated;

                if (element.IsLoaded && element.IsVisible) // Ensure visible for immediate update
                {
                    UpdatePosition(element, newConnection);
                }
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