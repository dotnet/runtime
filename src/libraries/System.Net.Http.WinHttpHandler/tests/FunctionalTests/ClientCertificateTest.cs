// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    public class ClientCertificateTest : BaseCertificateTest
    {
        public static bool DowngradeToHTTP1IfClientCertSet => PlatformDetection.WindowsVersion < 2004;

        [ConditionalFact(typeof(ServerCertificateTest), nameof(DowngradeToHTTP1IfClientCertSet))]
        public async Task UseClientCertOnHttp2_DowngradedToHttp1MutualAuth_Success()
        {
            using X509Certificate2 clientCert = Test.Common.Configuration.Certificates.GetClientCertificate();
            await LoopbackServer.CreateClientAndServerAsync(
                async address =>
                {
                    var handler = new WinHttpHandler();
                    handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
                    handler.ClientCertificates.Add(clientCert);
                    handler.ClientCertificateOption = ClientCertificateOption.Manual;
                    using (var client = new HttpClient(handler))
                    using (HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, address) { Version = HttpVersion.Version20 }))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.True(_validationCallbackHistory.WasCalled);
                        Assert.NotEmpty(_validationCallbackHistory.CertificateChain);
                        Assert.Equal(Test.Common.Configuration.Certificates.GetServerCertificate(), _validationCallbackHistory.CertificateChain[0]);
                    }
                },
                async s =>
                {
                    using (LoopbackServer.Connection connection = await s.EstablishConnectionAsync().ConfigureAwait(false))
                    {
                        SslStream sslStream = connection.Stream as SslStream;
                        Assert.NotNull(sslStream);
                        Assert.True(sslStream.IsMutuallyAuthenticated);
                        Assert.Equal(clientCert, sslStream.RemoteCertificate);
                        await connection.ReadRequestHeaderAndSendResponseAsync(HttpStatusCode.OK);
                    }
                }, new LoopbackServer.Options { UseSsl = true });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version2004OrGreater))]
        public async Task UseClientCertOnHttp2_OSSupportsIt_Success()
        {
            using X509Certificate2 clientCert = Test.Common.Configuration.Certificates.GetClientCertificate();
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async address =>
                {
                    var handler = new WinHttpHandler();
                    handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
                    handler.ClientCertificates.Add(clientCert);
                    handler.ClientCertificateOption = ClientCertificateOption.Manual;
                    using (var client = new HttpClient(handler))
                    using (HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, address) { Version = HttpVersion.Version20 }))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.True(_validationCallbackHistory.WasCalled);
                        Assert.NotEmpty(_validationCallbackHistory.CertificateChain);
                        Assert.Equal(Test.Common.Configuration.Certificates.GetServerCertificate(), _validationCallbackHistory.CertificateChain[0]);
                    }
                },
                async s =>
                {
                    using (Http2LoopbackConnection connection = await s.EstablishConnectionAsync().ConfigureAwait(false))
                    {
                        SslStream sslStream = connection.Stream as SslStream;
                        Assert.NotNull(sslStream);
                        Assert.True(sslStream.IsMutuallyAuthenticated);
                        Assert.Equal(clientCert, sslStream.RemoteCertificate);

                        int streamId = await connection.ReadRequestHeaderAsync();
                        await connection.SendDefaultResponseAsync(streamId);
                    }
                }, new Http2Options { ClientCertificateRequired = true });
        }

        [Fact]
        public async Task UseClientCertOnHttp2_OSSupportsItButCertNotSet_SuccessWithOneWayAuth()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async address =>
                {
                    var handler = new WinHttpHandler();
                    handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
                    using (var client = new HttpClient(handler))
                    using (HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, address) { Version = HttpVersion.Version20 }))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.True(_validationCallbackHistory.WasCalled);
                        Assert.NotEmpty(_validationCallbackHistory.CertificateChain);
                        Assert.Equal(Test.Common.Configuration.Certificates.GetServerCertificate(), _validationCallbackHistory.CertificateChain[0]);
                    }
                },
                async s =>
                {
                    using (Http2LoopbackConnection connection = await s.EstablishConnectionAsync().ConfigureAwait(false))
                    {
                        SslStream sslStream = connection.Stream as SslStream;
                        Assert.NotNull(sslStream);
                        Assert.False(sslStream.IsMutuallyAuthenticated);
                        Assert.Null(sslStream.RemoteCertificate);

                        int streamId = await connection.ReadRequestHeaderAsync();
                        await connection.SendDefaultResponseAsync(streamId);
                    }
                }, new Http2Options { ClientCertificateRequired = true });
        }
    }
}
