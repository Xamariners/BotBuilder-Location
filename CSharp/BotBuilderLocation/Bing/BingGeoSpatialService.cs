﻿namespace Microsoft.Bot.Builder.Location.Bing
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Configuration;
    using Newtonsoft.Json;

    [Serializable]
    internal sealed class BingGeoSpatialService : IGeoSpatialService
    {
        private readonly static string FindByQueryApiUrl = $"https://dev.virtualearth.net/REST/v1/Locations?q=";
        private readonly static string FindByPointUrl = $"https://dev.virtualearth.net/REST/v1/Locations/{{0}},{{1}}?q=";
        private readonly static string ImageUrlByPoint = $"https://dev.virtualearth.net/REST/V1/Imagery/Map/Road/{{0}},{{1}}/15?mapSize=500,500&pp={{0}},{{1}};1;{{2}}&dpi=1&logo=always";
        private readonly static string ImageUrlByBBox = $"https://dev.virtualearth.net/REST/V1/Imagery/Map/Road?mapArea={{0}},{{1}},{{2}},{{3}}&mapSize=500,500&pp={{4}},{{5}};1;{{6}}&dpi=1&logo=always";

        public async Task<LocationSet> GetLocationsByQueryAsync(string apiKey, string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(nameof(address));
            }

            return await this.GetLocationsAsync(apiKey, FindByQueryApiUrl + Uri.EscapeDataString(address) + "&key=" + apiKey);
        }

        public async Task<LocationSet> GetLocationsByPointAsync(string apiKey, double latitude, double longitude)
        {
            return await this.GetLocationsAsync(
                apiKey,
                string.Format(CultureInfo.InvariantCulture, FindByPointUrl, latitude, longitude) + "&key=" + apiKey);
        }

        public string GetLocationMapImageUrl(string apiKey, Location location, int? index = null)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var point = location.Point;
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ConfigurationErrorsException("BingMapsApiKey is missing in Web.config");
            }

            if (location.BoundaryBox != null && location.BoundaryBox.Count >= 4)
            {
                return string.Format(
                    ImageUrlByBBox,
                    location.BoundaryBox[0],
                    location.BoundaryBox[1],
                    location.BoundaryBox[2],
                    location.BoundaryBox[3],
                    point.Coordinates[0],
                    point.Coordinates[1], index)
                    + "&key=" + apiKey;
            }
            else
            {
                return string.Format(ImageUrlByPoint, point.Coordinates[0], point.Coordinates[1], index) + "&key=" + apiKey;
            }
        }

        private async Task<LocationSet> GetLocationsAsync(string apiKey, string url)
        {

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ConfigurationErrorsException("BingMapsApiKey is missing in Web.config");
            }

            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                var apiResponse = JsonConvert.DeserializeObject<LocationApiResponse>(response);

                // TODO: what is the right logic for picking a location set?
                return apiResponse.LocationSets?.FirstOrDefault();
            }
        }
    }
}