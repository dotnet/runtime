#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace InteropTestsWebsite
{
    public class Program
    {
        private const LogLevel MinimumLogLevel = LogLevel.Debug;

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddLogging(builder => builder.SetMinimumLevel(MinimumLogLevel));
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        // Support --port and --use_tls cmdline arguments normally supported
                        // by gRPC interop servers.
                        var http2Port = context.Configuration.GetValue<int>("port", 50052);
                        var http1Port = context.Configuration.GetValue<int>("port_http1", -1);
                        var http3Port = context.Configuration.GetValue<int>("port_http3", -1);
                        var useTls = context.Configuration.GetValue<bool>("use_tls", false);

                        options.Limits.MinRequestBodyDataRate = null;
                        options.ListenAnyIP(http2Port, o => ConfigureEndpoint(o, useTls, HttpProtocols.Http2));
                        if (http1Port != -1)
                        {
                            options.ListenAnyIP(http1Port, o => ConfigureEndpoint(o, useTls, HttpProtocols.Http1));
                        }
                        if (http3Port != -1)
                        {
#pragma warning disable CA2252 // This API requires opting into preview features
                            options.ListenAnyIP(http3Port, o => ConfigureEndpoint(o, useTls, HttpProtocols.Http3));
#pragma warning restore CA2252 // This API requires opting into preview features
                        }

                        void ConfigureEndpoint(ListenOptions listenOptions, bool useTls, HttpProtocols httpProtocols)
                        {
                            Console.WriteLine($"Enabling connection encryption: {useTls}");

                            if (useTls)
                            {
                                var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                                var certPath = Path.Combine(basePath!, "Certs", "server1.pfx");

                                listenOptions.UseHttps(certPath, "PLACEHOLDER");
                            }
                            listenOptions.Protocols = httpProtocols;
                        }
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
