namespace FileHost.Models
{
    public abstract class Item
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Revision { get; set; }
    }
}
