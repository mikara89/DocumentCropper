using DocumentCropper.API.Filters;
using DocumentCropper.Lib;

namespace DocumentCropper.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c => c.OperationFilter<SwaggerFileOperationFilter>());

            builder.Services.AddTransient<IImageProcessor,TransformImageProcessor>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
           // }

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}