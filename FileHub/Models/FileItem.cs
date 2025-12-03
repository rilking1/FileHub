
namespace FileHub.Models
{
    public class FileItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public long Size { get; set; }
        public string UploadedBy { get; set; }
        public string EditedBy { get; set; }
        public string Extension => System.IO.Path.GetExtension(Name).ToLowerInvariant();
    }
}
