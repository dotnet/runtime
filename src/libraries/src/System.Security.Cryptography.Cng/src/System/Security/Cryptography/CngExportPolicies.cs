// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Maps to the NCRYPT_EXPORT_POLICY_PROPERTY property flags.
    /// </summary>
    [Flags]
    public enum CngExportPolicies : int
    {
        None = 0x00000000,
        AllowExport = 0x00000001,               // NCRYPT_ALLOW_EXPORT_FLAG
        AllowPlaintextExport = 0x00000002,      // NCRYPT_ALLOW_PLAINTEXT_EXPORT_FLAG
        AllowArchiving = 0x00000004,            // NCRYPT_ALLOW_ARCHIVING_FLAG
        AllowPlaintextArchiving = 0x00000008,   // NCRYPT_ALLOW_PLAINTEXT_ARCHIVING_FLAG
    }
}

