// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class AesAEAD
    {
        public static void CheckKeySize(int keySizeInBytes)
        {
            if (keySizeInBytes != (128 / 8) && keySizeInBytes != (192 / 8) && keySizeInBytes != (256 / 8))
            {
                throw new CryptographicException(SR.Cryptography_InvalidKeySize);
            }
        }
    }
}
