using System.IO;
using Monetco.Host.Domain;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Swagger;

namespace Monetco.Host
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

            services.Configure<XmlCommands>(options => Configuration.GetSection("XmlCommands").Bind(options));
            services.Configure<Scopes>(options => Configuration.GetSection("Scopes").Bind(options));
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddMvc(options =>
            {
                options.InputFormatters.Add(new TextPlainInputFormatter());
            });

            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });

            services.AddSingleton(Mapper.Configuration);
            services.AddCors();
            services.AddScoped<IMapper>(sp =>
              new Mapper(sp.GetRequiredService<AutoMapper.IConfigurationProvider>(), sp.GetService));
            services.AddSingleton<IResponseService, ResponseService>();

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                var filePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    "Monetco.xml");
                c.IncludeXmlComments(filePath);
                c.SwaggerDoc("v1", new Info { Title = "Simulator API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //add NLog to ASP.NET Core
            //loggerFactory.AddNLog();
            loggerFactory.AddConsole();
            app.UseMvc();
            app.UseCors(builder =>
                builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
            //add NLog.Web
            //app.AddNLogWeb();
            var filePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    "nlog.config");
            //env.ConfigureNLog(filePath).Reload();

            //log all requests and responses
            app.UseLogRequestMiddleware();
            app.UseLogResponseMiddleware();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("api/manage/swagger/v1/swagger.json", "Simulator API V1");
            });
        }
    }
}
