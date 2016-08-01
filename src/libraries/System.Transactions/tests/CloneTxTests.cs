// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Transactions.Tests
{
    public enum CloneType { Normal, BlockingDependent, RollbackDependent };
    public class CloneTxTests
    {
        public CloneTxTests()
        {
        }

        [Theory]
        [InlineData(CloneType.Normal, IsolationLevel.Serializable, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.RepeatableRead, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.ReadCommitted, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.ReadUncommitted, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Snapshot, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Chaos, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Unspecified, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Serializable, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.RepeatableRead, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.ReadCommitted, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.ReadUncommitted, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Snapshot, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Chaos, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Unspecified, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Serializable, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.RepeatableRead, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.ReadCommitted, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.ReadUncommitted, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Snapshot, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Chaos, false, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Unspecified, false, false, TransactionStatus.Aborted)]
        // TODO: Issue #10353 - These variations need to be added once we have promotion support.
        /*
        [InlineData(CloneType.Normal, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.RepeatableRead, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.ReadCommitted, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.ReadUncommitted, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Snapshot, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Chaos, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Unspecified, false, false, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Serializable, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.RepeatableRead, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.ReadCommitted, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.ReadUncommitted, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Snapshot, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Chaos, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Unspecified, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Serializable, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.RepeatableRead, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.ReadCommitted, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.ReadUncommitted, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Snapshot, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Chaos, true, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Unspecified, true, true, TransactionStatus.Committed)]
        */
        public void Run(CloneType cloneType, IsolationLevel isoLevel, bool forcePromote, bool completeClone, TransactionStatus expectedStatus )
        {
            TransactionOptions options = new TransactionOptions
            {
                IsolationLevel = isoLevel,
                // Shorten the delay before a timeout for blocking clones.
                Timeout = TimeSpan.FromSeconds(1)
            };

            CommittableTransaction tx = new CommittableTransaction(options);

            Transaction clone;
            switch (cloneType)
            {
                case CloneType.Normal:
                    {
                        clone = tx.Clone();
                        break;
                    }
                case CloneType.BlockingDependent:
                    {
                        clone = tx.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
                        break;
                    }
                case CloneType.RollbackDependent:
                    {
                        clone = tx.DependentClone(DependentCloneOption.RollbackIfNotComplete);
                        break;
                    }
                default:
                    {
                        throw new Exception("Unexpected CloneType - " + cloneType.ToString());
                    }
            }

            if (forcePromote)
            {
                HelperFunctions.PromoteTx(tx);
            }

            Assert.Equal(clone.IsolationLevel, tx.IsolationLevel);
            Assert.Equal(clone.TransactionInformation.Status, tx.TransactionInformation.Status);
            Assert.Equal(clone.TransactionInformation.LocalIdentifier, tx.TransactionInformation.LocalIdentifier);
            Assert.Equal(clone.TransactionInformation.DistributedIdentifier, tx.TransactionInformation.DistributedIdentifier);

            CommittableTransaction cloneCommittable = clone as CommittableTransaction;
            Assert.Null(cloneCommittable);

            try
            {
                tx.Commit();
            }
            catch (TransactionAbortedException)
            {
                Assert.Equal(expectedStatus, TransactionStatus.Aborted);
            }

            Assert.Equal(expectedStatus, tx.TransactionInformation.Status);
        }
    }
}
