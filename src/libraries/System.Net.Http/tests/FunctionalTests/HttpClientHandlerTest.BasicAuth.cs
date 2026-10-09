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
        [SkipOnPlatform(TestPlatforms.Wasi, "PreAuthenticate is not supported on Wasi")]
        public async Task RefreshesPreAuthCredentialsOnChange()
        {
            CredentialPlugin credentialPlugin = new CredentialPlugin();

            // Keep accepted connections owned by the server so failure cleanup can abort outstanding reads.
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer(new Http2Options { DeferConnectionClose = true });
            server.AllowMultipleConnections = true;

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.PreAuthenticate = true;
            handler.Credentials = credentialPlugin;
            using HttpClient client = CreateHttpClient(handler);

            int connectionNumber = 0;
            await SendAndHandleAsync("", "username:password");

            credentialPlugin.ChangePassword();

            // The cached credential must be rejected before the plugin supplies the new password.
            await SendAndHandleAsync("username:password", "username:password1");

            async Task SendAndHandleAsync(string preAuth, string challengeAuth)
            {
                Task clientTask = SendAsync();
                Task serverTask = HandleRequestsAsync();
                try
                {
                    await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(30_000);
                }
                finally
                {
                    if (!clientTask.IsCompleted || !serverTask.IsCompleted)
                    {
                        // A timed-out wait does not cancel the underlying accept or read.
                        client.Dispose();
                        server.Dispose();
                        await IgnoreExceptions(Task.WhenAll(clientTask, serverTask).WaitAsync(TimeSpan.FromSeconds(30)));
                    }
                }

                async Task SendAsync()
                {
                    using HttpResponseMessage response = await client.GetAsync(server.Address);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                async Task HandleRequestsAsync()
                {
                    await HandleRequestAsync(preAuth, HttpStatusCode.Unauthorized);
                    await HandleRequestAsync(challengeAuth, HttpStatusCode.OK);
                }
            }

            async Task HandleRequestAsync(string expectedAuth, HttpStatusCode statusCode)
            {
                int currentConnection = ++connectionNumber;
                _output.WriteLine($"Establishing connection {currentConnection}");
                await using Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                _output.WriteLine($"Reading request on connection {currentConnection}");
                HttpRequestData data = await connection.ReadRequestDataAsync();
                HttpHeaderData header = data.Headers.SingleOrDefault(h => string.Equals(h.Name, "Authorization", StringComparison.OrdinalIgnoreCase));
                string auth = header.Value is null ? "" :
                    Encoding.UTF8.GetString(Convert.FromBase64String(header.Value.Replace("Basic", "", StringComparison.OrdinalIgnoreCase)));
                Assert.Equal(expectedAuth, auth);

                // Prevent the authentication retry from racing with connection shutdown.
                await connection.SendGoAway(data.RequestId);
                await connection.SendResponseAsync(statusCode, headers: statusCode == HttpStatusCode.Unauthorized ?
                    new[] { new HttpHeaderData("WWW-Authenticate", "Basic realm=\"test\"") } : null);
                _output.WriteLine($"Sent {(int)statusCode} on connection {currentConnection}, stream {data.RequestId}");
            }
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
