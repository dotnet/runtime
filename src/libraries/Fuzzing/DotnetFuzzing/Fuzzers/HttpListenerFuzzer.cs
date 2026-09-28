// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the server side of the managed <see cref="HttpListener"/> (used on Linux/macOS): each input is sent as raw bytes over
/// a new TCP connection to a local listener, whose request handler reads every request property, drains the body, and for
/// WebSocket upgrades accepts the WebSocket and reads messages from it. Anything other than the documented network/protocol
/// failures thrown by the server side, or a connection the server never finishes, is reported against the input.
/// </summary>
/// <remarks>Input layout: [0] flags (bit0 send in two writes), [1] split point, [2..] the raw request bytes.</remarks>
internal sealed class HttpListenerFuzzer : IFuzzer
{
    private static HttpListener? s_listener;
    private static int s_port;
    private static Exception? s_serverFailure;
    private static int s_active;

    public string[] TargetAssemblies { get; } = ["System.Net.HttpListener"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        EnsureListener();

        byte[] request = bytes.Slice(2).ToArray();
        int split = request.Length == 0 ? 0 : bytes[1] % request.Length;

        using (var client = new TcpClient())
        {
            client.Connect(IPAddress.Loopback, s_port);
            client.ReceiveTimeout = 5000;
            NetworkStream stream = client.GetStream();
            try
            {
                if ((bytes[0] & 1) != 0)
                {
                    stream.Write(request, 0, split);
                    stream.Flush();
                    stream.Write(request, split, request.Length - split);
                }
                else
                {
                    stream.Write(request);
                }

                client.Client.Shutdown(SocketShutdown.Send);

                // Read the server's answer until it closes the connection.
                byte[] buffer = new byte[16384];
                while (stream.Read(buffer) > 0)
                {
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut or SocketError.WouldBlock })
            {
                Throw($"the server neither answered nor closed the connection within 5 s for {Describe(request)}");
            }
            catch (IOException)
            {
                // Connection reset by the server: fine.
            }
        }

        // Wait for the request handler to finish with this connection before judging it.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (Volatile.Read(ref s_active) != 0 && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(1);
        }

        if (Volatile.Read(ref s_active) != 0)
        {
            Throw($"the request handler is still busy 5 s after the client finished, for {Describe(request)}");
        }

        if (Interlocked.Exchange(ref s_serverFailure, null) is Exception failure)
        {
            throw new InvalidOperationException($"HttpListener request handling threw {failure.GetType().Name} for {Describe(request)}: {failure}", failure);
        }
    }

    private static void EnsureListener()
    {
        if (s_listener is not null)
        {
            return;
        }

        var random = new Random();
        for (int attempt = 0; ; attempt++)
        {
            int port = random.Next(20000, 60000);
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            try
            {
                listener.Start();
            }
            catch (HttpListenerException) when (attempt < 50)
            {
                continue;
            }

            s_port = port;
            s_listener = listener;
            break;
        }

        _ = Task.Run(ServeAsync);
    }

    private static async Task ServeAsync()
    {
        while (true)
        {
            HttpListenerContext context = await s_listener!.GetContextAsync().ConfigureAwait(false);
            Interlocked.Increment(ref s_active);
            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleAsync(context).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsExpected(ex))
                {
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref s_serverFailure, ex, null);
                }
                finally
                {
                    try
                    {
                        context.Response.Abort();
                    }
                    catch
                    {
                    }

                    Interlocked.Decrement(ref s_active);
                }
            });
        }
    }

    private static async Task HandleAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;

        // Everything an application typically looks at.
        _ = request.HttpMethod;
        _ = request.RawUrl;
        _ = request.Url;
        _ = request.ProtocolVersion;
        _ = request.QueryString.Count;
        foreach (string? key in request.QueryString.AllKeys)
        {
            _ = request.QueryString.GetValues(key);
        }

        foreach (string? key in request.Headers.AllKeys)
        {
            _ = request.Headers.GetValues(key);
        }

        _ = request.Cookies.Count;
        _ = request.ContentType;
        _ = request.ContentEncoding;
        _ = request.ContentLength64;
        _ = request.HasEntityBody;
        _ = request.UserAgent;
        _ = request.UserHostName;
        _ = request.UserHostAddress;
        _ = request.UrlReferrer;
        _ = request.AcceptTypes;
        _ = request.UserLanguages;
        _ = request.KeepAlive;
        _ = request.IsWebSocketRequest;

        if (request.IsWebSocketRequest)
        {
            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
            WebSocket webSocket = webSocketContext.WebSocket;
            byte[] buffer = new byte[4096];
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, timeout.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }

            return;
        }

        byte[] body = new byte[16384];
        long total = 0;
        int read;
        while ((read = await request.InputStream.ReadAsync(body).ConfigureAwait(false)) > 0)
        {
            total += read;
        }

        context.Response.StatusCode = 200;
        byte[] reply = "ok"u8.ToArray();
        context.Response.ContentLength64 = reply.Length;
        await context.Response.OutputStream.WriteAsync(reply).ConfigureAwait(false);
        context.Response.Close();
    }

    private static bool IsExpected(Exception ex) => ex switch
    {
        HttpListenerException => true,
        IOException => true,
        ObjectDisposedException => true,
        WebSocketException => true,
        OperationCanceledException => true, // The WebSocket read timeout.
        _ => false,
    };

    private static string Describe(byte[] request) =>
        $"request {request.Length} bytes: {string.Concat(request.Take(300).Select(b => b is >= 0x20 and < 0x7F and not (byte)'\\' ? ((char)b).ToString() : $"\\x{b:X2}"))}";

    private static void Throw(string message) => throw new InvalidOperationException(message);
}
