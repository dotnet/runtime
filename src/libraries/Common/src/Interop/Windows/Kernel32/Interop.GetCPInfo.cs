// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CPINFO
        {
            internal int MaxCharSize;

            internal InlineArray2<byte> DefaultChar;
            internal InlineArray12<byte> LeadByte;
        }

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial BOOL GetCPInfo(uint codePage, CPINFO* lpCpInfo);
    }
}
