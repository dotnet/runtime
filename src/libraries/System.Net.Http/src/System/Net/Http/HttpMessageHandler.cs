// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this);
        }

        // We cannot add abstract member to a public class in order to not to break already established contract of this class.
        // So we add virtual method, override it everywhere internally and provide proper implementation.
        // Unfortunately we cannot force everyone to implement so in such case we have no other option that to do sync-over-async.
        protected internal virtual HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Doing sync-over-async due to lack of {nameof(Send)} override");
            return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
        }

        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            // Nothing to do in base class.
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
