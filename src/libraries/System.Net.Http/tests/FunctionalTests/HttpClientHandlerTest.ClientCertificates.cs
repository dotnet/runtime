// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientHandler_ClientCertificates_Test : HttpClientTestBase
    {
        public bool CanTestCertificates =>
            Capability.IsTrustedRootCertificateInstalled() &&
            (BackendSupportsCustomCertificateHandling || Capability.AreHostsFileNamesInstalled());

        public bool CanTestClientCertificates =>
            CanTestCertificates && BackendSupportsCustomCertificateHandling;

        public HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;
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
            using (var client = new HttpClient(handler))
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
            using (var client = new HttpClient(handler))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public void Manual_SendClientCertificateWithClientAuthEKUToRemoteServer_OK()
        {
            if (!CanTestClientCertificates) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(Manual_SendClientCertificateWithClientAuthEKUToRemoteServer_OK)}()");
                return;
            }

            // UAP HTTP stack caches connections per-process. This causes interference when these tests run in
            // the same process as the other tests. Each test needs to be isolated to its own process.
            // See dicussion: https://github.com/dotnet/corefx/issues/21945
            RemoteInvoke(async useSocketsHttpHandlerString =>
            {
                var cert = Configuration.Certificates.GetClientCertificate();
                HttpClientHandler handler = CreateHttpClientHandler(useSocketsHttpHandlerString);
                handler.ClientCertificates.Add(cert);
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(Configuration.Http.EchoClientCertificateRemoteServer);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync();
                    byte[] bytes = Convert.FromBase64String(body);
                    var receivedCert = new X509Certificate2(bytes);
                    Assert.Equal(cert, receivedCert);

                    return SuccessExitCode;
                }
            }, UseSocketsHttpHandler.ToString()).Dispose();
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public void Manual_SendClientCertificateWithServerAuthEKUToRemoteServer_Forbidden()
        {
            if (!CanTestClientCertificates) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(Manual_SendClientCertificateWithServerAuthEKUToRemoteServer_Forbidden)}()");
                return;
            }

            // UAP HTTP stack caches connections per-process. This causes interference when these tests run in
            // the same process as the other tests. Each test needs to be isolated to its own process.
            // See dicussion: https://github.com/dotnet/corefx/issues/21945
            RemoteInvoke(async useSocketsHttpHandlerString =>
            {
                var cert = Configuration.Certificates.GetServerCertificate();
                HttpClientHandler handler = CreateHttpClientHandler(useSocketsHttpHandlerString);
                handler.ClientCertificates.Add(cert);
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(Configuration.Http.EchoClientCertificateRemoteServer);
                    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

                    return SuccessExitCode;
                }
            }, UseSocketsHttpHandler.ToString()).Dispose();
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public void Manual_SendClientCertificateWithNoEKUToRemoteServer_OK()
        {
            if (!CanTestClientCertificates) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(Manual_SendClientCertificateWithNoEKUToRemoteServer_OK)}()");
                return;
            }

            // UAP HTTP stack caches connections per-process. This causes interference when these tests run in
            // the same process as the other tests. Each test needs to be isolated to its own process.
            // See dicussion: https://github.com/dotnet/corefx/issues/21945
            RemoteInvoke(async useSocketsHttpHandlerString =>
            {
                var cert = Configuration.Certificates.GetNoEKUCertificate();
                HttpClientHandler handler = CreateHttpClientHandler(useSocketsHttpHandlerString);
                handler.ClientCertificates.Add(cert);
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(Configuration.Http.EchoClientCertificateRemoteServer);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync();
                    byte[] bytes = Convert.FromBase64String(body);
                    var receivedCert = new X509Certificate2(bytes);
                    Assert.Equal(cert, receivedCert);

                    return SuccessExitCode;
                }
            }, UseSocketsHttpHandler.ToString()).Dispose();
        }

        [ActiveIssue(30056, TargetFrameworkMonikers.Uap)]
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

            Func<X509Certificate2, HttpClient> createClient = (cert) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = delegate { return true; };
                handler.ClientCertificates.Add(cert);
                Assert.True(handler.ClientCertificates.Contains(cert));
                return new HttpClient(handler);
            };

            Func<HttpClient, LoopbackServer, Uri, X509Certificate2, Task> makeAndValidateRequest = async (client, server, url, cert) =>
            {
                await TestHelper.WhenAllCompletedOrAnyFailed(
                    client.GetStringAsync(url),
                    server.AcceptConnectionAsync(async connection =>
                    {
                        SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);

                        // We can't do Assert.Equal(cert, sslStream.RemoteCertificate) because
                        // on .NET Framework sslStream.RemoteCertificate is always an X509Certificate
                        // object which is not equal to the X509Certificate2 object we use in the tests.
                        // So, we'll just compare a few properties to make sure it's the right certificate.
                        Assert.Equal(cert.Subject, sslStream.RemoteCertificate.Subject);
                        Assert.Equal(cert.Issuer, sslStream.RemoteCertificate.Issuer);

                        await connection.ReadRequestHeaderAndSendResponseAsync(additionalHeaders: "Connection: close\r\n");
                    }));
            };

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (X509Certificate2 cert = Configuration.Certificates.GetClientCertificate())
                {
                    if (reuseClient)
                    {
                        using (HttpClient client = createClient(cert))
                        {
                            for (int i = 0; i < numberOfRequests; i++)
                            {
                                await makeAndValidateRequest(client, server, url, cert);

                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numberOfRequests; i++)
                        {
                            using (HttpClient client = createClient(cert))
                            {
                                await makeAndValidateRequest(client, server, url, cert);
                            }

                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                }
            }, options);
        }

        [ActiveIssue(30056, TargetFrameworkMonikers.Uap)]
        [Theory]
        [InlineData(ClientCertificateOption.Manual)]
        [InlineData(ClientCertificateOption.Automatic)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Fails with \"Authentication failed\" error.")]
        public async Task AutomaticOrManual_DoesntFailRegardlessOfWhetherClientCertsAreAvailable(ClientCertificateOption mode)
        {
            if (!BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(AutomaticOrManual_DoesntFailRegardlessOfWhetherClientCertsAreAvailable)}()");
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = delegate { return true; };
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
