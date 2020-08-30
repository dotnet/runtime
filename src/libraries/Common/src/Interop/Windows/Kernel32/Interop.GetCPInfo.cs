// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CPINFO
        {
            internal int MaxCharSize;

            internal fixed byte DefaultChar[2 /* MAX_DEFAULTCHAR */];
            internal fixed byte LeadByte[12 /* MAX_LEADBYTES */];
        }

        [DllImport(Libraries.Kernel32)]
        internal static extern unsafe Interop.BOOL GetCPInfo(uint codePage, CPINFO* lpCpInfo);
    }
}
