// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    // TODO: [AndroidCrypto] Rename class to AndroidCrypto once all consumers are split in Android vs. Unix
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_BigNumToBinary")]
        private static extern unsafe int BigNumToBinary(SafeBignumHandle a, byte* to);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_GetBigNumBytes")]
        private static extern int GetBigNumBytes(SafeBignumHandle a);

        internal static unsafe byte[]? ExtractBignum(SafeBignumHandle? bignum, int targetSize)
        {
            if (bignum == null || bignum.IsInvalid)
                return null;

            int compactSize = GetBigNumBytes(bignum);
            if (targetSize < compactSize)
            {
                targetSize = compactSize;
            }

            // Android does not include leading zeroes (uses minimum number of bytes required to represent the value).
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

namespace System.Security.Cryptography
{
    internal sealed class SafeBignumHandle : Interop.JObjectLifetime.SafeJObjectHandle
    {
        public SafeBignumHandle()
        {
        }
    }
}
