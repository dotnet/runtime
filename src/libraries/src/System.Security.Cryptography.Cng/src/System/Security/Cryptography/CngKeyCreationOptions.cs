// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}

