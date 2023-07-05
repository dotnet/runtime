// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Test.Common
{
    public class HttpMessageHandlerLoopbackServer : GenericLoopbackServer
    {
        HttpRequestMessage _request;
        public HttpStatusCode ResponseStatusCode;
        public IList<HttpHeaderData> ResponseHeaders;
        public string ResponseContentString;
        public byte[] ResponseContentBytes;

        private HttpMessageHandlerLoopbackServer(HttpRequestMessage request)
        {
            _request = request;
        }

        public static async Task CreateClientAndServerAsync(Func<HttpMessageHandler, Uri, Task> clientFunc, Func<HttpMessageHandlerLoopbackServer, Task> serverFunc)
        {
            await clientFunc(new LoopbackServerHttpMessageHandler(serverFunc), new Uri("http://example.com")).ConfigureAwait(false);
        }

        public async override Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            ResponseStatusCode = statusCode;
            ResponseHeaders = headers;
            ResponseContentString = content;
            return await HttpRequestData.FromHttpRequestMessageAsync(_request).ConfigureAwait(false);
        }

        public async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode, IList<HttpHeaderData> headers, byte[] bytes)
        {
            ResponseStatusCode = statusCode;
            ResponseHeaders = headers;
            ResponseContentBytes = bytes;
            return await HttpRequestData.FromHttpRequestMessageAsync(_request).ConfigureAwait(false);
        }

        public override Task AcceptConnectionAsync(Func<GenericLoopbackConnection, Task> funcAsync) => throw new NotImplementedException();

        public override Task<GenericLoopbackConnection> EstablishGenericConnectionAsync() => throw new NotImplementedException();

        public override void Dispose() { }

        class LoopbackServerHttpMessageHandler : HttpMessageHandler
        {
            Func<HttpMessageHandlerLoopbackServer, Task> _serverFunc;

            public LoopbackServerHttpMessageHandler(Func<HttpMessageHandlerLoopbackServer, Task> serverFunc)
            {
                _serverFunc = serverFunc;
            }

#if NETCOREAPP
            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
            }
#endif

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var server = new HttpMessageHandlerLoopbackServer(request);
                await _serverFunc(server).ConfigureAwait(false);

                var response = new HttpResponseMessage(server.ResponseStatusCode);
                if (server.ResponseContentString != null)
                {
                    response.Content = new StringContent(server.ResponseContentString);
                }
                else
                {
                    response.Content = new ByteArrayContent(server.ResponseContentBytes);
                }

                foreach (var header in server.ResponseHeaders ?? Array.Empty<HttpHeaderData>())
                {
                    if (String.Equals(header.Name, "Content-Type", StringComparison.InvariantCultureIgnoreCase))
                    {
                        response.Content.Headers.Remove("Content-Type");
                        response.Content.Headers.TryAddWithoutValidation("Content-Type", header.Value);
                    }
                    else
                    {
                        response.Headers.Add(header.Name, header.Value);
                    }
                }

                return response;
            }
        }
    }
}
