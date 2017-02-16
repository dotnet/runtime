// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Common
{
    public abstract partial class DbProviderFactory
    {
        protected DbProviderFactory() { }

        public virtual bool CanCreateDataSourceEnumerator => false;

        public virtual DbCommand CreateCommand() => null;

        public virtual DbCommandBuilder CreateCommandBuilder() => null;

        public virtual DbConnection CreateConnection() => null;

        public virtual DbConnectionStringBuilder CreateConnectionStringBuilder() => null;

        public virtual DbDataAdapter CreateDataAdapter() => null;

        public virtual DbParameter CreateParameter() => null;

        public virtual DbDataSourceEnumerator CreateDataSourceEnumerator() => null;
    }
}
