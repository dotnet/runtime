// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract class HttpClientHandler_ClientCertificates_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ClientCertificateOptions_Default()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Equal(ClientCertificateOption.Manual, handler.ClientCertificateOptions);
            }
        }

        [Theory]
        [InlineData((ClientCertificateOption)2)]
        [InlineData((ClientCertificateOption)(-1))]
        public void ClientCertificateOptions_InvalidArg_ThrowsException(ClientCertificateOption option)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.ClientCertificateOptions = option);
            }
        }

        [Theory]
        [InlineData(ClientCertificateOption.Automatic)]
        [InlineData(ClientCertificateOption.Manual)]
        public void ClientCertificateOptions_ValueArg_Roundtrips(ClientCertificateOption option)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.ClientCertificateOptions = option;
                Assert.Equal(option, handler.ClientCertificateOptions);
            }
        }

        [Fact]
        public void ClientCertificates_ClientCertificateOptionsAutomatic_ThrowsException()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
                Assert.Throws<InvalidOperationException>(() => handler.ClientCertificates);
            }
        }

        private HttpClient CreateHttpClientWithCert(X509Certificate2 cert)
        {
            HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
            Assert.NotNull(cert);
            handler.ClientCertificates.Add(cert);
            Assert.True(handler.ClientCertificates.Contains(cert));

            return CreateHttpClient(handler);
        }

        [ConditionalTheory]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        public async Task Manual_CertificateOnlySentWhenValid_Success(int certIndex, bool serverExpectsClientCertificate)
        {
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/69238")]
            if (IsWinHttpHandler) throw new SkipTestException("https://github.com/dotnet/runtime/issues/69238");

            var options = new LoopbackServer.Options { UseSsl = true };

            X509Certificate2 GetClientCertificate(int certIndex) => certIndex switch
            {
                // This is a valid client cert since it has an EKU with a ClientAuthentication OID.
                1 => Configuration.Certificates.GetClientCertificate(),

                // This is a valid client cert since it has no EKU thus all usages are permitted.
                2 => Configuration.Certificates.GetNoEKUCertificate(),

                // This is an invalid client cert since it has an EKU but is missing ClientAuthentication OID.
                3 => Configuration.Certificates.GetServerCertificate(),
                _ => null
            };

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using X509Certificate2 cert = GetClientCertificate(certIndex);
                using HttpClient client = CreateHttpClientWithCert(cert);

                await TestHelper.WhenAllCompletedOrAnyFailed(
                    client.GetStringAsync(url),
                    server.AcceptConnectionAsync(async connection =>
                    {
                        SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);
                        if (serverExpectsClientCertificate)
                        {
                            _output.WriteLine(
                                "Client cert: {0}",
                                new X509Certificate2(sslStream.RemoteCertificate.Export(X509ContentType.Cert)).GetNameInfo(X509NameType.SimpleName, false));
                            Assert.Equal(cert, sslStream.RemoteCertificate);
                        }
                        else
                        {
                            Assert.Null(sslStream.RemoteCertificate);
                        }

                        await connection.ReadRequestHeaderAndSendResponseAsync(additionalHeaders: "Connection: close\r\n");
                    }));
            }, options);
        }

        [OuterLoop("Uses GC and waits for finalizers")]
        [Theory]
        [InlineData(6, false)]
        [InlineData(3, true)]
        public async Task Manual_CertificateSentMatchesCertificateReceived_Success(
            int numberOfRequests,
            bool reuseClient) // validate behavior with and without connection pooling, which impacts client cert usage
        {
            var options = new LoopbackServer.Options { UseSsl = true };

            async Task MakeAndValidateRequest(HttpClient client, LoopbackServer server, Uri url, X509Certificate2 cert)
            {
                await TestHelper.WhenAllCompletedOrAnyFailed(
                    client.GetStringAsync(url),
                    server.AcceptConnectionAsync(async connection =>
                    {
                        SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);
                        Assert.Equal(cert, sslStream.RemoteCertificate);

                        await connection.ReadRequestHeaderAndSendResponseAsync(additionalHeaders: "Connection: close\r\n");
                    }));
            };

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (X509Certificate2 cert = Configuration.Certificates.GetClientCertificate())
                {
                    if (reuseClient)
                    {
                        using (HttpClient client = CreateHttpClientWithCert(cert))
                        {
                            for (int i = 0; i < numberOfRequests; i++)
                            {
                                await MakeAndValidateRequest(client, server, url, cert);

                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numberOfRequests; i++)
                        {
                            using (HttpClient client = CreateHttpClientWithCert(cert))
                            {
                                await MakeAndValidateRequest(client, server, url, cert);
                            }

                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                }
            }, options);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/29419")]
        [Theory]
        [InlineData(ClientCertificateOption.Manual)]
        [InlineData(ClientCertificateOption.Automatic)]
        public async Task AutomaticOrManual_DoesntFailRegardlessOfWhetherClientCertsAreAvailable(ClientCertificateOption mode)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true))
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ClientCertificateOptions = mode;

                await LoopbackServer.CreateServerAsync(async server =>
                {
                    Task clientTask = client.GetStringAsync(server.Address);
                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);
                        await connection.ReadRequestHeaderAndSendResponseAsync();
                    });

                    await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed();
                }, new LoopbackServer.Options { UseSsl = true });
            }
        }
    }
}
