// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class BrowserHttpMessageHandler : HttpMessageHandler
    {
        // This partial implementation contains members common to Browser WebAssembly running on .NET Core.
        internal Interop.Browser.IHttpHandlerService? wasmHandler;

        public BrowserHttpMessageHandler()
        {
            wasmHandler = new Interop.Browser.BrowserHttpHandlerService();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (wasmHandler != null)
                {
                    wasmHandler.Dispose();
                    wasmHandler = null;
                }
            }
            base.Dispose(disposing);
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (wasmHandler == null)
                throw new ObjectDisposedException(GetType().ToString());

            return wasmHandler.SendAsync(request, cancellationToken);
        }
    }
}
