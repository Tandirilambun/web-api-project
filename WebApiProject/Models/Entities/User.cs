using Microsoft.AspNetCore.Identity;

namespace WebApiProject.Models
{
    public class User : IdentityUser
    {
        public string name { get; set; }
        public string slug { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}