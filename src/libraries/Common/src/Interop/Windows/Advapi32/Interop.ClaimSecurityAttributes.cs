// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct CLAIM_SECURITY_ATTRIBUTE_INFORMATION_V1
    {
        // defined as union in CLAIM_SECURITY_ATTRIBUTES_INFORMATION
        [FieldOffset(0)]
        public IntPtr pAttributeV1;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CLAIM_SECURITY_ATTRIBUTES_INFORMATION
    {
        /// WORD->unsigned short
        public ushort Version;

        /// WORD->unsigned short
        public ushort Reserved;

        /// DWORD->unsigned int
        public uint AttributeCount;

        /// CLAIM_SECURITY_ATTRIBUTE_V1
        public CLAIM_SECURITY_ATTRIBUTE_INFORMATION_V1 Attribute;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CLAIM_SECURITY_ATTRIBUTE_FQBN_VALUE
    {
        // DWORD64->unsigned __int64
        public ulong Version;

        // PWSTR->WCHAR*
        [MarshalAsAttribute(UnmanagedType.LPWStr)]
        public string Name;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CLAIM_SECURITY_ATTRIBUTE_OCTET_STRING_VALUE
    {
        /// PVOID->void*
        public IntPtr pValue;

        /// DWORD->unsigned int
        public uint ValueLength;
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    internal struct CLAIM_VALUES_ATTRIBUTE_V1
    {
        // PLONG64->__int64*
        [FieldOffset(0)]
        public IntPtr pInt64;

        // PDWORD64->unsigned __int64*
        [FieldOffset(0)]
        public IntPtr pUint64;

        // PWSTR*
        [FieldOffset(0)]
        public IntPtr ppString;

        // PCLAIM_SECURITY_ATTRIBUTE_FQBN_VALUE->_CLAIM_SECURITY_ATTRIBUTE_FQBN_VALUE*
        [FieldOffset(0)]
        public IntPtr pFqbn;

        // PCLAIM_SECURITY_ATTRIBUTE_OCTET_STRING_VALUE->_CLAIM_SECURITY_ATTRIBUTE_OCTET_STRING_VALUE*
        [FieldOffset(0)]
        public IntPtr pOctetString;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CLAIM_SECURITY_ATTRIBUTE_V1
    {
        // PWSTR->WCHAR*
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Name;

        // WORD->unsigned short
        public ClaimSecurityAttributeType ValueType;

        // WORD->unsigned short
        public ushort Reserved;

        // DWORD->unsigned int
        public uint Flags;

        // DWORD->unsigned int
        public uint ValueCount;

        // struct CLAIM_VALUES - a union of 4 possible values
        public CLAIM_VALUES_ATTRIBUTE_V1 Values;
    }

    internal enum ClaimSecurityAttributeType : ushort
    {
        // CLAIM_SECURITY_ATTRIBUTE_TYPE_INVALID -> 0x00
        CLAIM_SECURITY_ATTRIBUTE_TYPE_INVALID = 0,

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_INT64 -> 0x01
        CLAIM_SECURITY_ATTRIBUTE_TYPE_INT64 = 1,

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_UINT64 -> 0x02
        CLAIM_SECURITY_ATTRIBUTE_TYPE_UINT64 = 2,

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_STRING -> 0x03
        CLAIM_SECURITY_ATTRIBUTE_TYPE_STRING = 3,

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_FQBN -> 0x04
        CLAIM_SECURITY_ATTRIBUTE_TYPE_FQBN = 4,

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_SID -> 0x05
        CLAIM_SECURITY_ATTRIBUTE_TYPE_SID = 5,

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_BOOLEAN -> 0x06
        CLAIM_SECURITY_ATTRIBUTE_TYPE_BOOLEAN = 6,

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_OCTET_STRING -> 0x10
        CLAIM_SECURITY_ATTRIBUTE_TYPE_OCTET_STRING = 16,
    }
}
