// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
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
        private static bool ClientSupportsDHECipherSuites => (!PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1607OrGreater);

        public HttpClientHandler_ServerCertificates_Test(ITestOutputHelper output) : base(output) { }

        // This enables customizing ServerCertificateCustomValidationCallback in WinHttpHandler variants:
        protected bool AllowAllCertificates { get; set; } = true;
        protected new HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(
            useVersion: UseVersion,
            allowAllCertificates: UseVersion >= HttpVersion20.Value && AllowAllCertificates);
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
            foreach (Configuration.Http.RemoteServer remoteServer in Configuration.Http.RemoteServers)
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
            HttpClientHandler handler = CreateHttpClientHandler();
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                bool callbackCalled = false;
                handler.CheckCertificateRevocationList = checkRevocation;
                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => {
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
            HttpClientHandler handler = CreateHttpClientHandler();
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
            HttpClientHandler handler = CreateHttpClientHandler();
            using (HttpClient client = CreateHttpClient(handler))
            {
                var e = new DivideByZeroException();
                handler.ServerCertificateCustomValidationCallback = delegate { throw e; };

                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
                Assert.Same(e, ex.GetBaseException());
            }
        }

        public static readonly object[][] CertificateValidationServers =
        {
            new object[] { Configuration.Http.ExpiredCertRemoteServer },
            new object[] { Configuration.Http.SelfSignedCertRemoteServer },
            new object[] { Configuration.Http.WrongHostNameCertRemoteServer },
        };

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(ClientSupportsDHECipherSuites))]
        [MemberData(nameof(CertificateValidationServers))]
        public async Task NoCallback_BadCertificate_ThrowsException(string url)
        {
            using (HttpClient client = CreateHttpClient())
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
            }
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(nameof(ClientSupportsDHECipherSuites))]
        public async Task NoCallback_RevokedCertificate_NoRevocationChecking_Succeeds()
        {
            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RevokedCertRemoteServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        public async Task NoCallback_RevokedCertificate_RevocationChecking_Fails()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.CheckCertificateRevocationList = true;
            using (HttpClient client = CreateHttpClient(handler))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.RevokedCertRemoteServer));
            }
        }

        public static readonly object[][] CertificateValidationServersAndExpectedPolicies =
        {
            new object[] { Configuration.Http.ExpiredCertRemoteServer, SslPolicyErrors.RemoteCertificateChainErrors },
            new object[] { Configuration.Http.WrongHostNameCertRemoteServer , SslPolicyErrors.RemoteCertificateNameMismatch},
        };

        private async Task UseCallback_BadCertificate_ExpectedPolicyErrors_Helper(string url, string useHttp2String, SslPolicyErrors expectedErrors)
        {
            HttpClientHandler handler = CreateHttpClientHandler(useHttp2String);
            using (HttpClient client = CreateHttpClient(handler, useHttp2String))
            {
                bool callbackCalled = false;

                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                {
                    callbackCalled = true;
                    Assert.NotNull(request);
                    Assert.NotNull(cert);
                    Assert.NotNull(chain);
                    Assert.Equal(expectedErrors, errors);
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
        [Theory]
        [MemberData(nameof(CertificateValidationServersAndExpectedPolicies))]
        [SkipOnPlatform(TestPlatforms.Android, "Android rejects the certificate, the custom validation callback in .NET cannot override OS behavior in the current implementation")]
        public async Task UseCallback_BadCertificate_ExpectedPolicyErrors(string url, SslPolicyErrors expectedErrors)
        {
            const int SEC_E_BUFFER_TOO_SMALL = unchecked((int)0x80090321);

            if (!ClientSupportsDHECipherSuites)
            {
                return;
            }

            try
            {
                await UseCallback_BadCertificate_ExpectedPolicyErrors_Helper(url, UseVersion.ToString(), expectedErrors);
            }
            catch (HttpRequestException e) when (e.InnerException?.GetType().Name == "WinHttpException" &&
                e.InnerException.HResult == SEC_E_BUFFER_TOO_SMALL &&
                !PlatformDetection.IsWindows10Version1607OrGreater)
            {
                // Testing on old Windows versions can hit https://github.com/dotnet/runtime/issues/17005
                // Ignore SEC_E_BUFFER_TOO_SMALL error on such cases.
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Android, "Android rejects the certificate, the custom validation callback in .NET cannot override OS behavior in the current implementation")]
        public async Task UseCallback_SelfSignedCertificate_ExpectedPolicyErrors()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                bool callbackCalled = false;
                X509Certificate2 certificate = TestHelper.CreateServerSelfSignedCertificate();

                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                {
                    callbackCalled = true;
                    Assert.NotNull(request);
                    Assert.NotNull(cert);
                    Assert.NotNull(chain);
                    Assert.Equal(SslPolicyErrors.RemoteCertificateChainErrors, errors);
                    return true;
                };

                var options = new LoopbackServer.Options { UseSsl = true, Certificate = certificate };

                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        server.AcceptConnectionSendResponseAndCloseAsync(),
                        client.GetAsync($"https://{certificate.GetNameInfo(X509NameType.SimpleName, false)}:{url.Port}/"));
                }, options);

                Assert.True(callbackCalled);
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
        public void HttpClientUsesSslCertEnvironmentVariables()
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

            RemoteExecutor.Invoke(async (useVersionString, allowAllCertificatesString) =>
            {
                const string Url = "https://www.microsoft.com";
                var version = Version.Parse(useVersionString);
                using HttpClientHandler handler = CreateHttpClientHandler(
                    useVersion: version,
                    allowAllCertificates: version >= HttpVersion20.Value && bool.Parse(allowAllCertificatesString));
                using HttpClient client = CreateHttpClient(handler, useVersionString);

                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Url));
            }, UseVersion.ToString(), AllowAllCertificates.ToString(), new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }
    }
}
