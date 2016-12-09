//------------------------------------------------------------------------------
// <copyright file="SecurityLevel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;

	public enum ReplicationSecurityLevel: int {
		MutualAuthentication = 2, 
		Negotiate = 1,
		NegotiatePassThrough = 0
	}
}
