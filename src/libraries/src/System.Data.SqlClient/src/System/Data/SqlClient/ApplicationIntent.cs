// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


//------------------------------------------------------------------------------

namespace System.Data.SqlClient
{
    /// <summary>
    /// represents the application workload type when connecting to a server
    /// </summary>
    public enum ApplicationIntent
    {
        ReadWrite = 0,
        ReadOnly = 1,
    }
}
