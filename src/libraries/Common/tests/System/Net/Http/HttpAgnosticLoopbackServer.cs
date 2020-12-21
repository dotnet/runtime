// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Test.Common
{
    public class HttpAgnosticLoopbackServer : GenericLoopbackServer, IDisposable
    {
        private Socket _listenSocket;
        private HttpAgnosticOptions _options;
        private Uri _uri;

        public override Uri Address => _uri;

        public static HttpAgnosticLoopbackServer CreateServer()
        {
            return new HttpAgnosticLoopbackServer(new HttpAgnosticOptions());
        }

        public static HttpAgnosticLoopbackServer CreateServer(HttpAgnosticOptions options)
        {
            return new HttpAgnosticLoopbackServer(options);
        }

        private HttpAgnosticLoopbackServer(HttpAgnosticOptions options)
        {
            _options = options;
            _listenSocket = new Socket(_options.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(new IPEndPoint(_options.Address, 0));
            _listenSocket.Listen(_options.ListenBacklog);

            var localEndPoint = (IPEndPoint)_listenSocket.LocalEndPoint;
            var host = _options.Address.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{localEndPoint.Address}]" : localEndPoint.Address.ToString();
            var scheme = _options.UseSsl ? "https" : "http";
            _uri = new Uri($"{scheme}://{host}:{localEndPoint.Port}/");
        }

        public override void Dispose()
        {
            if (_listenSocket != null)
            {
                _listenSocket.Dispose();
                _listenSocket = null;
            }
        }

        public override async Task<GenericLoopbackConnection> EstablishGenericConnectionAsync()
        {
            Socket socket = await _listenSocket.AcceptAsync().ConfigureAwait(false);
            Stream stream = new NetworkStream(socket, ownsSocket: true);

            var options = new GenericLoopbackOptions()
            {
                Address = _options.Address,
                SslProtocols = _options.SslProtocols,
                UseSsl = false,
                ListenBacklog = _options.ListenBacklog
            };

            GenericLoopbackConnection connection = null;

            try
            {
                if (_options.UseSsl)
                {
                    var sslStream = new SslStream(stream, false, delegate { return true; });

                    using (X509Certificate2 cert = Configuration.Certificates.GetServerCertificate())
                    {
                        SslServerAuthenticationOptions sslOptions = new SslServerAuthenticationOptions();

                        sslOptions.EnabledSslProtocols = _options.SslProtocols;
                        sslOptions.ApplicationProtocols = _options.SslApplicationProtocols;
                        sslOptions.ServerCertificate = cert;

                        await sslStream.AuthenticateAsServerAsync(sslOptions, CancellationToken.None).ConfigureAwait(false);
                    }

                    stream = sslStream;
                    if (sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2)
                    {
                        // Do not pass original options so the CreateConnectionAsync won't try to do ALPN again.
                        return connection = await Http2LoopbackServerFactory.Singleton.CreateConnectionAsync(socket, stream, options).ConfigureAwait(false);
                    }
                    if (sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http11 ||
                        sslStream.NegotiatedApplicationProtocol == default)
                    {
                        // Do not pass original options so the CreateConnectionAsync won't try to do ALPN again.
                        return connection = await Http11LoopbackServerFactory.Singleton.CreateConnectionAsync(socket, stream, options).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new Exception($"Unsupported negotiated protocol {sslStream.NegotiatedApplicationProtocol}");
                    }
                }

                if (_options.ClearTextVersion is null)
                {
                    throw new Exception($"HTTP server does not accept clear text connections, either set '{nameof(HttpAgnosticOptions.UseSsl)}' or set up '{nameof(HttpAgnosticOptions.ClearTextVersion)}' in server options.");
                }

                var buffer = new byte[24];
                var position = 0;
                while (position < buffer.Length)
                {
                    var readBytes = await stream.ReadAsync(buffer, position, buffer.Length - position).ConfigureAwait(false);
                    if (readBytes == 0)
                    {
                        break;
                    }
                    position += readBytes;
                }
                
                var memory = new Memory<byte>(buffer, 0, position);
                stream = new ReturnBufferStream(stream, memory);

                var prefix = Text.Encoding.ASCII.GetString(memory.Span);
                if (prefix == Http2LoopbackConnection.Http2Prefix)
                {
                    if (_options.ClearTextVersion == HttpVersion.Version20 || _options.ClearTextVersion == HttpVersion.Unknown)
                    {
                        return connection = await Http2LoopbackServerFactory.Singleton.CreateConnectionAsync(socket, stream, options).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (_options.ClearTextVersion == HttpVersion.Version11 || _options.ClearTextVersion == HttpVersion.Unknown)
                    {
                        return connection = await Http11LoopbackServerFactory.Singleton.CreateConnectionAsync(socket, stream, options).ConfigureAwait(false);
                    }
                }

                throw new Exception($"HTTP/{_options.ClearTextVersion} server cannot establish connection due to unexpected data: '{prefix}'");
            }
            catch
            {            
                connection?.Dispose();
                connection = null;
                stream.Dispose();
                throw;
            }
            finally
            {
                if (connection != null)
                {
                    await connection.InitializeConnectionAsync().ConfigureAwait(false);
                }
            }
        }

        public override async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            using (GenericLoopbackConnection connection = await EstablishGenericConnectionAsync().ConfigureAwait(false))
            {
                return await connection.HandleRequestAsync(statusCode, headers, content).ConfigureAwait(false);
            }
        }

        public override async Task AcceptConnectionAsync(Func<GenericLoopbackConnection, Task> funcAsync)
        {
            using (GenericLoopbackConnection connection = await EstablishGenericConnectionAsync().ConfigureAwait(false))
            {
                await funcAsync(connection).ConfigureAwait(false);
            }
        }

        public static Task CreateClientAndServerAsync(Func<Uri, Task> clientFunc, Func<GenericLoopbackServer, Task> serverFunc, int timeout = 60_000)
        {
            return CreateClientAndServerAsync(clientFunc, serverFunc, null, timeout);
        }

        public static async Task CreateClientAndServerAsync(Func<Uri, Task> clientFunc, Func<GenericLoopbackServer, Task> serverFunc, HttpAgnosticOptions httpOptions, int timeout = 60_000)
        {
            using (var server = HttpAgnosticLoopbackServer.CreateServer(httpOptions ?? new HttpAgnosticOptions()))
            {
                Task clientTask = clientFunc(server.Address);
                Task serverTask = serverFunc(server);

                await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed(timeout).ConfigureAwait(false);
            }
        }
    }

    public class HttpAgnosticOptions : GenericLoopbackOptions
    {
        // Default null will raise an exception for any clear text protocol version
        // Use HttpVersion.Unknown to use protocol version detection for clear text.
        public Version ClearTextVersion { get; set; }
        public List<SslApplicationProtocol> SslApplicationProtocols { get; set; }
    }

    public sealed class HttpAgnosticLoopbackServerFactory : LoopbackServerFactory
    {
        public static readonly HttpAgnosticLoopbackServerFactory Singleton = new HttpAgnosticLoopbackServerFactory();

        public static async Task CreateServerAsync(Func<HttpAgnosticLoopbackServer, Uri, Task> funcAsync, int millisecondsTimeout = 60_000)
        {
            using (var server = HttpAgnosticLoopbackServer.CreateServer())
            {
                await funcAsync(server, server.Address).TimeoutAfter(millisecondsTimeout).ConfigureAwait(false);
            }
        }

        public override GenericLoopbackServer CreateServer(GenericLoopbackOptions options = null)
        {
            return HttpAgnosticLoopbackServer.CreateServer(CreateOptions(options));
        }

        public override Task<GenericLoopbackConnection> CreateConnectionAsync(Socket socket, Stream stream, GenericLoopbackOptions options = null)
        {
            // This method is always unacceptable to call for an agnostic server.
            throw new NotImplementedException("HttpAgnosticLoopbackServerFactory cannot create connection.");
        }

        private static HttpAgnosticOptions CreateOptions(GenericLoopbackOptions options)
        {
            HttpAgnosticOptions httpOptions = new HttpAgnosticOptions();
            if (options != null)
            {
                httpOptions.Address = options.Address;
                httpOptions.UseSsl = options.UseSsl;
                httpOptions.SslProtocols = options.SslProtocols;
                httpOptions.ListenBacklog = options.ListenBacklog;
            }
            return httpOptions;
        }

        public override async Task CreateServerAsync(Func<GenericLoopbackServer, Uri, Task> funcAsync, int millisecondsTimeout = 60_000, GenericLoopbackOptions options = null)
        {
            using (var server = CreateServer(options))
            {
                await funcAsync(server, server.Address).TimeoutAfter(millisecondsTimeout).ConfigureAwait(false);
            }
        }

        public override Version Version => HttpVersion.Unknown;
    }

    internal class ReturnBufferStream : Stream
    {
        private Stream _stream;
        private Memory<byte> _buffer;

        public ReturnBufferStream(Stream stream, Memory<byte> buffer)
        {
            _stream = stream;
            _buffer = buffer;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_buffer.IsEmpty)
            {
                return _stream.Read(buffer, offset, count);
            }

            var fromBuffer = Math.Min(_buffer.Length, count);
            _buffer.Slice(0, fromBuffer).CopyTo(new Memory<byte>(buffer, offset, count));
            _buffer = _buffer.Slice(fromBuffer);
            offset += fromBuffer;
            count -= fromBuffer;

            if (count > 0)
            {
                return _stream.Read(buffer, offset, count) + fromBuffer;
            }

            return fromBuffer;
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }
        public override void Flush() => _stream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => _stream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            _stream.Dispose();
        }
    }
}
