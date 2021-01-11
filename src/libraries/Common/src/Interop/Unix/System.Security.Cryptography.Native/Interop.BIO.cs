// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_CreateMemoryBio")]
        internal static extern SafeBioHandle CreateMemoryBio();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioNewFile")]
        internal static extern SafeBioHandle BioNewFile(string filename, string mode);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioDestroy")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BioDestroy(IntPtr a);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioGets")]
        internal static extern int BioGets(SafeBioHandle b, byte[] buf, int size);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioRead")]
        internal static extern int BioRead(SafeBioHandle b, byte[] data, int len);

        internal static int BioRead(SafeBioHandle b, Span<byte> destination) =>
            CryptoNative_BioRead(b, ref MemoryMarshal.GetReference(destination), destination.Length);

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_BioRead(SafeBioHandle b, ref byte data, int len);

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_BioRead(IntPtr b, ref byte data, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioWrite")]
        internal static extern int BioWrite(SafeBioHandle b, byte[] data, int len);

        internal static int BioWrite(SafeBioHandle b, ReadOnlySpan<byte> data) =>
            BioWrite(b, ref MemoryMarshal.GetReference(data), data.Length);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioWrite")]
        private static extern int BioWrite(SafeBioHandle b, ref byte data, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetMemoryBioSize")]
        internal static extern int GetMemoryBioSize(SafeBioHandle bio);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetMemoryBioSize")]
        private static extern int GetMemoryBioSize(IntPtr bio);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioCtrlPending")]
        internal static extern int BioCtrlPending(SafeBioHandle bio);

        internal static bool TryReadMemoryBio(SafeBioHandle source, Span<byte> destination, out int bytesWritten)
        {
            bool addedRef = false;

            try
            {
                source.DangerousAddRef(ref addedRef);
                IntPtr sourcePtr = source.DangerousGetHandle();

                int size = GetMemoryBioSize(sourcePtr);

                if (destination.Length < size)
                {
                    bytesWritten = 0;
                    return false;
                }

                bytesWritten = CryptoNative_BioRead(
                    sourcePtr,
                    ref MemoryMarshal.GetReference(destination),
                    destination.Length);

                Debug.Assert(bytesWritten == size);
                return true;
            }
            finally
            {
                if (addedRef)
                {
                    source.DangerousRelease();
                }
            }
        }
    }
}
