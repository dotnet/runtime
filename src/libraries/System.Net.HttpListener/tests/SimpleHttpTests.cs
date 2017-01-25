// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

// TODO: #13618
namespace System.Net.Tests
{
    public class SimpleHttpTests : IDisposable
    {
        private HttpListenerFactory _factory;
        private HttpListener _listener;
        private string _url;

        public SimpleHttpTests()
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
            _url = _factory.ListeningUrl;
        }

        public void Dispose() => _factory.Dispose();

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public static void Supported_True()
        {
            Assert.True(HttpListener.IsSupported);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task SimpleRequest_Succeeds()
        {
            const string expectedResponse = "hello from HttpListener";
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                var clientTask = client.GetStringAsync(_url);

                var serverContext = await serverContextTask;
                using (var response = serverContext.Response)
                {
                    var responseBuffer = Encoding.UTF8.GetBytes(expectedResponse);
                    response.ContentLength64 = responseBuffer.Length;

                    using (var output = response.OutputStream)
                    {
                        await output.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    }
                }

                var clientString = await clientTask;

                Assert.Equal(expectedResponse, clientString);
            }
        }
    }
}
