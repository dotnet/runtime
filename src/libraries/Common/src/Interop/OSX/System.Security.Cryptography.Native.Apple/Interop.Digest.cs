// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestFree")]
        internal static extern void DigestFree(IntPtr handle);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestCreate")]
        internal static extern SafeDigestCtxHandle DigestCreate(PAL_HashAlgorithm algorithm, out int cbDigest);

        internal static int DigestUpdate(SafeDigestCtxHandle ctx, ReadOnlySpan<byte> pbData, int cbData) =>
            DigestUpdate(ctx, ref MemoryMarshal.GetReference(pbData), cbData);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestUpdate")]
        private static extern int DigestUpdate(SafeDigestCtxHandle ctx, ref byte pbData, int cbData);

        internal static int DigestFinal(SafeDigestCtxHandle ctx, Span<byte> pbOutput, int cbOutput) =>
            DigestFinal(ctx, ref MemoryMarshal.GetReference(pbOutput), cbOutput);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestFinal")]
        private static extern int DigestFinal(SafeDigestCtxHandle ctx, ref byte pbOutput, int cbOutput);
    }
}

namespace System.Security.Cryptography.Apple
{
    internal sealed class SafeDigestCtxHandle : SafeHandle
    {
        internal SafeDigestCtxHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.AppleCrypto.DigestFree(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
