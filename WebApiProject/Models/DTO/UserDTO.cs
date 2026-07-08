namespace WebApiProject.Models
{
    public class RegisterData
    {
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string name { get; set; }
        public string? phone_number { get; set; }
        public string role { get; set; }
        public string confirmPassword { get; set; }
    }

    public class LoginData
    {
        public string username {get;set;}
        public string password {get;set;}
    }

    public class UserResponseDTO
    {
        public string? id { get; set; }
        public string? username { get; set; }
        public string? name { get; set; }
        public string? slug { get; set; }
        public bool twoFactorEnabled { get; set; }
        public bool emailConfirmed { get; set; }
        public bool pohneNumberConfirmed { get; set; }
        public string? email { get; set; }
        public string? phone_number { get; set; }
        public List<string>? roles { get; set; }
    }

    public class UserProfileDTO
    {
        public string? id { get; set; }
        public string? username { get; set; }
        public string? name { get; set; }
        public string? slug { get; set; }
        public bool twoFactorEnabled { get; set; }
        public bool emailConfirmed { get; set; }
        public bool pohneNumberConfirmed { get; set; }
        public string? email { get; set; }
        public string? phone_number { get; set; }
    }
}