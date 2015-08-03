// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

using NativeLong=System.IntPtr;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        private const int BIO_CTRL_INFO = 3;

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeBioHandle BIO_new(IntPtr type);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeBioHandle BIO_new_file(string filename, string mode);

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr BIO_s_mem();

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BIO_free(IntPtr a);

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        internal static extern int BIO_gets(SafeBioHandle b, StringBuilder buf, int size);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int BIO_write(SafeBioHandle b, byte[] data, int len);

        [DllImport(Libraries.LibCrypto)]
        private static extern NativeLong BIO_ctrl(SafeBioHandle bio, int cmd, NativeLong larg, IntPtr parg);

        internal static int GetMemoryBioSize(SafeBioHandle bio)
        {
            // This method is equivalent to BIO_get_mem_data(bio, NULL), except not a macro,
            // and doesn't expose the NULL.
            return BIO_ctrl(bio, BIO_CTRL_INFO, IntPtr.Zero, IntPtr.Zero).ToInt32();
        }
    }
}
