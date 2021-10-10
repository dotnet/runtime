// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        internal static int DigestUpdate(SafeDigestCtxHandle ctx, ReadOnlySpan<byte> data) =>
            DigestUpdate(ctx, ref MemoryMarshal.GetReference(data), data.Length);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestUpdate")]
        private static extern int DigestUpdate(SafeDigestCtxHandle ctx, ref byte pbData, int cbData);

        internal static int DigestFinal(SafeDigestCtxHandle ctx, Span<byte> output) =>
            DigestFinal(ctx, ref MemoryMarshal.GetReference(output), output.Length);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestFinal")]
        private static extern int DigestFinal(SafeDigestCtxHandle ctx, ref byte pbOutput, int cbOutput);

        internal static int DigestCurrent(SafeDigestCtxHandle ctx, Span<byte> output) =>
            DigestCurrent(ctx, ref MemoryMarshal.GetReference(output), output.Length);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestCurrent")]
        private static extern int DigestCurrent(SafeDigestCtxHandle ctx, ref byte pbOutput, int cbOutput);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestOneShot")]
        internal static unsafe extern int DigestOneShot(PAL_HashAlgorithm algorithm, byte* pbData, int cbData, byte* pbOutput, int cbOutput, out int cbDigest);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestReset")]
        internal static extern int DigestReset(SafeDigestCtxHandle ctx);
    }
}

namespace System.Security.Cryptography.Apple
{
    internal sealed class SafeDigestCtxHandle : SafeHandle
    {
        public SafeDigestCtxHandle()
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
