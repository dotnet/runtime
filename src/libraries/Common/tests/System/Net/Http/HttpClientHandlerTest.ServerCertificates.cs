// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
#if WINHTTPHANDLER_TEST
using System.ComponentModel;
#endif
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
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

    public abstract partial class HttpClientHandler_ServerCertificates_Test : HttpClientHandlerTestBase
    {
#if WINHTTPHANDLER_TEST
        private static bool ClientSupportsDHECipherSuites => (!PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1607OrGreater);
#endif

        public HttpClientHandler_ServerCertificates_Test(ITestOutputHelper output) : base(output) { }

        // This enables customizing ServerCertificateCustomValidationCallback in WinHttpHandler variants:
        protected bool AllowAllCertificates { get; set; } = true;
        protected new HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(allowTlsResume: true);
        protected new HttpClientHandler CreateHttpClientHandler(bool allowTlsResume) => CreateHttpClientHandler(
            useVersion: UseVersion,
            allowAllCertificates: UseVersion >= HttpVersion20.Value && AllowAllCertificates,
            allowTlsResume: allowTlsResume);
        protected override HttpClient CreateHttpClient() => CreateHttpClient(CreateHttpClientHandler());

        [Fact]
        public void Ctor_ExpectedDefaultValues()
        {
            if (IsWinHttpHandler && UseVersion >= HttpVersion20.Value)
            {
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Null(handler.ServerCertificateCustomValidationCallback);
                Assert.False(handler.CheckCertificateRevocationList);
            }
        }

        [Fact]
        public void ServerCertificateCustomValidationCallback_SetGet_Roundtrips()
        {
            if (IsWinHttpHandler && UseVersion >= HttpVersion20.Value)
            {
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Null(handler.ServerCertificateCustomValidationCallback);

                Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> callback1 = (req, cert, chain, policy) => throw new NotImplementedException("callback1");
                Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> callback2 = (req, cert, chain, policy) => throw new NotImplementedException("callback2");

                handler.ServerCertificateCustomValidationCallback = callback1;
                Assert.Same(callback1, handler.ServerCertificateCustomValidationCallback);

                handler.ServerCertificateCustomValidationCallback = callback2;
                Assert.Same(callback2, handler.ServerCertificateCustomValidationCallback);

                handler.ServerCertificateCustomValidationCallback = null;
                Assert.Null(handler.ServerCertificateCustomValidationCallback);
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        public async Task NoCallback_ValidCertificate_SuccessAndExpectedPropertyBehavior()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            using (HttpClient client = CreateHttpClient(handler))
            {
                using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.SecureRemoteEchoServer))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                Assert.Throws<InvalidOperationException>(() => handler.ServerCertificateCustomValidationCallback = null);
                Assert.Throws<InvalidOperationException>(() => handler.CheckCertificateRevocationList = false);
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        public async Task UseCallback_NotSecureConnection_CallbackNotCalled()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            using (HttpClient client = CreateHttpClient(handler))
            {
                bool callbackCalled = false;
                handler.ServerCertificateCustomValidationCallback = delegate { callbackCalled = true; return true; };

                using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                Assert.False(callbackCalled);
            }
        }

        public static IEnumerable<object[]> UseCallback_ValidCertificate_ExpectedValuesDuringCallback_Urls()
        {
            foreach (Configuration.Http.RemoteServer remoteServer in Configuration.Http.GetRemoteServers())
            {
                if (remoteServer.IsSecure)
                {
                    foreach (bool checkRevocation in BoolValues)
                    {
                        yield return new object[] {
                            remoteServer,
                            remoteServer.EchoUri,
                            checkRevocation };
                        yield return new object[] {
                            remoteServer,
                            remoteServer.RedirectUriForDestinationUri(
                                statusCode:302,
                                remoteServer.EchoUri,
                                hops:1),
                            checkRevocation };
                    }
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory]
        [MemberData(nameof(UseCallback_ValidCertificate_ExpectedValuesDuringCallback_Urls))]
        public async Task UseCallback_ValidCertificate_ExpectedValuesDuringCallback(Configuration.Http.RemoteServer remoteServer, Uri url, bool checkRevocation)
        {
            HttpClientHandler handler = CreateHttpClientHandler(allowTlsResume: false);
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                bool callbackCalled = false;
                handler.CheckCertificateRevocationList = checkRevocation;
                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                {
                    callbackCalled = true;
                    Assert.NotNull(request);

                    X509ChainStatusFlags flags = chain.ChainStatus.Aggregate(X509ChainStatusFlags.NoError, (cur, status) => cur | status.Status);
                    bool ignoreErrors = // https://github.com/dotnet/runtime/issues/22644#issuecomment-315555237
                        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                        checkRevocation &&
                        errors == SslPolicyErrors.RemoteCertificateChainErrors &&
                        flags == X509ChainStatusFlags.RevocationStatusUnknown;
                    Assert.True(ignoreErrors || errors == SslPolicyErrors.None, $"Expected {SslPolicyErrors.None}, got {errors} with chain status {flags}");

                    Assert.True(chain.ChainElements.Count > 0);
                    Assert.NotEmpty(cert.Subject);

                    // UWP always uses CheckCertificateRevocationList=true regardless of setting the property and
                    // the getter always returns true. So, for this next Assert, it is better to get the property
                    // value back from the handler instead of using the parameter value of the test.
                    Assert.Equal(
                        handler.CheckCertificateRevocationList ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                        chain.ChainPolicy.RevocationMode);
                    return true;
                };

                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                Assert.True(callbackCalled);
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        public async Task UseCallback_CallbackReturnsFailure_ThrowsException()
        {
            HttpClientHandler handler = CreateHttpClientHandler(allowTlsResume: false);
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = delegate { return false; };
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        public async Task UseCallback_CallbackThrowsException_ExceptionPropagatesAsBaseException()
        {
            HttpClientHandler handler = CreateHttpClientHandler(allowTlsResume: false);
            using (HttpClient client = CreateHttpClient(handler))
            {
                var e = new DivideByZeroException();
                handler.ServerCertificateCustomValidationCallback = delegate { throw e; };

                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
                Assert.Same(e, ex.GetBaseException());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateServerCertificate_IsEndEntity(bool selfSigned)
        {
            using X509Certificate2 rootCertificate = CreateCertificateAuthority();
            using X509Certificate2 certificate = TestHelper.CreateServerCertificate(selfSigned ? null : rootCertificate);

            Assert.False(Assert.Single(certificate.Extensions.OfType<X509BasicConstraintsExtension>()).CertificateAuthority);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task NoCallback_BadCertificate_ThrowsException(bool expired, bool wrongHostName)
        {
            using X509Certificate2 rootCertificate = CreateCertificateAuthority();

            using (HttpClientHandler handler = CreateHttpClientHandler(allowTlsResume: false))
            using (HttpClient client = CreateHttpClient(handler))
            {
                bool customChainPolicy = ConfigureCertificateChainPolicy(handler, rootCertificate);
                using X509Certificate2 certificate = TestHelper.CreateServerCertificate(
                    customChainPolicy && (expired || wrongHostName) ? rootCertificate : null,
                    name: wrongHostName ? "not-localhost" : "localhost", expired: expired);

                await LoopbackServer.CreateClientAndServerAsync(async uri =>
                {
                    HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
                        () => client.GetAsync(new UriBuilder(uri) { Host = "localhost" }.Uri));
#if WINHTTPHANDLER_TEST
                    const int ErrorWinHttpSecureFailure = 12175;
                    Assert.Equal(ErrorWinHttpSecureFailure, Assert.IsAssignableFrom<Win32Exception>(exception.InnerException).NativeErrorCode);
#else
                    if (customChainPolicy)
                    {
                        Assert.Equal(HttpRequestError.SecureConnectionError, exception.HttpRequestError);
                        Assert.IsType<AuthenticationException>(exception.InnerException);
                    }
#endif
                }, async server =>
                {
                    try
                    {
                        // Unexpected certificate acceptance must fail the assertion, not trigger a retry against an idle listener.
                        await server.HandleRequestAsync();
                    }
                    catch (Exception e) when (e is AuthenticationException or IOException)
                    {
                        _output.WriteLine($"Expected certificate validation failure: {e}");
                    }
                },
                options: new LoopbackServer.Options
                {
                    UseSsl = true,
                    Certificate = certificate,
                    SslProtocols = SslProtocols.None,
                });
            }
        }

        [Fact]
        public async Task LoopbackServer_AuthenticationFailure_DisposesStream()
        {
            bool streamDisposed = false;
            var expectedException = new IOException("Simulated TLS transport failure.");
            using var stream = new DelegateStream(
                canReadFunc: () => true,
                canWriteFunc: () => true,
                readAsyncFunc: (_, _, _, _) => Task.FromException<int>(expectedException),
                writeAsyncFunc: (_, _, _, _) => Task.CompletedTask,
                disposeFunc: disposing => streamDisposed |= disposing);
            using var socket = new SocketWrapper(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            using X509Certificate2 certificate = TestHelper.CreateServerSelfSignedCertificate();
            var options = new LoopbackServer.Options { UseSsl = true, Certificate = certificate };
#if !NETFRAMEWORK
            options.CertificateContext = SslStreamCertificateContext.Create(certificate, null);
#endif

            IOException exception = await Assert.ThrowsAsync<IOException>(() => LoopbackServer.Connection.CreateAsync(socket, stream, options));
            Assert.Same(expectedException, exception.GetBaseException());
            Assert.True(streamDisposed);
        }

#if WINHTTPHANDLER_TEST
        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(HttpClientHandler_ServerCertificates_Test), nameof(ClientSupportsDHECipherSuites))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/106634", typeof(PlatformDetection), nameof(PlatformDetection.IsAlpine))]
        public async Task NoCallback_RevokedCertificate_NoRevocationChecking_Succeeds()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.CheckCertificateRevocationList = false;
            using (HttpClient client = CreateHttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RevokedCertRemoteServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        public async Task NoCallback_RevokedCertificate_RevocationChecking_Fails()
        {
            HttpClientHandler handler = CreateHttpClientHandler(allowTlsResume: false);
            handler.CheckCertificateRevocationList = true;
            using (HttpClient client = CreateHttpClient(handler))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.RevokedCertRemoteServer));
            }
        }
#endif

        [Theory]
        [InlineData(true, false, SslPolicyErrors.RemoteCertificateChainErrors)]
        [InlineData(false, false, SslPolicyErrors.RemoteCertificateChainErrors)]
        [InlineData(false, true, SslPolicyErrors.RemoteCertificateNameMismatch)]
        [InlineData(true, true, SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch)]
        public async Task UseCallback_BadCertificate_ExpectedPolicyErrors(bool expired, bool wrongHostName, SslPolicyErrors expectedErrors)
        {
            using X509Certificate2 rootCertificate = CreateCertificateAuthority();

            using (HttpClientHandler handler = CreateHttpClientHandler(allowTlsResume: false))
            using (HttpClient client = CreateHttpClient(handler))
            {
                bool customChainPolicy = ConfigureCertificateChainPolicy(handler, rootCertificate);
                using X509Certificate2 certificate = TestHelper.CreateServerCertificate(
                    customChainPolicy && (expired || wrongHostName) ? rootCertificate : null,
                    name: wrongHostName ? "not-localhost" : "localhost", expired: expired);
                X509ChainStatusFlags expectedChainStatus = expired ? X509ChainStatusFlags.NotTimeValid :
                    wrongHostName ? X509ChainStatusFlags.NoError : X509ChainStatusFlags.UntrustedRoot;

                if (!customChainPolicy)
                {
                    // Native handlers cannot trust the generated root without changing the OS certificate store.
                    expectedErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
                    expectedChainStatus |= X509ChainStatusFlags.UntrustedRoot;
                }

                if (PlatformDetection.IsAndroid && expectedChainStatus != X509ChainStatusFlags.NoError)
                {
                    // Android cannot build a path through an expired or untrusted certificate.
                    expectedChainStatus = X509ChainStatusFlags.PartialChain;
                }

                string expectedThumbprint = certificate.Thumbprint;
                bool callbackCalled = false;

                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                {
                    callbackCalled = true;
                    Assert.NotNull(request);
                    Assert.NotNull(cert);
                    Assert.NotNull(chain);
                    Assert.Equal(expectedThumbprint, cert.Thumbprint);
                    Assert.Equal(expectedErrors, errors);
                    Assert.Equal(expectedChainStatus, chain.ChainStatus.Aggregate(
                        X509ChainStatusFlags.NoError, (flags, status) => flags | status.Status));
                    return true;
                };

                await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
                {
                    using HttpResponseMessage response = await client.GetAsync(new UriBuilder(uri) { Host = "localhost" }.Uri);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }, server => server.HandleRequestAsync(),
                options: new GenericLoopbackOptions
                {
                    UseSsl = true,
                    Certificate = certificate,
                    SslProtocols = SslProtocols.None,
                });

                Assert.True(callbackCalled);
            }
        }

        private static X509Certificate2 CreateCertificateAuthority()
        {
            using RSA key = RSA.Create();
            var request = new CertificateRequest("CN=Test Certificate Authority", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));

            DateTimeOffset now = DateTimeOffset.UtcNow;
            return request.CreateSelfSigned(now.AddYears(-1), now.AddYears(1));
        }

        private static bool ConfigureCertificateChainPolicy(HttpClientHandler handler, X509Certificate2 rootCertificate)
        {
#if !WINHTTPHANDLER_TEST
            if (GetUnderlyingSocketsHttpHandler(handler) is SocketsHttpHandler socketsHandler)
            {
                var policy = new X509ChainPolicy
                {
                    TrustMode = X509ChainTrustMode.CustomRootTrust,
                    RevocationMode = X509RevocationMode.NoCheck,
                    DisableCertificateDownloads = true,
                    // Android requires a nonempty trust store and does not check a trust anchor's expiration.
                    CustomTrustStore = { rootCertificate },
                };

                socketsHandler.SslOptions.CertificateChainPolicy = policy;
                return true;
            }
#endif
            return false;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task UseCallback_SelfSignedCertificate_ExpectedPolicyErrors(int requestCount)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler(allowTlsResume: false))
            using (HttpClient client = CreateHttpClient(handler))
            {
                int callbackCount = 0;
                using X509Certificate2 certificate = TestHelper.CreateServerSelfSignedCertificate();

                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                {
                    callbackCount++;
                    Assert.NotNull(request);
                    Assert.NotNull(cert);
                    Assert.NotNull(chain);
                    Assert.Equal(SslPolicyErrors.RemoteCertificateChainErrors, errors);
                    return true;
                };

                var options = new LoopbackServer.Options { UseSsl = true, Certificate = certificate };

                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    for (int i = 0; i < requestCount; i++)
                    {
#if NETFRAMEWORK
                        // The loopback authentication overload disposes its certificate after each handshake.
                        options.Certificate = new X509Certificate2(certificate);
#endif
                        Task<HttpResponseMessage> responseTask = client.GetAsync($"https://{certificate.GetNameInfo(X509NameType.SimpleName, false)}:{url.Port}/");
                        await TestHelper.WhenAllCompletedOrAnyFailed(
                            server.AcceptConnectionSendResponseAndCloseAsync(),
                            responseTask);
                        using HttpResponseMessage response = await responseTask;
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal(i + 1, callbackCount);
                    }
                }, options);

                Assert.Equal(requestCount, callbackCount);
            }
        }

        [OuterLoop("Uses external servers")]
        [PlatformSpecific(TestPlatforms.Windows)] // CopyToAsync(Stream, TransportContext) isn't used on unix
        [Fact]
        public async Task PostAsync_Post_ChannelBinding_ConfiguredCorrectly()
        {
            var content = new ChannelBindingAwareContent("Test contest");
            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.PostAsync(Configuration.Http.SecureRemoteEchoServer, content))
            {
                // Validate status.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Validate the ChannelBinding object exists.
                ChannelBinding channelBinding = content.ChannelBinding;
                Assert.NotNull(channelBinding);

                // Validate the ChannelBinding's validity.
                Assert.False(channelBinding.IsInvalid, "Expected valid binding");
                Assert.NotEqual(IntPtr.Zero, channelBinding.DangerousGetHandle());

                // Validate the ChannelBinding's description.
                string channelBindingDescription = channelBinding.ToString();
                Assert.NotNull(channelBindingDescription);
                Assert.NotEmpty(channelBindingDescription);
                Assert.True((channelBindingDescription.Length + 1) % 3 == 0, $"Unexpected length {channelBindingDescription.Length}");
                for (int i = 0; i < channelBindingDescription.Length; i++)
                {
                    char c = channelBindingDescription[i];
                    if (i % 3 == 2)
                    {
                        Assert.Equal(' ', c);
                    }
                    else
                    {
                        Assert.True((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'), $"Expected hex, got {c}");
                    }
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task HttpClientUsesSslCertEnvironmentVariables()
        {
            // We set SSL_CERT_DIR and SSL_CERT_FILE to empty locations.
            // The HttpClient should fail to validate the server certificate.
            var psi = new ProcessStartInfo();
            string sslCertDir = GetTestFilePath();
            Directory.CreateDirectory(sslCertDir);
            psi.Environment.Add("SSL_CERT_DIR", sslCertDir);

            string sslCertFile = GetTestFilePath();
            File.WriteAllText(sslCertFile, "");
            psi.Environment.Add("SSL_CERT_FILE", sslCertFile);

            await RemoteExecutor.Invoke(async (useVersionString, allowAllCertificatesString) =>
            {
                const string Url = "https://www.microsoft.com";
                var version = Version.Parse(useVersionString);
                using HttpClientHandler handler = CreateHttpClientHandler(
                    useVersion: version,
                    allowAllCertificates: version >= HttpVersion20.Value && bool.Parse(allowAllCertificatesString),
                    allowTlsResume: false);
                using HttpClient client = CreateHttpClient(handler, useVersionString);

                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Url));
            }, UseVersion.ToString(), AllowAllCertificates.ToString(), new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();
        }
    }
}
