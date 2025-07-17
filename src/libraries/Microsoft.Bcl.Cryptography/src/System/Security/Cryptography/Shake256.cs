// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Security.Cryptography
{
#if !NET
    internal sealed class Shake256 : IDisposable
    {
        internal void AppendData(ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        internal void GetHashAndReset(Span<byte> destination)
        {
            throw new NotImplementedException();
        }

        internal void GetCurrentHash(Span<byte> destination)
        {
            throw new NotImplementedException();
        }

        internal Shake256 Clone()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
#elif NET8_0
    internal static class Shake256Extensions
    {
        internal static Shake256 Clone(this Shake256 shake256)
        {
            throw new PlatformNotSupportedException();
        }
    }
#endif
}
