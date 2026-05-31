using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SocietyManagementSystem.ViewModels
{
    public class ComplaintViewModel
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public string Description { get; set; }

        public IFormFile AttachmentFile { get; set; }
    }
}