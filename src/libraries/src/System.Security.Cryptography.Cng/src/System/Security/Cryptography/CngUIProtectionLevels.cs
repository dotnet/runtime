// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Maps to the NCRYPT_UI_POLICY_PROPERTY property flags.
    /// </summary>
    [Flags]
    public enum CngUIProtectionLevels : int
    {
        None = 0,
        ProtectKey = 0x00000001,            // NCRYPT_UI_PROTECT_KEY_FLAG
        ForceHighProtection = 0x00000002,   // NCRYPT_UI_FORCE_HIGH_PROTECTION_FLAG
    }
}

