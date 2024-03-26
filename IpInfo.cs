using Newtonsoft.Json;

namespace VisualizeRoutingWpf
{
    public static class CoordExtension
    {
        public static double AsDouble(this string value)
        {
            if (string.IsNullOrEmpty(value)) return 0.0;
            return double.TryParse(value, out var v) ? v : 0.0;
        }
    }

    public class IpInfo
    {
        public const string CachePath = @"ipGps.json";

        [JsonProperty("ip")] public string Ip { get; set; }
        [JsonProperty("hostname")] public string Hostname { get; set; }
        [JsonProperty("city")] public string City { get; set; }
        [JsonProperty("region")] public string Region { get; set; }
        [JsonProperty("country")] public string Country { get; set; }
        [JsonProperty("loc")] public string Location { get; set; }
        [JsonProperty("org")] public string Organisation { get; set; }
        [JsonProperty("postal")] public string Postal { get; set; }
        [JsonProperty("timezone")] public string Timezone { get; set; }

        [JsonIgnore]
        public double Latitude
        {
            get
            {
                var loc = Location.Split(',');
                return loc[0].AsDouble();
            }
        }

        [JsonIgnore]
        public double Longitude
        {
            get
            {
                var loc = Location.Split(',');
                return loc[1].AsDouble();
            }
        }
    }
}
