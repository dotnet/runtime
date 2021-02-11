// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        [DllImport(Libraries.User32, SetLastError = true, EntryPoint = "LoadStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern unsafe int LoadString(IntPtr hInstance, uint uID, char* lpBuffer, int cchBufferMax);
    }
}
