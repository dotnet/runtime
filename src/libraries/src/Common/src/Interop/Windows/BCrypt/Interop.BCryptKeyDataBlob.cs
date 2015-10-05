// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class BCrypt
    {
        internal const int BCRYPT_KEY_DATA_BLOB_MAGIC = 0x4d42444b; // 'KDBM'
        internal const int BCRYPT_KEY_DATA_BLOB_VERSION1 = 1;
    }
}

