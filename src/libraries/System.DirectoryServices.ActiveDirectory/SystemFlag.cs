//------------------------------------------------------------------------------
// <copyright file="SystemFlag.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;

	internal enum SystemFlag: int {
		SystemFlagNtdsNC = 1,
		SystemFlagNtdsDomain = 2
	}
}
