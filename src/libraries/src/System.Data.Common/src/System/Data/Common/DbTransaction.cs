// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Data;

namespace System.Data.Common
{
    public abstract class DbTransaction : IDbTransaction,
        IDisposable
    {
        protected DbTransaction() : base()
        {
        }

        public DbConnection Connection
        {
            get
            {
                return DbConnection;
            }
        }


        abstract protected DbConnection DbConnection
        {
            get;
        }

        abstract public IsolationLevel IsolationLevel
        {
            get;
        }

        IDbConnection IDbTransaction.Connection
        {
            get
            {
                return DbConnection;
            }
        }

        abstract public void Commit();

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        abstract public void Rollback();
    }
}
