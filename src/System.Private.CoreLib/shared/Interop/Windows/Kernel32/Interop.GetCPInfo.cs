// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
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
