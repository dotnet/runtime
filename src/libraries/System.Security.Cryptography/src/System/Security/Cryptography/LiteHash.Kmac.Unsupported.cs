// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    internal static partial class LiteHashProvider
    {
        internal static LiteKmac CreateKmac(string algorithmId, ReadOnlySpan<byte> key, ReadOnlySpan<byte> customizationString, bool xof)
        {
            throw new PlatformNotSupportedException();
        }
    }

    internal readonly struct LiteKmac : ILiteHash
    {
#pragma warning disable CA1822 // Member does not access instance data
#pragma warning disable IDE0060 // Remove unused parameter
        public int HashSizeInBytes => throw new UnreachableException();
        public void Append(ReadOnlySpan<byte> data) => throw new UnreachableException();
        public int Finalize(Span<byte> destination) => throw new UnreachableException();
        public int Current(Span<byte> destination) => throw new UnreachableException();
        public void Reset() => throw new UnreachableException();
        public void Dispose() => throw new UnreachableException();
#pragma warning restore IDE0060
#pragma warning restore CA1822
    }

}
