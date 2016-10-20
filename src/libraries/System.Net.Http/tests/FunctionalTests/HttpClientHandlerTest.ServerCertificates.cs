// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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

    public class HttpClientHandler_ServerCertificates_Test
    {
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task NoCallback_ValidCertificate_CallbackNotCalled()
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                Assert.Null(handler.ServerCertificateCustomValidationCallback);
                Assert.False(handler.CheckCertificateRevocationList);

                using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.SecureRemoteEchoServer))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                Assert.Throws<InvalidOperationException>(() => handler.ServerCertificateCustomValidationCallback = null);
                Assert.Throws<InvalidOperationException>(() => handler.CheckCertificateRevocationList = false);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(BackendSupportsCustomCertificateHandling))]
        [ActiveIssue(12015, TestPlatforms.AnyUnix)]
        public void UseCallback_HaveNoCredsAndUseAuthenticatedCustomProxyAndPostToSecureServer_ProxyAuthenticationRequiredStatusCode()
        {
            int port;
            Task<LoopbackGetRequestHttpProxy.ProxyResult> proxyTask = LoopbackGetRequestHttpProxy.StartAsync(
                out port,
                requireAuth: true,
                expectCreds: false);
            Uri proxyUrl = new Uri($"http://localhost:{port}");

            var handler = new HttpClientHandler();
            handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, null);
            handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            using (var client = new HttpClient(handler))
            {
                Task<HttpResponseMessage> responseTask = client.PostAsync(
                    Configuration.Http.SecureRemoteEchoServer,
                    new StringContent("This is a test"));
                Task.WaitAll(proxyTask, responseTask);

                Assert.Equal(HttpStatusCode.ProxyAuthenticationRequired, responseTask.Result.StatusCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(BackendSupportsCustomCertificateHandling))]
        public async Task UseCallback_NotSecureConnection_CallbackNotCalled()
        {
            var handler = new HttpClientHandler();
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
        [ConditionalTheory(nameof(BackendSupportsCustomCertificateHandling))]
        [MemberData(nameof(UseCallback_ValidCertificate_ExpectedValuesDuringCallback_Urls))]
        public async Task UseCallback_ValidCertificate_ExpectedValuesDuringCallback(Uri url, bool checkRevocation)
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                bool callbackCalled = false;
                handler.CheckCertificateRevocationList = checkRevocation;
                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => {
                    callbackCalled = true;
                    Assert.NotNull(request);
                    Assert.Equal(SslPolicyErrors.None, errors);
                    Assert.True(chain.ChainElements.Count > 0);
                    Assert.NotEmpty(cert.Subject);
                    Assert.Equal(checkRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck, chain.ChainPolicy.RevocationMode);
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
        [ConditionalFact(nameof(BackendSupportsCustomCertificateHandling))]
        public async Task UseCallback_CallbackReturnsFailure_ThrowsException()
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = delegate { return false; };
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(BackendSupportsCustomCertificateHandling))]
        public async Task UseCallback_CallbackThrowsException_ExceptionPropagates()
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                var e = new DivideByZeroException();
                handler.ServerCertificateCustomValidationCallback = delegate { throw e; };
                Assert.Same(e, await Assert.ThrowsAsync<DivideByZeroException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer)));
            }
        }

        public readonly static object[][] CertificateValidationServers = 
        {
            new object[] { Configuration.Http.ExpiredCertRemoteServer },
            new object[] { Configuration.Http.SelfSignedCertRemoteServer },
            new object[] { Configuration.Http.WrongHostNameCertRemoteServer },
        };

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(CertificateValidationServers))]
        public async Task NoCallback_BadCertificate_ThrowsException(string url)
        {
            using (var client = new HttpClient())
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task NoCallback_RevokedCertificate_NoRevocationChecking_Succeeds()
        {
            using (var client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RevokedCertRemoteServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(BackendSupportsCustomCertificateHandling))]
        public async Task NoCallback_RevokedCertificate_RevocationChecking_Fails()
        {
            var handler = new HttpClientHandler() { CheckCertificateRevocationList = true };
            using (var client = new HttpClient(handler))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Configuration.Http.RevokedCertRemoteServer));
            }
        }

        public readonly static object[][] CertificateValidationServersAndExpectedPolicies =
        {
            new object[] { Configuration.Http.ExpiredCertRemoteServer, SslPolicyErrors.RemoteCertificateChainErrors },
            new object[] { Configuration.Http.SelfSignedCertRemoteServer, SslPolicyErrors.RemoteCertificateChainErrors },
            new object[] { Configuration.Http.WrongHostNameCertRemoteServer , SslPolicyErrors.RemoteCertificateNameMismatch},
        };

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(7812, TestPlatforms.Windows)]
        [ConditionalTheory(nameof(BackendSupportsCustomCertificateHandling))]
        [MemberData(nameof(CertificateValidationServersAndExpectedPolicies))]
        public async Task UseCallback_BadCertificate_ExpectedPolicyErrors(string url, SslPolicyErrors expectedErrors)
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
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

        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(BackendDoesNotSupportCustomCertificateHandling))]
        public async Task SSLBackendNotSupported_Callback_ThrowsPlatformNotSupportedException()
        {
            using (var client = new HttpClient(new HttpClientHandler() { ServerCertificateCustomValidationCallback = delegate { return true; } }))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(BackendDoesNotSupportCustomCertificateHandling))]
        public async Task SSLBackendNotSupported_Revocation_ThrowsPlatformNotSupportedException()
        {
            using (var client = new HttpClient(new HttpClientHandler() { CheckCertificateRevocationList = true }))
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
            using (var client = new HttpClient())
            using (HttpResponseMessage response = await client.PostAsync(Configuration.Http.SecureRemoteEchoServer, content))
            {
                // Validate status.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Validate the ChannelBinding object exists.
                ChannelBinding channelBinding = content.ChannelBinding;
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

        private static bool BackendSupportsCustomCertificateHandling =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            (CurlSslVersionDescription()?.StartsWith("OpenSSL") ?? false);

        private static bool BackendDoesNotSupportCustomCertificateHandling => !BackendSupportsCustomCertificateHandling;

        [DllImport("System.Net.Http.Native", EntryPoint = "HttpNative_GetSslVersionDescription")]
        private static extern string CurlSslVersionDescription();

    }
}
