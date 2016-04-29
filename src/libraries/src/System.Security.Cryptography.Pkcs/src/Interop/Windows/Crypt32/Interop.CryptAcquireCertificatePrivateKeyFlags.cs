// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum CryptAcquireCertificatePrivateKeyFlags : int
        {
            CRYPT_ACQUIRE_CACHE_FLAG = 0x00000001,
            CRYPT_ACQUIRE_USE_PROV_INFO_FLAG = 0x00000002,
            CRYPT_ACQUIRE_COMPARE_KEY_FLAG = 0x00000004,
            CRYPT_ACQUIRE_NO_HEALING = 0x00000008,

            CRYPT_ACQUIRE_SILENT_FLAG = 0x00000040,
            CRYPT_ACQUIRE_WINDOW_HANDLE_FLAG = 0x00000080,

            CRYPT_ACQUIRE_NCRYPT_KEY_FLAGS_MASK = 0x00070000,
            CRYPT_ACQUIRE_ALLOW_NCRYPT_KEY_FLAG = 0x00010000,
            CRYPT_ACQUIRE_PREFER_NCRYPT_KEY_FLAG = 0x00020000,
            CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG = 0x00040000,
        }
    }
}

