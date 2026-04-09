// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal struct TOKEN_PRIVILEGE
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges /*[ANYSIZE_ARRAY]*/;
        }
    }
}
