// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net
{
    internal sealed class SslStreamContext : TransportContext
    {
        private readonly SslStream _sslStream;

        internal SslStreamContext(SslStream sslStream)
        {
            Debug.Assert(sslStream != null);
            _sslStream = sslStream!;
        }

        public override ChannelBinding? GetChannelBinding(ChannelBindingKind kind) =>
            _sslStream.GetChannelBinding(kind);
    }
}
