// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class HttpConnectionHandler : HttpMessageHandlerStage
    {
        private readonly HttpConnectionPoolManager _poolManager;
        private readonly bool _doRequestAuth;

        public HttpConnectionHandler(HttpConnectionPoolManager poolManager, bool doRequestAuth)
        {
            _poolManager = poolManager;
            _doRequestAuth = doRequestAuth;
        }

        internal override async ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            bool doRequestAuth = _doRequestAuth && !request.IsAuthDisabled();
            return await _poolManager.SendAsync(request, async, doRequestAuth, cancellationToken).ConfigureAwait(false);
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
