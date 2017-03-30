// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------

namespace System.Data.SqlClient
{
    /// <summary>
    /// represents the application workload type when connecting to a server
    /// </summary>
    [Serializable]
    public enum ApplicationIntent
    {
        ReadWrite = 0,
        ReadOnly = 1,
    }
}
