using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Collector.AspNetCore;
using OpenTelemetry.Collector.Dependencies;
using OpenTelemetry.Exporter.Zipkin;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Sampler;
using System;
using System.Diagnostics;

namespace WebApplication1
{
    public class Startup
    {
        private ZipkinTraceExporter _exporter;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSingleton<ITracer>(Tracing.Tracer);
            services.AddSingleton<ISampler>(Samplers.AlwaysSample);
            //services.AddSingleton<IPropagationComponent>(new DefaultPropagationComponent());

            services.AddSingleton<RequestsCollectorOptions>(new RequestsCollectorOptions());
            services.AddSingleton<RequestsCollector>();

            services.AddSingleton<DependenciesCollectorOptions>(new DependenciesCollectorOptions());
            services.AddSingleton<DependenciesCollector>();

            services.AddSingleton<RabbitMqCollectorOptions>(new RabbitMqCollectorOptions());
            services.AddSingleton<RabbitMqCollector>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            lifetime.ApplicationStarted.Register(OnAppStarted(app.ApplicationServices));
            lifetime.ApplicationStopped.Register(OnAppStopped(app.ApplicationServices));
        }

        private Action OnAppStarted(IServiceProvider applicationServices)
        {
            return () =>
            {
                var collector = applicationServices.GetService<RequestsCollector>();
                var depCollector = applicationServices.GetService<DependenciesCollector>();
                var rmqCollector = applicationServices.GetService<RabbitMqCollector>();

                var traceConfig = Tracing.TraceConfig;
                var currentConfig = traceConfig.ActiveTraceParams;
                var newConfig = currentConfig.ToBuilder()
                    .SetSampler(Samplers.AlwaysSample)
                    .Build();
                traceConfig.UpdateActiveTraceParams(newConfig);

                _exporter = new ZipkinTraceExporter(
                    new ZipkinTraceExporterOptions()
                    {
                        Endpoint = new Uri("http://localhost:9411/api/v2/spans"),
                        ServiceName = typeof(Program).Assembly.GetName().Name,
                    },
                    Tracing.ExportComponent);

                _exporter.Start();
            };
        }

        private Action OnAppStopped(IServiceProvider applicationServices)
        {
            return () =>
            {
                _exporter.Stop();
            };
        }
    }
}
