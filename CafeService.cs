using CafeLocatorApp.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace CafeLocatorApp
{
    public class CafeService
    {
        private static readonly string baseUrl = "https://overpass-api.de/api/interpreter";
        private static readonly string nominatimBaseUrl = "https://nominatim.openstreetmap.org/reverse?format=json";
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly HttpClient _httpClient = new HttpClient();
        static CafeService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CafeLocatorApp/1.0 (kevinfred24@gmail.com)");
        }


        public static async Task<List<Cafe>> FindNearbyCafeAsync(double latitude, double longitude, int radius = 5000, bool onlyOpen = false, int page = 1, int pageSize = 10)
        {
            List<Cafe> cafes = new List<Cafe>();
            string cacheKey = $"cafes-{latitude}-{longitude}-{radius}-{onlyOpen}-{page}-{pageSize}";

            using (HttpClient client = new HttpClient())
            {
                // ✅ Check if data is cached
                if (_cache.TryGetValue(cacheKey, out List<Cafe> cachedCafes))
                {
                    return cachedCafes;
                }
                try
                {
                    string query = $@"
                    [out:json];
                    node
                      [""amenity""=""cafe""]
                      (around:{radius},{latitude},{longitude});
                    out;

                ";
                    string requestUrl = $"https://overpass-api.de/api/interpreter?data={Uri.EscapeDataString(query)}";
                    HttpResponseMessage response = await client.GetAsync(requestUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResult = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(jsonResult);

                        List<Task<Cafe>> cafeTasks = new List<Task<Cafe>>();

                        foreach (var element in data["elements"])
                        {
                            var name = element["tags"]?["name"]?.ToString();
                            var lat = element["lat"]?.ToObject<double>() ?? 0.0;
                            var lon = element["lon"]?.ToObject<double>() ?? 0.0;
                            var openingHours = element["tags"]?["opening_hours"]?.ToString();
                            bool isOpen = openingHours != null && openingHours.Contains("open"); // ✅ Basic check

                            // ✅ Filter based on 'onlyOpen' condition
                            if (!string.IsNullOrEmpty(name) && (!onlyOpen || isOpen))
                            {
                                cafeTasks.Add(GetCafeWithAddress(name, lat, lon));
                            }
                        }


                        // ✅ Await all address lookups in parallel
                        cafes = (await Task.WhenAll(cafeTasks)).ToList();

                        // ✅ Pagination
                        cafes = cafes.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                        // ✅ Store in cache for 10 minutes
                        _cache.Set(cacheKey, cafes, TimeSpan.FromMinutes(10));

                    }
                    else
                    {
                        Console.WriteLine($"Error fetching café data: HTTP {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching café data: {ex.Message}");
                }
            }

            return cafes;
        }

        private static async Task<Cafe> GetCafeWithAddress(string name, double lat, double lon)
        {
            string nominatimBaseUrl = "https://nominatim.openstreetmap.org/reverse?format=json";

            try
            {
                string nominatimUrl = $"{nominatimBaseUrl}&lat={lat}&lon={lon}";
                HttpResponseMessage addressResponse = await _httpClient.GetAsync(nominatimUrl);

                if (addressResponse.IsSuccessStatusCode)
                {
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

                    return new Cafe
                    {
                        Name = name,
                        Address = $"{street}, {city}, {postcode}, {state}, {country}",
                        Latitude = lat,
                        Longitude = lon
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching address for {name}: {ex.Message}");
            }

            return new Cafe
            {
                Name = name,
                Address = "Unknown Address",
                Latitude = lat,
                Longitude = lon
            };
        }
    }
}
