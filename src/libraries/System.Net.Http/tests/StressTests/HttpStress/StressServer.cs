// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Specialized;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

namespace HttpStress
{
    public class StressServer : IDisposable
    {
        // Header indicating expected response content length to be returned by the server
        public const string ExpectedResponseContentLength = "Expected-Response-Content-Length";

        private readonly IWebHost _webHost;

        public string ServerUri { get; }

        public StressServer(Configuration configuration)
        {
            ServerUri = configuration.ServerUri;
            (string scheme, string hostname, int port) = ParseServerUri(configuration.ServerUri);
            IWebHostBuilder host = WebHost.CreateDefaultBuilder();

            if (configuration.UseHttpSys && OperatingSystem.IsWindows())
            {
                // Use http.sys.  This requires additional manual configuration ahead of time;
                // see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys?view=aspnetcore-2.2#configure-windows-server.
                // In particular, you need to:
                // 1. Create a self-signed cert and install it into your local personal store, e.g. New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My"
                // 2. Pre-register the URL prefix, e.g. netsh http add urlacl url=https://localhost:5001/ user=Users
                // 3. Register the cert, e.g. netsh http add sslcert ipport=[::1]:5001 certhash=THUMBPRINTFROMABOVE appid="{some-guid}"
                host = host.UseHttpSys(hso =>
                {
                    hso.UrlPrefixes.Add(ServerUri);
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
                    // conservative estimation based on https://github.com/dotnet/aspnetcore/blob/caa910ceeba5f2b2c02c47a23ead0ca31caea6f0/src/Servers/Kestrel/Core/src/Internal/Http2/Http2Stream.cs#L204
                    ko.Limits.MaxRequestLineSize = Math.Max(ko.Limits.MaxRequestLineSize, configuration.MaxRequestUriSize + 100);
                    ko.Limits.MaxRequestHeaderCount = Math.Max(ko.Limits.MaxRequestHeaderCount, configuration.MaxRequestHeaderCount);
                    ko.Limits.MaxRequestHeadersTotalSize = Math.Max(ko.Limits.MaxRequestHeadersTotalSize, configuration.MaxRequestHeaderTotalSize);

                    ko.Limits.Http2.MaxStreamsPerConnection = configuration.ServerMaxConcurrentStreams ?? ko.Limits.Http2.MaxStreamsPerConnection;
                    ko.Limits.Http2.MaxFrameSize = configuration.ServerMaxFrameSize ?? ko.Limits.Http2.MaxFrameSize;
                    ko.Limits.Http2.InitialConnectionWindowSize = configuration.ServerInitialConnectionWindowSize ?? ko.Limits.Http2.InitialConnectionWindowSize;
                    ko.Limits.Http2.MaxRequestHeaderFieldSize = configuration.ServerMaxRequestHeaderFieldSize ?? ko.Limits.Http2.MaxRequestHeaderFieldSize;

                    switch (hostname)
                    {
                        case "+":
                        case "*":
                            ko.ListenAnyIP(port, ConfigureListenOptions);
                            break;
                        default:
                            IPAddress iPAddress = Dns.GetHostAddresses(hostname).First();
                            ko.Listen(iPAddress, port, ConfigureListenOptions);
                            break;

                    }

                    void ConfigureListenOptions(ListenOptions listenOptions)
                    {
                        if (scheme == "https")
                        {
                            // Create self-signed cert for server.
                            using (RSA rsa = RSA.Create())
                            {
                                var certReq = new CertificateRequest("CN=contoso.com", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                                certReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                                certReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                                certReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
                                X509Certificate2 cert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(1));
                                if (OperatingSystem.IsWindows())
                                {
                                    cert = new X509Certificate2(cert.Export(X509ContentType.Pfx));
                                }
                                listenOptions.UseHttps(cert);
                            }
                            if (configuration.HttpVersion == HttpVersion.Version30)
                            {
                                listenOptions.Protocols = HttpProtocols.Http3;
                            }
                        }
                        else
                        {
                            listenOptions.Protocols =
                                configuration.HttpVersion ==  HttpVersion.Version20 ?
                                HttpProtocols.Http2 :
                                HttpProtocols.Http1 ;
                        }
                    }
                });
            };

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            if (configuration.Trace)
            {
                if (!Directory.Exists(LogHttpEventListener.LogDirectory))
                {
                    Directory.CreateDirectory(LogHttpEventListener.LogDirectory);
                }
                // Clear existing logs first.
                foreach (var filename in Directory.GetFiles(LogHttpEventListener.LogDirectory, "server*.log"))
                {
                    try
                    {
                        File.Delete(filename);
                    } catch {}
                }

                loggerConfiguration = loggerConfiguration
                    // Output diagnostics to the file
                    .WriteTo.File(Path.Combine(LogHttpEventListener.LogDirectory, "server.log"), fileSizeLimitBytes: 100 << 20, rollOnFileSizeLimit: true)
                    .MinimumLevel.Debug();
            }
            if (configuration.LogAspNet)
            {
                loggerConfiguration = loggerConfiguration
                    // Output only warnings and errors
                    .WriteTo.Console(Serilog.Events.LogEventLevel.Warning);
            }
            Log.Logger = loggerConfiguration.CreateLogger();

            host = host
                .UseSerilog()
                // Set up how each request should be handled by the server.
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(MapRoutes);
                });

