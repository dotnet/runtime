// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Common
{
    public abstract class DbTransaction : MarshalByRefObject, IDbTransaction
    {
        protected DbTransaction() : base() { }

        public DbConnection Connection => DbConnection;

        IDbConnection IDbTransaction.Connection => DbConnection;

        protected abstract DbConnection DbConnection { get; }

        public abstract IsolationLevel IsolationLevel { get; }

        public abstract void Commit();

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing) { }

        public abstract void Rollback();
    }
}
