// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
    public abstract partial class DbProviderFactory
    {
        private bool? _canCreateDataAdapter;
        private bool? _canCreateCommandBuilder;

        protected DbProviderFactory() { }

        public virtual bool CanCreateDataSourceEnumerator => false;

        public virtual bool CanCreateDataAdapter
        {
            get
            {
                if (!_canCreateDataAdapter.HasValue)
                {
                    using (DbDataAdapter? adapter = CreateDataAdapter())
                    {
                        _canCreateDataAdapter = adapter != null;
                    }
                }

                return _canCreateDataAdapter.Value;
            }
        }

        public virtual bool CanCreateCommandBuilder
        {
            get
            {
                if (!_canCreateCommandBuilder.HasValue)
                {
                    using (DbCommandBuilder? builder = CreateCommandBuilder())
                    {
                        _canCreateCommandBuilder = builder != null;
                    }
                }

                return _canCreateCommandBuilder.Value;
            }
        }

        public virtual DbCommand? CreateCommand() => null;

        public virtual DbCommandBuilder? CreateCommandBuilder() => null;

        public virtual DbConnection? CreateConnection() => null;

        public virtual DbConnectionStringBuilder? CreateConnectionStringBuilder() => null;

        public virtual DbDataAdapter? CreateDataAdapter() => null;

        public virtual DbParameter? CreateParameter() => null;

        public virtual DbDataSourceEnumerator? CreateDataSourceEnumerator() => null;
    }
}
