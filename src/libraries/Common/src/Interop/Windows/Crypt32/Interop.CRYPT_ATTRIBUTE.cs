// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CRYPT_ATTRIBUTE
        {
            internal IntPtr pszObjId;
            internal int cValue;
            internal DATA_BLOB* rgValue;
        }
    }
}
