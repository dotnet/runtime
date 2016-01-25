// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Assert("SslStreamContext..ctor(): Not expecting a null sslStream!");
                }

                Debug.Fail("SslStreamContext..ctor(): Not expecting a null sslStream!");
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
