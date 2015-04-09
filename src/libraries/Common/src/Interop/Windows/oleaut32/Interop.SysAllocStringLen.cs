// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal partial class Interop
{
    internal partial class OleAut32
    {
        [DllImport(Libraries.OleAut32, CharSet = CharSet.Unicode)]
        internal static extern SafeBSTRHandle SysAllocStringLen(string src, uint len);  // BSTR

        [DllImport(Libraries.OleAut32, CharSet = CharSet.Unicode)]
        internal static extern SafeBSTRHandle SysAllocStringLen(IntPtr src, uint len);  // BSTR
    }
}
