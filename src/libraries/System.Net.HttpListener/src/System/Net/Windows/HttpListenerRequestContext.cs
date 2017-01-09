// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net
{
    internal class HttpListenerRequestContext : TransportContext
    {
        private readonly HttpListenerRequest _request;

        internal HttpListenerRequestContext(HttpListenerRequest request)
        {
            Debug.Assert(request != null, "HttpListenerRequestContext..ctor(): Not expecting a null request!");
            _request = request;
        }

        public override ChannelBinding GetChannelBinding(ChannelBindingKind kind)
        {
            if (kind != ChannelBindingKind.Endpoint)
            {
                throw new NotSupportedException(SR.Format(
                    SR.net_listener_invalid_cbt_type, kind.ToString()));
            }
            return _request.GetChannelBinding();
        }
    }
}

