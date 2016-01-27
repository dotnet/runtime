// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

