// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Version
    {
        [DllImport(Libraries.Version, CharSet = CharSet.Unicode, EntryPoint = "GetFileVersionInfoExW")]
        internal static extern bool GetFileVersionInfoEx(
                    uint dwFlags,
                    string lpwstrFilename,
                    uint dwHandle,
                    uint dwLen,
                    IntPtr lpData);
    }
}
