// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System.Data.Common;

namespace System.Data.SqlClient
{
    // SqlConnectionPoolKey: Implementation of a key to connection pool groups for specifically to be used for SqlConnection
    //  Connection string and SqlCredential are used as a key
    internal class SqlConnectionPoolKey : DbConnectionPoolKey
    {
        private int _hashValue;

        internal SqlConnectionPoolKey(string connectionString) : base(connectionString)
        {
            CalculateHashCode();
        }

        private SqlConnectionPoolKey(SqlConnectionPoolKey key) : base(key)
        {
            CalculateHashCode();
        }

        internal override DbConnectionPoolKey Clone()
        {
            return new SqlConnectionPoolKey(this);
        }

        internal override string ConnectionString
        {
            get
            {
                return base.ConnectionString;
            }

            set
            {
                base.ConnectionString = value;
                CalculateHashCode();
            }
        }



        public override bool Equals(object obj)
        {
            SqlConnectionPoolKey key = obj as SqlConnectionPoolKey;
            return (key != null &&
                ConnectionString == key.ConnectionString);
        }

        public override int GetHashCode()
        {
            return _hashValue;
        }

        private void CalculateHashCode()
        {
            _hashValue = base.GetHashCode();
        }
    }
}
