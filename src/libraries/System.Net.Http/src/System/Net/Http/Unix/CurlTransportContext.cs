// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
