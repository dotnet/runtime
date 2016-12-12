/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    IdentityType.cs

Abstract:

History:

    15-Mar-2005    RichaK     Created

--*/

using System;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{

    public enum IdentityType
    {
        SamAccountName	=	0,
	 Name = 1,
        UserPrincipalName	=	2,
        DistinguishedName	= 	3,
        Sid	=	4,
        Guid	=	5
    }

internal class IdentMap
{
    private IdentMap() { }
    
    internal static object[,] StringMap = { 
        {IdentityType.SamAccountName, IdentityTypeStringMap.SamAccount},
        {IdentityType.Name, IdentityTypeStringMap.Name},
         {IdentityType.UserPrincipalName, IdentityTypeStringMap.Upn},
         {IdentityType.DistinguishedName, IdentityTypeStringMap.DistinguishedName},
         {IdentityType.Sid, IdentityTypeStringMap.Sid},
         {IdentityType.Guid, IdentityTypeStringMap.Guid}};
}      
    static internal class IdentityTypeStringMap
    {    
        public const string Guid              = "ms-guid";
        public const string Sid               = "ms-sid";
        public const string DistinguishedName = "ldap-dn";
        public const string SamAccount        = "ms-nt4account";
        public const string Upn               = "ms-upn";
	 public const string Name = "ms-name";
    }
    
}
