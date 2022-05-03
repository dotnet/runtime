// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Wtsapi32
    {
        [LibraryImport(Libraries.Wtsapi32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);
    }
}
