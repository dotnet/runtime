// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        internal uint LowPart;
        internal int HighPart;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct TOKEN_GROUPS
    {
        internal uint GroupCount;
        internal SID_AND_ATTRIBUTES Groups; // SID_AND_ATTRIBUTES Groups[ANYSIZE_ARRAY];
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SID_AND_ATTRIBUTES
    {
        internal IntPtr Sid;
        internal uint Attributes;
        internal static readonly long SizeOf = (long)Marshal.SizeOf<SID_AND_ATTRIBUTES>();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct TOKEN_PRIMARY_GROUP
    {
        internal IntPtr PrimaryGroup;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct TOKEN_STATISTICS
    {
        internal LUID TokenId;
        internal LUID AuthenticationId;
        internal long ExpirationTime;
        internal uint TokenType;
        internal uint ImpersonationLevel;
        internal uint DynamicCharged;
        internal uint DynamicAvailable;
        internal uint GroupCount;
        internal uint PrivilegeCount;
        internal LUID ModifiedId;
    }
}
