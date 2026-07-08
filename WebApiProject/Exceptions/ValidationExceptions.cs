namespace WebApiProject.Exceptions
{
    public class ValidationExceptions : AppExceptions
    {
        public ValidationExceptions(string message) : base(message, 400){}
    }
}