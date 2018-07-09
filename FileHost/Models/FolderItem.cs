using Newtonsoft.Json;

namespace FileHost.Models
{
    public class FolderItem : Item
    {
        [JsonIgnore]
        public int ItemsAmount { get; set; }

        public override DocumentType Type => DocumentType.Folder;
    }
}
