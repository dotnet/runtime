// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Authentication.ExtendedProtection;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal sealed class WinHttpTransportContext : TransportContext
    {
        private WinHttpChannelBinding _channelBinding;

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
