using Newtonsoft.Json;

namespace FileHost.Models
{
    public abstract class Item
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("_id")]
        public string Id { get; set; }
        [JsonProperty("_rev")]
        public string Revision { get; set; }

        public bool ShouldSerializeId() => false;
        public bool ShouldSerializeRevision() => false;
    }
}
