// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct TOKEN_USER
    {
        public SID_AND_ATTRIBUTES sidAndAttributes;
    }
}
