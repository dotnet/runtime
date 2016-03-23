// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Tests
{
    public class HttpClientHandler_ServerCertificates_Test
    {
        [Fact]
        public async Task NoCallback_ValidCertificate_CallbackNotCalled()
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                Assert.Null(handler.ServerCertificateValidationCallback);
                Assert.False(handler.CheckCertificateRevocationList);

                using (HttpResponseMessage response = await client.GetAsync(HttpTestServers.SecureRemoteEchoServer))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                Assert.Throws<InvalidOperationException>(() => handler.ServerCertificateValidationCallback = null);
                Assert.Throws<InvalidOperationException>(() => handler.CheckCertificateRevocationList = false);
            }
        }

        [Fact]
        public async Task UseCallback_NotSecureConnection_CallbackNotCalled()
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                bool callbackCalled = false;
                handler.ServerCertificateValidationCallback = delegate { callbackCalled = true; return true; };

                using (HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteEchoServer))
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
                yield return new object[] { HttpTestServers.SecureRemoteEchoServer, checkRevocation };
                yield return new object[] { HttpTestServers.RedirectUriForDestinationUri(true, HttpTestServers.SecureRemoteEchoServer, 1), checkRevocation };
            }
        }

        [Theory]
        [MemberData(nameof(UseCallback_ValidCertificate_ExpectedValuesDuringCallback_Urls))]
        public async Task UseCallback_ValidCertificate_ExpectedValuesDuringCallback(Uri url, bool checkRevocation)
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                bool callbackCalled = false;
                handler.CheckCertificateRevocationList = checkRevocation;
                handler.ServerCertificateValidationCallback = (request, cert, chain, errors) => {
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

        [Fact]
        public async Task UseCallback_CallbackReturnsFailure_ThrowsException()
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateValidationCallback = delegate { return false; };
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(HttpTestServers.SecureRemoteEchoServer));
            }
        }

        [Fact]
        public async Task UseCallback_CallbackThrowsException_ExceptionPropagates()
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                var e = new DivideByZeroException();
                handler.ServerCertificateValidationCallback = delegate { throw e; };
                Assert.Same(e, await Assert.ThrowsAsync<DivideByZeroException>(() => client.GetAsync(HttpTestServers.SecureRemoteEchoServer)));
            }
        }

        [Theory]
        [InlineData(HttpTestServers.ExpiredCertRemoteServer)]
        [InlineData(HttpTestServers.SelfSignedCertRemoteServer)]
        [InlineData(HttpTestServers.WrongHostNameCertRemoteServer)]
        public async Task NoCallback_BadCertificate_ThrowsException(string url)
        {
            using (var client = new HttpClient())
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
            }
        }

        [Theory]
        [InlineData(HttpTestServers.ExpiredCertRemoteServer, SslPolicyErrors.RemoteCertificateChainErrors)]
        [InlineData(HttpTestServers.SelfSignedCertRemoteServer, SslPolicyErrors.RemoteCertificateChainErrors)]
        [InlineData(HttpTestServers.WrongHostNameCertRemoteServer, SslPolicyErrors.RemoteCertificateNameMismatch)]
        public async Task UseCallback_BadCertificate_ExpectedPolicyErrors(string url, SslPolicyErrors expectedErrors)
        {
            var handler = new HttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                bool callbackCalled = false;

                handler.ServerCertificateValidationCallback = (request, cert, chain, errors) =>
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
    }
}
