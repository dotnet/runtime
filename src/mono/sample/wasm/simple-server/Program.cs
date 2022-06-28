// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HttpServer
{
    public sealed class Program
    {
        private bool Verbose = false;

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
                System.Console.WriteLine("Don't know how to open url on this OS platform");
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

            if (path.StartsWith("/"))
                path = path.Substring(1);

            byte[]? buffer;
            try
            {
                buffer = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
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
                if (path.EndsWith(".js") || path.EndsWith(".mjs") || path.EndsWith(".cjs"))
                    contentType = "text/javascript";

                if (contentType != null)
                    context.Response.ContentType = contentType;

                context.Response.ContentLength64 = buffer.Length;
                context.Response.AppendHeader("cache-control", "public, max-age=31536000");
                var stream = context.Response.OutputStream;
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
