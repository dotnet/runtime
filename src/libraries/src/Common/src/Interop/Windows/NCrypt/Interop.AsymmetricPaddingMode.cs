// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        internal enum AsymmetricPaddingMode : int
        {
            None = 0x00000000,
            NCRYPT_NO_PADDING_FLAG = 0x00000001,
            NCRYPT_PAD_PKCS1_FLAG = 0x00000002,    // NCryptEncrypt/Decrypt or NCryptSignHash/VerifySignature
            NCRYPT_PAD_OAEP_FLAG = 0x00000004,     // NCryptEncrypt/Decrypt
            NCRYPT_PAD_PSS_FLAG = 0x00000008,      // NCryptSignHash/VerifySignature
        }
    }
}
