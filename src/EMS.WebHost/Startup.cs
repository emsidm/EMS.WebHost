using System;
using System.Linq;
using System.Reflection;
using EMS.Web.Abstractions;
using EMS.WebHost.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EMS.WebHost
{
    public class Startup
    {
        private ModuleConfiguration[] _modules;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            _modules = Configuration.GetSection("Modules").Get<ModuleConfiguration[]>();
            var mvcBuilder = services.AddMvc();

//            foreach (var module in _modules)
//            {
//                var assembly = Assembly.Load(module.AssemblyFile);
//            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            foreach (var module in _modules)
            {
                var assembly = Assembly.LoadFrom(module.AssemblyFile);
                var initializerType = assembly.GetTypes()
                    .Single(x => typeof(IModuleInitializer).IsAssignableFrom(x));

                var initializer = (IModuleInitializer) Activator.CreateInstance(initializerType);

                app.UseModule(module.BaseUrl,
                    services => initializer.ConfigureServices(services),
                    builder => initializer.Configure(builder, env, loggerFactory));
            }

            app.Run(async c => { await c.Response.WriteAsync("Not in extension!"); });
        }
    }
}