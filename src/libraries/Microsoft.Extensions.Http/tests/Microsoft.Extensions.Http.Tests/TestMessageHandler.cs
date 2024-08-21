// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http
{
    internal class TestMessageHandler : HttpMessageHandler
    {
        protected Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = _ => new HttpResponseMessage();

        public TestMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? responseFactory = null)
        {
            if (responseFactory is not null)
            {
                _responseFactory = responseFactory;
            }
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = _responseFactory(request);

            return Task.FromResult(response);
        }

#if NET
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => _responseFactory(request);
#endif
    }
}
