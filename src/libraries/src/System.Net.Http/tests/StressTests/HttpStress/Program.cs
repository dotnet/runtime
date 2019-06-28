// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using System.Text;
using System.Net;

/// <summary>
/// Simple HttpClient stress app that launches Kestrel in-proc and runs many concurrent requests of varying types against it.
/// Usage:
///     dotnet run -c Release [-t [path]]
/// e.g.
///     dotnet run -c Release
///     dotnet run -c Release -t
///     dotnet run -c Release -t d:\log.txt
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Print out environment information
        Console.WriteLine("   .NET Core: " + Path.GetFileName(Path.GetDirectoryName(typeof(object).Assembly.Location)));
        Console.WriteLine("ASP.NET Core: " + Path.GetFileName(Path.GetDirectoryName(typeof(WebHost).Assembly.Location)));

        // Handle command-line arguments.
        EventListener listener = null;
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "-t":
                    listener = new HttpEventListener(args.Length > 1 ? new StreamWriter(args[1]) { AutoFlush = true } : null);
                    break;
            }
        }

        // Basic configuration
        const int ConcurrentRequests = 150;
        const int AppxContentLength = 1_000;
        var httpVersion = new Version(2, 0);
        string content = string.Concat(Enumerable.Repeat("1234567890", AppxContentLength / 10));
        const int DisplayIntervalMilliseconds = 1000;

        const int HttpsPort = 5001;
        const string LocalhostName = "localhost";
        string serverUri = $"https://{LocalhostName}:{HttpsPort}";

        // Validation of a response message
        void ValidateResponse(HttpResponseMessage m)
        {
            if (m.Version != httpVersion)
            {
                throw new Exception($"Expected response version {httpVersion}, got {m.Version}");
            }
        }

        // Set of operations that the client can select from to run.  Each item is a tuple of the operation's name
        // and the delegate to invoke for it, provided with the HttpClient instance on which to make the call and
        // returning asynchronously the retrieved response string from the server.  Individual operations can be
        // commented out from here to turn them off, or additional ones can be added.
        var clientOperations = new (string, Func<HttpClient, Task<string>>)[]
        {
            ("GET",
            async client =>
            {
                using (HttpResponseMessage m = await client.GetAsync(serverUri))
                {
                    ValidateResponse(m);
                    return await m.Content.ReadAsStringAsync();
                }
            }),

            ("POST",
            async client =>
            {
                using (HttpResponseMessage m = await client.PostAsync(serverUri, new StringContent(content)))
                {
                    ValidateResponse(m);
                    return await m.Content.ReadAsStringAsync();
                }
            }),

            ("POST Duplex",
            async client =>
            {
                using (HttpResponseMessage m = await client.PostAsync(serverUri + "/duplex", new StringContent(content)))
                {
                    ValidateResponse(m);
                    return await m.Content.ReadAsStringAsync();
                }
            }),

            ("POST ExpectContinue",
            async client =>
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, serverUri) { Version = httpVersion })
                {
                    req.Content = new StringContent(content);
                    req.Headers.ExpectContinue = true;
                    using (HttpResponseMessage m = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                    {
                        ValidateResponse(m);
                        return await m.Content.ReadAsStringAsync();
                    }
                }
            }),

            ("GET Cancellation",
            async client =>
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, serverUri) { Version = httpVersion })
                {
                    var cts = new CancellationTokenSource();
                    Task<HttpResponseMessage> t = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    await Task.Delay(1);
                    cts.Cancel();
                    try
                    {
                        using (HttpResponseMessage m = await t)
                        {
                            ValidateResponse(m);
                            return await m.Content.ReadAsStringAsync();
                        }
                    }
                    catch (OperationCanceledException) { return null; }
                }
            }),

            ("POST Cancellation",
            async client =>
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, serverUri) { Version = httpVersion })
                {
                    var cts = new CancellationTokenSource();
                    req.Content = new CancelableContent(cts.Token);
                    Task<HttpResponseMessage> t = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    await Task.Delay(1);
                    cts.Cancel();
                    try
                    {
                        using (HttpResponseMessage m = await t)
                        {
                            ValidateResponse(m);
                            return await m.Content.ReadAsStringAsync();
                        }
                    }
                    catch (OperationCanceledException) { return null; }
                }
            }),

        };

        // Start the Kestrel web server in-proc.
        Console.WriteLine("Starting server.");
        WebHost.CreateDefaultBuilder(args)

            // Use Kestrel, and configure it for HTTPS with a self-signed test certificate.
            .UseKestrel(ko =>
            {
                ko.ListenLocalhost(HttpsPort, listenOptions =>
                {
                    using (RSA rsa = RSA.Create())
                    {
                        var certReq = new CertificateRequest($"CN={LocalhostName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
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
            })

            // Output only warnings and errors from Kestrel
            .ConfigureLogging(log => log.AddFilter("Microsoft.AspNetCore", level => level >= LogLevel.Warning))
            
            // Set up how each request should be handled by the server.
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", async context =>
                    {
                        // Get requests just send back the requested content.
                        await context.Response.WriteAsync(content);
                    });
                    endpoints.MapPost("/", async context =>
                    {
                        // Post echos back the requested content, first buffering it all server-side, then sending it all back.
                        var s = new MemoryStream();
                        await context.Request.Body.CopyToAsync(s);
                        await context.Response.StartAsync();
                        s.Position = 0;
                        await s.CopyToAsync(context.Response.Body);
                    });
                    endpoints.MapPost("/duplex", async context =>
                    {
                        // Echos back the requested content in a full duplex manner.
                        await context.Request.Body.CopyToAsync(context.Response.Body);
                    });
                });
            })
            .Build()
            .Start();

        // Start the client.
        Console.WriteLine($"Starting {ConcurrentRequests} client workers.");
        var handler = new SocketsHttpHandler()
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = delegate { return true; }
            }
        };
        using (var client = new HttpClient(handler) { DefaultRequestVersion = httpVersion })
        {
            // Track all successes and failures
            long total = 0;
            long[] success = new long[clientOperations.Length], fail = new long[clientOperations.Length];
            void Increment(ref long counter)
            {
                Interlocked.Increment(ref counter);
                Interlocked.Increment(ref total);
            }

            // Spin up a thread dedicated to outputting stats for each defined interval
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(DisplayIntervalMilliseconds);
                    lock (Console.Out)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("[" + DateTime.Now + "]");
                        Console.ResetColor();
                        Console.WriteLine(" Total: " + total.ToString("N0"));
                        for (int i = 0; i < clientOperations.Length; i++)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write("\t" + clientOperations[i].Item1.PadRight(30));
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("Success: ");
                            Console.ResetColor();
                            Console.Write(success[i].ToString("N0"));
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.Write("\tFail: ");
                            Console.ResetColor();
                            Console.WriteLine(fail[i].ToString("N0"));
                        }
                        Console.WriteLine();
                    }
                }
            }) { IsBackground = true }.Start();

            // Start N workers, each of which sits in a loop making requests.
            Task.WaitAll(Enumerable.Range(0, ConcurrentRequests).Select(taskNum => Task.Run(async () =>
            {
                for (long i = taskNum; ; i++)
                {
                    long opIndex = i % clientOperations.Length;
                    (string operation, Func<HttpClient, Task<string>> func) = clientOperations[opIndex];
                    try
                    {
                        string result = await func(client);

                        if (result != null && result != content)
                        {
                            throw new Exception("Unexpected response.  Got: " + result);
                        }

                        Increment(ref success[opIndex]);
                    }
                    catch (Exception e)
                    {
                        Increment(ref fail[opIndex]);
                        lock (Console.Out)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Error from iteration {i} ({operation}) in task {taskNum} with {success.Sum()} successes / {fail.Sum()} fails:");
                            Console.ResetColor();
                            Console.WriteLine(e);
                            Console.WriteLine();
                        }
                    }
                }
            })).ToArray());
        }

        // Make sure our EventListener doesn't go away.
        GC.KeepAlive(listener);
    }

    /// <summary>HttpContent that partially serializes and then waits for cancellation to be requested.</summary>
    private sealed class CancelableContent : HttpContent
    {
        private readonly CancellationToken _cancellationToken;

        public CancelableContent(CancellationToken cancellationToken) => _cancellationToken = cancellationToken;

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await stream.WriteAsync(new byte[] { 1, 2, 3 });

            var tcs = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);
            using (_cancellationToken.Register(() => tcs.SetResult(true)))
            {
                await tcs.Task.ConfigureAwait(false);
            }

            _cancellationToken.ThrowIfCancellationRequested();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 42;
            return true;
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
                        if (i > 0) sb.Append(", ");
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
                        if (i > 0) Console.Write(", ");
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
