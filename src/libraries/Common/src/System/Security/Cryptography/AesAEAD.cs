// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static class AesAEAD
    {
        internal static void CheckKeySize(int keySizeInBytes)
        {
            if (keySizeInBytes is not (128 / 8 or 192 / 8 or 256 / 8))
            {
                throw new CryptographicException(SR.Cryptography_InvalidKeySize);
            }
        }
    }
}
