// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net
{
    internal class SslStreamContext : TransportContext
    {
        internal SslStreamContext(SslStream sslStream)
        {
            if (sslStream == null)
            {
                NetEventSource.Fail(this, "Not expecting a null sslStream!");
            }

            _sslStream = sslStream;
        }

        public override ChannelBinding GetChannelBinding(ChannelBindingKind kind)
        {
            return _sslStream.GetChannelBinding(kind);
        }

        private readonly SslStream _sslStream;
    }
}
