// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        public static bool UsesIv(this CipherMode cipherMode)
        {
            return cipherMode != CipherMode.ECB;
        }

        public static byte[] GetCipherIv(this CipherMode cipherMode, byte[] iv)
        {
            if (cipherMode.UsesIv())
            {
                if (iv == null)
                {
                    throw new CryptographicException(SR.Cryptography_MissingIV);
                }

                return iv;
            }

            return null;
        }

        public static byte[] TrimLargeIV(byte[] currentIV, int blockSizeInBits)
        {
            int blockSizeBytes = checked((blockSizeInBits + 7) / 8);

            if (currentIV?.Length > blockSizeBytes)
            {
                byte[] tmp = new byte[blockSizeBytes];
                Buffer.BlockCopy(currentIV, 0, tmp, 0, tmp.Length);
                return tmp;
            }

            return currentIV;
        }
    }
}
