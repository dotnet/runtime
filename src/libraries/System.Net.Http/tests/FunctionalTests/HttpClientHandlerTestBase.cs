// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        public readonly ITestOutputHelper _output;

        protected virtual bool UseHttp2 => false;

        protected virtual bool UseCustomConnect => false;

        public HttpClientHandlerTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        protected Version VersionFromUseHttp2 => GetVersion(UseHttp2);

        protected static Version GetVersion(bool http2) => http2 ? new Version(2, 0) : HttpVersion.Version11;

        protected virtual HttpClient CreateHttpClient() => CreateHttpClient(CreateHttpClientHandler());

        protected HttpClient CreateHttpClient(HttpMessageHandler handler) =>
            new HttpClient(handler) { DefaultRequestVersion = VersionFromUseHttp2 };

        protected static HttpClient CreateHttpClient(string useHttp2String) =>
            CreateHttpClient(CreateHttpClientHandler(useHttp2String), useHttp2String);

        protected static HttpClient CreateHttpClient(HttpMessageHandler handler, string useHttp2String) =>
            new HttpClient(handler) { DefaultRequestVersion = GetVersion(bool.Parse(useHttp2String)) };

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseHttp2, UseCustomConnect);

        protected static HttpClientHandler CreateHttpClientHandler(string useHttp2LoopbackServerString) =>
            CreateHttpClientHandler(bool.Parse(useHttp2LoopbackServerString));

        protected static HttpClientHandler CreateHttpClientHandler(bool useHttp2LoopbackServer = false, bool useCustomConnectCallback = false)
        {
            HttpClientHandler handler = new HttpClientHandler();

            if (useHttp2LoopbackServer)
            {
                TestHelper.EnableUnencryptedHttp2IfNecessary(handler);
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            }

            if (useCustomConnectCallback)
            {
                var socketsHttpHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                socketsHttpHandler.ConnectCallback = async (string host, int port, CancellationToken cancellationToken) =>
                {
                    var innerStream = await ConnectHelper.ConnectAsync(host, port, cancellationToken);
                    return new System.IO.DelegateStream(
                        canReadFunc: () => innerStream.CanRead,
                        canSeekFunc: () => innerStream.CanSeek,
                        canWriteFunc: () => innerStream.CanWrite,
                        flushFunc: innerStream.Flush,
                        flushAsyncFunc: innerStream.FlushAsync,
                        lengthFunc: () => innerStream.Length,
                        positionGetFunc: () => innerStream.Position,
                        positionSetFunc: (value) => innerStream.Position = value,
                        readFunc: innerStream.Read,
                        readAsyncFunc: innerStream.ReadAsync,
                        seekFunc: innerStream.Seek,
                        setLengthFunc: innerStream.SetLength,
                        writeFunc: innerStream.Write,
                        writeAsyncFunc: innerStream.WriteAsync,
                        disposeFunc: (value) => { if (value) { innerStream.Dispose(); } });
                };
            }

            return handler;
        }

        protected static object GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            FieldInfo field = typeof(HttpClientHandler).GetField("_socketsHttpHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(handler);
        }

        protected LoopbackServerFactory LoopbackServerFactory =>
#if NETCOREAPP
            UseHttp2 ?
                (LoopbackServerFactory)Http2LoopbackServerFactory.Singleton :
#endif
                Http11LoopbackServerFactory.Singleton;

        // For use by remote server tests

        public static readonly IEnumerable<object[]> RemoteServersMemberData = Configuration.Http.RemoteServersMemberData;

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer)
        {
            return CreateHttpClientForRemoteServer(remoteServer, CreateHttpClientHandler());
        }

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer, HttpClientHandler httpClientHandler)
        {
            HttpMessageHandler wrappedHandler = httpClientHandler;

            // ActiveIssue #39293: WinHttpHandler will downgrade to 1.1 if you set Transfer-Encoding: chunked.
            // So, skip this verification if we're not using SocketsHttpHandler.
            if (PlatformDetection.SupportsAlpn)
            {
                wrappedHandler = new VersionCheckerHttpHandler(httpClientHandler, remoteServer.HttpVersion);
            }

            return new HttpClient(wrappedHandler) { DefaultRequestVersion = remoteServer.HttpVersion };
        }

        private sealed class VersionCheckerHttpHandler : DelegatingHandler
        {
            private readonly Version _expectedVersion;

            public VersionCheckerHttpHandler(HttpMessageHandler innerHandler, Version expectedVersion)
                : base(innerHandler)
            {
                _expectedVersion = expectedVersion;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected request version: expected {_expectedVersion}, saw {request.Version}");
                }

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                if (response.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected response version: expected {_expectedVersion}, saw {response.Version}");
                }

                return response;
            }
        }

        static class ConnectHelper
        {
            public static async ValueTask<Stream> ConnectAsync(string host, int port, CancellationToken cancellationToken)
            {
                // Rather than creating a new Socket and calling ConnectAsync on it, we use the static
                // Socket.ConnectAsync with a SocketAsyncEventArgs, as we can then use Socket.CancelConnectAsync
                // to cancel it if needed.
                var saea = new ConnectEventArgs();
                try
                {
                    saea.Initialize(cancellationToken);

                    // Configure which server to which to connect.
                    saea.RemoteEndPoint = new DnsEndPoint(host, port);

                    // Initiate the connection.
                    if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, saea))
                    {
                        // Connect completing asynchronously. Enable it to be canceled and wait for it.
                        using (cancellationToken.UnsafeRegister(s => Socket.CancelConnectAsync((SocketAsyncEventArgs)s), saea))
                        {
                            await saea.Builder.Task.ConfigureAwait(false);
                        }
                    }
                    else if (saea.SocketError != SocketError.Success)
                    {
                        // Connect completed synchronously but unsuccessfully.
                        throw new SocketException((int)saea.SocketError);
                    }

                    // Configure the socket and return a stream for it.
                    Socket socket = saea.ConnectSocket;
                    socket.NoDelay = true;
                    return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception error) when (!(error is OperationCanceledException))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(null, error, cancellationToken);
                    }
                    throw new HttpRequestException(error.Message, error);
                }
                finally
                {
                    saea.Dispose();
                }
            }

            /// <summary>SocketAsyncEventArgs that carries with it additional state for a Task builder and a CancellationToken.</summary>
            private sealed class ConnectEventArgs : SocketAsyncEventArgs
            {
                public AsyncTaskMethodBuilder Builder { get; private set; }
                public CancellationToken CancellationToken { get; private set; }

                public void Initialize(CancellationToken cancellationToken)
                {
                    CancellationToken = cancellationToken;
                    AsyncTaskMethodBuilder b = default;
                    _ = b.Task; // force initialization
                    Builder = b;
                }

                protected override void OnCompleted(SocketAsyncEventArgs _)
                {
                    switch (SocketError)
                    {
                        case SocketError.Success:
                            Builder.SetResult();
                            break;

                        case SocketError.OperationAborted:
                        case SocketError.ConnectionAborted:
                            if (CancellationToken.IsCancellationRequested)
                            {
                                Builder.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(CancellationToken)));
                                break;
                            }
                            goto default;

                        default:
                            Builder.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new SocketException((int)SocketError)));
                            break;
                    }
                }
            }
        }
        }
}
