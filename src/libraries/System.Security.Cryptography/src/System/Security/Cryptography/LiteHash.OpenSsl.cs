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
            // Shared handle - do not dispose.
            SafeEvpMacHandle? mac = algorithmId switch
            {
                HashAlgorithmNames.KMAC128 => Interop.Crypto.EvpMacAlgs.Kmac128,
                HashAlgorithmNames.KMAC256 => Interop.Crypto.EvpMacAlgs.Kmac256,
                _ => throw new CryptographicException(),
            };

            if (mac is null)
            {
                Debug.Fail("Platform should have previously checked support for algorithm.");
                throw new UnreachableException();
            }

            return new LiteKmac(mac, key, customizationString, xof);
        }
    }

    internal readonly struct LiteKmac : ILiteHash
    {
        private readonly SafeEvpMacCtxHandle _ctx;

        public int HashSizeInBytes => throw new NotSupportedException();

        internal LiteKmac(SafeEvpMacHandle algorithm, ReadOnlySpan<byte> key, ReadOnlySpan<byte> customizationString, bool xof)
        {
            Debug.Assert(!algorithm.IsInvalid);
            _ctx = Interop.Crypto.EvpMacCtxNew(algorithm);
            Interop.Crypto.EvpMacInit(_ctx, key, customizationString, xof);
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            Interop.Crypto.EvpMacUpdate(_ctx, data);
        }

        public int Current(Span<byte> destination)
        {
            Interop.Crypto.EvpMacCurrent(_ctx, destination);
            return destination.Length;
        }

        public int Finalize(Span<byte> destination)
        {
            Interop.Crypto.EvpMacFinal(_ctx, destination);
            return destination.Length;
        }

        public void Reset() => Interop.Crypto.EvpMacReset(_ctx);
        public void Dispose() => _ctx.Dispose();
    }
}
