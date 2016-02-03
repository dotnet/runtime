// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Common.Tests
{
    public class DbConnectionTests
    {
        private static volatile bool _wasFinalized;

        private class FinalizingConnection : DbConnection
        {
            public static void CreateAndRelease()
            {
                new FinalizingConnection();
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing)
                    _wasFinalized = true;
                base.Dispose(disposing);
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

            public override string Database
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string DataSource
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string ServerVersion
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override ConnectionState State
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override void ChangeDatabase(string databaseName)
            {
                throw new NotImplementedException();
            }

            public override void Close()
            {
                throw new NotImplementedException();
            }

            public override void Open()
            {
                throw new NotImplementedException();
            }

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                throw new NotImplementedException();
            }

            protected override DbCommand CreateDbCommand()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void CanBeFinalized()
        {
            FinalizingConnection.CreateAndRelease();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.True(_wasFinalized);
        }
    }
}
