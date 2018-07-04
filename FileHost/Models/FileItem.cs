using Newtonsoft.Json;

namespace FileHost.Models
{
    public class FileItem : Item
    {
        [JsonProperty("containingFolderId")]
        public string ContainingFolderId { get; set; }
        [JsonIgnore]
        public byte[] Data { get; set; }
    }
}
