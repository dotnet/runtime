// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

#if GENEVA_TELEMETRY
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter.Geneva;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
#endif

namespace NetCoreServer
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
#if GENEVA_TELEMETRY
            services.AddOpenTelemetryMetrics((builder) => builder
                .AddAspNetCoreInstrumentation()
                .AddGenevaMetricExporter(options =>
                {
                    options.PrepopulatedMetricDimensions = new Dictionary<string, object>()
                    {
                        ["CustomerResourceId"] = Configuration["GenevaExport:CustomerResourceId"],
                        ["LocationId"] = Configuration["GenevaExport:LocationId"]
                    };

                    options.ConnectionString = Configuration["GenevaExport:ConnectionString"];
                })
            );
#endif
            services.AddCors(o => o.AddPolicy("AnyCors", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("*")
                    ;
            }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("AnyCors");
            app.UseWebSockets();
            app.UseGenericHandler();
        }
    }
}
