// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Principal;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public partial class NtAuthTests : IClassFixture<NtAuthServers>
    {
        public static bool IsNtlmAvailable =>
            Capability.IsNtlmInstalled() || OperatingSystem.IsAndroid() || OperatingSystem.IsTvOS();

        public static bool IsNtlmAndAlpnAvailable => IsNtlmAvailable && PlatformDetection.SupportsAlpn;

        private static NetworkCredential s_testCredentialRight = new NetworkCredential("rightusername", "rightpassword");

        internal static async Task HandleAuthenticationRequestWithFakeServer(LoopbackServer.Connection connection, bool useNtlm)
        {
            HttpRequestData request = await connection.ReadRequestDataAsync();
            FakeNtlmServer? fakeNtlmServer = null;
            FakeNegotiateServer? fakeNegotiateServer = null;
            string authHeader = null;

            foreach (HttpHeaderData header in request.Headers)
            {
                if (header.Name == "Authorization")
                {
                    authHeader = header.Value;
                    break;
                }
            }

            if (string.IsNullOrEmpty(authHeader))
            {
                // This is initial request, we reject with showing supported mechanisms.
                if (useNtlm)
                {
                    authHeader = "WWW-Authenticate: NTLM\r\n";
                }
                else
                {
                    authHeader = "WWW-Authenticate: Negotiate\r\n";
                }

                await connection.SendResponseAsync(HttpStatusCode.Unauthorized, authHeader).ConfigureAwait(false);
                connection.CompleteRequestProcessing();

                // Read next requests and fall-back to loop bellow to process it.
                request = await connection.ReadRequestDataAsync();
            }

            bool isAuthenticated = false;
            do
            {
                foreach (HttpHeaderData header in request.Headers)
                {
                    if (header.Name == "Authorization")
                    {
                        authHeader = header.Value;
                        break;
                    }
                }

                Assert.NotNull(authHeader);
                var tokens = authHeader.Split(' ', 2, StringSplitOptions.TrimEntries);
                // Should be type and base64 encoded blob
                Assert.Equal(2, tokens.Length);

                if (fakeNtlmServer == null)
                {
                    fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight) { ForceNegotiateVersion = true };
                    if (!useNtlm)
                    {
                        fakeNegotiateServer = new FakeNegotiateServer(fakeNtlmServer);
                    }
                }

                byte[]? outBlob;

                if (fakeNegotiateServer != null)
                {
                    outBlob = fakeNegotiateServer.GetOutgoingBlob(Convert.FromBase64String(tokens[1]));
                    isAuthenticated = fakeNegotiateServer.IsAuthenticated;
                }
                else
                {
                    outBlob = fakeNtlmServer.GetOutgoingBlob(Convert.FromBase64String(tokens[1]));
                    isAuthenticated = fakeNtlmServer.IsAuthenticated;
                }

                if (outBlob != null)
                {
                    authHeader = $"WWW-Authenticate: {tokens[0]} {Convert.ToBase64String(outBlob)}\r\n";
                    await connection.SendResponseAsync(isAuthenticated ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, authHeader);
                    connection.CompleteRequestProcessing();

                    if (!isAuthenticated)
                    {
                        request = await connection.ReadRequestDataAsync();
                    }
                }
            }
            while (!isAuthenticated);

            fakeNtlmServer?.Dispose();

            await connection.SendResponseAsync(HttpStatusCode.OK);
        }

        private static HttpAgnosticOptions CreateHttpAgnosticOptions() => new HttpAgnosticOptions
        {
            UseSsl = true,
            SslApplicationProtocols = new List<SslApplicationProtocol>
            {
                SslApplicationProtocol.Http2,
                SslApplicationProtocol.Http11
            }
        };

        private static SocketsHttpHandler CreateCredentialHandler() => new SocketsHttpHandler
        {
            Credentials = s_testCredentialRight,
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = delegate { return true; }
            }
        };

        [ConditionalTheory(typeof(NtAuthTests), nameof(IsNtlmAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials and HttpListener is not supported on Browser")]
        public async Task DefaultHandler_FakeServer_Success(bool useNtlm)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMessage.Version = new Version(1, 1);

                    HttpMessageHandler handler = new HttpClientHandler() { Credentials = s_testCredentialRight };
                    using (var client = new HttpClient(handler))
                    {
                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await HandleAuthenticationRequestWithFakeServer(connection, useNtlm);
                    }).ConfigureAwait(false);
                });
        }

        [ConditionalTheory(nameof(IsNtlmAndAlpnAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials and HttpListener is not supported on Browser")]
        public async Task Http2_SessionAuthChallenge_DowngradesPoolToHttp11(bool useNtlm)
        {
            // When an HTTP/2 request receives a session-based auth challenge (NTLM/Negotiate),
            // the pool should skip HTTP/2 for future requests that can use HTTP/1.1
            // and retry the current request on HTTP/1.1.
            await HttpAgnosticLoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using SocketsHttpHandler handler = CreateCredentialHandler();
                    using var client = new HttpClient(handler);

                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Version = HttpVersion.Version20;
                    request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                    HttpResponseMessage response = await client.SendAsync(request);

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(HttpVersion.Version11, response.Version);
                },
                async server =>
                {
                    // First connection: HTTP/2 (via ALPN). Read request, send 401 with session auth challenge.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        var h2 = (Http2LoopbackConnection)connection;
                        int streamId = await h2.ReadRequestHeaderAsync();

                        string authScheme = useNtlm ? "NTLM" : "Negotiate";
                        await h2.SendResponseHeadersAsync(streamId, endStream: true, HttpStatusCode.Unauthorized,
                            headers: new[] { new HttpHeaderData("WWW-Authenticate", authScheme) });
                    });

                    // Second connection: HTTP/1.1 (pool disabled HTTP/2). Handle auth.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Assert.IsType<LoopbackServer.Connection>(connection);
                        await HandleAuthenticationRequestWithFakeServer((LoopbackServer.Connection)connection, useNtlm);
                    });
                },
                httpOptions: CreateHttpAgnosticOptions());
        }

        [ConditionalTheory(nameof(IsNtlmAndAlpnAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials and HttpListener is not supported on Browser")]
        public async Task Http2_SessionAuthChallenge_Http11RequestVersionOrHigher_DowngradesToHttp11(bool useNtlm)
        {
            // A request with Version=1.1 and RequestVersionOrHigher over TLS gets upgraded to HTTP/2.
            // When it receives a session auth challenge, it should still be retried on HTTP/1.1,
            // since the request's version (1.1) allows falling back to HTTP/1.1.
            await HttpAgnosticLoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using SocketsHttpHandler handler = CreateCredentialHandler();
                    using var client = new HttpClient(handler);

                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Version = HttpVersion.Version11;
                    request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                    HttpResponseMessage response = await client.SendAsync(request);

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(HttpVersion.Version11, response.Version);
                },
                async server =>
                {
                    // First connection: HTTP/2 (via ALPN, since Version=1.1 + RequestVersionOrHigher + TLS).
                    // Read request, send 401 with session auth challenge.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        var h2 = (Http2LoopbackConnection)connection;
                        int streamId = await h2.ReadRequestHeaderAsync();

                        string authScheme = useNtlm ? "NTLM" : "Negotiate";
                        await h2.SendResponseHeadersAsync(streamId, endStream: true, HttpStatusCode.Unauthorized,
                            headers: new[] { new HttpHeaderData("WWW-Authenticate", authScheme) });
                    });

                    // Second connection: HTTP/1.1 (pool skips HTTP/2 for this request). Handle auth.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Assert.IsType<LoopbackServer.Connection>(connection);
                        await HandleAuthenticationRequestWithFakeServer((LoopbackServer.Connection)connection, useNtlm);
                    });
                },
                httpOptions: CreateHttpAgnosticOptions());
        }

        [ConditionalTheory(nameof(IsNtlmAndAlpnAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials and HttpListener is not supported on Browser")]
        public async Task Http2_SessionAuthChallenge_SubsequentRequestsUseHttp11(bool useNtlm)
        {
            // After the pool downgrades to HTTP/1.1 due to session auth, subsequent requests
            // should also use HTTP/1.1 without attempting HTTP/2.
            await HttpAgnosticLoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using SocketsHttpHandler handler = CreateCredentialHandler();
                    using var client = new HttpClient(handler);

                    // First request: triggers pool downgrade.
                    var request1 = new HttpRequestMessage(HttpMethod.Get, uri);
                    request1.Version = HttpVersion.Version20;
                    request1.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                    HttpResponseMessage response1 = await client.SendAsync(request1);
                    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                    Assert.Equal(HttpVersion.Version11, response1.Version);

                    // Second request on the same handler: should go directly to HTTP/1.1
                    // without trying HTTP/2 first (no extra roundtrip).
                    var request2 = new HttpRequestMessage(HttpMethod.Get, uri);
                    request2.Version = HttpVersion.Version20;
                    request2.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                    HttpResponseMessage response2 = await client.SendAsync(request2);
                    Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
                    Assert.Equal(HttpVersion.Version11, response2.Version);
                },
                async server =>
                {
                    // First connection: HTTP/2. Read request, send 401 auth challenge.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        var h2 = (Http2LoopbackConnection)connection;
                        int streamId = await h2.ReadRequestHeaderAsync();

                        string authScheme = useNtlm ? "NTLM" : "Negotiate";
                        await h2.SendResponseHeadersAsync(streamId, endStream: true, HttpStatusCode.Unauthorized,
                            headers: new[] { new HttpHeaderData("WWW-Authenticate", authScheme) });
                    });

                    // Second connection: HTTP/1.1. Handle auth for first request.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Assert.IsType<LoopbackServer.Connection>(connection);
                        await HandleAuthenticationRequestWithFakeServer((LoopbackServer.Connection)connection, useNtlm);
                    });

                    // Third connection: HTTP/1.1 for second request (no auth needed, new connection).
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Assert.IsType<LoopbackServer.Connection>(connection);
                        await connection.HandleRequestAsync(HttpStatusCode.OK);
                    });
                },
                httpOptions: CreateHttpAgnosticOptions());
        }

        [ConditionalTheory(nameof(IsNtlmAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials and HttpListener is not supported on Browser")]
        public async Task Http2_SessionAuthChallenge_ExactVersionPolicy_Returns401(bool useNtlm)
        {
            // When the version policy is RequestVersionExact, we can't downgrade.
            // The 401 response should be returned as-is.
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Version = HttpVersion.Version20;
                    request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    using SocketsHttpHandler handler = CreateCredentialHandler();
                    using var client = new HttpClient(handler);

                    HttpResponseMessage response = await client.SendAsync(request);

                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    Assert.Equal(HttpVersion.Version20, response.Version);
                    Assert.True(response.Headers.WwwAuthenticate.Count > 0);
                },
                async server =>
                {
                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                    int streamId = await connection.ReadRequestHeaderAsync();

                    string authScheme = useNtlm ? "NTLM" : "Negotiate";
                    await connection.SendResponseHeadersAsync(streamId, endStream: true, HttpStatusCode.Unauthorized,
                        headers: new[] { new HttpHeaderData("WWW-Authenticate", authScheme) });
                });
        }

        [ConditionalTheory(nameof(IsNtlmAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials and HttpListener is not supported on Browser")]
        public async Task Http2_SessionAuthChallenge_WithContent_Returns401(bool useNtlm)
        {
            // Requests with content can't be safely retried, so the 401 should be returned.
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, uri);
                    request.Version = HttpVersion.Version20;
                    request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                    request.Content = new StringContent("test content");

                    using SocketsHttpHandler handler = CreateCredentialHandler();
                    using var client = new HttpClient(handler);

                    HttpResponseMessage response = await client.SendAsync(request);

                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    Assert.Equal(HttpVersion.Version20, response.Version);
                    Assert.True(response.Headers.WwwAuthenticate.Count > 0);
                },
                async server =>
                {
                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                    int streamId = await connection.ReadRequestHeaderAsync(expectEndOfStream: false);
                    await connection.ReadBodyAsync();

                    string authScheme = useNtlm ? "NTLM" : "Negotiate";
                    await connection.SendResponseHeadersAsync(streamId, endStream: true, HttpStatusCode.Unauthorized,
                        headers: new[] { new HttpHeaderData("WWW-Authenticate", authScheme) });
                });
        }

        [ConditionalTheory(nameof(IsNtlmAndAlpnAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials and HttpListener is not supported on Browser")]
        public async Task Http2_SessionAuthChallenge_PostSetsFlag_SubsequentGetUsesHttp11(bool useNtlm)
        {
            // A POST with content that gets a session auth challenge can't be retried,
            // but it should still set the pool flag so that subsequent downgradeable
            // requests (like GET) go directly to HTTP/1.1.
            await HttpAgnosticLoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using SocketsHttpHandler handler = CreateCredentialHandler();
                    using var client = new HttpClient(handler);

                    // First request: POST with content gets 401. Can't retry, but sets the flag.
                    var postRequest = new HttpRequestMessage(HttpMethod.Post, uri);
                    postRequest.Version = HttpVersion.Version20;
                    postRequest.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                    postRequest.Content = new StringContent("test content");

                    HttpResponseMessage postResponse = await client.SendAsync(postRequest);
                    Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
                    Assert.Equal(HttpVersion.Version20, postResponse.Version);

                    // Second request: GET without content. Should go directly to HTTP/1.1
                    // because the pool flag was set by the POST's auth challenge.
                    var getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
                    getRequest.Version = HttpVersion.Version20;
                    getRequest.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                    HttpResponseMessage getResponse = await client.SendAsync(getRequest);
                    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
                    Assert.Equal(HttpVersion.Version11, getResponse.Version);
                },
                async server =>
                {
                    // First connection: HTTP/2. POST with content gets 401 auth challenge.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        var h2 = (Http2LoopbackConnection)connection;
                        int streamId = await h2.ReadRequestHeaderAsync(expectEndOfStream: false);
                        await h2.ReadBodyAsync();

                        string authScheme = useNtlm ? "NTLM" : "Negotiate";
                        await h2.SendResponseHeadersAsync(streamId, endStream: true, HttpStatusCode.Unauthorized,
                            headers: new[] { new HttpHeaderData("WWW-Authenticate", authScheme) });
                    });

                    // Second connection: HTTP/1.1 for the GET (pool flag was set by POST).
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Assert.IsType<LoopbackServer.Connection>(connection);
                        await HandleAuthenticationRequestWithFakeServer((LoopbackServer.Connection)connection, useNtlm);
                    });
                },
                httpOptions: CreateHttpAgnosticOptions());
        }

        [ConditionalFact(nameof(IsNtlmAndAlpnAvailable))]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials and HttpListener is not supported on Browser")]
        public async Task Http2_SessionAuthChallenge_Http2OnlyRequestsStillWork()
        {
            // After a session auth challenge triggers the downgrade flag,
            // HTTP/2-only requests (RequestVersionExact) should still use HTTP/2 normally.
            await HttpAgnosticLoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using SocketsHttpHandler handler = CreateCredentialHandler();
                    using var client = new HttpClient(handler);

                    // First request: downgradeable, triggers the session auth flag.
                    var request1 = new HttpRequestMessage(HttpMethod.Get, uri);
                    request1.Version = HttpVersion.Version20;
                    request1.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                    HttpResponseMessage response1 = await client.SendAsync(request1);
                    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                    Assert.Equal(HttpVersion.Version11, response1.Version);

                    // Second request: HTTP/2-only. Should still use HTTP/2 despite the flag.
                    var request2 = new HttpRequestMessage(HttpMethod.Get, uri);
                    request2.Version = HttpVersion.Version20;
                    request2.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    HttpResponseMessage response2 = await client.SendAsync(request2);
                    Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
                    Assert.Equal(HttpVersion.Version20, response2.Version);
                },
                async server =>
                {
                    // First connection: HTTP/2. Send 401 auth challenge.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        var h2 = (Http2LoopbackConnection)connection;
                        int streamId = await h2.ReadRequestHeaderAsync();

                        await h2.SendResponseHeadersAsync(streamId, endStream: true, HttpStatusCode.Unauthorized,
                            headers: new[] { new HttpHeaderData("WWW-Authenticate", "NTLM") });
                    });

                    // Second connection: HTTP/1.1. Handle auth for the downgradeable request.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Assert.IsType<LoopbackServer.Connection>(connection);
                        await HandleAuthenticationRequestWithFakeServer((LoopbackServer.Connection)connection, useNtlm: true);
                    });

                    // Third connection: HTTP/2 for the HTTP/2-only request. Serve normally.
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        var h2 = (Http2LoopbackConnection)connection;
                        int streamId = await h2.ReadRequestHeaderAsync();
                        await h2.SendResponseHeadersAsync(streamId, endStream: true, HttpStatusCode.OK);
                    });
                },
                httpOptions: CreateHttpAgnosticOptions());
        }

        [ConditionalTheory(nameof(IsNtlmAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials and HttpListener is not supported on Browser")]
        public async Task Http2_SessionAuthChallenge_AuthDisabledAfterRedirect_Returns401(bool useNtlm)
        {
            // When auth is disabled on a request (e.g., due to a redirect with non-CredentialCache credentials),
            // the handler should NOT retry/downgrade, even if other conditions are met.
            // Auth is disabled by the redirect handler when credentials are NetworkCredential (not CredentialCache).
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Version = HttpVersion.Version20;
                    request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                    // Use NetworkCredential (not CredentialCache) so auth gets disabled on redirect.
                    using SocketsHttpHandler handler = CreateCredentialHandler();
                    using var client = new HttpClient(handler);

                    HttpResponseMessage response = await client.SendAsync(request);

                    // Should get 401 because auth was disabled after the redirect.
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    Assert.Equal(HttpVersion.Version20, response.Version);
                    Assert.True(response.Headers.WwwAuthenticate.Count > 0);
                },
                async server =>
                {
                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                    // First request: respond with a redirect (to the same server).
                    int streamId1 = await connection.ReadRequestHeaderAsync();
                    await connection.SendResponseHeadersAsync(streamId1, endStream: true, HttpStatusCode.Redirect,
                        headers: new[] { new HttpHeaderData("Location", server.Address.AbsoluteUri) });

                    // Second request (after redirect): send session auth challenge.
                    // Auth is now disabled on the request due to the redirect with NetworkCredential.
                    int streamId2 = await connection.ReadRequestHeaderAsync();
                    string authScheme = useNtlm ? "NTLM" : "Negotiate";
                    await connection.SendResponseHeadersAsync(streamId2, endStream: true, HttpStatusCode.Unauthorized,
                        headers: new[] { new HttpHeaderData("WWW-Authenticate", authScheme) });
                });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.Windows, "DefaultCredentials are unsupported for NTLM on Unix / Managed implementation")]
        public async Task DefaultHandler_FakeServer_DefaultCredentials()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMessage.Version = new Version(1, 1);

                    HttpMessageHandler handler = new HttpClientHandler() { Credentials = CredentialCache.DefaultCredentials };
                    using (var client = new HttpClient(handler))
                    {
                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        var authHeader = "WWW-Authenticate: NTLM\r\n";
                        await connection.SendResponseAsync(HttpStatusCode.Unauthorized, authHeader).ConfigureAwait(false);
                        connection.CompleteRequestProcessing();
                    }).ConfigureAwait(false);
                });
        }
    }
}
