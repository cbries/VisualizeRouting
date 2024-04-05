using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace VisualizeRoutingWpf
{
    public delegate void TracerouteStarted(object sender);
    public delegate void TracerouteHopped(object sender, int ttl, long ms, IPAddress ip);
    public delegate void TracerouteStopped(object sender);
    public delegate void TracerouteTimeout(object sender);
    public delegate void TracerouteFailed(object sender, string message);

    public class Traceroute
    {
        public event TracerouteStarted Started;
        public event TracerouteHopped Hopped;
        public event TracerouteStopped Stopped;
        public event TracerouteTimeout Timeout;
        public event TracerouteFailed Failed;

        public string Host { private set; get; }

        private IPAddress GetIpAddress(string ipOrHost)
        {
            try
            {
                var listOfDns = Dns.GetHostAddresses(ipOrHost);
                if (listOfDns.Length == 0) return IPAddress.None;
                return listOfDns[0];
            }
            catch (Exception ex)
            {
                Failed?.Invoke(this, ex.GetExceptionMessages());
            }

            return IPAddress.None;
        }

        public void ExecuteTraceroute(string ipAddressOrHostName, TimeSpan walltime)
        {
            var end = DateTime.Now + walltime;

            Started?.Invoke(this);

            Host = ipAddressOrHostName;
            var ipAddress = GetIpAddress(ipAddressOrHostName);
            if (ipAddress == IPAddress.None)
            {
                MessageBox.Show($@"Unknown host: {ipAddressOrHostName}", $@"Stopped", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Stopped?.Invoke(this);
            }
            var pingSender = new Ping();
            var pingOptions = new PingOptions();
            var stopWatch = new Stopwatch();

            var bytes = new byte[32];
            pingOptions.DontFragment = true;
            pingOptions.Ttl = 1;
            var maxHops = 30;
            
            try
            {
                for (var i = 1; i < maxHops + 1; i++)
                {
                    if (DateTime.Now > end)
                    {
                        Trace.WriteLine("TIMEOUT");
                        Timeout?.Invoke(this);
                        return;
                    }

                    stopWatch.Restart();
                    var pingReply = pingSender.Send(ipAddress, 5000, bytes, pingOptions);
                    stopWatch.Stop();

                    if (pingReply != null)
                        Hopped?.Invoke(this, i, stopWatch.ElapsedMilliseconds, pingReply.Address);
                    else
                        Hopped?.Invoke(this, i, stopWatch.ElapsedMilliseconds, IPAddress.None);

                    if (pingReply != null && pingReply.Status == IPStatus.Success)
                    {
                        Stopped?.Invoke(this);
                        return;
                    }

                    if (DateTime.Now > end)
                    {
                        Trace.WriteLine("TIMEOUT");
                        Timeout?.Invoke(this);
                        return;
                    }

                    pingOptions.Ttl++;
                }
            }
            catch
            {
                // ignore
            }

            Stopped?.Invoke(this);
        }
    }
}
