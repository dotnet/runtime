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
        public static bool IsNtlmAvailable =>
            Capability.IsNtlmInstalled() || OperatingSystem.IsAndroid() || OperatingSystem.IsTvOS();

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

            await connection.SendResponseAsync(HttpStatusCode.OK);
        }

        [ConditionalTheory(nameof(IsNtlmAvailable))]
        [InlineData(true)]
        [InlineData(false)]
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
    }
}
