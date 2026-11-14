// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/2391", TestRuntimes.Mono)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // httpsys component missing in Nano.
    public class HttpListenerIPv6Tests
    {
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.CanListenOnIPv6Loopback))]
        [InlineData("[::1]")]
        [InlineData("[0:0:0:0:0:0:0:1]")]
        public async Task Prefix_IPv6Literal_ServesRequest(string literal)
        {
            using var factory = new HttpListenerFactory(literal);
            using Socket client = factory.GetConnectedSocket();
            string hostHeader = $"[::1]:{factory.Port}";

            HttpListenerContext context = await GetContextAsync(factory.GetListener(), client,
                $"GET /{factory.Path}/ HTTP/1.1\r\nHost: {hostHeader}\r\n\r\n");

            Assert.Equal($"http://[::1]:{factory.Port}/{factory.Path}/", context.Request.Url.ToString());
            Assert.Equal(hostHeader, context.Request.UserHostName);
            Assert.Equal(IPAddress.IPv6Loopback, context.Request.LocalEndPoint.Address);
            context.Response.Close();
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.CanListenOnIPv6Loopback))]
        [InlineData("GET /{0}/ HTTP/1.1\r\nHost: [::1]:{1}\r\n\r\n")]
        [InlineData("GET http://[::1]:{1}/{0}/ HTTP/1.1\r\nHost: [::1]:{1}\r\n\r\n")]
        [InlineData("GET /{0}/ HTTP/1.0\r\n\r\n")]
        public async Task Request_IPv6LiteralPrefix_ReturnsExpectedUrl(string requestFormat)
        {
            using var factory = new HttpListenerFactory("[::1]");
            using Socket client = factory.GetConnectedSocket();

            HttpListenerContext context = await GetContextAsync(factory.GetListener(), client,
                string.Format(requestFormat, factory.Path, factory.Port));

            Assert.Equal($"http://[::1]:{factory.Port}/{factory.Path}/", context.Request.Url.ToString());
            context.Response.Close();
        }

        [ConditionalFact(typeof(HttpListenerFactory), nameof(HttpListenerFactory.SupportsWildcards))]
        public async Task Request_IPv6LiteralHostHeaderToWildcardPrefix_ReturnsExpectedUrl()
        {
            using var factory = new HttpListenerFactory("+");
            using Socket client = factory.GetConnectedSocket();
            string hostHeader = $"[::1]:{factory.Port}";

            HttpListenerContext context = await GetContextAsync(factory.GetListener(), client,
                $"GET /{factory.Path}/ HTTP/1.1\r\nHost: {hostHeader}\r\n\r\n");

            Assert.Equal($"http://[::1]:{factory.Port}/{factory.Path}/", context.Request.Url.ToString());
            Assert.Equal(hostHeader, context.Request.UserHostName);
            context.Response.Close();
        }

        private static async Task<HttpListenerContext> GetContextAsync(HttpListener listener, Socket client, string request)
        {
            Task<HttpListenerContext> getContext = listener.GetContextAsync();
            await client.SendAsync(Encoding.ASCII.GetBytes(request));

            // A request the listener rejects is answered on the socket and never reaches GetContextAsync.
            byte[] buffer = new byte[4096];
            Task<int> receive = client.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).AsTask();
            Task first = await Task.WhenAny(getContext, receive).WaitAsync(TimeSpan.FromSeconds(30));
            if (first == receive)
            {
                Assert.Fail($"The listener answered the request itself: {Encoding.ASCII.GetString(buffer, 0, await receive)}");
            }

            return await getContext;
        }
    }
}
