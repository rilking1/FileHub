using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FileHub.Models
{
    public class FileRecord
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        public string OriginalName { get; set; }
        public string Path { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        public string UploadedBy { get; set; }
        public string EditedBy { get; set; }
    }
}
