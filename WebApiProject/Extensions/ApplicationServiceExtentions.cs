using Microsoft.Extensions.Options;
using WebApiProject.Interfaces;
using WebApiProject.Models;
using WebApiProject.Services;
using WebApiProject.Services.Marketplace;

namespace WebApiProject.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MarketplaceSettings>(
                configuration.GetSection("MarketplaceSettings"));

            // Register your application services here
            services.AddScoped<IUserServices, UserServices>();
            services.AddScoped<IRoleServices, RoleServices>();
            services.AddScoped<ITokenServices, TokenServices>();

            // Marketplace product search + caching
            services.AddScoped<IMarketplaceSearchServices, MarketplaceSearchServices>();

            // Tambahkan provider marketplace baru di sini, misalnya:
            // services.AddHttpClient<IMarketplaceScraper, ShopeeScraper>(...);
            services.AddHttpClient<IMarketplaceScraper, TokopediaScraper>((serviceProvider, client) =>
            {
                var settings = serviceProvider
                    .GetRequiredService<IOptions<MarketplaceSettings>>()
                    .Value;

                client.BaseAddress = new Uri("https://gql.tokopedia.com/");
                client.Timeout = TimeSpan.FromSeconds(Math.Max(5, settings.HttpTimeoutSeconds));
                client.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(
                    "id-ID,id;q=0.9,en-US;q=0.8,en;q=0.7");
                client.DefaultRequestHeaders.Referrer = new Uri("https://www.tokopedia.com/");
                client.DefaultRequestHeaders.Add("Origin", "https://www.tokopedia.com");
            });

            return services;
        }
    }
}
