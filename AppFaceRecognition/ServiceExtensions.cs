using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace AppFaceRecognition.DependenciInjection
{
    public static class ServiceExtensions
    {
        public static void InstallServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
                options.AddPolicy("CORS", builder =>
                    builder.AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials()
                           .SetIsOriginAllowed((host) => true)));
        }
    }
}
