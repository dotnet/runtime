// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

