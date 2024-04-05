﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using GMap.NET;
using GMap.NET.MapProviders;
using System.Text;
using GMap.NET.WindowsPresentation;
using Newtonsoft.Json;
using GMapMarker = GMap.NET.WindowsPresentation.GMapMarker;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace VisualizeRoutingWpf
{
    public class GpsInfo : Dictionary<string, List<IpInfo>> { }

    public class ListIpGps : List<IpInfo>
    {
        public bool Has(string ip)
        {
            return Get(ip) != null;
        }

        public IpInfo Get(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return null;
            foreach (var it in this)
                if (it.Ip.Equals(ip, StringComparison.OrdinalIgnoreCase))
                    return it;
            return null;
        }
    }

    public class Map : GMapControl { }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        protected void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            field = value;
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        private string _statusMessage;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        // Fallback for local IP addresses is Bielefeld.
        private const double StartLatitude = 52.016;
        private const double StartLongitude = 8.51;

        // https://ipinfo.io/account/home
        private string Token
        {
            get
            {
                try
                {
                    return File.ReadAllText("ipinfo.token", Encoding.UTF8).Trim();
                }
                catch
                {
                    // ignore
                }

                return string.Empty;
            }
        }
        private const string BaseUrl = "https://ipinfo.io/";

        private string QueryUrl => BaseUrl + "{0}/?token=" + Token;

        public const int ZoomLevel = 5;

        public Traceroute TracerouteInstance = new Traceroute();

        public ListIpGps Data { get; set; } = new ListIpGps();

        private void LoadGpsInfoCacheData()
        {
            try
            {
                if (!File.Exists(IpInfo.CachePath)) return;
                var cnt = File.ReadAllText(IpInfo.CachePath);
                Data = JsonConvert.DeserializeObject<ListIpGps>(cnt);
            }
            catch
            {
                // ignore
            }
        }

        private void SaveGpsInfoCacheData()
        {
            try
            {
                var json = JsonConvert.SerializeObject(Data, Formatting.Indented);
                File.WriteAllText(IpInfo.CachePath, json, Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
        }

        #region Testdata

        private const string InputBasePath = @"Testdata";

        private Dictionary<string, List<string>> GetInputFromTraceFiles()
        {
            var res = new Dictionary<string, List<string>>();
            var files = Directory.GetFiles(InputBasePath, "*.txt", SearchOption.TopDirectoryOnly);
            foreach (var it in files)
            {
                var name = Path.GetFileNameWithoutExtension(it);
                var cnt = File.ReadAllLines(it, Encoding.UTF8);
                var ips = GetIpList(cnt.ToList());
                res.Add(name, ips);
            }

            return res;
        }

        #endregion Testdata

        public string GetHighlightText(IpInfo info)
        {
            var m = $"IP: {info.Ip}" + Environment.NewLine;
            if (!string.IsNullOrEmpty(info.Hostname))
                m += $"Hostname: {info.Hostname}" + Environment.NewLine;
            if (!string.IsNullOrEmpty(info.City))
                m += $"City: {info.City}" + Environment.NewLine;
            if (!string.IsNullOrEmpty(info.Region))
                m += $"Region: {info.Region}" + Environment.NewLine;
            if (!string.IsNullOrEmpty(info.Country))
                m += $"Country: {info.Country}" + Environment.NewLine;
            m += $"Location: {info.Latitude}, {info.Longitude}";
            return m;
        }

        /*
         Sample: https://ipinfo.io/23.192.59.241?token={my token}
         {
              "ip": "23.192.59.241",
              "hostname": "a23-192-59-241.deploy.static.akamaitechnologies.com",
              "city": "Secaucus",
              "region": "New Jersey",
              "country": "US",
              "loc": "40.7895,-74.0565",
              "org": "AS16625 Akamai Technologies, Inc.",
              "postal": "07094",
              "timezone": "America/New_York"
          }
         */
        private async Task<GpsInfo> GetCoords(Dictionary<string, List<string>> inputs)
        {
            var res = new GpsInfo();

            foreach (var it in inputs)
            {
                var name = it.Key;
                var ips = it.Value;

                var listOfGpsInfo = new List<IpInfo>();

                foreach (var ip in ips)
                {
                    var cacheGps = Data.Get(ip);
                    if (cacheGps != null)
                    {
                        listOfGpsInfo.Add(cacheGps);

                        continue;
                    }

                    if (IsPrivateIpAddress(ip))
                    {
                        var item = new IpInfo()
                        {
                            Ip = ip,
                            Location = $"{StartLatitude},{StartLongitude}"
                        };

                        Data.Add(item);
                    }
                    else
                    {
                        var url = string.Format(QueryUrl, ip);
                        var contentJson = await LoadWebPageAsync(url);
                        var ipInfo = JsonConvert.DeserializeObject<IpInfo>(contentJson);

                        Data.Add(ipInfo);
                        listOfGpsInfo.Add(ipInfo);
                    }
                }

                res.Add(name, listOfGpsInfo);
            }

            SaveGpsInfoCacheData();

            return res;
        }

        private static List<string> GetIpList(List<string> lines)
        {
            var res = new List<string>();

            foreach (var it in lines)
            {
                if (string.IsNullOrEmpty(it)) continue;
                if (it.IndexOf("ms", StringComparison.OrdinalIgnoreCase) == -1) continue;

                // check format
                var hasA = it.IndexOf("(", StringComparison.OrdinalIgnoreCase) != -1;
                var hasB = it.IndexOf(")", StringComparison.OrdinalIgnoreCase) != -1;
                if (hasA && hasB)
                {
                    var p0 = it.IndexOf("(", StringComparison.OrdinalIgnoreCase);
                    var p1 = it.IndexOf(")", StringComparison.OrdinalIgnoreCase);
                    var len = p1 - p0;
                    var ip = it.Substring(p0, len).Trim().Trim('(').Trim(')');
                    res.Add(ip);
                }
                else
                {
                    var lastIdx = it.LastIndexOf(' ');
                    if (lastIdx == -1) continue;
                    var ip = it.Substring(lastIdx).Trim().Trim('[').Trim(']').Trim();
                    res.Add(ip);
                }
            }

            return res;
        }

        public static bool IsPrivateIpAddress(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var ip)) return false;
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
            var bytes = ip.GetAddressBytes();
            var is10Private = bytes[0] == 10;
            var is172Private = bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31;
            var is192Private = bytes[0] == 192 && bytes[1] == 168;
            return is10Private || is172Private || is192Private;
        }

        public static async Task<string> LoadWebPageAsync(string url)
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            StatusMessage = "Loaded";
        }

        private static List<System.Windows.Media.Brush> Brushes = new List<System.Windows.Media.Brush>()
        {
            System.Windows.Media.Brushes.Navy,
            System.Windows.Media.Brushes.Red,
            System.Windows.Media.Brushes.DimGray,
            System.Windows.Media.Brushes.BlueViolet,
            System.Windows.Media.Brushes.Yellow,
            System.Windows.Media.Brushes.DarkMagenta,
            System.Windows.Media.Brushes.MediumAquamarine,
            System.Windows.Media.Brushes.LightSkyBlue,
            System.Windows.Media.Brushes.YellowGreen
        };

        private void PlotCoords(Dictionary<string, List<IpInfo>> fileCoords)
        {
            var idxColor = 0;

            foreach (var it in fileCoords)
            {
                var name = it.Key;
                var lineCoords = it.Value;

                // route/marker color
                var color = Brushes[idxColor];

                //
                // create list of points / marker
                //
                var points = new List<PointLatLng>();

                foreach (var p0 in lineCoords)
                {
                    var lat = p0.Latitude;
                    var lng = p0.Longitude;
                    var pLatLng = new PointLatLng(lat, lng);

                    points.Add(pLatLng);

                    var m = new GMapMarker(pLatLng);
                    m.Shape = new Marker(this, m, GetHighlightText(p0));
                    MapControl.Markers.Add(m);
                }

                //
                // create route
                //
                var route = new Route(color, points, lineCoords.Last().Hostname);
                MapControl.Markers.Add(route);

                ++idxColor;
            }
        }

        public async void RunTestmode()
        {
            var inputs = GetInputFromTraceFiles();
            var coords = await GetCoords(inputs);

            PlotCoords(coords);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_traceRunning)
            {
                System.Windows.MessageBox.Show("Trace is running!", "Traceroute", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var host = TxtHost.Text?.Trim();

            if (string.IsNullOrEmpty(host))
            {
                System.Windows.MessageBox.Show("No host entered.", "Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var idx = host.LastIndexOf('.');
            if (idx == -1)
                host += ".de";

            StatusMessage = $"Trace: {host}";

            _traceTask = System.Threading.Tasks.Task.Run(() =>
            {
                TracerouteInstance.ExecuteTraceroute(host, TimeSpan.FromSeconds(10));
            });
        }

        private System.Threading.Tasks.Task _traceTask;

        private void InitMap()
        {
            var gmap = MapControl;
            gmap.MapProvider = GMapProviders.GoogleMap;
            gmap.Position = new PointLatLng(StartLatitude, StartLongitude);
            gmap.CanDragMap = true;
            gmap.MinZoom = 1;
            gmap.MaxZoom = 18;
            gmap.Zoom = ZoomLevel;
            gmap.MouseWheelZoomEnabled = true;
            gmap.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionWithoutCenter;
            gmap.IgnoreMarkerOnMouseWheel = true;
        }

        private SynchronizationContext _uiCtx;
        private bool _initialized;

        private void MainWindow_OnInitialized(object sender, EventArgs e)
        {
            _uiCtx = SynchronizationContext.Current;

            InitMap();

            LoadGpsInfoCacheData();

            TracerouteInstance.Started += TracerouteInstanceOnStarted;
            TracerouteInstance.Hopped += TracerouteInstanceOnHopped;
            TracerouteInstance.Stopped += TracerouteInstanceOnStopped;
            TracerouteInstance.Timeout += TracerouteInstanceOnTimeout;
            TracerouteInstance.Failed += TracerouteInstanceOnFailed;

            TxtHost.KeyUp += TxtHostOnKeyUp;

            RunTestmode();

            _initialized = true;
        }

        private void TxtHostOnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Button_Click(null, new RoutedEventArgs());
        }

        private void TracerouteInstanceOnFailed(object sender, string message)
        {
            _traceRunning = false;
            IsLoading = false;

            Trace.WriteLine($"{message}");
            System.Windows.MessageBox.Show(message, "Trace Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Failed";
        }

        private void TracerouteInstanceOnTimeout(object sender)
        {
            _traceRunning = false;
            IsLoading = false;

            Trace.WriteLine("###");

            StatusMessage = "Timeout";
        }

        private bool _traceRunning = false;
        private int _colorIndex = -1;
        private Route _currentTraceRoute;

        private void TracerouteInstanceOnStarted(object sender)
        {
            _uiCtx.Post(o =>
            {
                _wBeforeRun = Width;
                _hBeforeRun = Height;
            }, null);

            _traceRunning = true;
            IsLoading = true;

            ++_colorIndex;
            if (_colorIndex >= Brushes.Count)
                _colorIndex = 0;

            _currentTraceRoute = null;

            Trace.WriteLine("+++");

            StatusMessage = "Started";
        }

        private void TracerouteInstanceOnStopped(object sender)
        {
            _traceRunning = false;
            IsLoading = false;

            Trace.WriteLine("---");

            StatusMessage = "Stopped";
        }

        private double _wBeforeRun;
        private double _hBeforeRun;

        private async void TracerouteInstanceOnHopped(object sender, int ttl, long ms, IPAddress ip)
        {
            if (ip == null) return;
            var ips = ip.ToString();

            var instance = sender as Traceroute;
            if (instance == null) return;

            var ipCoord = await GetCoords(new Dictionary<string, List<string>>()
            {
                { instance.Host, new List<string> { ips } }
            });

            var values = ipCoord[instance.Host];
            if (values.Count == 0) return;

            var lat = values[0].Latitude;
            var lng = values[0].Longitude;

            Trace.WriteLine($"{ttl}\t{ms} ms\t{ips}   Coord: {lat}, {lng}");

            _uiCtx.Post(o =>
            {
                var pLatLng = new PointLatLng(lat, lng);
                var m = new GMapMarker(pLatLng);
                m.Shape = new Marker(this, m, GetHighlightText(values[0]));
                MapControl.Markers.Add(m);

                if (_currentTraceRoute == null)
                {
                    var color = Brushes[_colorIndex];
                    _currentTraceRoute = new Route(color, new List<PointLatLng>() { pLatLng }, GetHighlightText(values[0]));
                    MapControl.Markers.Add(_currentTraceRoute);
                }
                else
                {
                    _currentTraceRoute.Points.Add(pLatLng);
                }

                MapControl.InvalidateVisual(true);

                var w = (int)_wBeforeRun;
                var h = (int)_hBeforeRun;

                Trace.WriteLine($"Geometry: {w} x {h}");

                MapControl.InitializeForBackgroundRendering(w, h);

            }, null);
        }

        private void CmdClear_OnClick(object sender, RoutedEventArgs e)
        {
            MapControl.Markers.Clear();
        }

        private void CmdForceStop_OnClick(object sender, RoutedEventArgs e)
        {
            _traceRunning = false;

            if (_traceTask != null)
            {
                try
                {
                    _traceTask.Dispose();
                }
                catch
                {
                    // ignore
                }

                _traceTask = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_initialized)
            {
                MapControl.InvalidateVisual(true);
                MapControl.Width = RenderSize.Width;
                MapControl.Height = RenderSize.Height;
            }
        }
    }
}
