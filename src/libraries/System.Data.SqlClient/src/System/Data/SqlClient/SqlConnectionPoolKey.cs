// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.Common;
using System.Diagnostics;

namespace System.Data.SqlClient
{
    // SqlConnectionPoolKey: Implementation of a key to connection pool groups for specifically to be used for SqlConnection
    //  Connection string and SqlCredential are used as a key
    internal class SqlConnectionPoolKey : DbConnectionPoolKey
    {
        private int _hashValue;
        private SqlCredential _credential;
        private readonly string _accessToken;

        internal SqlConnectionPoolKey(string connectionString, SqlCredential credential, string accessToken) : base(connectionString)
        {
            Debug.Assert(_credential == null || _accessToken == null, "Credential and AccessToken can't have the value at the same time.");
            _credential = credential;
            _accessToken = accessToken;
            CalculateHashCode();
        }

        private SqlConnectionPoolKey(SqlConnectionPoolKey key) : base(key)
        {
            _credential = key.Credential;
            _accessToken = key.AccessToken;
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

        internal SqlCredential Credential => _credential;

        internal string AccessToken
        {
            get
            {
                return _accessToken;
            }
        }

        public override bool Equals(object obj)
        {
            SqlConnectionPoolKey key = obj as SqlConnectionPoolKey;
            return (key != null && _credential == key._credential && ConnectionString == key.ConnectionString && Object.ReferenceEquals(_accessToken, key._accessToken));
        }

        public override int GetHashCode()
        {
            return _hashValue;
        }

        private void CalculateHashCode()
        {
            _hashValue = base.GetHashCode();

            if (_credential != null)
            {
                unchecked
                {
                    _hashValue = _hashValue * 17 + _credential.GetHashCode();
                }
            }
            else if (_accessToken != null)
            {
                unchecked
                {
                    _hashValue = _hashValue * 17 + _accessToken.GetHashCode();
                }
            }
        }
    }
}
