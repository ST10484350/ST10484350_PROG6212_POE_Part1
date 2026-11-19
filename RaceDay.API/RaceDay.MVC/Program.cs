using RaceDay.MVC.Services;

namespace RaceDay.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // adding MVC controllers and views
            builder.Services.AddControllersWithViews();

            // letting razor views like _Layout.cshtml use session data for role based menus
            builder.Services.AddHttpContextAccessor();

            // setting up session management to store the logged in user ID and role
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // registering the handler that forwards the api session cookie on outgoing requests
            builder.Services.AddTransient<ApiCookieHandler>();

            // registering an httpclient so the MVC app communicates with the P2 API and not the db
            var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5101/";
            builder.Services.AddHttpClient("RaceDayApi", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
            .AddHttpMessageHandler<ApiCookieHandler>();

            var app = builder.Build();

            // configuring the http request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // commenting out so local dev and docker run smoothly over http without cert issues
            //app.UseHttpsRedirection();
            app.UseRouting();

            // enabling sessions before authorization so controllers and views can check the logged in user
            app.UseSession();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}