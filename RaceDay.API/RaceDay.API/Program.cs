using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;

namespace RaceDay.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // register the database context using the SQL Server connection string
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // configure server-side sessions for authentication and role tracking
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60); // set session timeout to 1 hour
                options.Cookie.HttpOnly = true; // prevent client-side scripts from accessing the cookie
                options.Cookie.IsEssential = true;
            });

            // add services to the container
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // app.UseHttpsRedirection(); // temporarily disabled so the local .http file works without SSL certificate blocks

            // enable session middleware / before authorisation 
            app.UseSession();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}