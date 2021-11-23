// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CERT_EXTENSION
        {
            internal IntPtr pszObjId;
            internal int fCritical;
            internal DATA_BLOB Value;
        }
    }
}
