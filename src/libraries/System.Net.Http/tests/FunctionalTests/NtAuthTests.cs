// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class NtAuthServer : IDisposable
    {
        // When tests projects are run in parallel, overlapping port ranges can cause a race condition
        // when looking for free ports during dynamic port allocation.
        private const int PortMin = 5001;
        private const int PortMax = 8000;

        private HttpListener _listener;
        private Task _serverTask;

        public NtAuthServer(AuthenticationSchemes authSchemes)
        {
            // Create server.
            CreateServer(authSchemes);

            // Start listening for requests.
            _serverTask = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        HttpListenerContext context = await _listener.GetContextAsync();

                        bool noAccess = (context.Request.RawUrl == NoAccessUrl);
                        if (noAccess)
                        {
                            context.Response.AddHeader("Www-Authenticate", "Negotiate");
                        }

                        context.Response.StatusCode = noAccess ? 401 : 200;
                        context.Response.ContentLength64 = 0;
                        context.Response.OutputStream.Close();
                    }
                }
                catch (HttpListenerException)
                {
                    // Ignore.
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
                catch (InvalidOperationException)
                {
                    // Ignore.
                }
            });
        }

        public void Dispose()
        {
            _listener.Stop();
            _serverTask.Wait(TestHelper.PassingTestTimeoutMilliseconds);
        }

        public string BaseUrl { get; private set; }
        public string NoAccessUrl => "/noaccess";

        private void CreateServer(AuthenticationSchemes authSchemes)
        {
            for (int port = PortMin; port < PortMax; port++)
            {
                string url = $"http://localhost:{port}/";

                _listener = new HttpListener();
                _listener.Prefixes.Add(url);
                _listener.AuthenticationSchemes = authSchemes;

                try
                {
                    _listener.Start();
                    BaseUrl = url;
                    return;
                }
                catch (HttpListenerException)
                {
                }
            }

            throw new Exception("Failed to locate a free port.");
        }
    }

    public class NtAuthServers : IDisposable
    {
        public readonly NtAuthServer NtlmServer = new NtAuthServer(AuthenticationSchemes.Ntlm);
        public readonly NtAuthServer NegotiateServer = new NtAuthServer(AuthenticationSchemes.Negotiate);

        public void Dispose()
        {
            NtlmServer.Dispose();
            NegotiateServer.Dispose();
        }
    }

    public class NtAuthTests : IClassFixture<NtAuthServers>
    {
        private readonly NtAuthServers _servers;
        private readonly ITestOutputHelper _output;

        public NtAuthTests(NtAuthServers servers, ITestOutputHelper output)
        {
            _servers = servers;
            _output = output;
        }

        [OuterLoop]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsNotWindowsNanoServer))]    // HttpListener doesn't support nt auth on non-Windows platforms
        [InlineData(true, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.Unauthorized)]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(false, HttpStatusCode.Unauthorized)]
        public async Task GetAsync_NtAuthServer_ExpectedStatusCode(bool ntlm, HttpStatusCode expectedStatusCode)
        {
            NtAuthServer server = ntlm ? _servers.NtlmServer : _servers.NegotiateServer;

            var handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(server.BaseUrl);
                string path = expectedStatusCode == HttpStatusCode.Unauthorized ? server.NoAccessUrl : "";
                HttpResponseMessage response = await client.GetAsync(path);

                Assert.Equal(expectedStatusCode, response.StatusCode);
            }
        }
    }
}
