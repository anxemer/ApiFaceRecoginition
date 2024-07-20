
using Amazon.Rekognition;
using DataAccess;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.Rekognition;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Amazon;


namespace AppFaceRecognition.DependenciInjection
{
    public static class ConfigureServices
    {

        public static void InitializerDependencyInjection(this IServiceCollection services, IConfiguration configuration)
        {
            var awsOptions = configuration.GetSection("AwsOptions").Get<AwsOptions>();
            var awsCredentials = new BasicAWSCredentials("AKIATCKAP5TLFFX5JWFV", "GZABJm0Qa52So8yEyXFxFUPp3a0DSZmhxtxhgFmH");
            var region = RegionEndpoint.GetBySystemName(awsOptions.Region); // Đảm bảo khu vực là mã khu vực, ví dụ 'us-east-1'

            services.AddSingleton<IAmazonRekognition>(sp =>
                new AmazonRekognitionClient(awsCredentials, region)); // Sử dụng khu vực

            services.AddSingleton<IAmazonS3>(sp =>
                new AmazonS3Client(awsCredentials, region));

            services.AddDbContext<StudentDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.Configure<AwsOptions>(configuration.GetSection("AwsOptions"));
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });
            services.AddControllers();
        }


    }
}
