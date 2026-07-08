// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security
{
    // Stub partial impls for non-Linux/FreeBSD platforms. Always returns false so
    // SslStream's existing PAL paths run unchanged.
    public partial class SslStream
    {
#pragma warning disable CA1822 // partial method signature must match the Unix impl which is non-static
        private partial bool TryNextMessageViaTlsSession(ReadOnlySpan<byte> incomingBuffer, out ProtocolToken token, out int consumed)
        {
            token = default;
            consumed = 0;
            return false;
        }
#pragma warning restore CA1822
    }
}
