namespace WebApiProject.Exceptions
{
    public class AppExceptions : Exception
    {
        public int StatusCode { get;}
        public AppExceptions(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}