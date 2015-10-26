// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeBioHandle CreateMemoryBio();

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeBioHandle BioNewFile(string filename, string mode);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BioDestroy(IntPtr a);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int BioGets(SafeBioHandle b, byte[] buf, int size);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int BioRead(SafeBioHandle b, byte[] data, int len);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int BioWrite(SafeBioHandle b, byte[] data, int len);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int GetMemoryBioSize(SafeBioHandle bio);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int BioCtrlPending(SafeBioHandle bio);
    }
}
