// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_TRANSLATED_NAME
    {
        internal int Use;
        internal UNICODE_INTPTR_STRING Name;
        internal int DomainIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_TRANSLATED_SID2
    {
        internal int Use;
        internal IntPtr Sid;
        internal int DomainIndex;
        private uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_TRUST_INFORMATION
    {
        internal UNICODE_INTPTR_STRING Name;
        internal IntPtr Sid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_REFERENCED_DOMAIN_LIST
    {
        internal int Entries;
        internal IntPtr Domains;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct UNICODE_INTPTR_STRING
    {
        internal ushort Length;
        internal ushort MaxLength;
        internal IntPtr Buffer;
    }
}
