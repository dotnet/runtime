/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    UrnScheme.cs

Abstract:

    UrnScheme for IdentityClaims

History:

    23-Jul-2004    MattRim     Created

--*/

using System;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    static internal class UrnScheme
    {    
        public const string GuidScheme              = IdentityTypeStringMap.Guid;
        public const string SidScheme               = IdentityTypeStringMap.Sid;
        public const string DistinguishedNameScheme = IdentityTypeStringMap.DistinguishedName;
        public const string SamAccountScheme        = IdentityTypeStringMap.SamAccount;
        public const string UpnScheme               = IdentityTypeStringMap.Upn;
        public const string NameScheme              = IdentityTypeStringMap.Name;
    }
}