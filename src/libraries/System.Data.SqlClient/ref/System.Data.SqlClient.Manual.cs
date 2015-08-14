// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Data.SqlClient
{
    public sealed partial class SqlCommand : System.Data.Common.DbCommand
    {
        // SqlCommand expects IDisposable methods to be implemented via System.ComponentModel.Component, which it no longer inherits from
        override protected void Dispose(bool disposing) { }
    }
    public sealed partial class SqlConnection : System.Data.Common.DbConnection
    {
        // SqlConection expects IDisposable methods to be implemented via System.ComponentModel.Component, which it no longer inherits from
        override protected void Dispose(bool disposing) { }
    }
}
