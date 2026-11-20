// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.Tests.Common
{
    /// <summary>
    /// The default savepoint members of <see cref="DbTransaction"/>: Save and Rollback(string) (and their asynchronous
    /// forms) throw <see cref="NotSupportedException"/>, while Release and ReleaseAsync do nothing.
    /// </summary>
    public class DbTransactionSavepointTests
    {
        private const string SavepointName = "sp";

        [Fact]
        public void SupportsSavepoints_DefaultsToFalse()
        {
            using var transaction = new DefaultTransaction();

            Assert.False(transaction.SupportsSavepoints);
        }

        [Fact]
        public void Save_ByDefault_ThrowsNotSupportedException()
        {
            using var transaction = new DefaultTransaction();

            Assert.Throws<NotSupportedException>(() => transaction.Save(SavepointName));
        }

        [Fact]
        public void Rollback_ToSavepoint_ByDefault_ThrowsNotSupportedException()
        {
            using var transaction = new DefaultTransaction();

            Assert.Throws<NotSupportedException>(() => transaction.Rollback(SavepointName));
        }

        [Fact]
        public void Release_ByDefault_DoesNothing()
        {
            using var transaction = new DefaultTransaction();

            transaction.Release(SavepointName);

            Assert.False(transaction.IsRolledBack);
        }

        [Fact]
        public async Task SaveAsync_ByDefault_StoresNotSupportedExceptionInTheTask()
        {
            using var transaction = new DefaultTransaction();

            Task task = transaction.SaveAsync(SavepointName);

            await Assert.ThrowsAsync<NotSupportedException>(() => task);
        }

        [Fact]
        public async Task RollbackAsync_ToSavepoint_ByDefault_StoresNotSupportedExceptionInTheTask()
        {
            using var transaction = new DefaultTransaction();

            Task task = transaction.RollbackAsync(SavepointName);

            await Assert.ThrowsAsync<NotSupportedException>(() => task);
        }

        [Fact]
        public async Task ReleaseAsync_ByDefault_CompletesSuccessfully()
        {
            using var transaction = new DefaultTransaction();

            Task task = transaction.ReleaseAsync(SavepointName);

            await task;
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        [Fact]
        public void SavepointAsyncMethods_WithCanceledToken_ReturnCanceledTasksWithoutCallingTheSynchronousMethods()
        {
            using var transaction = new RecordingTransaction();
            using var source = new CancellationTokenSource();
            source.Cancel();

            Task save = transaction.SaveAsync(SavepointName, source.Token);
            Task rollback = transaction.RollbackAsync(SavepointName, source.Token);
            Task release = transaction.ReleaseAsync(SavepointName, source.Token);

            Assert.True(save.IsCanceled);
            Assert.True(rollback.IsCanceled);
            Assert.True(release.IsCanceled);
            Assert.Empty(transaction.Calls);
        }

        [Fact]
        public async Task SavepointAsyncMethods_CallTheOverriddenSynchronousMethods()
        {
            using var transaction = new RecordingTransaction();

            await transaction.SaveAsync(SavepointName);
            await transaction.RollbackAsync(SavepointName);
            await transaction.ReleaseAsync(SavepointName);

            Assert.Equal(new[] { "Save:sp", "Rollback:sp", "Release:sp" }, transaction.Calls);
        }

        [Fact]
        public async Task SavepointAsyncMethods_StoreExceptionsThrownByTheSynchronousMethods()
        {
            using var transaction = new RecordingTransaction { Failure = new InvalidOperationException("boom") };

            Task save = transaction.SaveAsync(SavepointName);
            Task rollback = transaction.RollbackAsync(SavepointName);
            Task release = transaction.ReleaseAsync(SavepointName);

            // Stored in the tasks, not thrown by the calls above.
            Assert.Same(transaction.Failure, (await Assert.ThrowsAsync<InvalidOperationException>(() => save)));
            Assert.Same(transaction.Failure, (await Assert.ThrowsAsync<InvalidOperationException>(() => rollback)));
            Assert.Same(transaction.Failure, (await Assert.ThrowsAsync<InvalidOperationException>(() => release)));
        }

        private abstract class TransactionBase : DbTransaction
        {
            public bool IsRolledBack { get; private set; }

            protected override DbConnection DbConnection => null;

            public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

            public override void Commit()
            {
            }

            public override void Rollback() => IsRolledBack = true;
        }

        /// <summary>Overrides no savepoint member.</summary>
        private sealed class DefaultTransaction : TransactionBase
        {
        }

        /// <summary>Overrides all three synchronous savepoint members.</summary>
        private sealed class RecordingTransaction : TransactionBase
        {
            public List<string> Calls { get; } = new List<string>();

            public Exception Failure { get; set; }

            public override bool SupportsSavepoints => true;

            public override void Save(string savepointName) => Record("Save", savepointName);

            public override void Rollback(string savepointName) => Record("Rollback", savepointName);

            public override void Release(string savepointName) => Record("Release", savepointName);

            private void Record(string call, string savepointName)
            {
                if (Failure is not null)
                {
                    throw Failure;
                }

                Calls.Add(call + ":" + savepointName);
            }
        }
    }
}
