// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Security.Cryptography.Apple;

#pragma warning disable CS3016 // Arrays as attribute arguments are not CLS Compliant

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacFree")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static partial void HmacFree(IntPtr handle);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacCreate")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static partial SafeHmacHandle HmacCreate(PAL_HashAlgorithm algorithm, ref int cbDigest);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacInit")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial int HmacInit(SafeHmacHandle ctx, byte* pbKey, int cbKey);

        internal static unsafe int HmacInit(SafeHmacHandle ctx, ReadOnlySpan<byte> key)
        {
            fixed (byte* pKey = &MemoryMarshal.GetReference(key))
            {
                return HmacInit(ctx, pKey, key.Length);
            }
        }

        internal static int HmacUpdate(SafeHmacHandle ctx, ReadOnlySpan<byte> data) =>
            HmacUpdate(ctx, ref MemoryMarshal.GetReference(data), data.Length);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacUpdate")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial int HmacUpdate(SafeHmacHandle ctx, ref byte pbData, int cbData);

        internal static int HmacFinal(SafeHmacHandle ctx, ReadOnlySpan<byte> output) =>
            HmacFinal(ctx, ref MemoryMarshal.GetReference(output), output.Length);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacFinal")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial int HmacFinal(SafeHmacHandle ctx, ref byte pbOutput, int cbOutput);

        internal static int HmacCurrent(SafeHmacHandle ctx, ReadOnlySpan<byte> output) =>
            HmacCurrent(ctx, ref MemoryMarshal.GetReference(output), output.Length);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacCurrent")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial int HmacCurrent(SafeHmacHandle ctx, ref byte pbOutput, int cbOutput);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacOneShot")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial int HmacOneShot(
            PAL_HashAlgorithm algorithm,
            byte* pKey,
            int cbKey,
            byte* pData,
            int cbData,
            byte* pOutput,
            int cbOutput,
            int* cbDigest);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_HmacClone")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static partial SafeHmacHandle HmacClone(SafeHmacHandle ctx);
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
