using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using GMap.NET.WindowsPresentation;

namespace VisualizeRoutingWpf
{
    public partial class Marker
    {
        private Popup _popup;
        private Label _label;
        private readonly GMapMarker _marker;
        private readonly MainWindow _mainWindow;

        public Marker(MainWindow window, GMapMarker marker, string title)
        {
            InitializeComponent();

            _mainWindow = window;
            _marker = marker;

            _popup = new Popup();
            _label = new Label();

            Unloaded += Marker_Unloaded;
            Loaded += Marker_Loaded;
            SizeChanged += Marker_SizeChanged;
            MouseEnter += MarkerControl_MouseEnter;
            MouseLeave += MarkerControl_MouseLeave;
            MouseMove += Marker_MouseMove;
            MouseLeftButtonUp += Marker_MouseLeftButtonUp;
            MouseLeftButtonDown += Marker_MouseLeftButtonDown;

            _popup.Placement = PlacementMode.Mouse;
            {
                _label.Background = Brushes.Azure;
                _label.Foreground = Brushes.Black;
                _label.BorderBrush = Brushes.WhiteSmoke;
                _label.BorderThickness = new Thickness(1);
                _label.Padding = new Thickness(2);
                _label.FontSize = 18;
                _label.Content = title;
            }
            _popup.Child = _label;
        }

        private void Marker_Loaded(object sender, RoutedEventArgs e)
        {
            if (Icon.Source.CanFreeze)
            {
                Icon.Source.Freeze();
            }
        }

        private void Marker_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= Marker_Unloaded;
            Loaded -= Marker_Loaded;
            SizeChanged -= Marker_SizeChanged;
            MouseEnter -= MarkerControl_MouseEnter;
            MouseLeave -= MarkerControl_MouseLeave;
            MouseMove -= Marker_MouseMove;
            MouseLeftButtonUp -= Marker_MouseLeftButtonUp;
            MouseLeftButtonDown -= Marker_MouseLeftButtonDown;

            _marker.Shape = null;
            Icon.Source = null;
            Icon = null;
            _popup = null;
            _label = null;
        }

        private void Marker_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _marker.Offset = new Point(-e.NewSize.Width / 2, -e.NewSize.Height);
        }

        private void Marker_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured)
            {
                var p = e.GetPosition(_mainWindow.MapControl);
                _marker.Position = _mainWindow.MapControl.FromLocalToLatLng((int)p.X, (int)p.Y);
            }
        }

        private void Marker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsMouseCaptured)
            {
                Mouse.Capture(this);
            }
        }

        private void Marker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                Mouse.Capture(null);
            }
        }

        private void MarkerControl_MouseLeave(object sender, MouseEventArgs e)
        {
            _marker.ZIndex -= 10000;
            _popup.IsOpen = false;
        }

        private void MarkerControl_MouseEnter(object sender, MouseEventArgs e)
        {
            _marker.ZIndex += 10000;
            _popup.IsOpen = true;
        }
    }
}
