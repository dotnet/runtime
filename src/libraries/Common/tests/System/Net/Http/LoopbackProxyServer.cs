// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    /// <summary>
    /// Provides a test-only HTTP proxy. Handles multiple connections/requests and CONNECT tunneling for HTTPS
    /// endpoints. Provides simulated proxy authentication for Basic, Digest, NTLM, and Negotiate schemes by
    /// checking only for a 'Proxy-Authorization' request header.
    /// </summary>
    public sealed class LoopbackProxyServer : IDisposable
    {
        private const string ProxyAuthorizationHeader = "Proxy-Authorization";
        private const string ProxyAuthenticateBasicHeader = "Proxy-Authenticate: Basic realm=\"NetCore\"\r\n";
        private const string ProxyAuthenticateDigestHeader = "Proxy-Authenticate: Digest realm=\"NetCore\", nonce=\"PwOnWgAAAAAAjnbW438AAJSQi1kAAAAA\", qop=\"auth\", stale=false\r\n";
        private const string ProxyAuthenticateNtlmHeader = "Proxy-Authenticate: NTLM\r\n";
        private const string ProxyAuthenticateNegotiateHeader = "Proxy-Authenticate: Negotiate\r\n";

        private const string ViaHeaderValue = "HTTP/1.1 LoopbackProxyServer";

        private readonly Socket _listener;
        private readonly Uri _uri;
        private readonly AuthenticationSchemes _authSchemes;
        private readonly bool _connectionCloseAfter407;
        private readonly bool _addViaRequestHeader;
        private readonly ManualResetEvent _serverStopped;
        private readonly List<ReceivedRequest> _requests;
        private int _connections;
        private bool _disposed;

        public int Connections => _connections;
        public List<ReceivedRequest> Requests => _requests;
        public Uri Uri => _uri;

        private LoopbackProxyServer(Options options)
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _listener.Listen(int.MaxValue);

            var ep = (IPEndPoint)_listener.LocalEndPoint;
            _uri = new Uri($"http://{ep.Address}:{ep.Port}/");
            _authSchemes = options.AuthenticationSchemes;
            _connectionCloseAfter407 = options.ConnectionCloseAfter407;
            _addViaRequestHeader = options.AddViaRequestHeader;
            _serverStopped = new ManualResetEvent(false);

            _requests = new List<ReceivedRequest>();
        }

        private void Start()
        {
            Task.Run(async () =>
            {
                var activeTasks = new ConcurrentDictionary<Task, int>();

                try
                {
                    while (true)
                    {
                        Socket s = await _listener.AcceptAsync().ConfigureAwait(false);

                        var connectionTask = Task.Run(async () =>
                        {
                            try
                            {
                                await ProcessConnection(s).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                EventSourceTestLogging.Log.TestAncillaryError(ex);
                            }
                        });

                        activeTasks.TryAdd(connectionTask, 0);
                        _ = connectionTask.ContinueWith(t => activeTasks.TryRemove(connectionTask, out _), TaskContinuationOptions.ExecuteSynchronously);
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    // caused during Dispose() to cancel the loop. ignore.
                }
                catch (Exception ex)
                {
                    EventSourceTestLogging.Log.TestAncillaryError(ex);
                }

                try
                {
                    await Task.WhenAll(activeTasks.Keys).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    EventSourceTestLogging.Log.TestAncillaryError(ex);
                }

                _serverStopped.Set();
            });
        }

        private async Task ProcessConnection(Socket s)
        {
            Interlocked.Increment(ref _connections);

            using (var ns = new NetworkStream(s, ownsSocket: true))
            using (var reader = new StreamReader(ns))
            using (var writer = new StreamWriter(ns) { AutoFlush = true })
            {
                while(true)
                {
                    if (!(await ProcessRequest(s, reader, writer).ConfigureAwait(false)))
                    {
                        break;
                    }
                }
            }
        }

        private async Task<bool> ProcessRequest(Socket clientSocket, StreamReader reader, StreamWriter writer)
        {
            var headers = new Dictionary<string, string>();
            string url = null;
            string method = null;
            string line = null;

            line = reader.ReadLine();
            if (line == null)
            {
                // Connection has been closed by client.
                return false;
            }

            var request = new ReceivedRequest();

            // Avoid concurrent writes to the request list
            lock (_requests)
            {
                _requests.Add(request);
            }

            request.RequestLine = line;
            string[] requestTokens = request.RequestLine.Split(' ');
            method = requestTokens[0];
            url = requestTokens[1];
            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {
                string[] headerParts = line.Split(':');
                headers.Add(headerParts[0].Trim(), headerParts[1].Trim());
            }

            // Store any authentication header and check if credentials are required.
            string authValue;
            if (headers.TryGetValue(ProxyAuthorizationHeader, out authValue))
            {
                string[] authTokens = authValue.Split(' ');
                request.AuthorizationHeaderValueScheme = authTokens[0];
                if (authTokens.Length > 1)
                {
                    request.AuthorizationHeaderValueToken = authTokens[1];
                }
            }
            else if (_authSchemes != AuthenticationSchemes.None)
            {
                Send407Response(writer);
                return !_connectionCloseAfter407;
            }

            // Handle methods.
            if (method.Equals("CONNECT"))
            {
                string[] tokens = url.Split(':');
                string remoteHost = tokens[0];
                int remotePort = int.Parse(tokens[1]);

                Send200Response(writer);
                await ProcessConnectMethod(clientSocket, (NetworkStream)reader.BaseStream, remoteHost, remotePort).ConfigureAwait(false);

                return false; // connection can't be used for any more requests
            }

            // Forward the request to the server.
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            foreach (var header in headers)
            {
                if (header.Key != ProxyAuthorizationHeader) // don't forward proxy auth to server
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }
            }

            // Add 'Via' header.
            if (_addViaRequestHeader)
            {
                requestMessage.Headers.Add("Via", ViaHeaderValue);
            }

            var handler = new HttpClientHandler() { UseProxy = false };
            using (HttpClient outboundClient = new HttpClient(handler))
            using (HttpResponseMessage response = await outboundClient.SendAsync(requestMessage).ConfigureAwait(false))
            {
                // Transfer the response headers from the server to the client.
                var sb = new StringBuilder($"HTTP/{response.Version.ToString(2)} {(int)response.StatusCode} {response.ReasonPhrase}\r\n");
                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
                {
                    sb.Append($"{header.Key}: {string.Join(", ", header.Value)}\r\n");
                }
                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
                {
                    sb.Append($"{header.Key}: {string.Join(", ", header.Value)}\r\n");
                }
                sb.Append("\r\n");
                writer.Write(sb.ToString());

                // Forward the response body from the server to the client.
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                writer.Write(responseBody);

                return true;
            }
        }

        private async Task ProcessConnectMethod(Socket clientSocket, NetworkStream clientStream, string remoteHost, int remotePort)
        {
            // Open connection to destination server.
            using Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await serverSocket.ConnectAsync(remoteHost, remotePort).ConfigureAwait(false);
            NetworkStream serverStream = new NetworkStream(serverSocket);

            // Relay traffic to/from client and destination server.
            Task clientCopyTask = Task.Run(async () =>
            {
                try
                {
                    await clientStream.CopyToAsync(serverStream).ConfigureAwait(false);
                    serverSocket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception ex)
                {
                    HandleExceptions(ex);
                }
            });

            Task serverCopyTask = Task.Run(async () =>
            {
                try
                {
                    await serverStream.CopyToAsync(clientStream).ConfigureAwait(false);
                    clientSocket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception ex)
                {
                    HandleExceptions(ex);
                }
            });

            await Task.WhenAll(new[] { clientCopyTask, serverCopyTask }).ConfigureAwait(false);

            /// <summary>Closes sockets to cause both tasks to end, and eats connection reset/aborted errors.</summary>
            void HandleExceptions(Exception ex)
            {
                SocketError sockErr = (ex.InnerException as SocketException)?.SocketErrorCode ?? SocketError.Success;

                // If aborted, the other task failed and is asking this task to end.
                if (sockErr == SocketError.OperationAborted)
                {
                    return;
                }

                // Ask the other task to end by disposing, causing OperationAborted.
                try
                {
                    clientSocket.Close();
                }
                catch (ObjectDisposedException)
                {
                }

                try
                {
                    serverSocket.Close();
                }
                catch (ObjectDisposedException)
                {
                }

                // Eat reset/abort.
                if (sockErr != SocketError.ConnectionReset && sockErr != SocketError.ConnectionAborted)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            }
        }

        private void Send200Response(StreamWriter writer)
        {
            string response =
                "HTTP/1.1 200 OK\r\n" +
                $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                "Content-Length: 0\r\n\r\n";
            writer.Write(response);
        }

        private void Send407Response(StreamWriter writer)
        {
            string proxyAuthenticateHeaders = string.Empty;
            if ((_authSchemes & AuthenticationSchemes.Basic) != 0)
            {
                proxyAuthenticateHeaders += ProxyAuthenticateBasicHeader;
            }

            if ((_authSchemes & AuthenticationSchemes.Digest) != 0)
            {
                proxyAuthenticateHeaders += ProxyAuthenticateDigestHeader;
            }

            if ((_authSchemes & AuthenticationSchemes.Ntlm) != 0)
            {
                proxyAuthenticateHeaders += ProxyAuthenticateNtlmHeader;
            }

            if ((_authSchemes & AuthenticationSchemes.Negotiate) != 0)
            {
                proxyAuthenticateHeaders += ProxyAuthenticateNegotiateHeader;
            }

            string response =
                "HTTP/1.1 407 Proxy Authentication Required\r\n" +
                $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                proxyAuthenticateHeaders +
                (_connectionCloseAfter407 ? "Connection: close\r\n" : "") +
                "Content-Length: 0\r\n\r\n";
            writer.Write(response);
        }

        public static LoopbackProxyServer Create(Options options = null)
        {
            options = options ?? new Options();

            var server = new LoopbackProxyServer(options);
            server.Start();

            return server;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _listener.Dispose();
                _serverStopped.WaitOne();
                _disposed = true;
            }
        }

        public string ViaHeader => ViaHeaderValue;

        public class ReceivedRequest
        {
            public string RequestLine { get; set; }
            public string AuthorizationHeaderValueScheme { get; set; }
            public string AuthorizationHeaderValueToken { get; set; }
        }

        public class Options
        {
            public AuthenticationSchemes AuthenticationSchemes { get; set; } = AuthenticationSchemes.None;
            public bool ConnectionCloseAfter407 { get; set; } = false;
            public bool AddViaRequestHeader { get; set; } = false;
        }
    }
}
