// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Threading;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class MockConnection : DbConnection
    {
        private ConnectionState _state;

        public bool Fail { get; set; }

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            _state = ConnectionState.Closed;
        }

        public override string ConnectionString
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotImplementedException();
        }

        public override string DataSource
        {
            get { throw new NotImplementedException(); }
        }

        public override string Database
        {
            get { throw new NotImplementedException(); }
        }

        public override void Open()
        {
            if (_state != ConnectionState.Closed)
            {
                throw new InvalidOperationException("You can only open closed connections");
            }
            if (Fail)
            {
                throw new InvalidOperationException("MockConnection was asked to fail");
            }
            _state = ConnectionState.Open;
        }

        public override string ServerVersion
        {
            get { throw new NotImplementedException(); }
        }

        public override System.Data.ConnectionState State
        {
            get { return _state; }
        }

        public CancellationToken CancellationToken { get; set; }
        public override System.Threading.Tasks.Task OpenAsync(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return base.OpenAsync(cancellationToken);
        }
    }
}
