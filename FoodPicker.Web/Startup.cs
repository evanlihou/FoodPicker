using System;
using System.Linq;
using System.Net;
using System.Reflection;
using FoodPicker.Infrastructure.Data;
using FoodPicker.Infrastructure.Models;
using FoodPicker.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using FoodPicker.Web.Data;
using FoodPicker.Web.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodPicker.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection"),
                    x => x.MigrationsAssembly("FoodPicker.Migrations")));
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();
            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.Cookie.Name = "FoodPicker";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.LoginPath = "/Auth/Login";
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                options.SlidingExpiration = true;
            });
            services.AddControllersWithViews();
            
            services.AddAuthorization(opt =>
            {
                opt.AddPolicy(AuthorizationPolicies.AccessInternalAdminAreas, pol =>
                {
                    pol.RequireRole("Admin");
                    pol.RequireClaim("PasswordLogin", "true");
                });
            });
            
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                
                foreach (var proxy in Configuration.GetSection("KnownProxies").Get<string[]>())
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }
            });

            if (bool.Parse(Configuration["RedirectToHttps"]))
            {
                // We're not running as HTTPS in the container, but we can trust the load balancer to be listening on 443
                services.AddHttpsRedirection(opt =>
                {
                    opt.HttpsPort = 443;
                });
            }

            switch (Configuration["MealService"])
            {
                case "HelloFresh":
                    services.AddScoped<MealService, HelloFreshMealService>();
                    // services.AddScoped<HelloFreshMealService>();
                    // services.AddScoped<MealService>((serviceProvider) => serviceProvider.GetRequiredService<HelloFreshMealService>());
                    break;
            }
            
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            
            var repoTypes = typeof(Repository).Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(Repository)));
            
            foreach(var repoType in repoTypes )
            {
                services.AddScoped(repoType);
            }
            
            var serviceTypes = typeof(IService).Assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IService)) && x != typeof(IService));
            foreach(var serviceType in serviceTypes )
            {
                services.AddScoped(serviceType);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApplicationDbContext dbContext, RoleManager<IdentityRole> roleManager)
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
            app.UseForwardedHeaders();
            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            dbContext.Database.Migrate();

            app.UseAuthentication();
            app.UseAuthorization();

            RoleDataInitializer.SeedData(roleManager).Wait();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}