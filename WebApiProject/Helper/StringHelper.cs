using System.Text.RegularExpressions;

namespace WebApiProject.Helper
{
    public static class StringHelper
    {
        public static string ConvertToSlug(string data)
        {
            if (string.IsNullOrEmpty(data))
                return "";

            string slug = data.ToLower().Trim();

            // Hapus karakter selain huruf, angka, spasi, dan tanda hubung
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // Ganti multiple space/hyphen jadi satu hyphen
            slug = Regex.Replace(slug, @"[\s-]+", "-");

            // Trim hyphen di awal/akhir
            return slug.Trim('-');
        }

        public static bool isValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Gunakan regex untuk memvalidasi format email
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}