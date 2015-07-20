// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System;
using System.Data;

namespace System.Data.Common
{
    public abstract class DbTransaction :
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
