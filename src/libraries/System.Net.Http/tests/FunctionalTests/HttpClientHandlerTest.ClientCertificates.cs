// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

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

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task Automatic_SSLBackendNotSupported_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task Manual_SSLBackendNotSupported_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ClientCertificates.Add(Configuration.Certificates.GetClientCertificate());
            using (HttpClient client = CreateHttpClient(handler))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        private HttpClient CreateHttpClientWithCert(X509Certificate2 cert)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            Assert.NotNull(cert);
            handler.ClientCertificates.Add(cert);
            Assert.True(handler.ClientCertificates.Contains(cert));

            return CreateHttpClient(handler);
        }
            
        [Theory]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        public async Task Manual_CertificateOnlySentWhenValid_Success(int certIndex, bool serverExpectsClientCertificate)
        {
            if (!BackendSupportsCustomCertificateHandling || IsCurlHandler) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(Manual_CertificateOnlySentWhenValid_Success)}()");
                return;
            }

            var options = new LoopbackServer.Options { UseSsl = true };

            X509Certificate2 GetClientCertificate(int certIndex)
            {
                X509Certificate2 cert = null;

                // Get client certificate. RemoteInvoke doesn't allow easy marshaling of complex types.
                // So we have to select the certificate at this point in the test execution.
                if (certIndex == 1)
                {
                    // This is a valid client cert since it has an EKU with a ClientAuthentication OID.
                    cert = Configuration.Certificates.GetClientCertificate();
                }
                else if (certIndex == 2)
                {
                    // This is a valid client cert since it has no EKU thus all usages are permitted.
                    cert = Configuration.Certificates.GetNoEKUCertificate();
                }
                else if (certIndex == 3)
                {
                    // This is an invalid client cert since it has an EKU but is missing ClientAuthentication OID.
                    cert = Configuration.Certificates.GetServerCertificate();
                }

                return cert;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (X509Certificate2 cert = GetClientCertificate(certIndex))
                using (HttpClient client = CreateHttpClientWithCert(cert))
                
                await TestHelper.WhenAllCompletedOrAnyFailed(
                    client.GetStringAsync(url),
                    server.AcceptConnectionAsync(async connection =>
                    {
                        SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);
                        if (serverExpectsClientCertificate)
                        {
                            _output.WriteLine(
                                "Client cert: {0}",
                                ((X509Certificate2)sslStream.RemoteCertificate).GetNameInfo(X509NameType.SimpleName, false));
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
            if (!BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(Manual_CertificateSentMatchesCertificateReceived_Success)}()");
                return;
            }

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

        [ActiveIssue(37336)]
        [Theory]
        [InlineData(ClientCertificateOption.Manual)]
        [InlineData(ClientCertificateOption.Automatic)]
        public async Task AutomaticOrManual_DoesntFailRegardlessOfWhetherClientCertsAreAvailable(ClientCertificateOption mode)
        {
            if (!BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(AutomaticOrManual_DoesntFailRegardlessOfWhetherClientCertsAreAvailable)}()");
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                handler.ClientCertificateOptions = mode;

                await LoopbackServer.CreateServerAsync(async server =>
                {
                    Task clientTask = client.GetStringAsync(server.Uri);
                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);
                        await connection.ReadRequestHeaderAndSendResponseAsync();
                    });

                    await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed();
                }, new LoopbackServer.Options { UseSsl = true });
            }
        }

        private bool BackendSupportsCustomCertificateHandling
        {
            get
            {
#if TargetsWindows
                return true;
#else
                if (UseSocketsHttpHandler)
                {
                    // Socket Handler is independent of platform curl.
                    return true;
                }

                return TestHelper.NativeHandlerSupportsSslConfiguration();
#endif
            }
        }
    }
}
