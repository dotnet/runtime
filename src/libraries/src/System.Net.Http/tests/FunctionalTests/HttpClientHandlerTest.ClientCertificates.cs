// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class HttpClientHandler_ClientCertificates_Test
    {
        public static bool CanTestCertificates =>
            Capability.IsTrustedRootCertificateInstalled() &&
            (BackendSupportsCustomCertificateHandling || Capability.AreHostsFileNamesInstalled());

        public static bool CanTestClientCertificates =>
            CanTestCertificates && BackendSupportsCustomCertificateHandling;

        public HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;
        [Fact]
        public void ClientCertificateOptions_Default()
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Equal(ClientCertificateOption.Manual, handler.ClientCertificateOptions);
            }
        }

        [Theory]
        [InlineData((ClientCertificateOption)2)]
        [InlineData((ClientCertificateOption)(-1))]
        public void ClientCertificateOptions_InvalidArg_ThrowsException(ClientCertificateOption option)
        {
            using (var handler = new HttpClientHandler())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.ClientCertificateOptions = option);
            }
        }

        [Theory]
        [InlineData(ClientCertificateOption.Automatic)]
        [InlineData(ClientCertificateOption.Manual)]
        public void ClientCertificateOptions_ValueArg_Roundtrips(ClientCertificateOption option)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = option;
                Assert.Equal(option, handler.ClientCertificateOptions);
            }
        }

        [Fact]
        public void ClientCertificates_ClientCertificateOptionsAutomatic_ThrowsException()
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
                Assert.Throws<InvalidOperationException>(() => handler.ClientCertificates);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task Automatic_SSLBackendNotSupported_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for the managed handler
            {
                return;
            }

            using (var client = new HttpClient(new HttpClientHandler() { ClientCertificateOptions = ClientCertificateOption.Automatic }))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task Manual_SSLBackendNotSupported_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for the managed handler
            {
                return;
            }

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(Configuration.Certificates.GetClientCertificate());
            using (var client = new HttpClient(handler))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task Manual_SendClientCertificateToRemoteServer_SentMatchesReceived()
        {
            if (!CanTestClientCertificates) // can't use [Conditional*] right now as it's evaluated at the wrong time for the managed handler
            {
                _output.WriteLine($"Skipping {nameof(Manual_CertificateSentMatchesCertificateReceived_Success)}()");
                return;
            }

            var handler = new HttpClientHandler();
            var cert = Configuration.Certificates.GetClientCertificate();
            handler.ClientCertificates.Add(cert);
            using (var client = new HttpClient(handler))
            {
                HttpResponseMessage response = await client.GetAsync(Configuration.Http.EchoClientCertificateRemoteServer);
                _output.WriteLine($"{(int)response.StatusCode} {response.ReasonPhrase}");
                string body = await response.Content.ReadAsStringAsync();
                _output.WriteLine(body);

                byte[] bytes = Convert.FromBase64String(body);
                var receivedCert = new X509Certificate2(bytes);
                Assert.Equal(cert, receivedCert);
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "dotnet/corefx #20010")]
        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(9543)] // fails sporadically with 'WinHttpException : The server returned an invalid or unrecognized response' or 'TaskCanceledException : A task was canceled'
        [Theory]
        [InlineData(6, false)]
        [InlineData(3, true)]
        public async Task Manual_CertificateSentMatchesCertificateReceived_Success(
            int numberOfRequests,
            bool reuseClient) // validate behavior with and without connection pooling, which impacts client cert usage
        {
            if (BackendDoesNotSupportCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for the managed handler
            {
                _output.WriteLine($"Skipping {nameof(Manual_CertificateSentMatchesCertificateReceived_Success)}()");
                return;
            }

            var options = new LoopbackServer.Options { UseSsl = true };

            Func<X509Certificate2, HttpClient> createClient = (cert) =>
            {
                var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = delegate { return true; } };
                handler.ClientCertificates.Add(cert);
                return new HttpClient(handler);
            };

            Func<HttpClient, Socket, Uri, X509Certificate2, Task> makeAndValidateRequest = async (client, server, url, cert) =>
            {
                await TestHelper.WhenAllCompletedOrAnyFailed(
                    client.GetStringAsync(url),
                    LoopbackServer.AcceptSocketAsync(server, async (socket, stream, reader, writer) =>
                    {
                        SslStream sslStream = Assert.IsType<SslStream>(stream);
                        Assert.Equal(cert, sslStream.RemoteCertificate);
                        await LoopbackServer.ReadWriteAcceptedAsync(socket, reader, writer);
                        return null;
                    }, options));
            };

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                if (reuseClient)
                {
                    using (X509Certificate2 cert = Configuration.Certificates.GetClientCertificate())
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
                }
                else
                {
                    for (int i = 0; i < numberOfRequests; i++)
                    {
                        using (X509Certificate2 cert = Configuration.Certificates.GetClientCertificate())
                        {
                            using (HttpClient client = createClient(cert))
                            {
                                await makeAndValidateRequest(client, server, url, cert);
                            }
                        }

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }, options);
        }

        private static bool BackendSupportsCustomCertificateHandling =>
            HttpClientHandler_ServerCertificates_Test.BackendSupportsCustomCertificateHandling;

        private static bool BackendDoesNotSupportCustomCertificateHandling => !BackendSupportsCustomCertificateHandling;
    }
}
