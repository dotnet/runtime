// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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

        public override object Clone()
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
