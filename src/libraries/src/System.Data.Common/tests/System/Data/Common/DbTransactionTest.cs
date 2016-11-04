// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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

            public bool IsCommitted
            {
                get { return _isCommitted; }
            }

            public bool IsRolledback
            {
                get { return _isRolledback; }
            }

            public bool IsDisposed
            {
                get { return _isDisposed; }
            }

            public bool Disposing
            {
                get { return _disposing; }
            }

            public override void Commit()
            {
                _isCommitted = true;
            }

            public override void Rollback()
            {
                _isRolledback = true;
            }

            protected override void Dispose(bool disposing)
            {
                _isDisposed = true;
                _disposing = disposing;
                base.Dispose(disposing);
            }

            private bool _isCommitted;
            private bool _isRolledback;
            private bool _isDisposed;
            private bool _disposing;
        }
    }
}
