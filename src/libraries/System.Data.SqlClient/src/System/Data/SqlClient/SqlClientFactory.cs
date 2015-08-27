// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System.Data.Common;


namespace System.Data.SqlClient
{
    public sealed class SqlClientFactory : DbProviderFactory
    {
        public static readonly SqlClientFactory Instance = new SqlClientFactory();

        private SqlClientFactory()
        {
        }


        public override DbCommand CreateCommand()
        {
            return new SqlCommand();
        }


        public override DbConnection CreateConnection()
        {
            return new SqlConnection();
        }

        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return new SqlConnectionStringBuilder();
        }


        public override DbParameter CreateParameter()
        {
            return new SqlParameter();
        }
    }
}

