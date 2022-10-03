// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data.Common
{
    internal sealed class DefaultDataSource : DbDataSource
    {
        private readonly DbProviderFactory _dbProviderFactory;
        private readonly string _connectionString;

        internal DefaultDataSource(DbProviderFactory dbProviderFactory, string connectionString)
        {
            _dbProviderFactory = dbProviderFactory;
            _connectionString = connectionString;
        }

        public override string ConnectionString => _connectionString;

        protected override DbConnection CreateDbConnection()
        {
            var connection = _dbProviderFactory.CreateConnection();
            if (connection is null)
            {
                throw new InvalidOperationException("DbProviderFactory returned a null connection");
            }

            connection.ConnectionString = _connectionString;

            return connection;
        }
    }
}
