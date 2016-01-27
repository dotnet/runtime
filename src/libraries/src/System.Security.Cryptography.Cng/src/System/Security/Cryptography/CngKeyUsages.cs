// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

