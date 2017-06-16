using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CondenserDotNet.Core
{
    public class ConsulApiClient:IDisposable
    {
        private HttpClient _client;
        private string _aclKey;

        public ConsulApiClient(HttpClient client, string aclKey = null)
        {
            _client = client;
            _aclKey = aclKey;
        }

        public Task<HttpResponseMessage> PutAsync<T>(string url, T body, CancellationToken token = default(CancellationToken))
        {
            var uri = new Uri(url);
            var content = GetStringContent(body);
            if (!string.IsNullOrWhiteSpace(_aclKey))
            {
                content.Headers.Add("X-Consul-Token", _aclKey);
            }
            return _client.PutAsync(uri, content, token);
        }

        public Task<HttpResponseMessage> PutAsync(string url, CancellationToken token = default(CancellationToken))
        {
            var uri = new Uri(url);
            var content = new StringContent("");
            if (!string.IsNullOrWhiteSpace(_aclKey))
            {
                content.Headers.Add("X-Consul-Token", _aclKey);
            }
            return _client.PutAsync(uri, null, token);
        }

        public Task<HttpResponseMessage> GetAsync(string url, CancellationToken token = default(CancellationToken))
        {
            if(!string.IsNullOrWhiteSpace(_aclKey))
            {
                return _client.GetAsync(url,token);
            }
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Consul-Token", _aclKey);
            return _client.SendAsync(request, token);
        }

        public async Task<T> GetAsync<T>(string url, CancellationToken token = default(CancellationToken))
        {
            var result = await GetAsync(url, token);
            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        private StringContent GetStringContent<T>(T objectForContent) => new StringContent(JsonConvert.SerializeObject(objectForContent, JsonSettings), Encoding.UTF8, "application/json");

        public void Dispose() => _client.Dispose();
    }
}
