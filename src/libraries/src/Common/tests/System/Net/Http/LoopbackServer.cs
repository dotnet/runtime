// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public sealed class LoopbackServer : IDisposable
    {
        private Socket _listenSocket;
        private enum AuthenticationProtocols
        {
            Basic,
            Digest,
            None
        }

        private Options _options;
        private Uri _uri;
        // Use CreateServerAsync or similar to create
        private LoopbackServer(Socket listenSocket, Options options)
        {
            _listenSocket = listenSocket;
            _options = options;

            var localEndPoint = (IPEndPoint)listenSocket.LocalEndPoint;
                string host = options.Address.AddressFamily == AddressFamily.InterNetworkV6 ? 
                $"[{localEndPoint.Address}]" :
                localEndPoint.Address.ToString();

            string scheme = options.UseSsl ? "https" : "http";
            if (options.WebSocketEndpoint)
            {
                scheme = options.UseSsl ? "wss" : "ws";
            }

            _uri = new Uri($"{scheme}://{host}:{localEndPoint.Port}/");
        }

        public void Dispose()
        {
            if (_listenSocket != null)
            {
                _listenSocket.Dispose();
                _listenSocket = null;
            }
        }

        public Uri Uri => _uri;

        public static async Task CreateServerAsync(Func<LoopbackServer, Task> funcAsync, Options options = null)
        {
            options = options ?? new Options();

            using (var listenSocket = new Socket(options.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                listenSocket.Bind(new IPEndPoint(options.Address, 0));
                listenSocket.Listen(options.ListenBacklog);

                using (var server = new LoopbackServer(listenSocket, options))
                {
                    await funcAsync(server);
                }
        public static Task<List<string>> ReadRequestAndAuthenticateAsync(Socket server, string response, Options options)
        {
            return AcceptSocketAsync(server, (s, stream, reader, writer) => ValidateAuthenticationAsync(s, reader, writer, response, options), options);
        }

            }
        }

        public static async Task<List<string>> ValidateAuthenticationAsync(Socket s, StreamReader reader, StreamWriter writer, string response, Options options)
        {
            // Send unauthorized response from server.
            await ReadWriteAcceptedAsync(s, reader, writer, response);

            // Read the request method.
            string line = await reader.ReadLineAsync().ConfigureAwait(false);
            int index = line != null ? line.IndexOf(' ') : -1;
            string requestMethod = null;
            if (index != -1)
            {
                requestMethod = line.Substring(0, index);
            }

            // Read the authorization header from client.
            AuthenticationProtocols protocol = AuthenticationProtocols.None;
            string clientResponse = null;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
            {
                if (line.StartsWith("Authorization"))
                {
                    clientResponse = line;
                    if (line.Contains(nameof(AuthenticationProtocols.Basic)))
                    {
                        protocol = AuthenticationProtocols.Basic;
                        break;
                    }
                    else if (line.Contains(nameof(AuthenticationProtocols.Digest)))
                    {
                        protocol = AuthenticationProtocols.Digest;
                        break;
                    }
                }
            }

            bool success = false;
            switch (protocol)
            {
                case AuthenticationProtocols.Basic:
                    success = IsBasicAuthTokenValid(line, options);
                    break;

                case AuthenticationProtocols.Digest:
                    // Read the request content.
                    string requestContent = null;
                    while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
                    {
                        if (line.Contains("Content-Length"))
                        {
                            line = await reader.ReadLineAsync().ConfigureAwait(false);
                            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
                            {
                                requestContent += line;
                            }
                        }
                    }

                    success = IsDigestAuthTokenValid(clientResponse, requestContent, requestMethod, options);
                    break;
            }

            if (success)
            {
                await writer.WriteAsync(DefaultHttpResponse).ConfigureAwait(false);
            }
            else
            {
                await writer.WriteAsync(response).ConfigureAwait(false);
            }

            return null;
        }

        private static bool IsBasicAuthTokenValid(string clientResponse, Options options)
        {
            string clientHash = clientResponse.Substring(clientResponse.IndexOf(nameof(AuthenticationProtocols.Basic), StringComparison.OrdinalIgnoreCase) +
                nameof(AuthenticationProtocols.Basic).Length).Trim();
            string userPass = string.IsNullOrEmpty(options.Domain) ? options.Username + ":" + options.Password : options.Domain + "\\" + options.Username + ":" + options.Password;
            return clientHash == Convert.ToBase64String(Encoding.UTF8.GetBytes(userPass));
        }

        private static bool IsDigestAuthTokenValid(string clientResponse, string requestContent, string requestMethod, Options options)
        {
            string clientHash = clientResponse.Substring(clientResponse.IndexOf(nameof(AuthenticationProtocols.Digest), StringComparison.OrdinalIgnoreCase) +
                nameof(AuthenticationProtocols.Digest).Length).Trim();
            string[] values = clientHash.Split(',');

            string username = null, uri = null, realm = null, nonce = null, response = null, algorithm = null, cnonce = null, opaque = null, qop = null, nc = null;
            bool userhash = false;
            for (int i = 0; i < values.Length; i++)
            {
                string trimmedValue = values[i].Trim();
                if (trimmedValue.Contains(nameof(username)))
                {
                    // Username is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"') + 1;

                    if (startIndex != -1)
                        username = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);

                    // Username is mandatory.
                    if (string.IsNullOrEmpty(username))
                        return false;
                }
                if (trimmedValue.Contains(nameof(userhash)) && trimmedValue.Contains("true"))
                {
                    userhash = true;
                }
                else if (trimmedValue.Contains(nameof(uri)))
                {
                    int startIndex = trimmedValue.IndexOf('"') + 1;
                    if (startIndex != -1)
                        uri = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);

                    // Request uri is mandatory.
                    if (string.IsNullOrEmpty(uri))
                        return false;
                }
                else if (trimmedValue.Contains(nameof(realm)))
                {
                    // Realm is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"') + 1;
                    if (startIndex != -1)
                        realm = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);

                    // Realm is mandatory.
                    if (string.IsNullOrEmpty(realm))
                        return false;
                }
                else if (trimmedValue.Contains(nameof(cnonce)))
                {
                    // CNonce is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"') + 1;
                    if (startIndex != -1)
                        cnonce = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                }
                else if (trimmedValue.Contains(nameof(nonce)))
                {
                    // Nonce is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"') + 1;
                    if (startIndex != -1)
                        nonce = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);

                    // Nonce is mandatory.
                    if (string.IsNullOrEmpty(nonce))
                        return false;
                }
                else if (trimmedValue.Contains(nameof(response)))
                {
                    // response is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"') + 1;
                    if (startIndex != -1)
                        response = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);

                    // Response is mandatory.
                    if (string.IsNullOrEmpty(response))
                        return false;
                }
                else if (trimmedValue.Contains(nameof(algorithm)))
                {
                    int startIndex = trimmedValue.IndexOf('=') + 1;
                    if (startIndex != -1)
                        algorithm = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex).Trim();

                    if (string.IsNullOrEmpty(algorithm))
                        algorithm = "sha-256";
                }
                else if (trimmedValue.Contains(nameof(opaque)))
                {
                    // Opaque is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"') + 1;
                    if (startIndex != -1)
                        opaque = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                }
                else if (trimmedValue.Contains(nameof(qop)))
                {
                    int startIndex = trimmedValue.IndexOf('=') + 1;
                    if (startIndex != -1)
                        qop = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex).Trim();
                }
                else if (trimmedValue.Contains(nameof(nc)))
                {
                    int startIndex = trimmedValue.IndexOf('=') + 1;
                    if (startIndex != -1)
                        nc = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex).Trim();
                }
            }

            // Verify username.
            if (userhash && ComputeHash(options.Username + ":" + realm, algorithm) != username)
            {
                return false;
            }

            if (!userhash && options.Username != username)
            {
                return false;
            }

            // Calculate response and compare with the client response hash.
            string a1 = options.Username + ":" + realm + ":" + options.Password;
            if (algorithm.Contains("sess"))
            {
                a1 = ComputeHash(a1, algorithm) + ":" + nonce + ":" + cnonce ?? string.Empty;
            }

            string a2 = requestMethod + ":" + uri;
            if (qop.Equals("auth-int"))
            {
                string content = requestContent ?? string.Empty;
                a2 = a2 + ":" + ComputeHash(content, algorithm);
            }

            string serverResponseHash = ComputeHash(ComputeHash(a1, algorithm) + ":" +
                                        nonce + ":" +
                                        nc + ":" +
                                        cnonce + ":" +
                                        qop + ":" +
                                        ComputeHash(a2, algorithm), algorithm);

            return response == serverResponseHash;
        }

        private static string ComputeHash(string data, string algorithm)
        {
            // Disable MD5 insecure warning.
#pragma warning disable CA5351
            using (HashAlgorithm hash = algorithm.Contains("SHA-256") ? SHA256.Create() : (HashAlgorithm)MD5.Create())
#pragma warning restore CA5351
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(data));

                StringBuilder sb = new StringBuilder(result.Length * 2);
                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }

        public static Task CreateServerAsync(Func<LoopbackServer, Uri, Task> funcAsync, Options options = null)
        {
            return CreateServerAsync(server => funcAsync(server, server.Uri), options);
        }

        public static Task CreateClientAndServerAsync(Func<Uri, Task> clientFunc, Func<LoopbackServer, Task> serverFunc)
        {
            return CreateServerAsync(async server =>
            {
                Task clientTask = clientFunc(server.Uri);
                Task serverTask = serverFunc(server);

                await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed();
            });
        }

        public async Task AcceptConnectionAsync(Func<Connection, Task> funcAsync)
        {
            using (Socket s = await _listenSocket.AcceptAsync().ConfigureAwait(false))
            {
                s.NoDelay = true;

                Stream stream = new NetworkStream(s, ownsSocket: false);
                if (_options.UseSsl)
                {
                    var sslStream = new SslStream(stream, false, delegate { return true; });
                    using (var cert = Configuration.Certificates.GetServerCertificate())
                    {
                        await sslStream.AuthenticateAsServerAsync(
                            cert,
                            clientCertificateRequired: true, // allowed but not required
                            enabledSslProtocols: _options.SslProtocols,
                            checkCertificateRevocation: false).ConfigureAwait(false);
                    }
                    stream = sslStream;
                }

                if (_options.StreamWrapper != null)
                {
                    stream = _options.StreamWrapper(stream);
                }

                using (var connection = new Connection(s, stream))
                {
                    await funcAsync(connection);
                }
            }
        }

        public async Task<List<string>> AcceptConnectionSendCustomResponseAndCloseAsync(string response)
        {
            List<string> lines = null;

            // Note, we assume there's no request body.  
            // We'll close the connection after reading the request header and sending the response.
            await AcceptConnectionAsync(async connection =>
            {
                lines = await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
            });

            return lines;
        }

        public async Task<List<string>> AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode statusCode = HttpStatusCode.OK, string additionalHeaders = null, string content = null)
        {
            List<string> lines = null;

            // Note, we assume there's no request body.  
            // We'll close the connection after reading the request header and sending the response.
            await AcceptConnectionAsync(async connection =>
            {
                lines = await connection.ReadRequestHeaderAndSendResponseAsync(statusCode, additionalHeaders, content);
            });

            return lines;
        }

        // Stolen from HttpStatusDescription code in the product code
        private static string GetStatusDescription(HttpStatusCode code)
        {
            switch ((int)code)
            {
                case 100: return "Continue";
                case 101: return "Switching Protocols";
                case 102: return "Processing";

                case 200: return "OK";
                case 201: return "Created";
                case 202: return "Accepted";
                case 203: return "Non-Authoritative Information";
                case 204: return "No Content";
                case 205: return "Reset Content";
                case 206: return "Partial Content";
                case 207: return "Multi-Status";

                case 300: return "Multiple Choices";
                case 301: return "Moved Permanently";
                case 302: return "Found";
                case 303: return "See Other";
                case 304: return "Not Modified";
                case 305: return "Use Proxy";
                case 307: return "Temporary Redirect";

                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 402: return "Payment Required";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 406: return "Not Acceptable";
                case 407: return "Proxy Authentication Required";
                case 408: return "Request Timeout";
                case 409: return "Conflict";
                case 410: return "Gone";
                case 411: return "Length Required";
                case 412: return "Precondition Failed";
                case 413: return "Request Entity Too Large";
                case 414: return "Request-Uri Too Long";
                case 415: return "Unsupported Media Type";
                case 416: return "Requested Range Not Satisfiable";
                case 417: return "Expectation Failed";
                case 422: return "Unprocessable Entity";
                case 423: return "Locked";
                case 424: return "Failed Dependency";
                case 426: return "Upgrade Required"; // RFC 2817

                case 500: return "Internal Server Error";
                case 501: return "Not Implemented";
                case 502: return "Bad Gateway";
                case 503: return "Service Unavailable";
                case 504: return "Gateway Timeout";
                case 505: return "Http Version Not Supported";
                case 507: return "Insufficient Storage";
            }
            return null;
        }

        public static string GetHttpResponse(HttpStatusCode statusCode = HttpStatusCode.OK, string additionalHeaders = null, string content = null) =>
            $"HTTP/1.1 {(int)statusCode} {GetStatusDescription(statusCode)}\r\n" +
            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
            $"Content-Length: {(content == null ? 0 : content.Length)}\r\n" + 
            additionalHeaders +
            "\r\n" + 
            content;

        public class Options
        {
            public IPAddress Address { get; set; } = IPAddress.Loopback;
            public int ListenBacklog { get; set; } = 1;
            public bool UseSsl { get; set; } = false;
            public SslProtocols SslProtocols { get; set; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            public bool WebSocketEndpoint { get; set; } = false;
            public Func<Stream, Stream> StreamWrapper { get; set; }
        }

        public sealed class Connection : IDisposable
        {
            private Socket _socket;
            private Stream _stream;
            private StreamReader _reader;
            private StreamWriter _writer;

            public Connection(Socket socket, Stream stream)
            {
                _socket = socket;
                _stream = stream;

                _reader = new StreamReader(stream, Encoding.ASCII);
                _writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
            }

            public Socket Socket => _socket;
            public Stream Stream => _stream;
            public StreamReader Reader => _reader;
            public StreamWriter Writer => _writer;

            public void Dispose()
            {
                try
                {
                    // Try to shutdown the send side of the socket.
                    // This seems to help avoid connection reset issues caused by buffered data
                    // that has not been sent/acked when the graceful shutdown timeout expires.
                    // This may throw if the socket was already closed, so eat any exception.
                    _socket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception) { }

                _reader.Dispose();
                _writer.Dispose();
                _stream.Dispose();
                _socket.Dispose();
            }

            public async Task<List<string>> ReadRequestHeaderAsync()
            {
                var lines = new List<string>();
                string line;
                while (!string.IsNullOrEmpty(line = await _reader.ReadLineAsync().ConfigureAwait(false)))
                {
                    lines.Add(line);
                }

                return lines;
            }

            public async Task SendResponseAsync(HttpStatusCode statusCode = HttpStatusCode.OK, string additionalHeaders = null, string content = null)
            {
                await _writer.WriteAsync(GetHttpResponse(statusCode, additionalHeaders, content));
            }

            public async Task<List<string>> ReadRequestHeaderAndSendCustomResponseAsync(string response)
            {
                List<string> lines = await ReadRequestHeaderAsync().ConfigureAwait(false);
                await _writer.WriteAsync(response);
                return lines;
            }

            public async Task<List<string>> ReadRequestHeaderAndSendResponseAsync(HttpStatusCode statusCode = HttpStatusCode.OK, string additionalHeaders = null, string content = null)
            {
                List<string> lines = await ReadRequestHeaderAsync().ConfigureAwait(false);
                await SendResponseAsync(statusCode, additionalHeaders, content);
                return lines;
            }
        }
    }
}
