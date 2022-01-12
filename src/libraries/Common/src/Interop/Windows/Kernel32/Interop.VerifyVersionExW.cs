// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const byte VER_GREATER_EQUAL = 0x3;
        internal const uint VER_MAJORVERSION = 0x0000002;
        internal const uint VER_MINORVERSION = 0x0000001;
        internal const uint VER_SERVICEPACKMAJOR = 0x0000020;
        internal const uint VER_SERVICEPACKMINOR = 0x0000010;

        [GeneratedDllImport(Libraries.Kernel32)]
        internal static partial bool VerifyVersionInfoW(ref OSVERSIONINFOEX lpVersionInfo, uint dwTypeMask, ulong dwlConditionMask);
    }
}
