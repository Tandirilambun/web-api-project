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
    }

    public class LoginData
    {
        public string username {get;set;}
        public string password {get;set;}
    }
}