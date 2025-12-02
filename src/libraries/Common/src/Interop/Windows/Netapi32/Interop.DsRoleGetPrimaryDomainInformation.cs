// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Netapi32
    {
        internal enum DSROLE_PRIMARY_DOMAIN_INFO_LEVEL
        {
            DsRolePrimaryDomainInfoBasic = 1,
            DsRoleUpgradeStatus = 2,
            DsRoleOperationState = 3,
            DsRolePrimaryDomainInfoBasicEx = 4
        }

        [LibraryImport(Libraries.Netapi32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DsRoleGetPrimaryDomainInformation(
            string? lpServer,
            DSROLE_PRIMARY_DOMAIN_INFO_LEVEL InfoLevel,
            out IntPtr Buffer);
    }
}
