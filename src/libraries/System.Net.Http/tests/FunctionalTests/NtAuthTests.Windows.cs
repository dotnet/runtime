// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        internal const string NtlmAuthHeader = "WWW-Authenticate: NTLM";
        internal const string NegotiateAuthHeader = "WWW-Authenticate: Negotiate";
        internal const string UserHeaderName = "X-User";

        internal static Task HandleNtlmAuthenticationRequest(LoopbackServer.Connection connection, bool closeConnection = true)
        {
            return HandleAuthenticationRequest(connection, useNtlm: true, useNegotiate: false, closeConnection);
        }

        internal static Task HandleNegotiateAuthenticationRequest(LoopbackServer.Connection connection, bool closeConnection = true)
        {
            return HandleAuthenticationRequest(connection, useNtlm: false, useNegotiate: true, closeConnection);
        }

        internal static async Task HandleAuthenticationRequest(LoopbackServer.Connection connection, bool useNtlm, bool useNegotiate, bool closeConnection)
        {
            HttpRequestData request = await connection.ReadRequestDataAsync();
            NegotiateAuthentication authContext = null;
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
                authHeader = string.Empty;
                if (useNtlm)
                {
                    authHeader += NtlmAuthHeader + "\r\n";
                }

                if (useNegotiate)
                {
                    authHeader += NegotiateAuthHeader + "\r\n";
                }

                await connection.SendResponseAsync(HttpStatusCode.Unauthorized, authHeader).ConfigureAwait(false);
                connection.CompleteRequestProcessing();

                // Read next requests and fall-back to loop bellow to process it.
                request = await connection.ReadRequestDataAsync();
            }

            NegotiateAuthenticationStatusCode statusCode;
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

                authContext ??= new NegotiateAuthentication(new NegotiateAuthenticationServerOptions { Package = tokens[0] });

                byte[]? outBlob = authContext.GetOutgoingBlob(Convert.FromBase64String(tokens[1]), out statusCode);

                if (outBlob != null && statusCode == NegotiateAuthenticationStatusCode.ContinueNeeded)
                {
                    authHeader = $"WWW-Authenticate: {tokens[0]} {Convert.ToBase64String(outBlob)}\r\n";
                    await connection.SendResponseAsync(HttpStatusCode.Unauthorized, authHeader);
                    connection.CompleteRequestProcessing();

                    request = await connection.ReadRequestDataAsync();
                }
            }
            while (statusCode == NegotiateAuthenticationStatusCode.ContinueNeeded);

            if (statusCode == NegotiateAuthenticationStatusCode.Completed)
            {
                // If authentication succeeded ask Windows about the identity and send it back as custom header.
                IIdentity identity = authContext.RemoteIdentity;

                authHeader = $"{UserHeaderName}: {identity.Name}\r\n";
                if (closeConnection)
                {
                    authHeader += "Connection: close\r\n";
                }

                await connection.SendResponseAsync(HttpStatusCode.OK, authHeader, "foo");
                authContext.Dispose();
            }
            else
            {
                await connection.SendResponseAsync(HttpStatusCode.Forbidden, "Connection: close\r\n", "boo");
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task DefaultHandler_DefaultCredentials_Success(bool useNtlm)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMessage.Version = new Version(1, 1);

                    var handler = new HttpClientHandler() { UseDefaultCredentials = true };
                    using (var client = new HttpClient(handler))
                    {
                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        _output.WriteLine($"Authenticated as {response.Headers.GetValues(NtAuthTests.UserHeaderName).First()}");
                        Assert.Equal("foo", await response.Content.ReadAsStringAsync());
                    }
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Task t = useNtlm ? HandleNtlmAuthenticationRequest(connection) : HandleNegotiateAuthenticationRequest(connection);
                        await t;
                    }).ConfigureAwait(false);
                });
        }
    }
}
