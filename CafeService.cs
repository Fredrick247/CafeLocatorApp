using CafeLocatorApp.Models;
using Newtonsoft.Json.Linq;

namespace CafeLocatorApp
{
    public class CafeService
    {
        private static readonly string baseUrl = "https://overpass-api.de/api/interpreter";
        private static readonly string nominatimBaseUrl = "https://nominatim.openstreetmap.org/reverse?format=json";


        public static async Task <List<Cafe>> FindNearbyCafeAsync(double latitude,double longitude, int radius = 5000, bool onlyOpen = false)
        {
            List<Cafe> cafes = new List<Cafe>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "CafeLocatorApp/1.0 (kevinfred24@gmail.com)");

                string query = $@"
                    [out:json];
                    node
                      [""amenity""=""cafe""]
                      (around:{radius},{latitude},{longitude});
                    out;

                ";

                string requestUrl = $"{baseUrl}?data={Uri.EscapeDataString(query)}";

                HttpResponseMessage response = await client.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResult = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(jsonResult);

                    foreach(var element in data["elements"])
                    {
                        var name = element["tags"]?["name"]?.ToString();
                        var lat = element["lat"]?.ToObject<double>() ?? 0.0;
                        var lon = element["lon"]?.ToObject<double>() ?? 0.0;
                        var openingHours = element["tags"]?["opening_hours"]?.ToString();
                        bool isOpen = openingHours != null && openingHours.Contains("open");



                        if (!string.IsNullOrEmpty(name) && (!onlyOpen || isOpen))
                        {
                            // Fetch accurate address details using Nominatim API
                            string nominatimUrl = $"{nominatimBaseUrl}&lat={lat}&lon={lon}";
                            HttpResponseMessage addressResponse = await client.GetAsync(nominatimUrl);

                            string addressJson = await addressResponse.Content.ReadAsStringAsync();
                            JObject addressData = JObject.Parse(addressJson);

                            var street = addressData["address"]?["road"]?.ToString() ?? "Unknown Street";
                            var city = addressData["address"]?["city"]?.ToString() ??
                                       addressData["address"]?["town"]?.ToString() ??
                                       addressData["address"]?["village"]?.ToString() ??
                                       "Unknown City";
                            var postcode = addressData["address"]?["postcode"]?.ToString() ?? "No Postcode";
                            var state = addressData["address"]?["state"]?.ToString() ?? "Unknown State";
                            var country = addressData["address"]?["country"]?.ToString() ?? "Unknown Country";



                            cafes.Add(new Cafe
                            {
                                Name = name,
                                Address = $"{street}, {city}, {postcode}",
                                Latitude = lat,
                                Longitude = lon
                            });
                        }
                    }
                }

            }
            return cafes;
        }
    }
}
