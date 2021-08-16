// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        // The returned value is a COLORREF. The docs don't say that explicitly, but
        // they do document the same macros (GetRValue, etc.). [0x00BBGGRR]
        //
        // This API sets last error, but we never check it and as such we won't take
        // the overhead in the P/Invoke. It will only fail if we try and grab a color
        // index that doesn't exist.

        [SuppressGCTransition]
        [DllImport(Libraries.User32, ExactSpelling = true)]
        internal static extern uint GetSysColor(int nIndex);
    }
}
