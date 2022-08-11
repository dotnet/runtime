// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;

namespace System.Net.Test.Common
{
    // Loopback server abstraction.
    // Tests that want to run over both HTTP/1.1 and HTTP/2 should use this instead of the protocol-specific loopback servers.

    public abstract class LoopbackServerFactory
    {
        public abstract GenericLoopbackServer CreateServer(GenericLoopbackOptions options = null);
        public abstract Task CreateServerAsync(Func<GenericLoopbackServer, Uri, Task> funcAsync, int millisecondsTimeout = 60_000, GenericLoopbackOptions options = null);

        public abstract Task<GenericLoopbackConnection> CreateConnectionAsync(SocketWrapper socket, Stream stream, GenericLoopbackOptions options = null);

        public abstract Version Version { get; }

        // Common helper methods

        public Task CreateClientAndServerAsync(Func<Uri, Task> clientFunc, Func<GenericLoopbackServer, Task> serverFunc, int millisecondsTimeout = 60_000, GenericLoopbackOptions options = null)
        {
            return CreateServerAsync(async (server, uri) =>
            {
                Task clientTask = clientFunc(uri);
                Task serverTask = serverFunc(server);

                await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed().ConfigureAwait(false);
            }, options: options).WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout));
        }
    }

    public abstract class GenericLoopbackServer : IDisposable
    {
        public virtual Uri Address { get; }

        // Accept a new connection, process a single request and send the specified response, and gracefully close the connection.
        public abstract Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "");

        // Accept a new connection, and hand it to provided delegate.
        public abstract Task AcceptConnectionAsync(Func<GenericLoopbackConnection, Task> funcAsync);

        public abstract Task<GenericLoopbackConnection> EstablishGenericConnectionAsync();

        public abstract void Dispose();

        // Legacy API.
        public Task<HttpRequestData> AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode statusCode = HttpStatusCode.OK, string content = "", IList<HttpHeaderData> additionalHeaders = null)
        {
            return HandleRequestAsync(statusCode, headers: additionalHeaders, content: content);
        }
    }

    public sealed class SocketWrapper : IDisposable
    {
        private Socket _socket;
        private WebSocket _websocket;

        public SocketWrapper(Socket socket)
        {
            _socket = socket;
        }
        public SocketWrapper(WebSocket websocket)
        {
            _websocket = websocket;
        }

        public void Dispose()
        {
            _socket?.Dispose();
            _websocket?.Dispose();
        }
        public void Close()
        {
            _socket?.Close();
            CloseWebSocket();
        }

        public async Task WaitForCloseAsync(CancellationToken cancellationToken)
        {
            while (_websocket != null
                    ? _websocket.State != WebSocketState.Closed
                    : !(_socket.Poll(1, SelectMode.SelectRead) && _socket.Available == 0))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100);
            }
        }

        public void Shutdown(SocketShutdown how)
        {
            _socket?.Shutdown(how);
            CloseWebSocket();
        }

        private void CloseWebSocket()
        {
            if (_websocket == null) return;

            var state = _websocket.State;
            if (state != WebSocketState.Open && state != WebSocketState.Connecting && state != WebSocketState.CloseSent) return;

            try
            {
                var task = _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing remoteLoop", CancellationToken.None);
                // Block and wait for the task to complete synchronously
                Task.WaitAll(task);
            }
            catch (Exception)
            {
            }
        }
    }

    public abstract class GenericLoopbackConnection : IAsyncDisposable
    {
        public abstract ValueTask DisposeAsync();

        public abstract Task InitializeConnectionAsync();

        /// <summary>Read request Headers and optionally request body as well.</summary>
        public abstract Task<HttpRequestData> ReadRequestDataAsync(bool readBody = true);
        /// <summary>Read complete request body if not done by ReadRequestData.</summary>
        public abstract Task<Byte[]> ReadRequestBodyAsync();

        /// <summary>Sends Response back with provided statusCode, headers and content.
        /// If isFinal is false, the body is not completed and you can call SendResponseBodyAsync to send more.</summary>
        public abstract Task SendResponseAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "", bool isFinal = true);
        /// <summary>Sends response headers.</summary>
        public abstract Task SendResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null);
        /// <summary>Sends valid but incomplete headers. Once called, there is no way to continue the response past this point.</summary>
        public abstract Task SendPartialResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null);
        /// <summary>Sends Response body after SendResponse was called with isFinal: false.</summary>
        public abstract Task SendResponseBodyAsync(byte[] content, bool isFinal = true);

        /// <summary>Reads Request, sends Response and closes connection.</summary>
        public abstract Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "");

        /// <summary>Waits for the client to signal cancellation.</summary>
        public abstract Task WaitForCancellationAsync(bool ignoreIncomingData = true);

        /// <summary>Waits for the client to signal cancellation.</summary>
        public abstract Task WaitForCloseAsync(CancellationToken cancellationToken);

        /// <summary>Reset the connection's internal state so it can process further requests.</summary>
        public virtual void CompleteRequestProcessing() { }

        /// <summary>Helper function to make it easier to convert old test with strings.</summary>
        public async Task SendResponseBodyAsync(string content, bool isFinal = true)
        {
            await SendResponseBodyAsync(String.IsNullOrEmpty(content) ? new byte[0] : Encoding.ASCII.GetBytes(content), isFinal);
        }
    }

    public class GenericLoopbackOptions
    {
        public IPAddress Address { get; set; } = IPAddress.Loopback;
        public bool UseSsl { get; set; } = PlatformDetection.SupportsAlpn && !Capability.Http2ForceUnencryptedLoopback();
        public X509Certificate2 Certificate { get; set; }
        public SslProtocols SslProtocols { get; set; } =
#if !NETSTANDARD2_0 && !NETFRAMEWORK
                SslProtocols.Tls13 |
#endif
                SslProtocols.Tls12;

        public int ListenBacklog { get; set; } = 1;
    }

    public struct HttpHeaderData
    {
        // http://httpwg.org/specs/rfc7541.html#rfc.section.4.1
        public const int RfcOverhead = 32;

        public string Name { get; }
        public string Value { get; }
        public bool HuffmanEncoded { get; }
        public byte[] Raw { get; }
        public Encoding ValueEncoding { get; }

        public HttpHeaderData(string name, string value, bool huffmanEncoded = false, byte[] raw = null, Encoding valueEncoding = null)
        {
            Name = name;
            Value = value;
            HuffmanEncoded = huffmanEncoded;
            Raw = raw;
            ValueEncoding = valueEncoding;
        }

        public override string ToString() => Name == null ? "<empty>" : (Name + ": " + (Value ?? string.Empty));
    }

    public class HttpRequestData
    {
        public byte[] Body;
        public string Method;
        public string Path;
        public Version Version;
        public List<HttpHeaderData> Headers { get; }
        public int RequestId;       // HTTP/2 StreamId.

        public HttpRequestData()
        {
            Headers = new List<HttpHeaderData>();
        }

        public static async Task<HttpRequestData> FromHttpRequestMessageAsync(System.Net.Http.HttpRequestMessage request)
        {
            var result = new HttpRequestData();
            result.Method = request.Method.ToString();
            result.Path = request.RequestUri?.AbsolutePath;
            result.Version = request.Version;

            foreach (var header in request.Headers)
            {
                foreach (var value in header.Value)
                {
                    result.Headers.Add(new HttpHeaderData(header.Key, value));
                }
            }

            if (request.Content != null)
            {
                result.Body = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                foreach (var header in request.Content.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        result.Headers.Add(new HttpHeaderData(header.Key, value));
                    }
                }
            }

            return result;
        }

        public string[] GetHeaderValues(string headerName)
        {
            return Headers.Where(h => h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                    .Select(h => h.Value)
                    .ToArray();
        }

        public string GetSingleHeaderValue(string headerName)
        {
            string[] values = GetHeaderValues(headerName);
            if (values.Length != 1)
            {
                throw new Exception(
                    $"Expected single value for {headerName} header, actual count: {values.Length}{Environment.NewLine}" +
                    $"{"\t"}{string.Join(Environment.NewLine + "\t", values)}");
            }

            return values[0];
        }

        public int GetHeaderValueCount(string headerName)
        {
            return Headers.Where(h => h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase)).Count();
        }

        public override string ToString() => $"{Method} {Path} HTTP/{Version}\r\n{string.Join("\r\n", Headers)}\r\n\r\n";
    }
}
