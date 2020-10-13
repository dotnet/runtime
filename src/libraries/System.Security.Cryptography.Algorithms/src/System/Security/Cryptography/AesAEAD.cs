// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal partial class AesAEAD
    {
        public static void CheckKeySize(int keySizeInBits)
        {
            if (keySizeInBits != 128 && keySizeInBits != 192 && keySizeInBits != 256)
            {
                throw new CryptographicException(SR.Cryptography_InvalidKeySize);
            }
        }

        public static void CheckArgumentsForNull(
            byte[] nonce,
            byte[] plaintext,
            byte[] ciphertext,
            byte[] tag)
        {
            if (nonce is null)
                throw new ArgumentNullException(nameof(nonce));

            if (plaintext is null)
                throw new ArgumentNullException(nameof(plaintext));

            if (ciphertext is null)
                throw new ArgumentNullException(nameof(ciphertext));

            if (tag is null)
                throw new ArgumentNullException(nameof(tag));
        }
    }
}
