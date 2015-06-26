// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Authentication.ExtendedProtection;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal class WinHttpTransportContext : TransportContext
    {
        private WinHttpChannelBinding _channelBinding = null;

        internal WinHttpTransportContext()
        {
        }

        public override ChannelBinding GetChannelBinding(ChannelBindingKind kind)
        {
            // WinHTTP only supports retrieval of ChannelBindingKind.Endpoint for CBT.
            if (kind == ChannelBindingKind.Endpoint)
            {
                return _channelBinding;
            }

            return null;
        }

        internal void SetChannelBinding(SafeWinHttpHandle requestHandle)
        {
            var channelBinding = new WinHttpChannelBinding(requestHandle);

            if (channelBinding.IsInvalid)
            {
                channelBinding.Dispose();
            }
            else
            {
                _channelBinding = channelBinding;
            }
        }
    }
}
