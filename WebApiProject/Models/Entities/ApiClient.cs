using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebApiProject.Models
{
    public class app_api_client
    {
        [Key]
        [MaxLength(50)]
        public string client_id { get; set; }
        [MaxLength(100)]
        public string? client_name { get; set; }
        [MaxLength(255)]
        public string api_key_hash { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}