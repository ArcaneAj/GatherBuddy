using GatherBuddy.Plugin;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using static GatherBuddy.FishTimer.FishRecordTimes;

namespace GatherBuddy.FishTimer.Http
{
    public class HttpService
    {
        private readonly HttpClient _client = new HttpClient(new HttpClientHandler { Proxy = null, UseProxy = false});

        public HttpService() {
            _client.DefaultRequestHeaders.Add("x-functions-key", GatherBuddy.Config.CloudKey);
        }

        public Dictionary<uint, Dictionary<uint, Times>>? GetFishData(string spotId) {
            var url = string.Join("/", GatherBuddy.Config.CloudBaseUrl, "api/SyncRead", spotId);
            return Get<Dictionary<uint, Dictionary<uint, Times>>>(url);
        }

        private T? Get<T>(string url)
        {
            Communicator.Print(url);

            var webRequest = new HttpRequestMessage(HttpMethod.Get, url);
            var response = _client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());
            return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }
    }
}
