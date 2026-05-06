// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Data.Common;

using Xunit;

namespace System.Data.Tests.Common
{
    public class DbTransactionTest
    {
        [Fact]
        public void DisposeTest()
        {
            MockTransaction trans = new MockTransaction();
            trans.Dispose();

            Assert.False(trans.IsCommitted);
            Assert.False(trans.IsRolledback);
            Assert.True(trans.IsDisposed);
            Assert.True(trans.Disposing);
        }

        private class MockTransaction : DbTransaction
        {
            protected override DbConnection DbConnection
            {
                get { return null; }
            }

            public override IsolationLevel IsolationLevel
            {
                get { return IsolationLevel.RepeatableRead; }
            }

            public bool IsCommitted { get; private set; }

            public bool IsRolledback { get; private set; }

            public bool IsDisposed { get; private set; }

            public bool Disposing { get; private set; }

            public override void Commit()
            {
                IsCommitted = true;
            }

            public override void Rollback()
            {
                IsRolledback = true;
            }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                Disposing = disposing;
                base.Dispose(disposing);
            }
        }
    }
}
