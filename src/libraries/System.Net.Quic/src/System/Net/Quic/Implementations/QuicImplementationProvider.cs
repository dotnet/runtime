// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace System.Net.Quic.Implementations
{
    public abstract class QuicImplementationProvider
    {
        internal QuicImplementationProvider() { }

        public abstract bool IsSupported { get; }

        internal virtual QuicListenerProvider CreateListener(QuicListenerOptions options)
            => throw new NotImplementedException();

        internal virtual QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options)
            => throw new NotImplementedException();
    }
}
