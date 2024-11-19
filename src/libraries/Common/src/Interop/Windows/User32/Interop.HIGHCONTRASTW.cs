// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public unsafe partial struct HIGHCONTRASTW
        {
            internal uint cbSize;
            internal HIGHCONTRASTW_FLAGS dwFlags;
            internal void* lpszDefaultScheme;
        }

        [Flags]
        public enum HIGHCONTRASTW_FLAGS : uint
        {
            HCF_HIGHCONTRASTON = 0x00000001,
            HCF_AVAILABLE = 0x00000002,
            HCF_HOTKEYACTIVE = 0x00000004,
            HCF_CONFIRMHOTKEY = 0x00000008,
            HCF_HOTKEYSOUND = 0x00000010,
            HCF_INDICATOR = 0x00000020,
            HCF_HOTKEYAVAILABLE = 0x00000040,
            HCF_OPTION_NOTHEMECHANGE = 0x00001000
        }
    }
}
