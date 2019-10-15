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
    public class CloneTxTests : IDisposable
    {
        public CloneTxTests()
        {
            // Make sure we start with Transaction.Current = null.
            Transaction.Current = null;
        }

        public void Dispose()
        {
            Transaction.Current = null;
        }

        [Theory]
        [InlineData(CloneType.Normal, IsolationLevel.Serializable, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.RepeatableRead, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.ReadCommitted, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.ReadUncommitted, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Snapshot, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Chaos, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Unspecified, false, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Serializable, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.RepeatableRead, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.ReadCommitted, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.ReadUncommitted, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Snapshot, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Chaos, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Unspecified, false, TransactionStatus.Aborted)]
        // TODO: Issue #10353 - These variations need to be added once we have promotion support.
        /*
        [InlineData(CloneType.Normal, true, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.RepeatableRead, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.ReadCommitted, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.ReadUncommitted, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Snapshot, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Chaos, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, IsolationLevel.Unspecified, false, TransactionStatus.Committed)]
        [InlineData(CloneType.Normal, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Serializable, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.RepeatableRead, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.ReadCommitted, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.ReadUncommitted, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Snapshot, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Chaos, true, TransactionStatus.Committed)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Unspecified, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Serializable, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.RepeatableRead, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.ReadCommitted, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.ReadUncommitted, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Snapshot, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Chaos, true, TransactionStatus.Committed)]
        [InlineData(CloneType.RollbackDependent, IsolationLevel.Unspecified, true, TransactionStatus.Committed)]
        */
        public void Run(CloneType cloneType, IsolationLevel isoLevel, bool forcePromote, TransactionStatus expectedStatus )
        {
            TransactionOptions options = new TransactionOptions
            {
                IsolationLevel = isoLevel,
                // Shorten the delay before a timeout for blocking clones.
                Timeout = TimeSpan.FromSeconds(1)
            };

            // If we are dealing with a "normal" clone, we fully expect the transaction to commit successfully.
            // But a timeout of 1 seconds may not be enough for that to happen. So increase the timeout
            // for "normal" clones. This will not increase the test execution time in the "passing" scenario.
            if (cloneType == CloneType.Normal)
            {
                options.Timeout = TimeSpan.FromSeconds(10);
            }

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

            Assert.Equal(tx.IsolationLevel, clone.IsolationLevel);
            Assert.Equal(tx.TransactionInformation.Status, clone.TransactionInformation.Status);
            Assert.Equal(tx.TransactionInformation.LocalIdentifier, clone.TransactionInformation.LocalIdentifier);
            Assert.Equal(tx.TransactionInformation.DistributedIdentifier, clone.TransactionInformation.DistributedIdentifier);

            CommittableTransaction cloneCommittable = clone as CommittableTransaction;
            Assert.Null(cloneCommittable);

            try
            {
                tx.Commit();
            }
            catch (TransactionAbortedException ex)
            {
                Assert.Equal(TransactionStatus.Aborted, expectedStatus);
                switch (cloneType)
                {
                    case CloneType.Normal:
                        {
                            // We shouldn't be getting TransactionAbortedException for "normal" clones,
                            // so we have these two Asserts to possibly help determine what went wrong.
                            Assert.Null(ex.InnerException);
                            Assert.Equal("There shouldn't be any exception with this Message property", ex.Message);
                            break;
                        }
                    case CloneType.BlockingDependent:
                        {
                            Assert.IsType<TimeoutException>(ex.InnerException);
                            break;
                        }
                    case CloneType.RollbackDependent:
                        {
                            Assert.Null(ex.InnerException);
                            break;
                        }
                    default:
                        {
                            throw new Exception("Unexpected CloneType - " + cloneType.ToString());
                        }
                }
            }

            Assert.Equal(expectedStatus, tx.TransactionInformation.Status);
        }

        [OuterLoop] // transaction timeout of 1.5 seconds per InlineData invocation.
        [Theory]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Serializable, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.RepeatableRead, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.ReadCommitted, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.ReadUncommitted, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Snapshot, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Chaos, false, TransactionStatus.Aborted)]
        [InlineData(CloneType.BlockingDependent, IsolationLevel.Unspecified, false, TransactionStatus.Aborted)]
        public void RunOuterLoop(CloneType cloneType, IsolationLevel isoLevel, bool forcePromote, TransactionStatus expectedStatus)
        {
            Run(cloneType, isoLevel, forcePromote, expectedStatus);
        }
    }
}
