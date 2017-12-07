// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public class LoopbackServer
    {
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> AllowAllCertificates = (_, __, ___, ____) => true;

        public class Options
        {
            public IPAddress Address { get; set; } = IPAddress.Loopback;
            public int ListenBacklog { get; set; } = 1;
            public bool UseSsl { get; set; } = false;
            public SslProtocols SslProtocols { get; set; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            public bool WebSocketEndpoint { get; set; } = false;
            public Func<Stream, Stream> ResponseStreamWrapper { get; set; }
        }

        public static Task CreateServerAsync(Func<Socket, Uri, Task> funcAsync, Options options = null)
        {
            IPEndPoint ignored;
            return CreateServerAsync(funcAsync, out ignored, options);
        }

        public static Task CreateServerAsync(Func<Socket, Uri, Task> funcAsync, out IPEndPoint localEndPoint, Options options = null)
        {
            options = options ?? new Options();
            try
            {
                var server = new Socket(options.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                server.Bind(new IPEndPoint(options.Address, 0));
                server.Listen(options.ListenBacklog);

                localEndPoint = (IPEndPoint)server.LocalEndPoint;
                string host = options.Address.AddressFamily == AddressFamily.InterNetworkV6 ? 
                    $"[{localEndPoint.Address}]" :
                    localEndPoint.Address.ToString();

                string scheme = options.UseSsl ? "https" : "http";
                if (options.WebSocketEndpoint)
                {
                    scheme = options.UseSsl ? "wss" : "ws";
                }

                var url = new Uri($"{scheme}://{host}:{localEndPoint.Port}/");

                return funcAsync(server, url).ContinueWith(t =>
                {
                    server.Dispose();
                    t.GetAwaiter().GetResult();
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
            catch (Exception e)
            {
                localEndPoint = null;
                return Task.FromException(e);
            }
        }

        public static string DefaultHttpResponse => $"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n";

        public static IPAddress GetIPv6LinkLocalAddress() =>
            NetworkInterface
                .GetAllNetworkInterfaces()
                .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address)
                .Where(a => a.IsIPv6LinkLocal)
                .FirstOrDefault();

        public static Task<List<string>> ReadRequestAndSendResponseAsync(Socket server, string response = null, Options options = null)
        {
            return AcceptSocketAsync(server, (s, stream, reader, writer) => ReadWriteAcceptedAsync(s, reader, writer, response), options);
        }

        public static async Task<List<string>> ReadWriteAcceptedAsync(Socket s, StreamReader reader, StreamWriter writer, string response = null)
        {
            // Read request line and headers. Skip any request body.
            var lines = new List<string>();
            string line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
            {
                lines.Add(line);
            }

            await writer.WriteAsync(response ?? DefaultHttpResponse).ConfigureAwait(false);

            return lines;
        }

        public static async Task<bool> WebSocketHandshakeAsync(Socket s, StreamReader reader, StreamWriter writer)
        {
            string serverResponse = null;
            string currentRequestLine;
            while (!string.IsNullOrEmpty(currentRequestLine = await reader.ReadLineAsync().ConfigureAwait(false)))
            {
                string[] tokens = currentRequestLine.Split(new char[] { ':' }, 2);
                if (tokens.Length == 2)
                {
                    string headerName = tokens[0];
                    if (headerName == "Sec-WebSocket-Key")
                    {
                        string headerValue = tokens[1].Trim();
                        string responseSecurityAcceptValue = ComputeWebSocketHandshakeSecurityAcceptValue(headerValue);
                        serverResponse =
                            "HTTP/1.1 101 Switching Protocols\r\n" +
                            "Upgrade: websocket\r\n" +
                            "Connection: Upgrade\r\n" +
                            "Sec-WebSocket-Accept: " + responseSecurityAcceptValue + "\r\n\r\n";
                    }
                }
            }

            if (serverResponse != null)
            {
                // We received a valid WebSocket opening handshake. Send the appropriate response.
                await writer.WriteAsync(serverResponse).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        private static string ComputeWebSocketHandshakeSecurityAcceptValue(string secWebSocketKey)
        {
            // GUID specified by RFC 6455.
            const string Rfc6455Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string combinedKey = secWebSocketKey + Rfc6455Guid;

            // Use of SHA1 hash is required by RFC 6455.
            SHA1 sha1Provider = new SHA1CryptoServiceProvider();
            byte[] sha1Hash = sha1Provider.ComputeHash(Encoding.UTF8.GetBytes(combinedKey));
            return Convert.ToBase64String(sha1Hash);
        }

        public static async Task<List<string>> AcceptSocketAsync(Socket server, Func<Socket, Stream, StreamReader, StreamWriter, Task<List<string>>> funcAsync, Options options = null)
        {
            options = options ?? new Options();
            Socket s = await server.AcceptAsync().ConfigureAwait(false);
            try
            {
                Stream stream = new NetworkStream(s, ownsSocket: false);
                if (options.UseSsl)
                {
                    var sslStream = new SslStream(stream, false, delegate { return true; });
                    using (var cert = Configuration.Certificates.GetServerCertificate())
                    {
                        await sslStream.AuthenticateAsServerAsync(
                            cert,
                            clientCertificateRequired: true, // allowed but not required
                            enabledSslProtocols: options.SslProtocols,
                            checkCertificateRevocation: false).ConfigureAwait(false);
                    }
                    stream = sslStream;
                }

                using (var reader = new StreamReader(stream, Encoding.ASCII))
                using (var writer = new StreamWriter(options?.ResponseStreamWrapper?.Invoke(stream) ?? stream, Encoding.ASCII) { AutoFlush = true })
                {
                    return await funcAsync(s, stream, reader, writer).ConfigureAwait(false);
                }
            }
            finally
            {
                try
                {
                    s.Shutdown(SocketShutdown.Send);
                    s.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // In case the test itself disposes of the socket
                }
            }
        }

        public enum TransferType
        {
            None = 0,
            ContentLength,
            Chunked
        }

        public enum TransferError
        {
            None = 0,
            ContentLengthTooLarge,
            ChunkSizeTooLarge,
            MissingChunkTerminator
        }

        public static Task StartTransferTypeAndErrorServer(
            TransferType transferType,
            TransferError transferError,
            out IPEndPoint localEndPoint)
        {
            return CreateServerAsync((server, url) => AcceptSocketAsync(server, async (client, stream, reader, writer) =>
            {
                // Read past request headers.
                string line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine())) ;

                // Determine response transfer headers.
                string transferHeader = null;
                string content = "This is some response content.";
                if (transferType == TransferType.ContentLength)
                {
                    transferHeader = transferError == TransferError.ContentLengthTooLarge ?
                        $"Content-Length: {content.Length + 42}\r\n" :
                        $"Content-Length: {content.Length}\r\n";
                }
                else if (transferType == TransferType.Chunked)
                {
                    transferHeader = "Transfer-Encoding: chunked\r\n";
                }

                // Write response header
                await writer.WriteAsync("HTTP/1.1 200 OK\r\n").ConfigureAwait(false);
                await writer.WriteAsync($"Date: {DateTimeOffset.UtcNow:R}\r\n").ConfigureAwait(false);
                await writer.WriteAsync("Content-Type: text/plain\r\n").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(transferHeader))
                {
                    await writer.WriteAsync(transferHeader).ConfigureAwait(false);
                }
                await writer.WriteAsync("\r\n").ConfigureAwait(false);

                // Write response body
                if (transferType == TransferType.Chunked)
                {
                    string chunkSizeInHex = string.Format(
                        "{0:x}\r\n",
                        content.Length + (transferError == TransferError.ChunkSizeTooLarge ? 42 : 0));
                    await writer.WriteAsync(chunkSizeInHex).ConfigureAwait(false);
                    await writer.WriteAsync($"{content}\r\n").ConfigureAwait(false);
                    if (transferError != TransferError.MissingChunkTerminator)
                    {
                        await writer.WriteAsync("0\r\n\r\n").ConfigureAwait(false);
                    }
                }
                else
                {
                    await writer.WriteAsync($"{content}\r\n").ConfigureAwait(false);
                }

                return null;
            }), out localEndPoint);
        }        
    }
}
