// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class HttpListenerRequestTests
    {
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task GetClientCertificate_NoCertificate_ReturnsNull()
        {
            await GetRequest("POST", null, null, (_, request) =>
            {
                Assert.Null(request.GetClientCertificate());
                Assert.Equal(0, request.ClientCertificateError);
            });
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task GetClientCertificateAsync_NoCertificate_ReturnsNull()
        {
            await GetRequest("POST", null, null, (_, request) =>
            {
                Assert.Null(request.GetClientCertificateAsync().Result);
            });
        }

        private async Task GetRequest(string requestType, string query, string[] headers, Action<Socket, HttpListenerRequest> requestAction, bool sendContent = true)
        {
            using (HttpListenerFactory factory = new HttpListenerFactory())
            using (Socket client = factory.GetConnectedSocket())
            {
                client.Send(factory.GetContent(requestType, query, sendContent ? "Text" : "", headers, true));

                HttpListener listener = factory.GetListener();
                HttpListenerContext context = await listener.GetContextAsync();

                HttpListenerRequest request = context.Request;
                requestAction(client, request);
            }
        }
    }
}
