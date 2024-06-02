using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using static GatherBuddy.FishTimer.FishRecordTimes;

namespace GatherBuddy.FishTimer.Http
{
    public class HttpService
    {
        private readonly HttpClient _client = new HttpClient(new HttpClientHandler { Proxy = null, UseProxy = false});

        public HttpService()
        {
            var playerName = Dalamud.ClientState.LocalPlayer?.Name ?? Guid.NewGuid().ToString();
            var identifier = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(playerName.ToString())));
            _client.DefaultRequestHeaders.Add("Identifier", identifier);
        }

        public Dictionary<uint, Dictionary<uint, Times>>? GetFishData(string spotId) {
            var url = string.Join("/", GatherBuddy.Config.CloudBaseUrl, "api/SyncRead", spotId);
            return Get<Dictionary<uint, Dictionary<uint, Times>>>(url);
        }

        public List<int>? UploadFishData(IEnumerable<FishRecord.JsonStruct> body)
        {
            var url = string.Join("/", GatherBuddy.Config.CloudBaseUrl, "api/SyncWrite");
            return Post<List<int>>(url, body);
        }

        private T? Get<T>(string url)
        {
            var webRequest = new HttpRequestMessage(HttpMethod.Get, url);
            var response = _client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());
            return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }

        private T? Post<T>(string url, object body)
        {
            var webRequest = new HttpRequestMessage(HttpMethod.Post, url);
            webRequest.Content = new StringContent(JsonConvert.SerializeObject(body));
            var response = _client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());
            return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }
    }
}
