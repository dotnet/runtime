// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication.ExtendedProtection;
using System.Threading;

namespace System.Net.Http
{
    internal sealed class CurlTransportContext : TransportContext
    {
        private CurlChannelBinding _channelBinding;

        internal CurlChannelBinding CurlChannelBinding => LazyInitializer.EnsureInitialized(ref _channelBinding);

        public override ChannelBinding GetChannelBinding(ChannelBindingKind kind) =>
            kind == ChannelBindingKind.Endpoint ? CurlChannelBinding : null;
            // Parity with WinHTTP : CurHandler only supports retrieval of ChannelBindingKind.Endpoint for CBT.
    }
}
