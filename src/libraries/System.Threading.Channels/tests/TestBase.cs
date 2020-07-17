// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

#pragma warning disable 0649 // unused fields there for future testing needs

namespace System.Threading.Channels.Tests
{
    public abstract class TestBase
    {
        protected void AssertSynchronouslyCanceled(Task task, CancellationToken token)
        {
            Assert.Equal(TaskStatus.Canceled, task.Status);
            OperationCanceledException oce = Assert.ThrowsAny<OperationCanceledException>(() => task.GetAwaiter().GetResult());
            if (PlatformDetection.IsNetCore)
            {
                // Earlier netstandard versions didn't have the APIs to always make this possible.
                Assert.Equal(token, oce.CancellationToken);
            }
        }

        protected async Task AssertCanceled(Task task, CancellationToken token)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            AssertSynchronouslyCanceled(task, token);
        }

        protected void AssertSynchronousSuccess<T>(ValueTask<T> task) => Assert.True(task.IsCompletedSuccessfully);
        protected void AssertSynchronousSuccess(ValueTask task) => Assert.True(task.IsCompletedSuccessfully);
        protected void AssertSynchronousSuccess(Task task) => Assert.Equal(TaskStatus.RanToCompletion, task.Status);

        protected void AssertSynchronousTrue(Task<bool> task)
        {
            AssertSynchronousSuccess(task);
            Assert.True(task.Result);
        }

        protected void AssertSynchronousTrue(ValueTask<bool> task)
        {
            AssertSynchronousSuccess(task);
            Assert.True(task.Result);
        }

        internal sealed class DelegateObserver<T> : IObserver<T>
        {
            public Action<T> OnNextDelegate = null;
            public Action<Exception> OnErrorDelegate = null;
            public Action OnCompletedDelegate = null;

            void IObserver<T>.OnNext(T value) => OnNextDelegate?.Invoke(value);

            void IObserver<T>.OnError(Exception error) => OnErrorDelegate?.Invoke(error);

            void IObserver<T>.OnCompleted() => OnCompletedDelegate?.Invoke();
        }
    }
}
