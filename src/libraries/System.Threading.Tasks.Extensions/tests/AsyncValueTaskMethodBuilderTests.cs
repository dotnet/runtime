// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class AsyncValueTaskMethodBuilderTests
    {
        [Fact]
        public void Create_ReturnsDefaultInstance()
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            Assert.Equal(default(AsyncValueTaskMethodBuilder<int>), b); // implementation detail being verified
        }

        [Fact]
        public void SetResult_BeforeAccessTask_ValueTaskContainsValue()
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            b.SetResult(42);
            ValueTask<int> vt = b.Task;
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.False(WrapsTask(vt));
            Assert.Equal(42, vt.Result);
        }

        [Fact]
        public void SetResult_AfterAccessTask_ValueTaskContainsValue()
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            ValueTask<int> vt = b.Task;
            b.SetResult(42);
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.True(WrapsTask(vt));
            Assert.Equal(42, vt.Result);
        }

        [Fact]
        public void SetException_BeforeAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            var e = new FormatException();
            b.SetException(e);
            ValueTask<int> vt = b.Task;
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void SetException_AfterAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            var e = new FormatException();
            ValueTask<int> vt = b.Task;
            b.SetException(e);
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void SetException_OperationCanceledException_CancelsTask()
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            var e = new OperationCanceledException();
            ValueTask<int> vt = b.Task;
            b.SetException(e);
            Assert.True(vt.IsCanceled);
            Assert.Same(e, Assert.Throws<OperationCanceledException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void Start_InvokesMoveNext()
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            int invokes = 0;
            var dsm = new DelegateStateMachine { MoveNextDelegate = () => invokes++ };
            b.Start(ref dsm);
            Assert.Equal(1, invokes);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AwaitOnCompleted_InvokesStateMachineMethods(bool awaitUnsafe)
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            var ignored = b.Task;

            var callbackCompleted = new TaskCompletionSource<bool>();
            IAsyncStateMachine foundSm = null;
            var dsm = new DelegateStateMachine
            {
                MoveNextDelegate = () => callbackCompleted.SetResult(true),
                SetStateMachineDelegate = sm => foundSm = sm
            };

            TaskAwaiter t = Task.CompletedTask.GetAwaiter();
            if (awaitUnsafe)
            {
                b.AwaitUnsafeOnCompleted(ref t, ref dsm);
            }
            else
            {
                b.AwaitOnCompleted(ref t, ref dsm);
            }

            await callbackCompleted.Task;
            Assert.Equal(dsm, foundSm);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        public void AwaitOnCompleted_ForcesTaskCreation(int numAwaits, bool awaitUnsafe)
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();

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
        public void SetStateMachine_InvalidArgument_ThrowsException()
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            Assert.Throws<ArgumentNullException>("stateMachine", () => b.SetStateMachine(null));
            b.SetStateMachine(new DelegateStateMachine());
        }

        [Fact]
        public void Start_ExecutionContextChangesInMoveNextDontFlowOut()
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

            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            b.Start(ref dsm);
            Assert.Equal(2, al.Value); // change should not be visible
            Assert.Equal(3, calls);

            // Make sure we've not caused the Task to be allocated
            b.SetResult(42);
            ValueTask<int> vt = b.Task;
            Assert.False(WrapsTask(vt));
        }

        /// <summary>Gets whether the ValueTask has a non-null Task.</summary>
        private static bool WrapsTask<T>(ValueTask<T> vt) => ReferenceEquals(vt.AsTask(), vt.AsTask());

        private struct DelegateStateMachine : IAsyncStateMachine
        {
            internal Action MoveNextDelegate;
            public void MoveNext() => MoveNextDelegate?.Invoke();

            internal Action<IAsyncStateMachine> SetStateMachineDelegate;
            public void SetStateMachine(IAsyncStateMachine stateMachine) => SetStateMachineDelegate?.Invoke(stateMachine);
        }
    }
}
