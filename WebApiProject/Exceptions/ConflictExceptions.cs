namespace WebApiProject.Exceptions
{
    public class ConflictExceptions : AppExceptions
    {
        public ConflictExceptions(string message) : base(message, 409){}
    }
}