// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Maps to the "dwFlags" parameter of the NCryptCreatePersistedKey() api.
    /// </summary>
    [Flags]
    public enum CngKeyCreationOptions : int
    {
        None = 0x00000000,
        MachineKey = 0x00000020,            // NCRYPT_MACHINE_KEY_FLAG
        OverwriteExistingKey = 0x00000080,  // NCRYPT_OVERWRITE_KEY_FLAG
        PreferVbs = 0x00010000,             // NCRYPT_PREFER_VBS_FLAG
        RequireVbs = 0x00020000,            // NCRYPT_REQUIRE_VBS_FLAG
        UsePerBootKey = 0x00040000,         // NCRYPT_USE_PER_BOOT_KEY_FLAG
    }
}