            _webHost = host.Build();
            _webHost.Start();
        }

        private static void MapRoutes(IEndpointRouteBuilder endpoints)
        {
            var loggerFactory = endpoints.ServiceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<StressServer>();
            var head = new[] { "HEAD" };

            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("ok");
            });
            endpoints.MapGet("/get", async context =>
            {
                // Get requests just send back the requested content.
                string content = CreateResponseContent(context);
                await context.Response.WriteAsync(content);
            });
            endpoints.MapGet("/slow", async context =>
            {
                // Sends back the content a character at a time.
                string content = CreateResponseContent(context);

                for (int i = 0; i < content.Length; i++)
                {
                    await context.Response.WriteAsync(content[i].ToString());
                    await context.Response.Body.FlushAsync();
                }
            });
            endpoints.MapGet("/headers", async context =>
            {
                (string name, StringValues values)[] headersToEcho =
                        context.Request.Headers
                        .Where(h => h.Key.StartsWith("header-"))
                        // kestrel does not seem to be splitting comma separated header values, handle here
                        .Select(h => (h.Key, new StringValues(h.Value.SelectMany(v => v.Split(',')).Select(x => x.Trim()).ToArray())))
                        .ToArray();

                foreach ((string name, StringValues values) in headersToEcho)
                {
                    context.Response.Headers.Add(name, values);
                }

                // send back a checksum of all the echoed headers
                ulong checksum = CRC.CalculateHeaderCrc(headersToEcho);
                AppendChecksumHeader(context.Response.Headers, checksum);

                await context.Response.WriteAsync("ok");

                if (context.Response.SupportsTrailers())
                {
                    // just add variations of already echoed headers as trailers
                    foreach ((string name, StringValues values) in headersToEcho)
                    {
                        context.Response.AppendTrailer(name + "-trailer", values);
                    }
                }

            });
            endpoints.MapGet("/variables", async context =>
            {
                NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(context.Request.QueryString.Value!);

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
                string content = CreateResponseContent(context);
                await context.Response.WriteAsync(content.Substring(0, content.Length / 2));
                context.Abort();
            });
            endpoints.MapPost("/", async context =>
            {
                // Post echos back the requested content, first buffering it all server-side, then sending it all back.
                var s = new MemoryStream();
                await context.Request.Body.CopyToAsync(s);

                ulong checksum = CRC.CalculateCRC(s.ToArray());
                AppendChecksumHeader(context.Response.Headers, checksum);

                s.Position = 0;
                await s.CopyToAsync(context.Response.Body);
            });
            endpoints.MapPost("/duplex", async context =>
            {
                // Echos back the requested content in a full duplex manner.
                ArrayPool<byte> bufferPool = ArrayPool<byte>.Shared;

                byte[] buffer = bufferPool.Rent(512);
                ulong hashAcc = CRC.InitialCrc;
                int read;

                try
                {
                    while ((read = await context.Request.Body.ReadAsync(buffer)) != 0)
                    {
                        hashAcc = CRC.update_crc(hashAcc, buffer, read);
                        await context.Response.Body.WriteAsync(buffer, 0, read);
                    }
                }
                finally
                {
                    bufferPool.Return(buffer);
                }

                hashAcc = CRC.InitialCrc ^ hashAcc;

                if (context.Response.SupportsTrailers())
                {
                    context.Response.AppendTrailer("crc32", hashAcc.ToString());
                }
            });
            endpoints.MapPost("/duplexSlow", async context =>
            {
                // Echos back the requested content in a full duplex manner, but one byte at a time.
                var buffer = new byte[1];
                ulong hashAcc = CRC.InitialCrc;
                while ((await context.Request.Body.ReadAsync(buffer)) != 0)
                {
                    hashAcc = CRC.update_crc(hashAcc, buffer, buffer.Length);
                    await context.Response.Body.WriteAsync(buffer);
                }

                hashAcc = CRC.InitialCrc ^ hashAcc;

                if (context.Response.SupportsTrailers())
                {
                    context.Response.AppendTrailer("crc32", hashAcc.ToString());
                }
            });
            endpoints.MapMethods("/", head, context =>
            {
                // Just set the max content length on the response.
                string content = CreateResponseContent(context);
                context.Response.Headers.ContentLength = content.Length;
                return Task.CompletedTask;
            });
            endpoints.MapPut("/", async context =>
            {
                // Read the full request but don't send back a response body.
                await context.Request.Body.CopyToAsync(Stream.Null);
            });
        }

        private static void AppendChecksumHeader(IHeaderDictionary headers, ulong checksum)
        {
            headers.Add("crc32", checksum.ToString());
        }

        public void Dispose()
        {
            _webHost.Dispose();
        }

        private static (string scheme, string hostname, int port) ParseServerUri(string serverUri)
        {
            try
            {
                var uri = new Uri(serverUri);
                return (uri.Scheme, uri.Host, uri.Port);
            }
            catch (UriFormatException)
            {
                // Simple uri parser: used to parse values valid in Kestrel
                // but not representable by the System.Uri class, e.g. https://+:5050
                Match m = Regex.Match(serverUri, "^(?<scheme>https?)://(?<host>[^:/]+)(:(?<port>[0-9]+))?");

                if (!m.Success) throw;

                string scheme = m.Groups["scheme"].Value;
                string hostname = m.Groups["host"].Value;
                int port = m.Groups["port"].Success ? int.Parse(m.Groups["port"].Value) : (scheme == "https" ? 443 : 80);
                return (scheme, hostname, port);
            }
        }

        private static string CreateResponseContent(HttpContext ctx)
        {
            return ServerContentUtils.CreateStringContent(GetExpectedContentLength());

            int GetExpectedContentLength()
            {
                if (ctx.Request.Headers.TryGetValue(ExpectedResponseContentLength, out StringValues values) &&
                    values.Count == 1 &&
                    int.TryParse(values[0], out int result))
                {
                    return result;
                }

                throw new Exception($"Could not parse {ExpectedResponseContentLength} header");
            }
        }
    }

    public static class ServerContentUtils
    {
        // deterministically generate ascii string of given length
        public static string CreateStringContent(int contentSize) =>
            new String(
                Enumerable
                    .Range(0, contentSize)
                    .Select(i => (char)(i % 128))
                    .ToArray());

        // used for validating content on client side
        public static bool IsValidServerContent(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != i % 128)
                    return false;
            }

            return true;
        }
    }
}
