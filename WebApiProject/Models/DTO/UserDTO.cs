namespace WebApiProject.Models
{
    public class RegisterData
    {
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string name { get; set; }
        public string role { get; set; }
        public bool termsnconditon { get; set; }
    }

    public class LoginData
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class UserResponseDTO
    {
        public string? id { get; set; }
        public string? username { get; set; }
        public string? name { get; set; }
        public string? slug { get; set; }
        public bool twoFactorEnabled { get; set; }
        public bool emailConfirmed { get; set; }
        public bool phoneNumberConfirmed { get; set; }
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
        public bool phoneNumberConfirmed { get; set; }
        public string? email { get; set; }
        public string? phone_number { get; set; }
    }

    public class LoginResponseDTO
    {
        public class user_data
        {
            public string id { get; set; }
            public string username { get; set; }
            public string email { get; set; }
            public string name { get; set; }
            public string slug { get; set; }
            public string? phone_number { get; set; }
            public List<string> roles { get; set; }
        }
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }

        public user_data user { get; set; }
    }

    public class LoginResponse
    {
        public LoginResponseDTO response { get; set; }
        public string refresh_token { get; set; }
    }


}