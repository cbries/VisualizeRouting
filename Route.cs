using GMap.NET;
using GMap.NET.WindowsPresentation;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;

namespace VisualizeRoutingWpf
{
    public class Route : GMapRoute
    {
        private readonly System.Windows.Media.Brush _brush;
        private readonly string _tooltip;

        public Route(System.Windows.Media.Brush color,
            IEnumerable<PointLatLng> points,
            string tooltip) : base(points)
        {
            _brush = color;
            _tooltip = tooltip;
        }

        public override Path CreatePath(List<Point> localPath, bool addBlurEffect)
        {
            var basePath = base.CreatePath(localPath, false);
            basePath.StrokeThickness = 4;
            basePath.Stroke = _brush;
            if(!string.IsNullOrEmpty(_tooltip))
                basePath.ToolTip = _tooltip;
            return basePath;
        }
    }
}
