//------------------------------------------------------------------------------
// <copyright file="WellKnownDN.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;

	internal enum WellKnownDN: int {
		RootDSE = 0,
		DefaultNamingContext = 1,
		SchemaNamingContext = 2,
		ConfigurationNamingContext = 3,
		PartitionsContainer = 4,
		SitesContainer = 5,
		SystemContainer = 6,
		RidManager = 7,
		Infrastructure = 8,
		RootDomainNamingContext = 9,
		Schema = 10
	}
}
