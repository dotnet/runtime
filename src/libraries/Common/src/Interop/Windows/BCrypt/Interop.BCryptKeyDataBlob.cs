// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        internal const int BCRYPT_KEY_DATA_BLOB_MAGIC = 0x4d42444b; // 'KDBM'
        internal const int BCRYPT_KEY_DATA_BLOB_VERSION1 = 1;
    }
}
