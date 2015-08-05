// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Maps to the NCRYPT_KEY_USAGE_PROPERTY property flags.
    /// </summary>
    [Flags]
    public enum CngKeyUsages : int
    {
        None = 0,
        Decryption = 0x00000001,     // NCRYPT_ALLOW_DECRYPT_FLAG        
        Signing = 0x00000002,        // NCRYPT_ALLOW_SIGNING_FLAG
        KeyAgreement = 0x00000004,   // NCRYPT_ALLOW_KEY_AGREEMENT_FLAG
        AllUsages = 0xffffff,        // NCRYPT_ALLOW_ALL_USAGES
    }
}

