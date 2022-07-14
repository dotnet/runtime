// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    public class ClientCertificateTest : BaseCertificateTest
    {
        public static bool DowngradeToHTTP1IfClientCertSet => PlatformDetection.WindowsVersion < 2004;

        public ClientCertificateTest(ITestOutputHelper output) : base(output)
        { }

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
                    using (HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, address) { Version = HttpVersion20.Value }))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.True(_validationCallbackHistory.WasCalled);
                        Assert.NotEmpty(_validationCallbackHistory.CertificateChain);
                        Assert.Equal(Test.Common.Configuration.Certificates.GetServerCertificate(), _validationCallbackHistory.CertificateChain[0]);
                    }
                },
                async s =>
                {
                    await using (LoopbackServer.Connection connection = await s.EstablishConnectionAsync().ConfigureAwait(false))
                    {
                        SslStream sslStream = connection.Stream as SslStream;
                        Assert.NotNull(sslStream);
                        Assert.True(sslStream.IsMutuallyAuthenticated);
                        Assert.Equal(clientCert, sslStream.RemoteCertificate);
                        await connection.ReadRequestHeaderAndSendResponseAsync(HttpStatusCode.OK);
                    }
                }, new LoopbackServer.Options { UseSsl = true });
        }

// Disabling it for full .Net Framework due to a missing ALPN API which leads to a protocol downgrade
#if !NETFRAMEWORK
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version19573OrGreater))]
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
                    using (HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, address) { Version = HttpVersion20.Value }))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.True(_validationCallbackHistory.WasCalled);
                        Assert.NotEmpty(_validationCallbackHistory.CertificateChain);
                        Assert.Equal(Test.Common.Configuration.Certificates.GetServerCertificate(), _validationCallbackHistory.CertificateChain[0]);
                    }
                },
                async s =>
                {
                    await using (Http2LoopbackConnection connection = await s.EstablishConnectionAsync().ConfigureAwait(false))
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
#endif
        [OuterLoop]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1607OrGreater))]
        public async Task UseClientCertOnHttp2_OSSupportsItButCertNotSet_SuccessWithOneWayAuth()
        {
            WinHttpHandler handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = CustomServerCertificateValidationCallback;
            string payload = "Mutual Authentication Test";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Test.Common.Configuration.Http.Http2RemoteEchoServer) { Version = HttpVersion20.Value };
            request.Content = new StringContent(payload);
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(HttpVersion20.Value, response.Version);
                string responsePayload = await response.Content.ReadAsStringAsync();
                var responseContent = JsonConvert.DeserializeAnonymousType(responsePayload, new { Method = "_", BodyContent = "_", ClientCertificatePresent = "_", ClientCertificate = "_" });
                Assert.Equal("POST", responseContent.Method);
                Assert.Equal(payload, responseContent.BodyContent);
                Assert.Equal("false", responseContent.ClientCertificatePresent);
                Assert.Null(responseContent.ClientCertificate);
                Assert.True(_validationCallbackHistory.WasCalled);
                Assert.NotEmpty(_validationCallbackHistory.CertificateChain);
                ConfirmValidCertificate("*.azurewebsites.net");
            };
        }
    }
}
