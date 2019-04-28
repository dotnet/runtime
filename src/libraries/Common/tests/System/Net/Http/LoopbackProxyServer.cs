// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
                try
                {
                    while (true)
                    {
                        Socket s = await _listener.AcceptAsync();
                        var ignored = Task.Run(async () =>
                        {
                            try
                            {
                                await ProcessConnection(s);
                            }
                            catch (Exception)
                            {
                                // Ignore exceptions.
                            }
                        });
                    }
                }
                catch (Exception)
                {
                    // Ignore exceptions.
                }

                _serverStopped.Set();
            });
        }

        private async Task ProcessConnection(Socket s)
        {
            Interlocked.Increment(ref _connections);

            using (var ns = new NetworkStream(s))
            using (var reader = new StreamReader(ns))
            using (var writer = new StreamWriter(ns) { AutoFlush = true })
            {
                while(true)
                {
                    if (!(await ProcessRequest(reader, writer)))
                    {
                        break;
                    }
                }
            }
        }

        private async Task<bool> ProcessRequest(StreamReader reader, StreamWriter writer)
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
            _requests.Add(request);

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
                await ProcessConnectMethod((NetworkStream)reader.BaseStream, remoteHost, remotePort);

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
            using (HttpResponseMessage response =
                await outboundClient.SendAsync(requestMessage))
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
                string responseBody = await response.Content.ReadAsStringAsync();
                writer.Write(responseBody);

                return true;
            }
        }

        private async Task ProcessConnectMethod(NetworkStream clientStream, string remoteHost, int remotePort)
        {
            // Open connection to destination server.
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await serverSocket.ConnectAsync(remoteHost, remotePort);
            NetworkStream serverStream = new NetworkStream(serverSocket);

            // Relay traffic to/from client and destination server.
            Task clientCopyTask = Task.Run(async () =>
            {
                try
                {
                    byte[] buffer = new byte[8000];
                    int bytesRead;
                    while ((bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await serverStream.WriteAsync(buffer, 0, bytesRead);
                    }
                    serverStream.Flush();
                    serverSocket.Shutdown(SocketShutdown.Send);
                }
                catch (IOException)
                {
                    // Ignore rude disconnects from either side.
                }
            });
            Task serverCopyTask = Task.Run(async () =>
            {
                try
                {
                    byte[] buffer = new byte[8000];
                    int bytesRead;
                    while ((bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await clientStream.WriteAsync(buffer, 0, bytesRead);
                    }
                    clientStream.Flush();
                }
                catch (IOException)
                {
                    // Ignore rude disconnects from either side.
                }
            });

            Task.WhenAny(new[] { clientCopyTask, serverCopyTask }).Wait();
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
