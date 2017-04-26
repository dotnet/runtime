// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class MockCommand : DbCommand
    {
        public bool Fail { get; set; }

        public string LastCommand { get; set; }

        public object ScalarResult { get; set; }

        public IEnumerable<object[]> Results { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public bool WaitForCancel { get; set; }

        private ManualResetEvent _cancelEvent = new ManualResetEvent(false);

        private ManualResetEvent _waitingForCancelEvent = new ManualResetEvent(false);

        public override void Cancel()
        {
            _cancelEvent.Set();
        }

        public override string CommandText
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

        public override int CommandTimeout
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

        public override System.Data.CommandType CommandType
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

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbConnection DbConnection
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

        protected override DbParameterCollection DbParameterCollection
        {
            get { throw new NotImplementedException(); }
        }

        protected override DbTransaction DbTransaction
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

        public override bool DesignTimeVisible
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

        protected override DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior)
        {
            CheckCancel();
            if (Fail) { throw new InvalidOperationException("Failure requested"); }
            LastCommand = "ExecuteReader";
            return new MockDataReader() { Results = Results.GetEnumerator() };
        }

        public override int ExecuteNonQuery()
        {
            CheckCancel();
            if (Fail) { throw new InvalidOperationException("Failure requested"); }
            LastCommand = "ExecuteNonQuery";
            return (int)ScalarResult;
        }

        public override object ExecuteScalar()
        {
            CheckCancel();
            if (Fail) { throw new InvalidOperationException("Failure requested"); }
            LastCommand = "ExecuteScalar";
            return ScalarResult;
        }

        public override void Prepare()
        {
            throw new NotImplementedException();
        }

        public override System.Data.UpdateRowSource UpdatedRowSource
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

        public CommandBehavior CommandBehavior { get; set; }

        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            CommandBehavior = behavior;
            return base.ExecuteDbDataReaderAsync(behavior, cancellationToken);
        }

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return base.ExecuteNonQueryAsync(cancellationToken);
        }

        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return base.ExecuteScalarAsync(cancellationToken);
        }

        public void WaitForWaitingForCancel()
        {
            _waitingForCancelEvent.WaitOne();
        }

        private void CheckCancel()
        {
            if (WaitForCancel)
            {
                _cancelEvent.Reset();
                _waitingForCancelEvent.Set();
                _cancelEvent.WaitOne();
                _waitingForCancelEvent.Reset();

                throw new Exception("Command canceled");
            }
        }
    }
}
