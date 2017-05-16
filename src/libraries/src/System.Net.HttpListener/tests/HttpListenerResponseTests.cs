// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class HttpListenerResponseTests : IDisposable
    {
        private HttpListenerFactory Factory { get; }
        private HttpClient Client { get; }
        private Task<HttpResponseMessage> Message { get; set; }

        public HttpListenerResponseTests()
        {
            Factory = new HttpListenerFactory();
            Client = new HttpClient();
        }

        public void Dispose()
        {
            Message?.Dispose();
            Factory.Dispose();
            Client.Dispose();
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task ContentEncoding_SetCustom_DoesNothing()
        {
            // Setting HttpListenerResponse.ContentEncoding does nothing - it is never used.
            HttpListenerResponse response = await GetResponse();
            Assert.Null(response.ContentEncoding);

            response.ContentEncoding = Encoding.Unicode;
            Assert.Equal(Encoding.Unicode, response.ContentEncoding);
            response.Close();

            HttpResponseMessage clientResponse = await Message;
            Assert.Empty(clientResponse.Content.Headers.ContentEncoding);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task ContentEncoding_SetDisposed_DoesNothing()
        {
            HttpListenerResponse response = await GetResponse();
            response.Close();

            response.ContentEncoding = Encoding.Unicode;
            Assert.Equal(Encoding.Unicode, response.ContentEncoding);

            HttpResponseMessage clientResponse = await Message;
            Assert.Empty(clientResponse.Content.Headers.ContentEncoding);
        }

        private async Task<HttpListenerResponse> GetResponse()
        {
            Message = Client.GetAsync(Factory.ListeningUrl);
            HttpListenerContext context = await Factory.GetListener().GetContextAsync();
            return context.Response;
        }
    }
}
