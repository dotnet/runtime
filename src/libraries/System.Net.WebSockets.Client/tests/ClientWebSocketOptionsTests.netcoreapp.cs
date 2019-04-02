// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Test.Common;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public partial class ClientWebSocketOptionsTests : ClientWebSocketTestBase
    {
        [ConditionalFact(nameof(WebSocketsSupported), nameof(ClientCertificatesSupported))]
        public void RemoteCertificateValidationCallback_Roundtrips()
        {
            using (var cws = new ClientWebSocket())
            {
                Assert.Null(cws.Options.RemoteCertificateValidationCallback);

                RemoteCertificateValidationCallback callback = delegate { return true; };
                cws.Options.RemoteCertificateValidationCallback = callback;
                Assert.Same(callback, cws.Options.RemoteCertificateValidationCallback);

                cws.Options.RemoteCertificateValidationCallback = null;
                Assert.Null(cws.Options.RemoteCertificateValidationCallback);
            }
        }

        [OuterLoop("Connects to remote service")]
        [ConditionalTheory(nameof(WebSocketsSupported), nameof(ClientCertificatesSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RemoteCertificateValidationCallback_PassedRemoteCertificateInfo(bool secure)
        {
            if (PlatformDetection.IsWindows7)
            {
                return; // [ActiveIssue(27846)]
            }

            bool callbackInvoked = false;

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.RemoteCertificateValidationCallback = (source, cert, chain, errors) =>
                    {
                        Assert.NotNull(source);
                        Assert.NotNull(cert);
                        Assert.NotNull(chain);
                        Assert.NotEqual(SslPolicyErrors.None, errors);
                        callbackInvoked = true;
                        return true;
                    };
                    await cws.ConnectAsync(uri, cts.Token);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Assert.True(await LoopbackHelper.WebSocketHandshakeAsync(connection));
            }),
            new LoopbackServer.Options { UseSsl = secure, WebSocketEndpoint = true });

            Assert.Equal(secure, callbackInvoked);
        }

        [OuterLoop("Connects to remote service")]
        [ConditionalFact(nameof(WebSocketsSupported), nameof(ClientCertificatesSupported))]
        public async Task ClientCertificates_ValidCertificate_ServerReceivesCertificateAndConnectAsyncSucceeds()
        {
            if (PlatformDetection.IsWindows7)
            {
                return; // [ActiveIssue(27846)]
            }

            using (X509Certificate2 clientCert = Test.Common.Configuration.Certificates.GetClientCertificate())
            {
                await LoopbackServer.CreateClientAndServerAsync(async uri =>
                {
                    using (var clientSocket = new ClientWebSocket())
                    using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                    {
                        clientSocket.Options.ClientCertificates.Add(clientCert);
                        clientSocket.Options.RemoteCertificateValidationCallback = delegate { return true; };
                        await clientSocket.ConnectAsync(uri, cts.Token);
                    }
                }, server => server.AcceptConnectionAsync(async connection =>
                {
                    // Validate that the client certificate received by the server matches the one configured on
                    // the client-side socket.
                    SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);
                    Assert.NotNull(sslStream.RemoteCertificate);
                    Assert.Equal(clientCert, new X509Certificate2(sslStream.RemoteCertificate));

                    // Complete the WebSocket upgrade over the secure channel. After this is done, the client-side
                    // ConnectAsync should complete.
                    Assert.True(await LoopbackHelper.WebSocketHandshakeAsync(connection));
                }), new LoopbackServer.Options { UseSsl = true, WebSocketEndpoint = true });
            }
        }

        [ConditionalTheory(nameof(WebSocketsSupported))]
        [InlineData("ws://")]
        [InlineData("wss://")]
        public async Task NonSecureConnect_ConnectThruProxy_CONNECTisUsed(string connectionType)
        {
            if (PlatformDetection.IsWindows7)
            {
                return; // [ActiveIssue(27846)]
            }

            bool connectionAccepted = false;

            await LoopbackServer.CreateClientAndServerAsync(async proxyUri =>
            {
                using (var cws = new ClientWebSocket())
                {
                    cws.Options.Proxy = new WebProxy(proxyUri);
                    try { await cws.ConnectAsync(new Uri(connectionType + Guid.NewGuid().ToString("N")), default); } catch { }
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Assert.Contains("CONNECT", await connection.ReadLineAsync());
                connectionAccepted = true;
            }));

            Assert.True(connectionAccepted);
        }
    }
}
