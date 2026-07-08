namespace WebApiProject.Exceptions
{
    public class NotFoundExceptions : AppExceptions
    {
        public NotFoundExceptions(string message) : base(message, 404) { }
    }
}