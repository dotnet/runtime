// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources.Tests;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class AsyncValueTaskMethodBuilderTests
    {
        [Fact]
        public void NonGeneric_Create_ReturnsDefaultInstance()
        {
            AsyncValueTaskMethodBuilder b = default;
            Assert.Equal(default, b); // implementation detail being verified
        }

        [Fact]
        public void Generic_Create_ReturnsDefaultInstance()
        {
            AsyncValueTaskMethodBuilder<int> b = default;
            Assert.Equal(default, b); // implementation detail being verified
        }

        [Fact]
        public void NonGeneric_SetResult_BeforeAccessTask_ValueTaskIsDefault()
        {
            AsyncValueTaskMethodBuilder b = default;
            b.SetResult();
            ValueTask vt = b.Task;
            Assert.True(vt == default);
        }

        [Fact]
        public void Generic_SetResult_BeforeAccessTask_ValueTaskContainsValue()
        {
            AsyncValueTaskMethodBuilder<int> b = default;
            b.SetResult(42);
            ValueTask<int> vt = b.Task;
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.False(WrapsTask(vt));
            Assert.Equal(42, vt.Result);
        }

        [Fact]
        public void NonGeneric_SetResult_AfterAccessTask_ValueTaskContainsValue()
        {
            AsyncValueTaskMethodBuilder b = default;
            ValueTask vt = b.Task;
            b.SetResult();
            Assert.False(vt == default);
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.True(WrapsTask(vt));
        }

        [Fact]
        public void Generic_SetResult_AfterAccessTask_ValueTaskContainsValue()
        {
            AsyncValueTaskMethodBuilder<int> b = default;
            ValueTask<int> vt = b.Task;
            b.SetResult(42);
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.True(WrapsTask(vt));
            Assert.Equal(42, vt.Result);
        }

        [Fact]
        public void NonGeneric_SetException_BeforeAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder b = default;
            var e = new FormatException();
            b.SetException(e);
            ValueTask vt = b.Task;
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void Generic_SetException_BeforeAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder<int> b = default;
            var e = new FormatException();
            b.SetException(e);
            ValueTask<int> vt = b.Task;
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void NonGeneric_SetException_AfterAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder b = default;
            var e = new FormatException();
            ValueTask vt = b.Task;
            b.SetException(e);
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void Generic_SetException_AfterAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder<int> b = default;
            var e = new FormatException();
            ValueTask<int> vt = b.Task;
            b.SetException(e);
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void NonGeneric_SetException_OperationCanceledException_CancelsTask()
        {
            AsyncValueTaskMethodBuilder b = default;
            var e = new OperationCanceledException();
            ValueTask vt = b.Task;
            b.SetException(e);
            Assert.True(vt.IsCanceled);
            Assert.Same(e, Assert.Throws<OperationCanceledException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void Generic_SetException_OperationCanceledException_CancelsTask()
        {
            AsyncValueTaskMethodBuilder<int> b = default;
            var e = new OperationCanceledException();
            ValueTask<int> vt = b.Task;
            b.SetException(e);
            Assert.True(vt.IsCanceled);
            Assert.Same(e, Assert.Throws<OperationCanceledException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void NonGeneric_Start_InvokesMoveNext()
        {
            AsyncValueTaskMethodBuilder b = default;
            int invokes = 0;
            var dsm = new DelegateStateMachine { MoveNextDelegate = () => invokes++ };
            b.Start(ref dsm);
            Assert.Equal(1, invokes);
        }

        [Fact]
        public void Generic_Start_InvokesMoveNext()
        {
            AsyncValueTaskMethodBuilder<int> b = default;
            int invokes = 0;
            var dsm = new DelegateStateMachine { MoveNextDelegate = () => invokes++ };
            b.Start(ref dsm);
            Assert.Equal(1, invokes);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        public void NonGeneric_AwaitOnCompleted_ForcesTaskCreation(int numAwaits, bool awaitUnsafe)
        {
            AsyncValueTaskMethodBuilder b = default;

            var dsm = new DelegateStateMachine();
            TaskAwaiter<int> t = new TaskCompletionSource<int>().Task.GetAwaiter();

            Assert.InRange(numAwaits, 1, int.MaxValue);
            for (int i = 1; i <= numAwaits; i++)
            {
                if (awaitUnsafe)
                {
                    b.AwaitUnsafeOnCompleted(ref t, ref dsm);
                }
                else
                {
                    b.AwaitOnCompleted(ref t, ref dsm);
                }
            }

            b.SetResult();

            Assert.True(WrapsTask(b.Task));
            Assert.True(b.Task.IsCompletedSuccessfully);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        public void Generic_AwaitOnCompleted_ForcesTaskCreation(int numAwaits, bool awaitUnsafe)
        {
            AsyncValueTaskMethodBuilder<int> b = default;

            var dsm = new DelegateStateMachine();
            TaskAwaiter<int> t = new TaskCompletionSource<int>().Task.GetAwaiter();

            Assert.InRange(numAwaits, 1, int.MaxValue);
            for (int i = 1; i <= numAwaits; i++)
            {
                if (awaitUnsafe)
                {
                    b.AwaitUnsafeOnCompleted(ref t, ref dsm);
                }
                else
                {
                    b.AwaitOnCompleted(ref t, ref dsm);
                }
            }

            b.SetResult(42);

            Assert.True(WrapsTask(b.Task));
            Assert.Equal(42, b.Task.Result);
        }

        [Fact]
        public void NonGeneric_SetStateMachine_InvalidArgument_ThrowsException()
        {
            AsyncValueTaskMethodBuilder b = default;
            AssertExtensions.Throws<ArgumentNullException>("stateMachine", () => b.SetStateMachine(null));
        }

        [Fact]
        public void Generic_SetStateMachine_InvalidArgument_ThrowsException()
        {
            AsyncValueTaskMethodBuilder<int> b = default;
            AssertExtensions.Throws<ArgumentNullException>("stateMachine", () => b.SetStateMachine(null));
        }

        [Fact]
        public void NonGeneric_Start_ExecutionContextChangesInMoveNextDontFlowOut()
        {
            var al = new AsyncLocal<int> { Value = 0 };
            int calls = 0;

            var dsm = new DelegateStateMachine
            {
                MoveNextDelegate = () =>
                {
                    al.Value++;
                    calls++;
                }
            };

            dsm.MoveNext();
            Assert.Equal(1, al.Value);
            Assert.Equal(1, calls);

            dsm.MoveNext();
            Assert.Equal(2, al.Value);
            Assert.Equal(2, calls);

            AsyncValueTaskMethodBuilder b = default;
            b.Start(ref dsm);
            Assert.Equal(2, al.Value); // change should not be visible
            Assert.Equal(3, calls);

            // Make sure we've not caused the Task to be allocated
            b.SetResult();
            ValueTask vt = b.Task;
            Assert.False(WrapsTask(vt));
        }

        [Fact]
        public void Generic_Start_ExecutionContextChangesInMoveNextDontFlowOut()
        {
            var al = new AsyncLocal<int> { Value = 0 };
            int calls = 0;

            var dsm = new DelegateStateMachine
            {
                MoveNextDelegate = () =>
                {
                    al.Value++;
                    calls++;
                }
            };

            dsm.MoveNext();
            Assert.Equal(1, al.Value);
            Assert.Equal(1, calls);

            dsm.MoveNext();
            Assert.Equal(2, al.Value);
            Assert.Equal(2, calls);

            AsyncValueTaskMethodBuilder<int> b = default;
            b.Start(ref dsm);
            Assert.Equal(2, al.Value); // change should not be visible
            Assert.Equal(3, calls);

            // Make sure we've not caused the Task to be allocated
            b.SetResult(42);
            ValueTask<int> vt = b.Task;
            Assert.False(WrapsTask(vt));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public static async Task NonGeneric_UsedWithAsyncMethod_CompletesSuccessfully(int yields)
        {
            await ValueTaskReturningAsyncMethod(42);

            ValueTask vt = ValueTaskReturningAsyncMethod(84);
            Assert.Equal(yields > 0, WrapsTask(vt));

            async ValueTask ValueTaskReturningAsyncMethod(int result)
            {
                for (int i = 0; i < yields; i++) await Task.Yield();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public static async Task Generic_UsedWithAsyncMethod_CompletesSuccessfully(int yields)
        {
            Assert.Equal(42, await ValueTaskReturningAsyncMethod(42));

            ValueTask<int> vt = ValueTaskReturningAsyncMethod(84);
            Assert.Equal(yields > 0, WrapsTask(vt));
            Assert.Equal(84, await vt);

            async ValueTask<int> ValueTaskReturningAsyncMethod(int result)
            {
                for (int i = 0; i < yields; i++) await Task.Yield();
                return result;
            }
        }

        [Fact]
        public static async Task AwaitTasksAndValueTasks_InTaskAndValueTaskMethods()
        {
            for (int i = 0; i < 2; i++)
            {
                await TaskReturningMethod();
                Assert.Equal(17, await TaskInt32ReturningMethod());
                await ValueTaskReturningMethod();
                Assert.Equal(18, await ValueTaskInt32ReturningMethod());
            }

            async Task TaskReturningMethod()
            {
                for (int i = 0; i < 3; i++)
                {
                    // Complete
                    await Task.CompletedTask;
                    await Task.FromResult(42);
                    await new ValueTask();
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(Task.FromException<int>(new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(ManualResetValueTaskSourceFactory.Completed(0, new FormatException()), 0));
                    Assert.Equal(42, await new ValueTask<int>(42));
                    Assert.Equal(42, await new ValueTask<int>(Task.FromResult(42)));
                    Assert.Equal(42, await new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(42, null), 0));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(Task.FromException<int>(new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(0, new FormatException()), 0));

                    // Incomplete
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(Task.Delay(1).ContinueWith(_ => throw new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(ManualResetValueTaskSourceFactory.Delay(1, 0, new FormatException()), 0));
                    Assert.Equal(42, await new ValueTask<int>(Task.Delay(1).ContinueWith(_ => 42)));
                    Assert.Equal(42, await new ValueTask<int>(ManualResetValueTaskSourceFactory.Delay(1, 42, null), 0));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(Task.Delay(1).ContinueWith<int>(_ => throw new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(ManualResetValueTaskSourceFactory.Delay(1, 0, new FormatException()), 0));
                    await Task.Yield();
                }
            }

            async Task<int> TaskInt32ReturningMethod()
            {
                for (int i = 0; i < 3; i++)
                {
                    // Complete
                    await Task.CompletedTask;
                    await Task.FromResult(42);
                    await new ValueTask();
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(Task.FromException<int>(new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(ManualResetValueTaskSourceFactory.Completed(0, new FormatException()), 0));
                    Assert.Equal(42, await new ValueTask<int>(42));
                    Assert.Equal(42, await new ValueTask<int>(Task.FromResult(42)));
                    Assert.Equal(42, await new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(42, null), 0));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(Task.FromException<int>(new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(0, new FormatException()), 0));

                    // Incomplete
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(Task.Delay(1).ContinueWith(_ => throw new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(ManualResetValueTaskSourceFactory.Delay(1, 0, new FormatException()), 0));
                    Assert.Equal(42, await new ValueTask<int>(Task.Delay(1).ContinueWith(_ => 42)));
                    Assert.Equal(42, await new ValueTask<int>(ManualResetValueTaskSourceFactory.Delay(1, 42, null), 0));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(Task.Delay(1).ContinueWith<int>(_ => throw new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(ManualResetValueTaskSourceFactory.Delay(1, 0, new FormatException()), 0));
                    await Task.Yield();
                }
                return 17;
            }

            async ValueTask ValueTaskReturningMethod()
            {
                for (int i = 0; i < 3; i++)
                {
                    // Complete
                    await Task.CompletedTask;
                    await Task.FromResult(42);
                    await new ValueTask();
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(Task.FromException<int>(new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(ManualResetValueTaskSourceFactory.Completed(0, new FormatException()), 0));
                    Assert.Equal(42, await new ValueTask<int>(42));
                    Assert.Equal(42, await new ValueTask<int>(Task.FromResult(42)));
                    Assert.Equal(42, await new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(42, null), 0));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(Task.FromException<int>(new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(0, new FormatException()), 0));

                    // Incomplete
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(Task.Delay(1).ContinueWith(_ => throw new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(ManualResetValueTaskSourceFactory.Delay(1, 0, new FormatException()), 0));
                    Assert.Equal(42, await new ValueTask<int>(Task.Delay(1).ContinueWith(_ => 42)));
                    Assert.Equal(42, await new ValueTask<int>(ManualResetValueTaskSourceFactory.Delay(1, 42, null), 0));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(Task.Delay(1).ContinueWith<int>(_ => throw new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(ManualResetValueTaskSourceFactory.Delay(1, 0, new FormatException()), 0));
                    await Task.Yield();
                }
            }

            async ValueTask<int> ValueTaskInt32ReturningMethod()
            {
                for (int i = 0; i < 3; i++)
                {
                    // Complete
                    await Task.CompletedTask;
                    await Task.FromResult(42);
                    await new ValueTask();
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(Task.FromException<int>(new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(ManualResetValueTaskSourceFactory.Completed(0, new FormatException()), 0));
                    Assert.Equal(42, await new ValueTask<int>(42));
                    Assert.Equal(42, await new ValueTask<int>(Task.FromResult(42)));
                    Assert.Equal(42, await new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(42, null), 0));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(Task.FromException<int>(new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(0, new FormatException()), 0));

                    // Incomplete
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(Task.Delay(1).ContinueWith(_ => throw new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask(ManualResetValueTaskSourceFactory.Delay(1, 0, new FormatException()), 0));
                    Assert.Equal(42, await new ValueTask<int>(Task.Delay(1).ContinueWith(_ => 42)));
                    Assert.Equal(42, await new ValueTask<int>(ManualResetValueTaskSourceFactory.Delay(1, 42, null), 0));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(Task.Delay(1).ContinueWith<int>(_ => throw new FormatException())));
                    await Assert.ThrowsAsync<FormatException>(async () => await new ValueTask<int>(ManualResetValueTaskSourceFactory.Delay(1, 0, new FormatException()), 0));
                    await Task.Yield();
                }
                return 18;
            }
        }

        private static bool WrapsTask(ValueTask vt) => vt != default;
        private static bool WrapsTask<T>(ValueTask<T> vt) => ReferenceEquals(vt.AsTask(), vt.AsTask());

        private struct DelegateStateMachine : IAsyncStateMachine
        {
            internal Action MoveNextDelegate;
            public void MoveNext() => MoveNextDelegate?.Invoke();

            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }
    }
}
