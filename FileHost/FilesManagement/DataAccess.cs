﻿using System;
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
            var url = new StringBuilder($"_design/filehost/_view/{viewName}?include_docs={includeDocs}");

            if (key != null)
            {
                url.Append($"&key=\"{key}\"");
            }

            return await Client.GetAsync(url.ToString());
        }

        public async Task<HttpResponseMessage> Get(string url)
        {
            return await Client.GetAsync(url);
        }

        public async Task<HttpResponseMessage> PostAsJson(string url, object obj)
        {
            return await Client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json"));
        }

        public async Task<HttpResponseMessage> PutBinary(string url, byte[] byteArray)
        {
            return await Client.PutAsync(url, new ByteArrayContent(byteArray));
        }

        public async Task<HttpResponseMessage> Delete(string url)
        {
            return await Client.DeleteAsync(url);
        }
    }
}
