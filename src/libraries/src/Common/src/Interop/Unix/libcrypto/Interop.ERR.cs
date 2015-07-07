// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        private static extern void ERR_load_crypto_strings();

        [DllImport(Libraries.LibCrypto)]
        private static extern uint ERR_get_error();

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        private static extern void ERR_error_string_n(uint e, StringBuilder buf, int len);

        private static string ERR_error_string_n(uint error)
        {
            StringBuilder buf = new StringBuilder(1024);

            ERR_error_string_n(error, buf, buf.Capacity);
            return buf.ToString();
        }

        internal static Exception CreateOpenSslCryptographicException()
        {
            const uint ReasonMask = 0x0FFF;
            const uint ERR_R_MALLOC_FAILURE = 64 | 1;

            uint error = ERR_get_error();
            uint last = error;

            while (last != 0)
            {
                last = ERR_get_error();
            }

            if (error == 0)
            {
                return new CryptographicException();
            }

            uint reason = error & ReasonMask;

            if (reason == ERR_R_MALLOC_FAILURE)
            {
                return new OutOfMemoryException();
            }

            return new CryptographicException(ERR_error_string_n(error));
        }

        internal static void CheckValidOpenSslHandle(SafeHandle handle)
        {
            if (handle == null || handle.IsInvalid)
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void CheckValidOpenSslHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw CreateOpenSslCryptographicException();
            }
        }
    }
}
