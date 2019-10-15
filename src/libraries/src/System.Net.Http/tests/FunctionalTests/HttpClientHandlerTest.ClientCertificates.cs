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
        public bool CanTestCertificates =>
            Capability.IsTrustedRootCertificateInstalled() &&
            (BackendSupportsCustomCertificateHandling || Capability.AreHostsFileNamesInstalled());

        public bool CanTestClientCertificates =>
            CanTestCertificates && BackendSupportsCustomCertificateHandling;

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

        public static IEnumerable<object[]> SelectClientCertificateForRemoteServer_MemberData()
        {
            yield return new object[] { 1, HttpStatusCode.OK };
            yield return new object[] { 2, HttpStatusCode.OK };
            yield return new object[] { 3, HttpStatusCode.Forbidden };
        }

        [OuterLoop("Uses external server")]
        [Theory]
        [MemberData(nameof(SelectClientCertificateForRemoteServer_MemberData))]
        public void Manual_SelectClientCertificateForRemoteServer_ServerOnlyReceivesValidClientCert(
            int certIndex, HttpStatusCode expectedStatusCode)
        {
            if (!CanTestClientCertificates) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(Manual_SelectClientCertificateForRemoteServer_ServerOnlyReceivesValidClientCert)}()");
                return;
            }

            // UAP HTTP stack caches connections per-process. This causes interference when these tests run in
            // the same process as the other tests. Each test needs to be isolated to its own process.
            // See dicussion: https://github.com/dotnet/corefx/issues/21945
            RemoteExecutor.Invoke((certIndexString, expectedStatusCodeString, useSocketsHttpHandlerString, useHttp2String) =>
            {
                X509Certificate2 clientCert = null;

                // Get client certificate. RemoteInvoke doesn't allow easy marshaling of complex types.
                // So we have to select the certificate at this point in the test execution.
                if (certIndexString == "1")
                {
                    // This is a valid client cert since it has an EKU with a ClientAuthentication OID.
                    clientCert = Configuration.Certificates.GetClientCertificate();
                }
                else if (certIndexString == "2")
                {
                    // This is a valid client cert since it has no EKU thus all usages are permitted.
                    clientCert = Configuration.Certificates.GetNoEKUCertificate();
                }
                else if (certIndexString == "3")
                {
                    // This is an invalid client cert since it has an EKU but is missing ClientAuthentication OID.
                    clientCert = Configuration.Certificates.GetServerCertificate();
                }

                Assert.NotNull(clientCert);

                var statusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), expectedStatusCodeString);
                HttpClientHandler handler = CreateHttpClientHandler(useSocketsHttpHandlerString, useHttp2String);
                handler.ClientCertificates.Add(clientCert);
                using (HttpClient client = CreateHttpClient(handler, useHttp2String))
                {
                    var request = new HttpRequestMessage();
                    request.RequestUri = new Uri(Configuration.Http.EchoClientCertificateRemoteServer);

                    // Issue #35239. Force HTTP/1.1.
                    request.Version = new Version(1,1);
                    HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult(); // need a 4-arg overload of RemoteInvoke that returns a Task
                    Assert.Equal(statusCode, response.StatusCode);

                    if (statusCode == HttpStatusCode.OK)
                    {
                        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult(); // need a 4-arg overload of RemoteInvoke that returns a Task
                        byte[] bytes = Convert.FromBase64String(body);
                        var receivedCert = new X509Certificate2(bytes);
                        Assert.Equal(clientCert, receivedCert);
                    }

                    return RemoteExecutor.SuccessExitCode;
                }
            }, certIndex.ToString(), expectedStatusCode.ToString(), UseSocketsHttpHandler.ToString(), UseHttp2.ToString()).Dispose();
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

            HttpClient CreateClient(X509Certificate2 cert)
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                handler.ClientCertificates.Add(cert);
                Assert.True(handler.ClientCertificates.Contains(cert));
                return CreateHttpClient(handler);
            }

            async Task MakeAndValidateRequest(HttpClient client, LoopbackServer server, Uri url, X509Certificate2 cert)
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
                        using (HttpClient client = CreateClient(cert))
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
                            using (HttpClient client = CreateClient(cert))
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
        [ActiveIssue(30056, TargetFrameworkMonikers.Uap)]
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
