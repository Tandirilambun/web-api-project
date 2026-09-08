using WebApiProject.Interfaces;
using WebApiProject.Repositories;

namespace WebApiProject.Extensions
{
    public static class ApplicationRepositoryExtensions
    {
        public static IServiceCollection AddApplicationRepositories(this IServiceCollection services)
        {
            services.AddScoped<ITokenRepository, TokenRepositories>();

            return services;
        }
    }
}