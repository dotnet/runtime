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
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract partial class HttpClientHandler_ServerCertificates_Test : HttpClientHandlerTestBase
    {
        private static bool ClientSupportsDHECipherSuites => (!PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1607OrGreater);
        private bool BackendSupportsCustomCertificateHandlingAndClientSupportsDHECipherSuites =>
            (BackendSupportsCustomCertificateHandling && ClientSupportsDHECipherSuites);

        public HttpClientHandler_ServerCertificates_Test(ITestOutputHelper output) : base(output) { }

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

        [Fact]
        public void ServerCertificateCustomValidationCallback_SetGet_Roundtrips()
        {
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

        [OuterLoop("Uses external server")]
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

        [ActiveIssue(37250)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP won't send requests through a custom proxy")]
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task UseCallback_HaveCredsAndUseAuthenticatedCustomProxyAndPostToSecureServer_Success()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                return;
            }

            if (IsWinHttpHandler && PlatformDetection.IsWindows7)
            {
                // Issue #27612
                return;
            }

            var options = new LoopbackProxyServer.Options
                { AuthenticationSchemes = AuthenticationSchemes.Basic,
                  ConnectionCloseAfter407 = true
                };
            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create(options))
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                handler.Proxy = new WebProxy(proxyServer.Uri)
                {
                    Credentials = new NetworkCredential("rightusername", "rightpassword")
                };

                const string content = "This is a test";

                using (HttpClient client = CreateHttpClient(handler))
                using (HttpResponseMessage response = await client.PostAsync(
                        Configuration.Http.SecureRemoteEchoServer,
                        new StringContent(content)))
                {
                    string responseContent = await response.Content.ReadAsStringAsync();

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        content);
                }
            }
        }

        [ActiveIssue(37250)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP won't send requests through a custom proxy")]
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task UseCallback_HaveNoCredsAndUseAuthenticatedCustomProxyAndPostToSecureServer_ProxyAuthenticationRequiredStatusCode()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                return;
            }

            var options = new LoopbackProxyServer.Options
                { AuthenticationSchemes = AuthenticationSchemes.Basic,
                  ConnectionCloseAfter407 = true
                };
            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create(options))
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.Proxy = new WebProxy(proxyServer.Uri);
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                using (HttpClient client = CreateHttpClient(handler))
                using (HttpResponseMessage response = await client.PostAsync(
                    Configuration.Http.SecureRemoteEchoServer,
                    new StringContent("This is a test")))
                {
                    Assert.Equal(HttpStatusCode.ProxyAuthenticationRequired, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task UseCallback_NotSecureConnection_CallbackNotCalled()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(UseCallback_NotSecureConnection_CallbackNotCalled)}()");
                return;
            }

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

        [OuterLoop("Uses external server")]
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
            using (HttpClient client = CreateHttpClient(handler))
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

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task UseCallback_CallbackReturnsFailure_ThrowsException()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(UseCallback_CallbackReturnsFailure_ThrowsException)}()");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = delegate { return false; };
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task UseCallback_CallbackThrowsException_ExceptionPropagatesAsBaseException()
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(UseCallback_CallbackThrowsException_ExceptionPropagatesAsBaseException)}()");
                return;
            }

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

        [OuterLoop("Uses external server")]
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
        [OuterLoop("Uses external server")]
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
                if (UseSocketsHttpHandler || !ShouldSuppressRevocationException)
                    throw;
            }
        }

        [OuterLoop("Uses external server")]
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
            using (HttpClient client = CreateHttpClient(handler))
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

        private async Task UseCallback_BadCertificate_ExpectedPolicyErrors_Helper(string url, string useSocketsHttpHandlerString, string useHttp2String, SslPolicyErrors expectedErrors)
        {
            if (!BackendSupportsCustomCertificateHandling)
            {
                Console.WriteLine($"Skipping {nameof(UseCallback_BadCertificate_ExpectedPolicyErrors)}({url}, {expectedErrors})");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler(useSocketsHttpHandlerString, useHttp2String);
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

        [ActiveIssue(30054, TargetFrameworkMonikers.Uap)]
        [OuterLoop("Uses external server")]
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
                    RemoteExecutor.Invoke((remoteUrl, remoteExpectedErrors, useSocketsHttpHandlerString, useHttp2String) =>
                    {
                        UseCallback_BadCertificate_ExpectedPolicyErrors_Helper(
                            remoteUrl,
                            useSocketsHttpHandlerString,
                            useHttp2String,
                            (SslPolicyErrors)Enum.Parse(typeof(SslPolicyErrors), remoteExpectedErrors)).Wait();

                        return RemoteExecutor.SuccessExitCode;
                    }, url, expectedErrors.ToString(), UseSocketsHttpHandler.ToString(), UseHttp2.ToString()).Dispose();
                }
                else
                {
                    await UseCallback_BadCertificate_ExpectedPolicyErrors_Helper(url, UseSocketsHttpHandler.ToString(), UseHttp2.ToString(), expectedErrors);
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

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task SSLBackendNotSupported_Callback_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling)
            {
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = delegate { return true; }; // Do not use TestHelper.AllowAllCertificates / HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            using (HttpClient client = CreateHttpClient(handler))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop("Uses external server")]
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
            using (HttpClient client = CreateHttpClient(handler))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop("Uses external server")]
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
