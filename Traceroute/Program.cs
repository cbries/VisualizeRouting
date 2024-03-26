using System;
using System.Net;

namespace Traceroute
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var instance = new VisualizeRoutingWpf.Traceroute();
            instance.Hopped += InstanceOnHopped;
            instance.Stopped += InstanceOnStopped;
            instance.ExecuteTraceroute("35.128.0.34", TimeSpan.FromSeconds(10));
            Console.ReadKey();
        }

        private static void InstanceOnStopped(object sender)
        {
            Console.WriteLine("Trace complete.");
        }

        private static void InstanceOnHopped(object sender, int ttl, long ms, IPAddress ip)
        {
            Console.WriteLine("{0}\t{1} ms\t{2}", ttl, ms, ip.Address);
        }
    }
}
