// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacFree")]
        internal static extern void HmacFree(IntPtr handle);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacCreate")]
        internal static extern SafeHmacHandle HmacCreate(PAL_HashAlgorithm algorithm, ref int cbDigest);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacInit")]
        internal static extern int HmacInit(SafeHmacHandle ctx, [In] byte[] pbKey, int cbKey);

        internal static int HmacUpdate(SafeHmacHandle ctx, ReadOnlySpan<byte> data) =>
            HmacUpdate(ctx, ref MemoryMarshal.GetReference(data), data.Length);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacUpdate")]
        private static extern int HmacUpdate(SafeHmacHandle ctx, ref byte pbData, int cbData);

        internal static int HmacFinal(SafeHmacHandle ctx, ReadOnlySpan<byte> output) =>
            HmacFinal(ctx, ref MemoryMarshal.GetReference(output), output.Length);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacFinal")]
        private static extern int HmacFinal(SafeHmacHandle ctx, ref byte pbOutput, int cbOutput);

        internal static int HmacCurrent(SafeHmacHandle ctx, ReadOnlySpan<byte> output) =>
            HmacCurrent(ctx, ref MemoryMarshal.GetReference(output), output.Length);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacCurrent")]
        private static extern int HmacCurrent(SafeHmacHandle ctx, ref byte pbOutput, int cbOutput);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacOneShot")]
        internal static unsafe extern int HmacOneShot(
            PAL_HashAlgorithm algorithm,
            byte* pKey,
            int cbKey,
            byte* pData,
            int cbData,
            byte* pOutput,
            int cbOutput,
            out int cbDigest);
    }
}

namespace System.Security.Cryptography.Apple
{
    internal sealed class SafeHmacHandle : SafeHandle
    {
        public SafeHmacHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.AppleCrypto.HmacFree(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
