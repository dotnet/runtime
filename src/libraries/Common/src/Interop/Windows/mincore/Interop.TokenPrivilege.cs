// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class mincore
    {
        internal unsafe partial struct TOKEN_PRIVILEGE
        {
            public uint PrivilegeCount;
            public InlineArray_LUID_AND_ATTRIBUTES_1 Privileges;
        }
    }
}