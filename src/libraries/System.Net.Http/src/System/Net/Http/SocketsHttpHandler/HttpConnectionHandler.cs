// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class HttpConnectionHandler : HttpMessageHandler
    {
        private readonly HttpConnectionPoolManager _poolManager;

        public HttpConnectionHandler(HttpConnectionPoolManager poolManager)
        {
            _poolManager = poolManager;
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _poolManager.SendAsync(request, doRequestAuth: false, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _poolManager.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
