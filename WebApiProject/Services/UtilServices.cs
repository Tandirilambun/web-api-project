using WebApiProject.Interfaces;

namespace WebApiProject.Services
{
    public class UtilServices : IUtilServices
    {
        public UtilServices()
        {
        }

        public string ConvertToSlug(string data)
        {
            string toLower = data.ToLower();
            
            return "";
        }
    }
}