// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class UnwrapTests
    {
        /// <summary>Tests unwrap argument validation.</summary>
        [Fact]
        public void ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => { ((Task<Task>)null).Unwrap(); });
            Assert.Throws<ArgumentNullException>(() => { ((Task<Task<int>>)null).Unwrap(); });
            Assert.Throws<ArgumentNullException>(() => { ((Task<Task<string>>)null).Unwrap(); });
        }

        /// <summary>
        /// Tests Unwrap when both the outer task and non-generic inner task have completed by the time Unwrap is called.
        /// </summary>
        /// <param name="inner">Will be run with a RanToCompletion, Faulted, and Canceled task.</param>
        [Theory]
        [MemberData(nameof(CompletedNonGenericTasks))]
        public void NonGeneric_Completed_Completed(Task inner)
        {
            Task<Task> outer = Task.FromResult(inner);
            Task unwrappedInner = outer.Unwrap();
            Assert.True(unwrappedInner.IsCompleted);
            AssertTasksAreEqual(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when both the outer task and non-generic inner task have completed by the time Unwrap is called.
        /// </summary>
        /// <param name="inner">Will be run with a RanToCompletion, Faulted, and Canceled task.</param>
        [Theory]
        [MemberData(nameof(CompletedNonGenericTasks))]
        public void NonGeneric_Completed_Completed_OptimizeToUseSameInner(Task inner)
        {
            Task<Task> outer = Task.FromResult(inner);
            Task unwrappedInner = outer.Unwrap();
            Assert.True(unwrappedInner.IsCompleted);
            Assert.Same(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when both the outer task and generic inner task have completed by the time Unwrap is called.
        /// </summary>
        /// <param name="inner">The inner task.</param>
        [Theory]
        [MemberData(nameof(CompletedStringTasks))]
        public void Generic_Completed_Completed(Task<string> inner)
        {
            Task<Task<string>> outer = Task.FromResult(inner);
            Task<string> unwrappedInner = outer.Unwrap();
            Assert.True(unwrappedInner.IsCompleted);
            AssertTasksAreEqual(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when both the outer task and generic inner task have completed by the time Unwrap is called.
        /// </summary>
        /// <param name="inner">The inner task.</param>
        [Theory]
        [MemberData(nameof(CompletedStringTasks))]
        public void Generic_Completed_Completed_OptimizeToUseSameInner(Task<string> inner)
        {
            Task<Task<string>> outer = Task.FromResult(inner);
            Task<string> unwrappedInner = outer.Unwrap();
            Assert.True(unwrappedInner.IsCompleted);
            Assert.Same(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when the non-generic inner task has completed but the outer task has not completed by the time Unwrap is called.
        /// </summary>
        /// <param name="inner">The inner task.</param>
        [Theory]
        [MemberData(nameof(CompletedNonGenericTasks))]
        public void NonGeneric_NotCompleted_Completed(Task inner)
        {
            var outerTcs = new TaskCompletionSource<Task>();
            Task<Task> outer = outerTcs.Task;

            Task unwrappedInner = outer.Unwrap();
            Assert.False(unwrappedInner.IsCompleted);

            outerTcs.SetResult(inner);
            AssertTasksAreEqual(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when the generic inner task has completed but the outer task has not completed by the time Unwrap is called.
        /// </summary>
        /// <param name="inner">The inner task.</param>
        [Theory]
        [MemberData(nameof(CompletedStringTasks))]
        public void Generic_NotCompleted_Completed(Task<string> inner)
        {
            var outerTcs = new TaskCompletionSource<Task<string>>();
            Task<Task<string>> outer = outerTcs.Task;

            Task<string> unwrappedInner = outer.Unwrap();
            Assert.False(unwrappedInner.IsCompleted);

            outerTcs.SetResult(inner);
            AssertTasksAreEqual(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when the non-generic inner task has not yet completed but the outer task has completed by the time Unwrap is called.
        /// </summary>
        /// <param name="innerStatus">How the inner task should be completed.</param>
        [Theory]
        [InlineData(TaskStatus.RanToCompletion)]
        [InlineData(TaskStatus.Faulted)]
        [InlineData(TaskStatus.Canceled)]
        public void NonGeneric_Completed_NotCompleted(TaskStatus innerStatus)
        {
            var innerTcs = new TaskCompletionSource();
            Task inner = innerTcs.Task;

            Task<Task> outer = Task.FromResult(inner);
            Task unwrappedInner = outer.Unwrap();
            Assert.False(unwrappedInner.IsCompleted);

            switch (innerStatus)
            {
                case TaskStatus.RanToCompletion:
                    innerTcs.SetResult();
                    break;
                case TaskStatus.Faulted:
                    innerTcs.SetException(new InvalidProgramException());
                    break;
                case TaskStatus.Canceled:
                    innerTcs.SetCanceled();
                    break;
            }

            AssertTasksAreEqual(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when the non-generic inner task has not yet completed but the outer task has completed by the time Unwrap is called.
        /// </summary>
        /// <param name="innerStatus">How the inner task should be completed.</param>
        [Theory]
        [InlineData(TaskStatus.RanToCompletion)]
        [InlineData(TaskStatus.Faulted)]
        [InlineData(TaskStatus.Canceled)]
        public void Generic_Completed_NotCompleted(TaskStatus innerStatus)
        {
            var innerTcs = new TaskCompletionSource<int>();
            Task<int> inner = innerTcs.Task;

            Task<Task<int>> outer = Task.FromResult(inner);
            Task<int> unwrappedInner = outer.Unwrap();
            Assert.False(unwrappedInner.IsCompleted);

            switch (innerStatus)
            {
                case TaskStatus.RanToCompletion:
                    innerTcs.SetResult(42);
                    break;
                case TaskStatus.Faulted:
                    innerTcs.SetException(new InvalidProgramException());
                    break;
                case TaskStatus.Canceled:
                    innerTcs.SetCanceled();
                    break;
            }

            AssertTasksAreEqual(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when neither the non-generic inner task nor the outer task has completed by the time Unwrap is called.
        /// </summary>
        /// <param name="outerCompletesFirst">Whether the outer task or the inner task completes first.</param>
        /// <param name="innerStatus">How the inner task should be completed.</param>
        [Theory]
        [InlineData(true, TaskStatus.RanToCompletion)]
        [InlineData(true, TaskStatus.Canceled)]
        [InlineData(true, TaskStatus.Faulted)]
        [InlineData(false, TaskStatus.RanToCompletion)]
        [InlineData(false, TaskStatus.Canceled)]
        [InlineData(false, TaskStatus.Faulted)]
        public void NonGeneric_NotCompleted_NotCompleted(bool outerCompletesFirst, TaskStatus innerStatus)
        {
            var innerTcs = new TaskCompletionSource();
            Task inner = innerTcs.Task;

            var outerTcs = new TaskCompletionSource<Task>();
            Task<Task> outer = outerTcs.Task;

            Task unwrappedInner = outer.Unwrap();
            Assert.False(unwrappedInner.IsCompleted);

            if (outerCompletesFirst)
            {
                outerTcs.SetResult(inner);
                Assert.False(unwrappedInner.IsCompleted);
            }

            switch (innerStatus)
            {
                case TaskStatus.RanToCompletion:
                    innerTcs.SetResult();
                    break;
                case TaskStatus.Faulted:
                    innerTcs.SetException(new InvalidOperationException());
                    break;
                case TaskStatus.Canceled:
                    innerTcs.SetCanceled(CreateCanceledToken());
                    break;
            }

            if (!outerCompletesFirst)
            {
                Assert.False(unwrappedInner.IsCompleted);
                outerTcs.SetResult(inner);
            }

            AssertTasksAreEqual(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when neither the generic inner task nor the outer task has completed by the time Unwrap is called.
        /// </summary>
        /// <param name="outerCompletesFirst">Whether the outer task or the inner task completes first.</param>
        /// <param name="innerStatus">How the inner task should be completed.</param>
        [Theory]
        [InlineData(true, TaskStatus.RanToCompletion)]
        [InlineData(true, TaskStatus.Canceled)]
        [InlineData(true, TaskStatus.Faulted)]
        [InlineData(false, TaskStatus.RanToCompletion)]
        [InlineData(false, TaskStatus.Canceled)]
        [InlineData(false, TaskStatus.Faulted)]
        public void Generic_NotCompleted_NotCompleted(bool outerCompletesFirst, TaskStatus innerStatus)
        {
            var innerTcs = new TaskCompletionSource<int>();
            Task<int> inner = innerTcs.Task;

            var outerTcs = new TaskCompletionSource<Task<int>>();
            Task<Task<int>> outer = outerTcs.Task;

            Task<int> unwrappedInner = outer.Unwrap();
            Assert.False(unwrappedInner.IsCompleted);

            if (outerCompletesFirst)
            {
                outerTcs.SetResult(inner);
                Assert.False(unwrappedInner.IsCompleted);
            }

            switch (innerStatus)
            {
                case TaskStatus.RanToCompletion:
                    innerTcs.SetResult(42);
                    break;
                case TaskStatus.Faulted:
                    innerTcs.SetException(new InvalidOperationException());
                    break;
                case TaskStatus.Canceled:
                    innerTcs.SetCanceled(CreateCanceledToken());
                    break;
            }

            if (!outerCompletesFirst)
            {
                Assert.False(unwrappedInner.IsCompleted);
                outerTcs.SetResult(inner);
            }

            AssertTasksAreEqual(inner, unwrappedInner);
        }

        /// <summary>
        /// Tests Unwrap when the outer task for a non-generic inner fails in some way.
        /// </summary>
        /// <param name="outerCompletesFirst">Whether the outer task completes before Unwrap is called.</param>
        /// <param name="outerStatus">How the outer task should be completed (RanToCompletion means returning null).</param>
        [Theory]
        [InlineData(true, TaskStatus.RanToCompletion)]
        [InlineData(true, TaskStatus.Faulted)]
        [InlineData(true, TaskStatus.Canceled)]
        [InlineData(false, TaskStatus.RanToCompletion)]
        [InlineData(false, TaskStatus.Faulted)]
        [InlineData(false, TaskStatus.Canceled)]
        public void NonGeneric_UnsuccessfulOuter(bool outerCompletesBeforeUnwrap, TaskStatus outerStatus)
        {
            var outerTcs = new TaskCompletionSource<Task>();
            Task<Task> outer = outerTcs.Task;

            Task unwrappedInner = null;

            if (!outerCompletesBeforeUnwrap)
                unwrappedInner = outer.Unwrap();

            switch (outerStatus)
            {
                case TaskStatus.RanToCompletion:
                    outerTcs.SetResult(null);
                    break;
                case TaskStatus.Canceled:
                    outerTcs.SetCanceled(CreateCanceledToken());
                    break;
                case TaskStatus.Faulted:
                    outerTcs.SetException(new InvalidCastException());
                    break;
            }

            if (outerCompletesBeforeUnwrap)
                unwrappedInner = outer.Unwrap();

            WaitNoThrow(unwrappedInner);

            switch (outerStatus)
            {
                case TaskStatus.RanToCompletion:
                    Assert.True(unwrappedInner.IsCanceled);
                    break;
                default:
                    AssertTasksAreEqual(outer, unwrappedInner);
                    break;
            }
        }

        /// <summary>
        /// Tests Unwrap when the outer task for a generic inner fails in some way.
        /// </summary>
        /// <param name="outerCompletesFirst">Whether the outer task completes before Unwrap is called.</param>
        /// <param name="outerStatus">How the outer task should be completed (RanToCompletion means returning null).</param>
        [Theory]
        [InlineData(true, TaskStatus.RanToCompletion)]
        [InlineData(true, TaskStatus.Faulted)]
        [InlineData(true, TaskStatus.Canceled)]
        [InlineData(false, TaskStatus.RanToCompletion)]
        [InlineData(false, TaskStatus.Faulted)]
        [InlineData(false, TaskStatus.Canceled)]
        public void Generic_UnsuccessfulOuter(bool outerCompletesBeforeUnwrap, TaskStatus outerStatus)
        {
            var outerTcs = new TaskCompletionSource<Task<int>>();
            Task<Task<int>> outer = outerTcs.Task;

            Task<int> unwrappedInner = null;

            if (!outerCompletesBeforeUnwrap)
                unwrappedInner = outer.Unwrap();

            switch (outerStatus)
            {
                case TaskStatus.RanToCompletion:
                    outerTcs.SetResult(null); // cancellation
                    break;
                case TaskStatus.Canceled:
                    outerTcs.SetCanceled(CreateCanceledToken());
                    break;
                case TaskStatus.Faulted:
                    outerTcs.SetException(new InvalidCastException());
                    break;
            }

            if (outerCompletesBeforeUnwrap)
                unwrappedInner = outer.Unwrap();

            WaitNoThrow(unwrappedInner);

            switch (outerStatus)
            {
                case TaskStatus.RanToCompletion:
                    Assert.True(unwrappedInner.IsCanceled);
                    break;
                default:
                    AssertTasksAreEqual(outer, unwrappedInner);
                    break;
            }
        }

        /// <summary>
        /// Test Unwrap when the outer task for a non-generic inner task is marked as AttachedToParent.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void NonGeneric_AttachedToParent()
        {
            Exception error = new InvalidTimeZoneException();
            Task parent = Task.Factory.StartNew(() =>
            {
                var outerTcs = new TaskCompletionSource<Task>(TaskCreationOptions.AttachedToParent);
                Task<Task> outer = outerTcs.Task;

                Task inner = Task.FromException(error);

                Task unwrappedInner = outer.Unwrap();
                Assert.Equal(TaskCreationOptions.AttachedToParent, unwrappedInner.CreationOptions);

                outerTcs.SetResult(inner);
                AssertTasksAreEqual(inner, unwrappedInner);
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            WaitNoThrow(parent);
            Assert.Equal(TaskStatus.Faulted, parent.Status);
            Assert.Same(error, parent.Exception.Flatten().InnerException);
        }

        /// <summary>
        /// Test Unwrap when the outer task for a generic inner task is marked as AttachedToParent.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Generic_AttachedToParent()
        {
            Exception error = new InvalidTimeZoneException();
            Task parent = Task.Factory.StartNew(() =>
            {
                var outerTcs = new TaskCompletionSource<Task<object>>(TaskCreationOptions.AttachedToParent);
                Task<Task<object>> outer = outerTcs.Task;

                Task<object> inner = Task.FromException<object>(error);

                Task<object> unwrappedInner = outer.Unwrap();
                Assert.Equal(TaskCreationOptions.AttachedToParent, unwrappedInner.CreationOptions);

                outerTcs.SetResult(inner);
                AssertTasksAreEqual(inner, unwrappedInner);
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            WaitNoThrow(parent);
            Assert.Equal(TaskStatus.Faulted, parent.Status);
            Assert.Same(error, parent.Exception.Flatten().InnerException);
        }

        /// <summary>
        /// Test that Unwrap with a non-generic task doesn't use TaskScheduler.Current.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void NonGeneric_DefaultSchedulerUsed()
        {
            var scheduler = new CountingScheduler();
            Task.Factory.StartNew(() =>
            {
                int initialCallCount = scheduler.QueueTaskCalls;

                Task<Task> outer = Task.Factory.StartNew(() => Task.Run(() => { }),
                    CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                Task unwrappedInner = outer.Unwrap();
                unwrappedInner.Wait();

                Assert.Equal(initialCallCount, scheduler.QueueTaskCalls);
            }, CancellationToken.None, TaskCreationOptions.None, scheduler).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Test that Unwrap with a generic task doesn't use TaskScheduler.Current.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Generic_DefaultSchedulerUsed()
        {
            var scheduler = new CountingScheduler();
            Task.Factory.StartNew(() =>
            {
                int initialCallCount = scheduler.QueueTaskCalls;

                Task<Task<int>> outer = Task.Factory.StartNew(() => Task.Run(() => 42),
                    CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                Task<int> unwrappedInner = outer.Unwrap();
                unwrappedInner.Wait();

                Assert.Equal(initialCallCount, scheduler.QueueTaskCalls);
            }, CancellationToken.None, TaskCreationOptions.None, scheduler).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Test that a long chain of Unwraps can execute without overflowing the stack.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void RunStackGuardTests()
        {
            const int DiveDepth = 12000;

            Func<int, Task<int>> func = null;
            func = count =>
                ++count < DiveDepth ?
                    Task.Factory.StartNew(() => func(count), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Unwrap() :
                    Task.FromResult(count);

            // This test will overflow if it fails.
            Assert.Equal(DiveDepth, func(0).Result);
        }

        /// <summary>Gets an enumerable of already completed non-generic tasks.</summary>
        public static IEnumerable<object[]> CompletedNonGenericTasks
        {
            get
            {
                yield return new object[] { Task.CompletedTask };
                yield return new object[] { Task.FromCanceled(CreateCanceledToken()) };
                yield return new object[] { Task.FromException(new FormatException()) };

                var tcs = new TaskCompletionSource<int>();
                tcs.SetCanceled(); // cancel task without a token
                yield return new object[] { tcs.Task };
            }
        }

        /// <summary>Gets an enumerable of already completed generic tasks.</summary>
        public static IEnumerable<object[]> CompletedStringTasks
        {
            get
            {
                yield return new object[] { Task.FromResult("Tasks") };
                yield return new object[] { Task.FromCanceled<string>(CreateCanceledToken()) };
                yield return new object[] { Task.FromException<string>(new FormatException()) };

                var tcs = new TaskCompletionSource<string>();
                tcs.SetCanceled(); // cancel task without a token
                yield return new object[] { tcs.Task };
            }
        }

        /// <summary>Asserts that two non-generic tasks are logically equal with regards to completion status.</summary>
        private static void AssertTasksAreEqual(Task expected, Task actual)
        {
            Assert.NotNull(actual);
            WaitNoThrow(actual);

            Assert.Equal(expected.Status, actual.Status);
            switch (expected.Status)
            {
                case TaskStatus.Faulted:
                    Assert.Equal((IEnumerable<Exception>)expected.Exception.InnerExceptions, actual.Exception.InnerExceptions);
                    break;
                case TaskStatus.Canceled:
                    Assert.Equal(GetCanceledTaskToken(expected), GetCanceledTaskToken(actual));
                    break;
            }
        }

        /// <summary>Asserts that two non-generic tasks are logically equal with regards to completion status.</summary>
        private static void AssertTasksAreEqual<T>(Task<T> expected, Task<T> actual)
        {
            AssertTasksAreEqual((Task)expected, actual);
            if (expected.Status == TaskStatus.RanToCompletion)
            {
                if (typeof(T).GetTypeInfo().IsValueType)
                    Assert.Equal(expected.Result, actual.Result);
                else
#pragma warning disable xUnit2005 // Do not use Assert.Same() on value type 'T'. Value types do not have identity. Use Assert.Equal instead.
                    Assert.Same((object)expected.Result, (object)actual.Result);
#pragma warning restore xUnit2005
            }
        }

        /// <summary>Creates an already canceled token.</summary>
        private static CancellationToken CreateCanceledToken()
        {
            // Create an already canceled token.  We construct a new CTS rather than
            // just using CT's Boolean ctor in order to better validate the right
            // token ends up in the resulting unwrapped task.
            var cts = new CancellationTokenSource();
            cts.Cancel();
            return cts.Token;
        }

        /// <summary>Waits for a task to complete without throwing any exceptions.</summary>
        private static void WaitNoThrow(Task task)
        {
            ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
        }

        /// <summary>Extracts the CancellationToken associated with a task.</summary>
        private static CancellationToken GetCanceledTaskToken(Task task)
        {
            Assert.True(task.IsCanceled);
            try
            {
                task.GetAwaiter().GetResult();
                Assert.Fail("Canceled task should have thrown from GetResult");
                return default(CancellationToken);
            }
            catch (OperationCanceledException oce)
            {
                return oce.CancellationToken;
            }
        }

        private sealed class CountingScheduler : TaskScheduler
        {
            public int QueueTaskCalls = 0;

            protected override void QueueTask(Task task)
            {
                Interlocked.Increment(ref QueueTaskCalls);
                Task.Run(() => TryExecuteTask(task));
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) { return false; }

            protected override IEnumerable<Task> GetScheduledTasks() { return null; }
        }

    }
}
