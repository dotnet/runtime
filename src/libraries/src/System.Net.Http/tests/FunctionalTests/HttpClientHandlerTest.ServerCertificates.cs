// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws PNSE for ServerCertificateCustomValidationCallback")]
    public partial class HttpClientHandler_ServerCertificates_Test : HttpClientTestBase
    {
        private static bool ClientSupportsDHECipherSuites => (!PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1607OrGreater);
        private bool BackendSupportsCustomCertificateHandlingAndClientSupportsDHECipherSuites =>
            (BackendSupportsCustomCertificateHandling && ClientSupportsDHECipherSuites);

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Uap)]
        public void Ctor_ExpectedDefaultPropertyValues_UapPlatform()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Null(handler.ServerCertificateCustomValidationCallback);
                Assert.True(handler.CheckCertificateRevocationList);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        public void Ctor_ExpectedDefaultValues_NotUapPlatform()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Null(handler.ServerCertificateCustomValidationCallback);
                Assert.False(handler.CheckCertificateRevocationList);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task NoCallback_ValidCertificate_SuccessAndExpectedPropertyBehavior()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.SecureRemoteEchoServer))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                Assert.Throws<InvalidOperationException>(() => handler.ServerCertificateCustomValidationCallback = null);
                Assert.Throws<InvalidOperationException>(() => handler.CheckCertificateRevocationList = false);
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP won't send requests through a custom proxy")]
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task UseCallback_HaveNoCredsAndUseAuthenticatedCustomProxyAndPostToSecureServer_ProxyAuthenticationRequiredStatusCode()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                return;
            }
            if (UseManagedHandler)
            {
                return; // TODO #23136: SSL proxy tunneling not yet implemented in ManagedHandler
            }

            int port;
            Task<LoopbackGetRequestHttpProxy.ProxyResult> proxyTask = LoopbackGetRequestHttpProxy.StartAsync(
                out port,
                requireAuth: true,
                expectCreds: false);
            Uri proxyUrl = new Uri($"http://localhost:{port}");

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, null);
            handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            using (var client = new HttpClient(handler))
            {
                Task<HttpResponseMessage> responseTask = client.PostAsync(
                    Configuration.Http.SecureRemoteEchoServer,
                    new StringContent("This is a test"));
                await TestHelper.WhenAllCompletedOrAnyFailed(proxyTask, responseTask);
                using (responseTask.Result)
                {
                    Assert.Equal(HttpStatusCode.ProxyAuthenticationRequired, responseTask.Result.StatusCode);
                }
            }
        }
        
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task UseCallback_NotSecureConnection_CallbackNotCalled()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(UseCallback_NotSecureConnection_CallbackNotCalled)}()");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
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
            foreach (bool checkRevocation in new[] { true, false })
            {
                yield return new object[] { Configuration.Http.SecureRemoteEchoServer, checkRevocation };
                yield return new object[] {
                    Configuration.Http.RedirectUriForDestinationUri(
                        secure:true,
                        statusCode:302,
                        destinationUri:Configuration.Http.SecureRemoteEchoServer,
                        hops:1),
                    checkRevocation };
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(UseCallback_ValidCertificate_ExpectedValuesDuringCallback_Urls))]
        public async Task UseCallback_ValidCertificate_ExpectedValuesDuringCallback(Uri url, bool checkRevocation)
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(UseCallback_ValidCertificate_ExpectedValuesDuringCallback)}({url}, {checkRevocation})");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                bool callbackCalled = false;
                handler.CheckCertificateRevocationList = checkRevocation;
                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => {
                    callbackCalled = true;
                    Assert.NotNull(request);

                    X509ChainStatusFlags flags = chain.ChainStatus.Aggregate(X509ChainStatusFlags.NoError, (cur, status) => cur | status.Status);
                    bool ignoreErrors = // https://github.com/dotnet/corefx/issues/21922#issuecomment-315555237
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task UseCallback_CallbackReturnsFailure_ThrowsException()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(UseCallback_CallbackReturnsFailure_ThrowsException)}()");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = delegate { return false; };
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task UseCallback_CallbackThrowsException_ExceptionPropagatesAsBaseException()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(UseCallback_CallbackThrowsException_ExceptionPropagatesAsBaseException)}()");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
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

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(ClientSupportsDHECipherSuites))]
        [MemberData(nameof(CertificateValidationServers))]
        public async Task NoCallback_BadCertificate_ThrowsException(string url)
        {
            using (HttpClient client = CreateHttpClient())
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP doesn't allow revocation checking to be turned off")]
        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(ClientSupportsDHECipherSuites))]
        public async Task NoCallback_RevokedCertificate_NoRevocationChecking_Succeeds()
        {
            // On macOS (libcurl+darwinssl) we cannot turn revocation off.
            // But we also can't realistically say that the default value for
            // CheckCertificateRevocationList throws in the general case.
            try
            {
                using (HttpClient client = CreateHttpClient())
                using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RevokedCertRemoteServer))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
            catch (HttpRequestException)
            {
                if (UseManagedHandler || !ShouldSuppressRevocationException)
                    throw;
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task NoCallback_RevokedCertificate_RevocationChecking_Fails()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(NoCallback_RevokedCertificate_RevocationChecking_Fails)}()");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.CheckCertificateRevocationList = true;
            using (var client = new HttpClient(handler))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.RevokedCertRemoteServer));
            }
        }

        public static readonly object[][] CertificateValidationServersAndExpectedPolicies =
        {
            new object[] { Configuration.Http.ExpiredCertRemoteServer, SslPolicyErrors.RemoteCertificateChainErrors },
            new object[] { Configuration.Http.SelfSignedCertRemoteServer, SslPolicyErrors.RemoteCertificateChainErrors },
            new object[] { Configuration.Http.WrongHostNameCertRemoteServer , SslPolicyErrors.RemoteCertificateNameMismatch},
        };

        private async Task UseCallback_BadCertificate_ExpectedPolicyErrors_Helper(string url, bool useManagedHandler, SslPolicyErrors expectedErrors)
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(UseCallback_BadCertificate_ExpectedPolicyErrors)}({url}, {expectedErrors})");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler(useManagedHandler);
            using (var client = new HttpClient(handler))
            {
                bool callbackCalled = false;

                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                {
                    callbackCalled = true;
                    Assert.NotNull(request);
                    Assert.NotNull(cert);
                    Assert.NotNull(chain);
                    if (!useManagedHandler)
                    {
                        // TODO #23137: This test is failing with the managed handler on the exact value of the managed errors,
                        // e.g. reporting "RemoteCertificateNameMismatch, RemoteCertificateChainErrors" when we only expect
                        // "RemoteCertificateChainErrors"
                        Assert.Equal(expectedErrors, errors);
                    }
                    return true;
                };

                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                Assert.True(callbackCalled);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(CertificateValidationServersAndExpectedPolicies))]
        public async Task UseCallback_BadCertificate_ExpectedPolicyErrors(string url, SslPolicyErrors expectedErrors)
        {
            const int SEC_E_BUFFER_TOO_SMALL = unchecked((int)0x80090321);

            if (!BackendSupportsCustomCertificateHandlingAndClientSupportsDHECipherSuites)
            {
                return;
            }

            try
            {
                if (PlatformDetection.IsUap)
                {
                    // UAP HTTP stack caches connections per-process. This causes interference when these tests run in
                    // the same process as the other tests. Each test needs to be isolated to its own process.
                    // See dicussion: https://github.com/dotnet/corefx/issues/21945
                    RemoteInvoke((remoteUrl, remoteExpectedErrors, useManagedHandlerString) =>
                    {
                        UseCallback_BadCertificate_ExpectedPolicyErrors_Helper(
                            remoteUrl,
                            bool.Parse(useManagedHandlerString),
                            (SslPolicyErrors)Enum.Parse(typeof(SslPolicyErrors), remoteExpectedErrors)).Wait();

                        return SuccessExitCode;
                    }, url, expectedErrors.ToString(), UseManagedHandler.ToString()).Dispose();
                }
                else
                {
                    await UseCallback_BadCertificate_ExpectedPolicyErrors_Helper(url, UseManagedHandler, expectedErrors);
                }
            }
            catch (HttpRequestException e) when (e.InnerException?.GetType().Name == "WinHttpException" &&
                e.InnerException.HResult == SEC_E_BUFFER_TOO_SMALL &&
                !PlatformDetection.IsWindows10Version1607OrGreater)
            {
                // Testing on old Windows versions can hit https://github.com/dotnet/corefx/issues/7812
                // Ignore SEC_E_BUFFER_TOO_SMALL error on such cases.
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SSLBackendNotSupported_Callback_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling)
            {
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            using (var client = new HttpClient(handler))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        // For macOS the "custom handling" means that revocation can't be *disabled*. So this test does not apply.
        [PlatformSpecific(~TestPlatforms.OSX)]
        public async Task SSLBackendNotSupported_Revocation_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling)
            {
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.CheckCertificateRevocationList = true;
            using (var client = new HttpClient(handler))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop] // TODO: Issue #11345
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
                if (PlatformDetection.IsUap)
                {
                    // UAP currently doesn't expose channel binding information.
                    Assert.Null(channelBinding);
                }
                else
                {
                    Assert.NotNull(channelBinding);

                    // Validate the ChannelBinding's validity.
                    if (BackendSupportsCustomCertificateHandling)
                    {
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
                    else
                    {
                        // Backend doesn't support getting the details to create the CBT.
                        Assert.True(channelBinding.IsInvalid, "Expected invalid binding");
                        Assert.Equal(IntPtr.Zero, channelBinding.DangerousGetHandle());
                        Assert.Null(channelBinding.ToString());
                    }
                }
            }
        }
    }
}
