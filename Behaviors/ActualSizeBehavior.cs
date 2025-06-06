using System.Windows;

namespace MyGraph.Behaviors
{
    /// <summary>
    /// A behavior that allows binding to the ActualWidth and ActualHeight of a FrameworkElement.
    /// </summary>
    public static class ActualSizeBehavior
    {
        #region ObserveActualSize Attached Property

        /// <summary>
        /// A property to enable or disable the size observation.
        /// </summary>
        public static readonly DependencyProperty ObserveActualSizeProperty =
            DependencyProperty.RegisterAttached(
                "ObserveActualSize",
                typeof(bool),
                typeof(ActualSizeBehavior),
                new PropertyMetadata(false, OnObserveActualSizeChanged));

        public static bool GetObserveActualSize(FrameworkElement element) => (bool)element.GetValue(ObserveActualSizeProperty);
        public static void SetObserveActualSize(FrameworkElement element, bool value) => element.SetValue(ObserveActualSizeProperty, value);

        #endregion

        #region ActualWidth Attached Property

        public static readonly DependencyProperty ActualWidthProperty =
            DependencyProperty.RegisterAttached(
                "ActualWidth",
                typeof(double),
                typeof(ActualSizeBehavior),
                new PropertyMetadata(0.0));

        public static double GetActualWidth(FrameworkElement element) => (double)element.GetValue(ActualWidthProperty);
        public static void SetActualWidth(FrameworkElement element, double value) => element.SetValue(ActualWidthProperty, value);

        #endregion

        #region ActualHeight Attached Property

        public static readonly DependencyProperty ActualHeightProperty =
            DependencyProperty.RegisterAttached(
                "ActualHeight",
                typeof(double),
                typeof(ActualSizeBehavior),
                new PropertyMetadata(0.0));

        public static double GetActualHeight(FrameworkElement element) => (double)element.GetValue(ActualHeightProperty);
        public static void SetActualHeight(FrameworkElement element, double value) => element.SetValue(ActualHeightProperty, value);

        #endregion

        private static void OnObserveActualSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.SizeChanged += OnElementSizeChanged;
                    UpdateSize(element); // Set initial value
                }
                else
                {
                    element.SizeChanged -= OnElementSizeChanged;
                }
            }
        }

        private static void OnElementSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UpdateSize(element);
            }
        }

        private static void UpdateSize(FrameworkElement element)
        {
            element.SetValue(ActualWidthProperty, element.ActualWidth);
            element.SetValue(ActualHeightProperty, element.ActualHeight);
        }
    }
}