// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal abstract class SP800108HmacCounterKdfImplementationBase : IDisposable
    {
        internal abstract void DeriveBytes(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination);
        internal abstract void DeriveBytes(byte[] label, byte[] context, Span<byte> destination);
        internal abstract void DeriveBytes(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination);

        public abstract void Dispose();
    }
}
