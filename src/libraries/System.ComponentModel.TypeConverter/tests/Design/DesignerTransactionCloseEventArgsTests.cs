// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesignerTransactionCloseEventArgsTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Commit(bool commit)
        {
#pragma warning disable 0618
            var eventArgs = new DesignerTransactionCloseEventArgs(commit);
#pragma warning restore 0618

            Assert.Equal(commit, eventArgs.TransactionCommitted);
            Assert.True(eventArgs.LastTransaction);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Ctor_Commit_LastTransaction(bool commit, bool lastTransaction)
        {
#pragma warning disable 0618
            var eventArgs = new DesignerTransactionCloseEventArgs(commit, lastTransaction);
#pragma warning restore 0618

            Assert.Equal(commit, eventArgs.TransactionCommitted);
            Assert.Equal(lastTransaction, eventArgs.LastTransaction);
        }
    }
}
