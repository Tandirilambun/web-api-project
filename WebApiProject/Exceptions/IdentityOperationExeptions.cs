using Microsoft.AspNetCore.Http.HttpResults;

namespace WebApiProject.Exceptions
{
    public class IdentityOperationExceptions : Exception
    {
        public IdentityOperationExceptions(IEnumerable<string> errors) : base (string.Join(", ", errors)){}
    }
}