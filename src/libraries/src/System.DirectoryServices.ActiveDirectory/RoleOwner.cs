//------------------------------------------------------------------------------
// <copyright file="RolwOwner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;

	public enum ActiveDirectoryRole: int {	
		SchemaRole= 0, 
		NamingRole = 1,
		PdcRole = 2, 
		RidRole = 3,
		InfrastructureRole = 4
	}

	public enum AdamRole: int {	
		SchemaRole = 0, 
		NamingRole = 1
	}
}
