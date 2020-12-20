using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NIBSSForexValidationService.Interfaces;
using NIBSSForexValidationService.Middleware;
using NIBSSForexValidationService.Models;
using NIBSSForexValidationService.Services;
using Swashbuckle.AspNetCore.Swagger;

namespace NIBSSForexValidationService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddControllers();


            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                
                //Remove when going live
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    
                };
            });


            var ConnectionStringsSettingsSection = Configuration.GetSection("ConnectionStrings");
            services.Configure<ConnectionStrings>(ConnectionStringsSettingsSection);
            var connectionStrings = ConnectionStringsSettingsSection.Get<ConnectionStrings>();

          //  services.AddTransient(_ => new MySqlConnection(connectionStrings.FXMySQLDBConnectionString));
          //  services.AddTransient(_ => new SqlConnection(connectionStrings.BVNMSSQLDbConnectionString));


            services.AddTransient<IFXTransaction,FXTransactionService>();
            services.AddTransient<IAuthenticationAndAuthorisation, AuthenticationService>();


            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = Configuration["AppSettings:AppName"], Version = "v1" });
                c.ResolveConflictingActions( apiDescriptions => apiDescriptions.Last());

            });



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            
            app.UseAuthentication();

           // app.UseMiddleware<DecryptAndEncryptMiddleware>();

            app.UseRouting();

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", Configuration["AppSettings:AppName"]);
            });



        }
    }
}
