using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CondenserDotNet.Core
{
    public class ConsulApiClient : IDisposable
    {
        private HttpClient _client;
        private AgentConfig _config;

        private static readonly JsonSerializerSettings s_jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public ConsulApiClient(AgentConfig config)
        {
            _config = config ?? new AgentConfig();
#if NET452
            System.Net.ServicePointManager.DefaultConnectionLimit = 50;
            _client = new HttpClient()
#else
            _client = new HttpClient(new HttpClientHandler() { MaxConnectionsPerServer = 50 })

#endif
            {
                BaseAddress = new UriBuilder(_config.Scheme, _config.Address, _config.Port).Uri,
                Timeout = _config.Timeout,
            };
        }

        public Task<HttpResponseMessage> PutAsync<T>(string url, T body, CancellationToken token = default(CancellationToken))
        {
            var content = GetStringContent(body);
            AddAccessToken(content.Headers);
            return _client.PutAsync(url, content, token);
        }

        public Task<HttpResponseMessage> PutAsync(string url, CancellationToken token = default(CancellationToken))
        {
            var content = new StringContent("");
            AddAccessToken(content.Headers);
            return _client.PutAsync(url, null, token);
        }

        public Task<HttpResponseMessage> GetAsync(string url, CancellationToken token = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddAccessToken(request.Headers);
            return _client.SendAsync(request, token);
        }

        public async Task<T> GetAsync<T>(string url, CancellationToken token = default(CancellationToken))
        {
            var result = await GetAsync(url, token);
            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        private void AddAccessToken(HttpHeaders headers)
        {
            if (!string.IsNullOrWhiteSpace(_config.AccessControlToken))
            {
                headers.Add("X-Consul-Token", _config.AccessControlToken);
            }
        }

        private StringContent GetStringContent<T>(T objectForContent) => new StringContent(JsonConvert.SerializeObject(objectForContent, s_jsonSettings), Encoding.UTF8, "application/json");

        public void Dispose() => _client.Dispose();
    }
}
