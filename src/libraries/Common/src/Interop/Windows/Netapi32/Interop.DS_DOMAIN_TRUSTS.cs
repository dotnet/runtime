// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Netapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct DS_DOMAIN_TRUSTS
        {
            public IntPtr NetbiosDomainName;
            public IntPtr DnsDomainName;
            public DS_DOMAINTRUST_FLAG Flags;
            public int ParentIndex;
            public int TrustType;
            public int TrustAttributes;
            public IntPtr DomainSid;
            public Guid DomainGuid;
        }
    }
}
