// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class OleAut32
    {
        [DllImport(Libraries.OleAut32)]
        internal static extern IntPtr SysAllocStringByteLen(byte[]? str, uint len);
    }
}
