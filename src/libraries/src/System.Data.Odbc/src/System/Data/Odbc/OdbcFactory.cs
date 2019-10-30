// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;

namespace System.Data.Odbc
{
    public sealed partial class OdbcFactory : DbProviderFactory
    {
        public static readonly OdbcFactory Instance = new OdbcFactory();

        private OdbcFactory()
        {
        }

        public override DbCommand CreateCommand()
        {
            return new OdbcCommand();
        }

        public override DbCommandBuilder CreateCommandBuilder()
        {
            return new OdbcCommandBuilder();
        }

        public override DbConnection CreateConnection()
        {
            return new OdbcConnection();
        }

        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return new OdbcConnectionStringBuilder();
        }

        public override DbDataAdapter CreateDataAdapter()
        {
            return new OdbcDataAdapter();
        }

        public override DbParameter CreateParameter()
        {
            return new OdbcParameter();
        }
    }
}
