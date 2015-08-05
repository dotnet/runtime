// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Maps to the "dwFlags" parameter of the NCryptOpenKey() api.
    /// </summary>
    [Flags]
    public enum CngKeyOpenOptions : int
    {
        None = 0x00000000,
        UserKey = 0x00000000,      
        MachineKey = 0x00000020,   // NCRYPT_MACHINE_KEY_FLAG
        Silent = 0x00000040,       // NCRYPT_SILENT_FLAG
    }
}

