// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BigNumDestroy")]
        internal static extern void BigNumDestroy(IntPtr a);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BigNumFromBinary")]
        private static extern unsafe IntPtr BigNumFromBinary(byte* s, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BigNumToBinary")]
        private static extern unsafe int BigNumToBinary(SafeBignumHandle a, byte* to);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetBigNumBytes")]
        private static extern int GetBigNumBytes(SafeBignumHandle a);

        private static unsafe IntPtr CreateBignumPtr(ReadOnlySpan<byte> bigEndianValue)
        {
            fixed (byte* pBigEndianValue = bigEndianValue)
            {
                IntPtr ret = BigNumFromBinary(pBigEndianValue, bigEndianValue.Length);

                if (ret == IntPtr.Zero)
                {
                    throw CreateOpenSslCryptographicException();
                }

                return ret;
            }
        }

        internal static SafeBignumHandle CreateBignum(ReadOnlySpan<byte> bigEndianValue)
        {
            IntPtr handle = CreateBignumPtr(bigEndianValue);
            return new SafeBignumHandle(handle, true);
        }

        internal static byte[]? ExtractBignum(IntPtr bignum, int targetSize)
        {
            // Given that the only reference held to bignum is an IntPtr, create an unowned SafeHandle
            // to ensure that we don't destroy the key after extraction.
            using (SafeBignumHandle handle = new SafeBignumHandle(bignum, ownsHandle: false))
            {
                return ExtractBignum(handle, targetSize);
            }
        }

        internal static unsafe byte[]? ExtractBignum(SafeBignumHandle? bignum, int targetSize)
        {
            if (bignum == null || bignum.IsInvalid)
            {
                return null;
            }

            int compactSize = GetBigNumBytes(bignum);

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
                BigNumToBinary(bignum, start);
            }

            return buf;
        }
    }
}
