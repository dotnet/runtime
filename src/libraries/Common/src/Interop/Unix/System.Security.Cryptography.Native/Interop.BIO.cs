// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_CreateMemoryBio")]
        internal static partial SafeBioHandle CreateMemoryBio();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioNewFile", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial SafeBioHandle BioNewFile(string filename, string mode);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioDestroy")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BioDestroy(IntPtr a);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioGets")]
        private static unsafe partial int BioGets(SafeBioHandle b, byte* buf, int size);

        internal static unsafe int BioGets(SafeBioHandle b, Span<byte> buf)
        {
            fixed (byte* pBuf = buf)
            {
                return BioGets(b, pBuf, buf.Length);
            }
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioRead")]
        internal static partial int BioRead(SafeBioHandle b, byte[] data, int len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioRead")]
        private static partial int BioRead(SafeBioHandle b, Span<byte> data, int len);
        internal static int BioRead(SafeBioHandle b, Span<byte> data) => BioRead(b, data, data.Length);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioWrite")]
        internal static partial int BioWrite(SafeBioHandle b, byte[] data, int len);

        internal static int BioWrite(SafeBioHandle b, ReadOnlySpan<byte> data) =>
            BioWrite(b, ref MemoryMarshal.GetReference(data), data.Length);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioWrite")]
        private static partial int BioWrite(SafeBioHandle b, ref byte data, int len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetMemoryBioSize")]
        internal static partial int GetMemoryBioSize(SafeBioHandle bio);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioCtrlPending")]
        internal static partial int BioCtrlPending(SafeBioHandle bio);
    }
}
