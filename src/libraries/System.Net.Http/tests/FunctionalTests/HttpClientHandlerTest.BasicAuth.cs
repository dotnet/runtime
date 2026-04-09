// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;

using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class HttpClientHandlerTest_BasicAuth : HttpClientHandlerTestBase
    {
        public HttpClientHandlerTest_BasicAuth(ITestOutputHelper output) : base(output)
        {
        }

        protected override Version UseVersion => HttpVersion.Version20;

        [Fact]
        public async Task RefreshesPreAuthCredentialsOnChange()
        {
            CredentialPlugin credentialPlugin = new CredentialPlugin();

            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            server.AllowMultipleConnections = true;

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.PreAuthenticate = true;
            handler.Credentials = credentialPlugin;
            using HttpClient client = CreateHttpClient(handler);

            Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

            async Task<string> GetAuth(GenericLoopbackConnection connection)
            {
                HttpRequestData data = await connection.ReadRequestDataAsync();
                HttpHeaderData? header = data.Headers.FirstOrDefault(h => string.Equals(h.Name, "Authorization", StringComparison.OrdinalIgnoreCase));

                if (header == null)
                {
                    return "";
                }

                return Encoding.UTF8.GetString(Convert.FromBase64String(header.Value.Value.Replace("Basic", "", StringComparison.OrdinalIgnoreCase)));
            }

            await server.HandleRequestAsync(HttpStatusCode.Unauthorized, headers: new[] { new HttpHeaderData("WWW-Authenticate", "Basic realm=\"test\"") });
            await server.AcceptConnectionAsync(async conn =>
            {
                Assert.Equal("username:password", await GetAuth(conn));
                await conn.SendResponseAsync(HttpStatusCode.OK);
            }).WaitAsync(TimeSpan.FromSeconds(30));

            HttpResponseMessage response = await sendTask;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // change password and try again
            credentialPlugin.ChangePassword();
            sendTask = client.GetAsync(server.Address);

            // first one reuses the cached credentials -> 401
            await server.AcceptConnectionAsync(async conn =>
            {
                Assert.Equal("username:password", await GetAuth(conn));
                await conn.SendResponseAsync(HttpStatusCode.Unauthorized, headers: new[] { new HttpHeaderData("WWW-Authenticate", "Basic realm=\"test\"") });
            }).WaitAsync(TimeSpan.FromSeconds(30));

            // client should try again with correct credentials
            await server.AcceptConnectionAsync(async conn =>
            {
                Assert.Equal("username:password1", await GetAuth(conn));
                await conn.SendResponseAsync(HttpStatusCode.OK);
            }).WaitAsync(TimeSpan.FromSeconds(30));

            response = await sendTask;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    internal class CredentialPlugin : ICredentials
    {
        public CredentialPlugin()
        {
            UserName = "username";
            counter = 0;
            Password = "password";
        }

        private int counter;
        public string UserName { get; private set; }
        public string Password { get; private set; }

        public void ChangePassword()
        {
            counter++;
            Password = "password" + counter;
        }

        NetworkCredential? ICredentials.GetCredential(Uri uri, string authType)
        {
            if (authType == "Basic")
            {
                return new NetworkCredential(UserName, Password);
            }

            return null;
        }
    }
}
