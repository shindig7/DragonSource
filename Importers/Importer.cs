using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Importers
{
    abstract class Importer : IDisposable
    {
        HttpClient client;

        public Importer()
        {
            client = new HttpClient();
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public async Task<HttpResponseMessage> LaunchRequest(string url) => await LaunchRequest(url, HttpMethod.Get);
        public async Task<HttpResponseMessage> LaunchRequest(string url, HttpMethod method) => await LaunchRequest(url, method, new List<Tuple<string, string>>());
        public async Task<HttpResponseMessage> LaunchRequest(string url, HttpMethod method, List<Tuple<string, string>>ExtraHeaders)
        {
            var WebRequest = new HttpRequestMessage(method, url);
            foreach(var header in ExtraHeaders)
            {
                WebRequest.Headers.Add(header.Item1, header.Item2);
            }

            var response = await client.SendAsync(WebRequest);
            return response;
        }

        public abstract Task<IEnumerable<string>> GetArticleLinks();
        public abstract Task<string> GetArticleData(string url);
    }
}
