// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Http
{
    internal class CurlTransportContext : TransportContext
    {
        private CurlChannelBinding _channelBinding = new CurlChannelBinding();

        internal CurlChannelBinding CurlChannelBinding
        {
            get
            {
                return _channelBinding;
            }
        }

        internal CurlTransportContext()
        {
        }

        public override ChannelBinding GetChannelBinding(ChannelBindingKind kind)
        {
            // Parity with WinHTTP : CurHandler only supports retrieval of ChannelBindingKind.Endpoint for CBT.
            if (kind == ChannelBindingKind.Endpoint)
            {
                return _channelBinding;
            }

            return null;
        }
    }
}
