// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal partial class Interop
{
    internal partial class OleAut32
    {
        [DllImport(Libraries.OleAut32)]
        internal static extern uint SysStringLen(SafeBSTRHandle bstr);

        [DllImport(Libraries.OleAut32)]
        internal static extern uint SysStringLen(IntPtr bstr);

        [DllImport(Libraries.OleAut32)]
        internal static extern void SysFreeString(IntPtr bstr);
    }
}
