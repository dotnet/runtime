// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    public enum IdentityType
    {
        SamAccountName = 0,
        Name = 1,
        UserPrincipalName = 2,
        DistinguishedName = 3,
        Sid = 4,
        Guid = 5
    }

    internal static class IdentMap
    {
        internal static object[,] StringMap = {
            {IdentityType.SamAccountName, IdentityTypeStringMap.SamAccount},
            {IdentityType.Name, IdentityTypeStringMap.Name},
            {IdentityType.UserPrincipalName, IdentityTypeStringMap.Upn},
            {IdentityType.DistinguishedName, IdentityTypeStringMap.DistinguishedName},
            {IdentityType.Sid, IdentityTypeStringMap.Sid},
            {IdentityType.Guid, IdentityTypeStringMap.Guid}
        };
    }

    internal static class IdentityTypeStringMap
    {
        public const string Guid = "ms-guid";
        public const string Sid = "ms-sid";
        public const string DistinguishedName = "ldap-dn";
        public const string SamAccount = "ms-nt4account";
        public const string Upn = "ms-upn";
        public const string Name = "ms-name";
    }
}
