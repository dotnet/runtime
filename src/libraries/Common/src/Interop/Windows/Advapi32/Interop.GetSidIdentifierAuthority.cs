// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SID_IDENTIFIER_AUTHORITY
        {
            public byte b1;
            public byte b2;
            public byte b3;
            public byte b4;
            public byte b5;
            public byte b6;
        }

        [DllImport(Libraries.Advapi32)]
        internal static extern IntPtr GetSidIdentifierAuthority(IntPtr sid);

        [DllImport(Interop.Libraries.Advapi32)]
        internal static extern IntPtr GetSidSubAuthority(IntPtr sid, int index);

        [DllImport(Interop.Libraries.Advapi32)]
        internal static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);
    }
}
