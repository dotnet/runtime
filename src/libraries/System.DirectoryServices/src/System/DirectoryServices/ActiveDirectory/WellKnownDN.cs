// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    internal enum WellKnownDN : int
    {
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
