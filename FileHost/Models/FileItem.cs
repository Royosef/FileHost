namespace FileHost.Models
{
    public class FileItem
    {
        public string Extension { get; set; }
        public int Size { get; set; }
        public string ContainingFolder { get; set; }
    }
}
