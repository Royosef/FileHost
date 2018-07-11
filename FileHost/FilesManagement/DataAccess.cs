using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FileHost.FilesManagement
{
    public class DataAccess
    {
        private HttpClient Client { get; } = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5984/filehost/") };

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
