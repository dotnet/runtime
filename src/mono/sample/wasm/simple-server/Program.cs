// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System;

namespace HttpServer
{
    public sealed class Session
    {
        public Task CurrentDelay { get; set; } = Task.CompletedTask;
        public int Started { get; set; } = 0;
        public int Finished { get; set; } = 0;
    }

    public sealed class Program
    {
        private bool Verbose = false;
        private ConcurrentDictionary<string, Session> Sessions = new ConcurrentDictionary<string, Session>();
        private Dictionary<string, byte[]> cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public static int Main()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("error: HttpListener is not supported.");
                return -1;
            }

            // retry upto 9 times to find free port
            for (int i = 0; i < 10; i++)
            {
                if (new Program().StartServer())
                    break;
            }

            return 0;
        }

        private bool StartServer()
        {
            var port = 8000 + Random.Shared.Next(1000);
            var listener = new HttpListener();
            var url = $"http://localhost:{port}/";
            listener.Prefixes.Add(url);

            try
            {
                listener.Start();
            }
            catch (HttpListenerException)
            {
                return false;
            }

            Console.WriteLine($"Listening on {url}");
            OpenUrl(url);

            while (true)
                HandleRequest(listener);
        }

        private void OpenUrl(string url)
        {
            var proc = new Process();
            var si = new ProcessStartInfo();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                si.FileName = url;
                si.UseShellExecute = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                si.FileName = "xdg-open";
                si.ArgumentList.Add(url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                si.FileName = "open";
                si.ArgumentList.Add(url);
            }
            else
            {
                Console.WriteLine("Don't know how to open url on this OS platform");
            }

            proc.StartInfo = si;
            proc.Start();
        }

        private void HandleRequest(HttpListener listener)
        {
            var context = listener.GetContext();
            if (Verbose)
                Console.WriteLine($"request url: {context.Request.Url}");

            if (context.Request.HttpMethod == "GET")
                ServeAsync(context);
            else if (context.Request.HttpMethod == "POST")
                ReceivePostAsync(context);
        }

        private async Task<byte[]?> GetFileContent(string path)
        {
            if (Verbose)
                await Console.Out.WriteLineAsync($"get content for: {path}");

            if (cache.ContainsKey(path))
            {
                if (Verbose)
                    await Console.Out.WriteLineAsync($"returning cached content for: {path}");

                return cache[path];
            }

            var content = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            if (content == null)
                return null;

            if (Verbose)
                await Console.Out.WriteLineAsync($"adding content to cache for: {path}");

            cache[path] = content;

            return content;
        }

        private async void ReceivePostAsync(HttpListenerContext context)
        {
            if (Verbose)
            {
                Console.WriteLine("got POST request");
                Console.WriteLine($"  content type: {context.Request.ContentType}");
            }

            var url = context.Request.Url;
            if (url == null)
                return;

            var path = url.LocalPath;
            var contentType = context.Request.ContentType;
            if (contentType != null && contentType.StartsWith("text/plain") && path.StartsWith("/"))
            {
                path = path.Substring(1);
                if (Verbose)
                    Console.WriteLine($"  writting POST stream to '{path}' file");

                var content = await new StreamReader(context.Request.InputStream).ReadToEndAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(path, content).ConfigureAwait(false);
            }
            else
                return;

            var stream = context.Response.OutputStream;
            stream.Close();
            context.Response.Close();
        }

        private async void ServeAsync(HttpListenerContext context)
        {
            if (Verbose)
                Console.WriteLine("got GET request");

            var request = context.Request;
            var url = request.Url;
            if (url == null)
                return;

            string path = url.LocalPath == "/" ? "index.html" : url.LocalPath;
            if (Verbose)
                Console.WriteLine($"  serving: {path}");

            Session? session = null;
            var throttleMbps = 0.0;
            var latencyMs = 0;
            string? sessionId = null;
            if (path.StartsWith("/unique/")) // like /unique/7a3da2c7-bf35-477e-a585-a207ea30730c/dotnet.js
            {
                sessionId = path.Substring(0, 45);
                session = Sessions.GetOrAdd(sessionId, new Session());
                path = path.Substring(45);
                throttleMbps = 30.0;
                latencyMs = 100;
            }
            else if (path.StartsWith("/"))
                path = path.Substring(1);

            byte[]? buffer;
            try
            {
                buffer = await GetFileContent(path);

                if (buffer != null && throttleMbps > 0)
                {
                    double delaySeconds = (buffer.Length * 8) / (throttleMbps * 1024 * 1024);
                    int delayMs = (int)(delaySeconds * 1000);
                    if (session != null)
                    {
                        Task currentDelay;
                        int myIndex;
                        lock (session)
                        {
                            currentDelay = session.CurrentDelay;
                            myIndex = session.Started;
                            session.Started++;
                        }

                        while (true)
                        {
                            // wait for everybody else to finish in this while loop
                            await currentDelay;

                            lock (session)
                            {
                                // it's my turn to insert delay for others
                                if (session.Finished == myIndex)
                                {
                                    session.CurrentDelay = Task.Delay(delayMs);
                                    break;
                                }
                                else
                                {
                                    currentDelay = session.CurrentDelay;
                                }
                            }
                        }
                        // wait my own delay
                        await Task.Delay(delayMs + latencyMs);

                        lock (session)
                        {
                            session.Finished++;
                            if (session.Finished == session.Started)
                            {
                                Sessions.TryRemove(sessionId!, out _);
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(delayMs + latencyMs);
                    }
                }
            }
            catch (Exception)
            {
                buffer = null;
            }

            if (buffer != null)
            {
                string? contentType = null;
                if (path.EndsWith(".wasm"))
                    contentType = "application/wasm";
                if (path.EndsWith(".webcil") || path.EndsWith(".dll") || path.EndsWith(".pdb"))
                    contentType = "application/octet-stream";
                if (path.EndsWith(".json"))
                    contentType = "application/json";
                if (path.EndsWith(".js") || path.EndsWith(".mjs") || path.EndsWith(".cjs"))
                    contentType = "text/javascript";

                var stream = context.Response.OutputStream;

                // test download re-try
                if (url.Query.Contains("testError"))
                {
                    Console.WriteLine("Faking 500 " + url);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await stream.WriteAsync(buffer, 0, 0).ConfigureAwait(false);
                    await stream.FlushAsync();
                    context.Response.Close();
                    return;
                }

                if (contentType != null)
                    context.Response.ContentType = contentType;

                context.Response.ContentLength64 = buffer.Length;
                context.Response.AppendHeader("cache-control", "public, max-age=31536000");

                // test download re-try
                if (url.Query.Contains("testAbort"))
                {
                    Console.WriteLine("Faking abort " + url);
                    await stream.WriteAsync(buffer, 0, 10).ConfigureAwait(false);
                    await stream.FlushAsync();
                    await Task.Delay(100);
                    context.Response.Abort();
                    return;
                }

                try
                {
                    await stream.WriteAsync(buffer).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (Verbose)
                        Console.WriteLine($"interrupted: {e.Message}");
                }

                stream.Close();
                context.Response.Close();
            }
            else
            {
                if (Verbose)
                    Console.WriteLine("  => not found");

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            if (Verbose)
                Console.WriteLine($"finished url: {context.Request.Url}");
        }
    }
}
