namespace WebApiProject.Models
{
    public class GlobalJsonResponse<T>
    {
        public string message {get;set;}
        public T? data {get;set;}

        public GlobalJsonResponse(string message, T? data)
        {
            this.message = message;
            this.data = data;
        }
    }
}