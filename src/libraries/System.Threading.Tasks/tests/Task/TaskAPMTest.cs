// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Test class that verifies the integration with APM (Task => APM) section 2.5.11 in the TPL spec
// "Asynchronous Programming Model", or the "Begin/End" pattern
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    /// <summary>
    /// A class that implements the APM pattern to ensure that TPL can support APM pattern
    /// </summary>
    public sealed class TaskAPMTests : IDisposable
    {
        /// <summary>
        /// Used to indicate whether to test TPL's Task or Future functionality for the APM pattern
        /// </summary>
        private bool _hasReturnType;

        /// <summary>
        /// Used to synchronize between Main thread and async thread, by blocking the main thread until
        /// the thread that invokes the TaskCompleted method via AsyncCallback finishes
        /// </summary>
        private ManualResetEvent _mre = new ManualResetEvent(false);

        /// <summary>
        /// The input value to LongTask<int>.DoTask/BeginDoTask
        /// </summary>
        private const int IntInput = 1000;

        /// <summary>
        /// The constant that defines the number of milliseconds to spinwait (to simulate work) in the LongTask class
        /// </summary>
        private const int LongTaskMilliseconds = 100;

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        [InlineData(true)]
        [InlineData(false)]
        public void WaitUntilCompleteTechnique(bool hasReturnType)
        {
            _hasReturnType = hasReturnType;

            LongTask longTask;
            if (_hasReturnType)
                longTask = new LongTask<int>(LongTaskMilliseconds);
            else
                longTask = new LongTask(LongTaskMilliseconds);

            // Prove that the Wait-until-done technique works
            IAsyncResult asyncResult = longTask.BeginDoTask(null, null);
            longTask.EndDoTask(asyncResult);

            AssertTaskCompleted(asyncResult);
            Assert.False(asyncResult.CompletedSynchronously, "Should not have completed synchronously.");
        }

        [Theory]
        [OuterLoop]
        [InlineData(true)]
        [InlineData(false)]
        public void PollUntilCompleteTechnique(bool hasReturnType)
        {
            _hasReturnType = hasReturnType;

            LongTask longTask;
            if (_hasReturnType)
                longTask = new LongTask<int>(LongTaskMilliseconds);
            else
                longTask = new LongTask(LongTaskMilliseconds);

            IAsyncResult asyncResult = longTask.BeginDoTask(null, null);
            var mres = new ManualResetEventSlim();
            while (!asyncResult.IsCompleted)
            {
                mres.Wait(1);
            }

            AssertTaskCompleted(asyncResult);
            Assert.False(asyncResult.CompletedSynchronously, "Should not have completed synchronously.");
        }

        [Theory]
        [OuterLoop]
        [InlineData(true)]
        [InlineData(false)]
        public void WaitOnAsyncWaitHandleTechnique(bool hasReturnType)
        {
            _hasReturnType = hasReturnType;

            LongTask longTask;
            if (_hasReturnType)
                longTask = new LongTask<int>(LongTaskMilliseconds);
            else
                longTask = new LongTask(LongTaskMilliseconds);

            IAsyncResult asyncResult = longTask.BeginDoTask(null, null);
            asyncResult.AsyncWaitHandle.WaitOne();

            AssertTaskCompleted(asyncResult);
            Assert.False(asyncResult.CompletedSynchronously, "Should not have completed synchronously.");
        }

        [Fact]
        public void AsyncWaitHandle_DisposeHandleThenCompleteTask_Succeeds()
        {
            var tcs = new TaskCompletionSource();
            ((IAsyncResult)tcs.Task).AsyncWaitHandle.Dispose();
            tcs.SetResult();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        [InlineData(true)]
        [InlineData(false)]
        public void CallbackTechnique(bool hasReturnType)
        {
            _hasReturnType = hasReturnType;

            LongTask longTask;
            if (_hasReturnType)
                longTask = new LongTask<int>(LongTaskMilliseconds);
            else
                longTask = new LongTask(LongTaskMilliseconds);

            IAsyncResult asyncResult;
            if (_hasReturnType)
                asyncResult = ((LongTask<int>)longTask).BeginDoTask(IntInput, TaskCompleted, longTask);
            else
                asyncResult = longTask.BeginDoTask(TaskCompleted, longTask);

            _mre.WaitOne(); //Block the main thread until async thread finishes executing the call back

            AssertTaskCompleted(asyncResult);
            Assert.False(asyncResult.CompletedSynchronously, "Should not have completed synchronously.");
        }

        /// <summary>
        /// Method used by the callback implementation by the APM
        /// </summary>
        /// <param name="ar"></param>
        private void TaskCompleted(IAsyncResult ar)
        {
            if (_hasReturnType)
            {
                LongTask<int> lt = (LongTask<int>)ar.AsyncState;
                int retValue = lt.EndDoTask(ar);
                if (retValue != IntInput)
                    Assert.Fail(string.Format("Mismatch: Return = {0} vs Expect = {1}", retValue, IntInput));
            }
            else
            {
                LongTask lt = (LongTask)ar.AsyncState;
                lt.EndDoTask(ar);
            }

            _mre.Set();
        }

        /// <summary>
        /// Assert that the IAsyncResult represent a completed Task
        /// </summary>
        private void AssertTaskCompleted(IAsyncResult ar)
        {
            Assert.True(ar.IsCompleted);
            Assert.Equal(TaskStatus.RanToCompletion, ((Task)ar).Status);
        }

        public void Dispose()
        {
            _mre.Dispose();
        }
    }

    /// <summary>
    /// A dummy class that simulates a long running task that implements IAsyncResult methods
    /// </summary>
    public class LongTask
    {
        // Amount of time to SpinWait, in milliseconds.
        protected readonly int _msWaitDuration;

        public LongTask(int milliseconds)
        {
            _msWaitDuration = milliseconds;
        }

        // Synchronous version of time-consuming method
        public void DoTask()
        {
            // Simulate time-consuming task
            SpinWait.SpinUntil(() => false, _msWaitDuration);
        }

        // Asynchronous version of time-consuming method (Begin part)
        public IAsyncResult BeginDoTask(AsyncCallback callback, object state)
        {
            // Create IAsyncResult object identifying the asynchronous operation
            Task task = Task.Factory.StartNew(
                delegate
                {
                    DoTask(); //simulates workload
                },
                state);

            if (callback != null)
            {
                task.ContinueWith(delegate
                {
                    callback(task);
                });
            }

            return task; // Return the IAsyncResult to the caller
        }

        // Asynchronous version of time-consuming method (End part)
        public void EndDoTask(IAsyncResult asyncResult)
        {
            // We know that the IAsyncResult is really a Task object
            Task task = (Task)asyncResult;
            // Wait for operation to complete
            task.Wait();
        }
    }

    /// <summary>
    /// A dummy class that simulates a long running Future that implements IAsyncResult methods
    /// </summary>
    public sealed class LongTask<T> : LongTask
    {
        public LongTask(int milliseconds)
            : base(milliseconds)
        {
        }

        // Synchronous version of time-consuming method
        public T DoTask(T input)
        {
            SpinWait.SpinUntil(() => false, _msWaitDuration); // Simulate time-consuming task
            return input; // Return some result, for now, just return the input
        }

        public IAsyncResult BeginDoTask(T input, AsyncCallback callback, object state)
        {
            // Create IAsyncResult object identifying the asynchronous operation
            Task<T> task = Task<T>.Factory.StartNew(
                delegate
                {
                    return DoTask(input);
                },
                state);

            task.ContinueWith(delegate
            {
                callback(task);
            });

            return task; // Return the IAsyncResult to the caller
        }

        // Asynchronous version of time-consuming method (End part)
        public new T EndDoTask(IAsyncResult asyncResult)
        {
            // We know that the IAsyncResult is really a Task object
            Task<T> task = (Task<T>)asyncResult;

            // Return the result
            return task.Result;
        }
    }
}
