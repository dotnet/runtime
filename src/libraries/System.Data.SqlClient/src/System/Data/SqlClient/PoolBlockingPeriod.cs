// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Data.SqlClient
{
    public enum PoolBlockingPeriod
    {
        Auto = 0,         // Blocking period OFF for Azure SQL servers, but ON for all other SQL servers.
        AlwaysBlock = 1,  // Blocking period ON for all SQL servers including Azure SQL servers.    
        NeverBlock = 2,   // Blocking period OFF for all SQL servers including Azure SQL servers.
    }
}
