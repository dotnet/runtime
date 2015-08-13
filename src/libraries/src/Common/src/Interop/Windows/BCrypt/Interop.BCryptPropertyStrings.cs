// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class BCrypt
    {
        internal static class BCryptPropertyStrings
        {
            internal const string BCRYPT_HASH_LENGTH = "HashDigestLength";
        }
    }
}

