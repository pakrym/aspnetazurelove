using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace AzureSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddAzureWebAppDiagnostics();
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var config = GetConfiguration(env);

            app.Run(async (context) =>
            {
                var logger = context.RequestServices.GetService<ILogger<Startup>>();
                logger.LogTrace("Request query string is '{query}'.", context.Request.QueryString.Value);
                logger.LogWarning("The time is now {Time}, it's getting late!", DateTimeOffset.Now);
                await context.Response.WriteAsync("KeyVault configuration property value is '" + config["KeyVaultConfigProperty"]+"'");
            });
        }

        private IConfigurationRoot GetConfiguration(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder();
            builder.SetFileProvider(env.ContentRootFileProvider);
            builder.AddJsonFile("settings.json");

            var config = builder.Build();

            var store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var cert = store.Certificates.Find(X509FindType.FindByThumbprint, config["CertificateThumbprint"], false);
            builder.AddAzureKeyVault(
                config["Vault"],
                config["ClientId"],
                cert.OfType<X509Certificate2>().Single());

            store.Close();

            return builder.Build();
        }
    }
}
