// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        [ActiveIssue("https://github.com/dotnet/corefx/issues/22506", TargetFrameworkMonikers.UapAot)]
        public void SetStateMachine_InvalidArgument_ThrowsException()
        {
            AsyncValueTaskMethodBuilder<int> b = ValueTask<int>.CreateAsyncMethodBuilder();
            AssertExtensions.Throws<ArgumentNullException>("stateMachine", () => b.SetStateMachine(null));
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

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public static async Task UsedWithAsyncMethod_CompletesSuccessfully(int yields)
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

        /// <summary>Gets whether the ValueTask has a non-null Task.</summary>
        private static bool WrapsTask<T>(ValueTask<T> vt) => ReferenceEquals(vt.AsTask(), vt.AsTask());

        private struct DelegateStateMachine : IAsyncStateMachine
        {
            internal Action MoveNextDelegate;
            public void MoveNext() => MoveNextDelegate?.Invoke();

            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }
    }
}
