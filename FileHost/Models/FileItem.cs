using Newtonsoft.Json;

namespace FileHost.Models
{
    public class FileItem : Item
    {
        [JsonProperty("containing_folder_id")]
        public string ContainingFolderId { get; set; }
		[JsonProperty("size")]
		public int Size { get; set; }
		[JsonIgnore]
        public byte[] Data { get; set; }

        public override DocumentType Type => DocumentType.File;
    }
}
