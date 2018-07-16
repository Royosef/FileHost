using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FileHost.FilesManagement
{
    public class DataAccess
    {
        private static readonly  Lazy<DataAccess> LazyInstance = new Lazy<DataAccess>(() => new DataAccess());

        public static DataAccess Instance => LazyInstance.Value;

        private HttpClient Client { get; } = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5984/filehost/") };

        private DataAccess() { }

        public async Task<HttpResponseMessage> GetByView(string viewName, string key = null, bool includeDocs = false)
        {
            var urlStringBuilder = new StringBuilder($"_design/filehost/_view/{viewName}?include_docs={includeDocs}");

            if (key != null)
            {
                urlStringBuilder.Append($"&key=\"{key}\"");
            }

            return await Client.GetAsync(urlStringBuilder.ToString());
        }

        public async Task<HttpResponseMessage> Get(string url, string rev = null)
        {
            return await Client.GetAsync(GetUrl(url, rev));
        }

        public async Task<HttpResponseMessage> PostAsJson(object obj, string url, string rev = null)
        {
            return await Client.PostAsync(GetUrl(url, rev), new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json"));
        }

        public async Task<HttpResponseMessage> PutBinary(byte[] byteArray, string url, string rev = null)
        {
            return await Client.PutAsync(GetUrl(url, rev), new ByteArrayContent(byteArray));
        }

        public async Task<HttpResponseMessage> Delete(string url, string rev = null)
        {
            return await Client.DeleteAsync(GetUrl(url, rev));
        }

        private string GetUrl(string url, string rev = null)
        {
            var urlStringBuilder = new StringBuilder(url);

            if (rev != null)
            {
                urlStringBuilder.Append($"?rev={rev}");
            }

            return urlStringBuilder.ToString();
        }
    }
}
