// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace HttpStress
{
    public class StressServer : IDisposable
    {
        private EventListener _eventListener;
        private readonly IWebHost _webHost;

        public Uri ServerUri { get; }

        public StressServer(Uri serverUri, bool httpSys, int maxContentLength, int maxRequestUriSize, string logPath, bool enableAspNetLogs)
        {
            ServerUri = serverUri;
            IWebHostBuilder host = WebHost.CreateDefaultBuilder();

            if (httpSys)
            {
                // Use http.sys.  This requires additional manual configuration ahead of time;
                // see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys?view=aspnetcore-2.2#configure-windows-server.
                // In particular, you need to:
                // 1. Create a self-signed cert and install it into your local personal store, e.g. New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My"
                // 2. Pre-register the URL prefix, e.g. netsh http add urlacl url=https://localhost:5001/ user=Users
                // 3. Register the cert, e.g. netsh http add sslcert ipport=[::1]:5001 certhash=THUMBPRINTFROMABOVE appid="{some-guid}"
                host = host.UseHttpSys(hso =>
                {
                    hso.UrlPrefixes.Add(ServerUri.ToString());
                    hso.Authentication.Schemes = Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes.None;
                    hso.Authentication.AllowAnonymous = true;
                    hso.MaxConnections = null;
                    hso.MaxRequestBodySize = null;
                });
            }
            else
            {
                // Use Kestrel, and configure it for HTTPS with a self-signed test certificate.
                host = host.UseKestrel(ko =>
                {
                    // conservative estimation based on https://github.com/aspnet/AspNetCore/blob/caa910ceeba5f2b2c02c47a23ead0ca31caea6f0/src/Servers/Kestrel/Core/src/Internal/Http2/Http2Stream.cs#L204
                    ko.Limits.MaxRequestLineSize = Math.Max(ko.Limits.MaxRequestLineSize, maxRequestUriSize + 100);

                    ko.ListenLocalhost(serverUri.Port, listenOptions =>
                    {
                        // Create self-signed cert for server.
                        using (RSA rsa = RSA.Create())
                        {
                            var certReq = new CertificateRequest($"CN={ServerUri.Host}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                            certReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                            certReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                            certReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
                            X509Certificate2 cert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(1));
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                cert = new X509Certificate2(cert.Export(X509ContentType.Pfx));
                            }
                            listenOptions.UseHttps(cert);
                        }
                    });
                });
            };

            // Output only warnings and errors from Kestrel
            host = host
                .ConfigureLogging(log => log.AddFilter("Microsoft.AspNetCore", level => enableAspNetLogs ? level >= LogLevel.Warning : false))
                // Set up how each request should be handled by the server.
                .Configure(app =>
                {
                    var head = new[] { "HEAD" };
                    app.UseRouting();
                    app.UseEndpoints(e => MapRoutes(e, maxContentLength));
                });

            // Handle command-line arguments.
            _eventListener =
                logPath == null ? null :
                new HttpEventListener(logPath != "console" ? new StreamWriter(logPath) { AutoFlush = true } : null);

            SetUpJustInTimeLogging();

            _webHost = host.Build();
            _webHost.Start();
        }

        private void MapRoutes(IEndpointRouteBuilder endpoints, int maxContentLength)
        {
            string contentSource = string.Concat(Enumerable.Repeat("1234567890", maxContentLength / 10));
            var head = new[] { "HEAD" };

            endpoints.MapGet("/", async context =>
            {
                // Get requests just send back the requested content.
                await context.Response.WriteAsync(contentSource);
            });
            endpoints.MapGet("/slow", async context =>
            {
                // Sends back the content a character at a time.
                for (int i = 0; i < contentSource.Length; i++)
                {
                    await context.Response.WriteAsync(contentSource[i].ToString());
                    await context.Response.Body.FlushAsync();
                }
            });
            endpoints.MapGet("/headers", async context =>
            {
                (string name, StringValues values)[] headersToEcho = 
                        context.Request.Headers
                        // filter the pseudo-headers surfaced by Kestrel
                        .Where(h => !h.Key.StartsWith(':'))
                        // kestrel does not seem to be splitting comma separated header values, handle here
                        .Select(h => (h.Key, new StringValues(h.Value.SelectMany(v => v.Split(',')).Select(x => x.Trim()).ToArray())))
                        .ToArray();

                foreach((string name, StringValues values) in headersToEcho)
                {
                    context.Response.Headers.Add(name, values);
                }

                await context.Response.WriteAsync("ok");

                if (context.Response.SupportsTrailers())
                {
                    // just add variations of already echoed headers as trailers
                    foreach ((string name, StringValues values) in headersToEcho)
                    {
                        context.Response.AppendTrailer(name + "-Trailer", values);
                    }
                }

            });
            endpoints.MapGet("/variables", async context =>
            {
                string queryString = context.Request.QueryString.Value;
                NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(queryString);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < nameValueCollection.Count; i++)
                {
                    sb.Append(nameValueCollection[$"Var{i}"]);
                }

                await context.Response.WriteAsync(sb.ToString());
            });
            endpoints.MapGet("/abort", async context =>
            {
                // Server writes some content, then aborts the connection
                await context.Response.WriteAsync(contentSource.Substring(0, contentSource.Length / 2));
                context.Abort();
            });
            endpoints.MapPost("/", async context =>
            {
                // Post echos back the requested content, first buffering it all server-side, then sending it all back.
                var s = new MemoryStream();
                await context.Request.Body.CopyToAsync(s);
                s.Position = 0;
                await s.CopyToAsync(context.Response.Body);
            });
            endpoints.MapPost("/duplex", async context =>
            {
                // Echos back the requested content in a full duplex manner.
                await context.Request.Body.CopyToAsync(context.Response.Body);
            });
            endpoints.MapPost("/duplexSlow", async context =>
            {
                // Echos back the requested content in a full duplex manner, but one byte at a time.
                var buffer = new byte[1];
                while ((await context.Request.Body.ReadAsync(buffer)) != 0)
                {
                    await context.Response.Body.WriteAsync(buffer);
                }
            });
            endpoints.MapMethods("/", head, context =>
            {
                // Just set the max content length on the response.
                context.Response.Headers.ContentLength = maxContentLength;
                return Task.CompletedTask;
            });
            endpoints.MapPut("/", async context =>
            {
                // Read the full request but don't send back a response body.
                await context.Request.Body.CopyToAsync(Stream.Null);
            });
        }

        public void Dispose()
        {
            _webHost.Dispose(); _eventListener?.Dispose();
        }

        private void SetUpJustInTimeLogging()
        {
            if (_eventListener == null)
            {
                // If no command-line requested logging, enable the user to press 'L' to enable logging to the console
                // during execution, so that it can be done just-in-time when something goes awry.
                new Thread(() =>
                {
                    while (true)
                    {
                        if (Console.ReadKey(intercept: true).Key == ConsoleKey.L)
                        {
                            Console.WriteLine("Enabling console event logger");
                            _eventListener = new HttpEventListener();
                            break;
                        }
                    }
                })
                { IsBackground = true }.Start();
            }
        }

        /// <summary>EventListener that dumps HTTP events out to either the console or a stream writer.</summary>
        private sealed class HttpEventListener : EventListener
        {
            private readonly StreamWriter _writer;

            public HttpEventListener(StreamWriter writer = null) => _writer = writer;

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == "Microsoft-System-Net-Http")
                    EnableEvents(eventSource, EventLevel.LogAlways);
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                lock (Console.Out)
                {
                    if (_writer != null)
                    {
                        var sb = new StringBuilder().Append($"[{eventData.EventName}] ");
                        for (int i = 0; i < eventData.Payload.Count; i++)
                        {
                            if (i > 0)
                                sb.Append(", ");
                            sb.Append(eventData.PayloadNames[i]).Append(": ").Append(eventData.Payload[i]);
                        }
                        _writer.WriteLine(sb);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write($"[{eventData.EventName}] ");
                        Console.ResetColor();
                        for (int i = 0; i < eventData.Payload.Count; i++)
                        {
                            if (i > 0)
                                Console.Write(", ");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write(eventData.PayloadNames[i] + ": ");
                            Console.ResetColor();
                            Console.Write(eventData.Payload[i]);
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
