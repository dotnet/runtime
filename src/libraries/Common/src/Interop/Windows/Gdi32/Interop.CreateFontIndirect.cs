// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        [GeneratedDllImport(Libraries.Gdi32, CharSet = CharSet.Unicode)]
        public static partial IntPtr CreateFontIndirectW(ref User32.LOGFONT lplf);
    }
}
