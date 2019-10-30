// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesignerTransactionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            using (var transaction = new TestDesignerTransaction())
            {
                Assert.Empty(transaction.Description);
                Assert.False(transaction.Canceled);
                Assert.False(transaction.Committed);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Description")]
        public void Ctor_Default(string description)
        {
            using (var transaction = new TestDesignerTransaction(description))
            {
                Assert.Same(description, transaction.Description);
                Assert.False(transaction.Canceled);
                Assert.False(transaction.Committed);
            }
        }

        [Fact]
        public void Cancel_NotCommitted_Success()
        {
            using (var transaction = new TestDesignerTransaction())
            {
                transaction.Cancel();
                Assert.Equal(1, transaction.CancelCount);
                Assert.True(transaction.Canceled);

                transaction.Cancel();
                Assert.Equal(1, transaction.CancelCount);
                Assert.True(transaction.Canceled);
            }
        }

        [Fact]
        public void Cancel_Committed_Success()
        {
            using (var transaction = new TestDesignerTransaction())
            {
                transaction.Commit();

                transaction.Cancel();
                Assert.Equal(0, transaction.CancelCount);
                Assert.False(transaction.Canceled);
            }
        }

        [Fact]
        public void Commit_NotCommitted_Success()
        {
            using (var transaction = new TestDesignerTransaction())
            {
                transaction.Commit();
                Assert.Equal(1, transaction.CommitCount);
                Assert.True(transaction.Committed);

                transaction.Commit();
                Assert.Equal(1, transaction.CommitCount);
                Assert.True(transaction.Committed);
            }
        }

        [Fact]
        public void Commit_Cancelled_Success()
        {
            using (var transaction = new TestDesignerTransaction())
            {
                transaction.Cancel();

                transaction.Commit();
                Assert.Equal(0, transaction.CommitCount);
                Assert.False(transaction.Committed);
            }
        }

        [Fact]
        public void Dispose_FinalizeSuppressed_Success()
        {
            var transaction = new NonDisposingDesignerTransaction();
            transaction.Cancel();
            ((IDisposable)transaction).Dispose();

            Assert.True(transaction.Canceled);
        }

        [Fact]
        public void Dispose_FinalizeNotSuppressed_Success()
        {
            var transaction = new NonDisposingDesignerTransaction();
            ((IDisposable)transaction).Dispose();

            Assert.False(transaction.Canceled);
        }

        private class NonDisposingDesignerTransaction : DesignerTransaction
        {
            protected override void Dispose(bool disposing) { }

            protected override void OnCancel() { }
            protected override void OnCommit() { }
        }

        private class TestDesignerTransaction : DesignerTransaction
        {
            public TestDesignerTransaction() : base() { }
            public TestDesignerTransaction(string description) : base(description) { }

            public int CancelCount { get; set; }
            protected override void OnCancel() => CancelCount++;

            public int CommitCount { get; set; }
            protected override void OnCommit() => CommitCount++;
        }
    }
}
