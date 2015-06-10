// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        internal static extern void BN_clear_free(IntPtr a);

        [DllImport(Libraries.LibCrypto)]
        private static extern IntPtr BN_bin2bn(byte[] s, int len, IntPtr zero);

        [DllImport(Libraries.LibCrypto)]
        private static extern unsafe int BN_bn2bin(SafeBignumHandle a, byte* to);

        [DllImport(Libraries.LibCrypto)]
        private static extern int BN_num_bits(SafeBignumHandle a);

        /// <summary>
        /// Returns the number of bytes needed to export a BIGNUM.
        /// </summary>
        /// <remarks>This is a macro in bn.h, expanded here.</remarks>
        private static int BN_num_bytes(SafeBignumHandle a)
        {
            return (BN_num_bits(a) + 7) / 8;
        }

        internal static IntPtr CreateBignumPtr(byte[] bigEndianValue)
        {
            if (bigEndianValue == null)
            {
                return IntPtr.Zero;
            }

            IntPtr handle = BN_bin2bn(bigEndianValue, bigEndianValue.Length, IntPtr.Zero);
            return handle;
        }

        internal static SafeBignumHandle CreateBignum(byte[] bigEndianValue)
        {
            IntPtr handle = CreateBignumPtr(bigEndianValue);
            return new SafeBignumHandle(handle, true);
        }

        private static byte[] ExtractBignum(IntPtr bignum, int targetSize)
        {
            // Given that the only reference held to bignum is an IntPtr, create an unowned SafeHandle
            // to ensure that we don't destroy the key after extraction.
            using (SafeBignumHandle handle = new SafeBignumHandle(bignum, ownsHandle: false))
            {
                return ExtractBignum(handle, targetSize);
            }
        }

        private static unsafe byte[] ExtractBignum(SafeBignumHandle bignum, int targetSize)
        {
            if (bignum == null || bignum.IsInvalid)
            {
                return null;
            }

            int compactSize = BN_num_bytes(bignum);

            if (targetSize < compactSize)
            {
                targetSize = compactSize;
            }

            // OpenSSL BIGNUM values do not record leading zeroes.
            // Windows Crypt32 does.
            //
            // Since RSACryptoServiceProvider already checks that RSAParameters.DP.Length is
            // exactly half of RSAParameters.Modulus.Length, we need to left-pad (big-endian)
            // the array with zeroes.
            int offset = targetSize - compactSize;

            byte[] buf = new byte[targetSize];

            fixed (byte* to = buf)
            {
                byte* start = to + offset;
                BN_bn2bin(bignum, start);
            }

            return buf;
        }
    }
}
