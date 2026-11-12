namespace WebApiProject.Exceptions
{
    public class AppExceptions : Exception
    {
        public int StatusCode { get;}
        public List<string> Errors { get; }
        public AppExceptions(string message, int statusCode, List<string>? errors = null) : base(message)
        {
            StatusCode = statusCode;
            Errors = errors ?? new List<string>();
        }
    }
}