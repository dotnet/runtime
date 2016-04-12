// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.Common;
using System.Data.ProviderBase;

namespace System.Data.SqlClient
{
    sealed internal class SqlConnectionPoolGroupProviderInfo : DbConnectionPoolGroupProviderInfo
    {
        private string _alias;
        private string _failoverPartner;
        private bool _useFailoverPartner;

        internal SqlConnectionPoolGroupProviderInfo(SqlConnectionString connectionOptions)
        {
            // This is for the case where the user specified the failover partner
            // in the connection string and we have not yet connected to get the 
            // env change.
            _failoverPartner = connectionOptions.FailoverPartner;

            if (string.IsNullOrEmpty(_failoverPartner))
            {
                _failoverPartner = null;
            }
        }

        internal string FailoverPartner
        {
            get
            {
                return _failoverPartner;
            }
        }

        internal bool UseFailoverPartner
        {
            get
            {
                return _useFailoverPartner;
            }
        }

        internal void AliasCheck(string server)
        {
            if (_alias != server)
            {
                lock (this)
                {
                    if (null == _alias)
                    {
                        _alias = server;
                    }
                    else if (_alias != server)
                    {
                        base.PoolGroup.Clear();
                        _alias = server;
                    }
                }
            }
        }


        internal void FailoverCheck(SqlInternalConnection connection, bool actualUseFailoverPartner, SqlConnectionString userConnectionOptions, string actualFailoverPartner)
        {
            if (UseFailoverPartner != actualUseFailoverPartner)
            {
                base.PoolGroup.Clear();
                _useFailoverPartner = actualUseFailoverPartner;
            }
            // Only construct a new permission set when we're connecting to the
            // primary data source, not the failover partner.
            if (!_useFailoverPartner && _failoverPartner != actualFailoverPartner)
            {
                // NOTE: we optimistically generate the permission set to keep 
                //       lock short, but we only do this when we get a new
                //       failover partner.

                lock (this)
                {
                    if (_failoverPartner != actualFailoverPartner)
                    {
                        _failoverPartner = actualFailoverPartner;
                    }
                }
            }
        }
    }
}
