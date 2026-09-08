using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebApiProject.Models
{
    public class app_refresh_token
    {
        [Key]
        public Guid id { get; set; }
        public string user_id { get; set; } = default!;
        public string token { get; set; } = default!;
        public DateTime expired_at { get; set; }
        public DateTime created_at { get; set; }
        public bool revoked { get; set; }
        public DateTime? revoked_at { get; set; }
        public string? replaced_by_token { get; set; }
        public User? user { get; set; } = default!;
    }
}