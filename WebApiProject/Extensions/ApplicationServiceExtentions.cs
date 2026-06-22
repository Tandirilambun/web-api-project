using WebApiProject.Interfaces;
using WebApiProject.Services;

namespace WebApiProject.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register your application services here
            services.AddScoped<IUserServices, UserServices>();
            services.AddScoped<IRoleServices, RoleServices>();
            services.AddScoped<ITokenServices, TokenServices>();
            services.AddScoped<IUtilServices, UtilServices>();
            
            return services;
        }
    }
}