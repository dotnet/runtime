// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class RunContinuationsAsynchronouslyTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Direct(bool useRunContinuationsAsynchronously)
        {
            Run(useRunContinuationsAsynchronously, t => t);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ViaUnwrap(bool useRunContinuationsAsynchronously)
        {
            Run(useRunContinuationsAsynchronously, t => ((Task<Task>)t).Unwrap());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ViaWhenAll(bool useRunContinuationsAsynchronously)
        {
            Run(useRunContinuationsAsynchronously, t => Task.WhenAll(t));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ViaWhenAny(bool useRunContinuationsAsynchronously)
        {
            Run(useRunContinuationsAsynchronously, t => Task.WhenAny(t));
        }

        private static void Run(bool useRunContinuationsAsynchronously, Func<Task,Task> getIntermediateContinuation)
        {
            Task t = Task.Run(() => // run test off of xunit's thread so as not to be confused by its TaskScheduler or SynchronizationContext
            {
                int callingThreadId = Environment.CurrentManagedThreadId;

                var tcs = new TaskCompletionSource<Task>(useRunContinuationsAsynchronously ?
                    TaskCreationOptions.RunContinuationsAsynchronously :
                    TaskCreationOptions.None);

                Task cont = getIntermediateContinuation(tcs.Task).ContinueWith(
                    _ => Assert.NotEqual(useRunContinuationsAsynchronously, callingThreadId == Environment.CurrentManagedThreadId),
                    TaskContinuationOptions.ExecuteSynchronously);

                tcs.SetResult(Task.CompletedTask);

                ((IAsyncResult)cont).AsyncWaitHandle.WaitOne(); // ensure we don't inline as part of waiting
                cont.GetAwaiter().GetResult(); // propagate any errors
            });
            ((IAsyncResult)t).AsyncWaitHandle.WaitOne(); // ensure we don't inline as part of waiting
            t.GetAwaiter().GetResult(); // propagate any errors
        }
    }
}
