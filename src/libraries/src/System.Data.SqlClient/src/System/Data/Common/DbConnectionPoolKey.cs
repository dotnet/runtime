// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Diagnostics;

namespace System.Data.Common
{
    // DbConnectionPoolKey: Base class implementation of a key to connection pool groups
    //  Only connection string is used as a key
    internal class DbConnectionPoolKey
    {
        private string _connectionString;

        internal DbConnectionPoolKey(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected DbConnectionPoolKey(DbConnectionPoolKey key)
        {
            _connectionString = key.ConnectionString;
        }

        internal virtual DbConnectionPoolKey Clone()
        {
            return new DbConnectionPoolKey(this);
        }

        internal virtual string ConnectionString
        {
            get
            {
                return _connectionString;
            }

            set
            {
                _connectionString = value;
            }
        }

        public override bool Equals(object obj)
        {
            DbConnectionPoolKey key = obj as DbConnectionPoolKey;
            Debug.Assert(obj.GetType() == typeof(DbConnectionPoolKey), "Derived classes should not be using DbConnectionPoolKey.Equals");

            return (key != null && _connectionString == key._connectionString);
        }

        public override int GetHashCode()
        {
            return _connectionString == null ? 0 : _connectionString.GetHashCode();
        }
    }
}
