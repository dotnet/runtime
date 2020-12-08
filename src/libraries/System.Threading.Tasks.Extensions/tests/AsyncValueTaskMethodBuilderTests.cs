// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class AsyncValueTaskMethodBuilderTests
    {
        [Fact]
        public void Create_ReturnsDefaultInstance() // implementation detail being verified
        {
            Assert.Equal(default, AsyncValueTaskMethodBuilder.Create()); 
            Assert.Equal(default, AsyncValueTaskMethodBuilder<int>.Create());
        }

        [Fact]
        public void NonGeneric_SetResult_BeforeAccessTask_ValueTaskIsDefault()
        {
            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();

            b.SetResult();
            
            Assert.Equal(default, b.Task);
        }

        [Fact]
        public void Generic_SetResult_BeforeAccessTask_ValueTaskContainsValue()
        {
            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();
            
            b.SetResult(42);

            ValueTask<int> vt = b.Task;
            Assert.Equal(vt, b.Task);
            Assert.Equal(new ValueTask<int>(42), vt);
        }

        [Fact]
        public void NonGeneric_SetResult_AfterAccessTask_ValueTaskContainsValue()
        {
            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();

            ValueTask vt = b.Task;
            Assert.NotEqual(default, vt);
            Assert.Equal(vt, b.Task);

            b.SetResult();
            
            Assert.Equal(vt, b.Task);
            Assert.True(vt.IsCompletedSuccessfully);
        }

        [Fact]
        public void Generic_SetResult_AfterAccessTask_ValueTaskContainsValue()
        {
            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();

            ValueTask<int> vt = b.Task;
            Assert.NotEqual(default, vt);
            Assert.Equal(vt, b.Task);
            
            b.SetResult(42);
            
            Assert.Equal(vt, b.Task);
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.Equal(42, vt.Result);
        }

        [Fact]
        public void NonGeneric_SetException_BeforeAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();
            
            var e = new FormatException();
            b.SetException(e);

            ValueTask vt = b.Task;
            Assert.Equal(vt, b.Task);
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void Generic_SetException_BeforeAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();

            var e = new FormatException();
            b.SetException(e);

            ValueTask<int> vt = b.Task;
            Assert.Equal(vt, b.Task);
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void NonGeneric_SetException_AfterAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();

            ValueTask vt = b.Task;
            Assert.Equal(vt, b.Task);

            var e = new FormatException();
            b.SetException(e);

            Assert.Equal(vt, b.Task);
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void Generic_SetException_AfterAccessTask_FaultsTask()
        {
            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();

            ValueTask<int> vt = b.Task;
            Assert.Equal(vt, b.Task);

            var e = new FormatException();
            b.SetException(e);

            Assert.Equal(vt, b.Task);
            Assert.True(vt.IsFaulted);
            Assert.Same(e, Assert.Throws<FormatException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void NonGeneric_SetException_OperationCanceledException_CancelsTask()
        {
            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();

            ValueTask vt = b.Task;
            Assert.Equal(vt, b.Task);

            var e = new OperationCanceledException();
            b.SetException(e);
            
            Assert.Equal(vt, b.Task);
            Assert.True(vt.IsCanceled);
            Assert.Same(e, Assert.Throws<OperationCanceledException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void Generic_SetException_OperationCanceledException_CancelsTask()
        {
            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();

            ValueTask<int> vt = b.Task;
            Assert.Equal(vt, b.Task);

            var e = new OperationCanceledException();
            b.SetException(e);
            
            Assert.Equal(vt, b.Task);
            Assert.True(vt.IsCanceled);
            Assert.Same(e, Assert.Throws<OperationCanceledException>(() => vt.GetAwaiter().GetResult()));
        }

        [Fact]
        public void NonGeneric_SetExceptionWithNullException_Throws()
        {
            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();
            AssertExtensions.Throws<ArgumentNullException>("exception", () => b.SetException(null));
        }

        [Fact]
        public void Generic_SetExceptionWithNullException_Throws()
        {
            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();
            AssertExtensions.Throws<ArgumentNullException>("exception", () => b.SetException(null));
        }

        [Fact]
        public void NonGeneric_Start_InvokesMoveNext()
        {
            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();

            int invokes = 0;
            var dsm = new DelegateStateMachine { MoveNextDelegate = () => invokes++ };
            b.Start(ref dsm);
            
            Assert.Equal(1, invokes);
        }

        [Fact]
        public void Generic_Start_InvokesMoveNext()
        {
            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();

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
            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();

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

            ValueTask vt = b.Task;
            Assert.NotEqual(default, vt);
            Assert.True(vt.IsCompletedSuccessfully);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        public void Generic_AwaitOnCompleted_ForcesTaskCreation(int numAwaits, bool awaitUnsafe)
        {
            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();

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

            ValueTask<int> vt = b.Task;
            Assert.NotEqual(default, vt);
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.Equal(42, vt.Result);
        }

        [Fact]
        public void NonGeneric_SetStateMachine_InvalidArgument_ThrowsException()
        {
            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();
            AssertExtensions.Throws<ArgumentNullException>("stateMachine", () => b.SetStateMachine(null));
        }

        [Fact]
        public void Generic_SetStateMachine_InvalidArgument_ThrowsException()
        {
            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();
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

            AsyncValueTaskMethodBuilder b = AsyncValueTaskMethodBuilder.Create();
            b.Start(ref dsm);
            Assert.Equal(2, al.Value); // change should not be visible
            Assert.Equal(3, calls);

            // Make sure we've not caused the Task to be allocated
            b.SetResult();
            Assert.Equal(default, b.Task);
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

            AsyncValueTaskMethodBuilder<int> b = AsyncValueTaskMethodBuilder<int>.Create();
            b.Start(ref dsm);
            Assert.Equal(2, al.Value); // change should not be visible
            Assert.Equal(3, calls);

            // Make sure we've not caused the Task to be allocated
            b.SetResult(42);
            Assert.Equal(new ValueTask<int>(42), b.Task);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public static async Task NonGeneric_UsedWithAsyncMethod_CompletesSuccessfully(int yields)
        {
            StrongBox<int> result;

            result = new StrongBox<int>();
            await ValueTaskReturningAsyncMethod(42, result);
            Assert.Equal(42 + yields, result.Value);

            result = new StrongBox<int>();
            await ValueTaskReturningAsyncMethod(84, result);
            Assert.Equal(84 + yields, result.Value);

            async ValueTask ValueTaskReturningAsyncMethod(int result, StrongBox<int> output)
            {
                for (int i = 0; i < yields; i++)
                {
                    await Task.Yield();
                    result++;
                }
                output.Value = result;
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public static async Task Generic_UsedWithAsyncMethod_CompletesSuccessfully(int yields)
        {
            Assert.Equal(42 + yields, await ValueTaskReturningAsyncMethod(42));
            Assert.Equal(84 + yields, await ValueTaskReturningAsyncMethod(84));

            async ValueTask<int> ValueTaskReturningAsyncMethod(int result)
            {
                for (int i = 0; i < yields; i++)
                {
                    await Task.Yield();
                    result++;
                }
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

        [Fact]
        public async Task NonGeneric_ConcurrentBuilders_WorkCorrectly()
        {
            await Task.WhenAll(Enumerable.Range(0, Environment.ProcessorCount).Select(async _ =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await ValueTaskAsync();

                    static async ValueTask ValueTaskAsync()
                    {
                        await Task.Delay(1);
                    }
                }
            }));
        }

        [Fact]
        public async Task Generic_ConcurrentBuilders_WorkCorrectly()
        {
            await Task.WhenAll(Enumerable.Range(0, Environment.ProcessorCount).Select(async _ =>
            {
                for (int i = 0; i < 10; i++)
                {
                    Assert.Equal(42 + i, await ValueTaskAsync(i));

                    static async ValueTask<int> ValueTaskAsync(int i)
                    {
                        await Task.Delay(1);
                        return 42 + i;
                    }
                }
            }));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("1", null)]
        [InlineData("true", null)]
        [InlineData("true", "1")]
        [InlineData("true", "100")]
        [InlineData("false", null)]
        [InlineData("false", "100")]
        public void PoolingAsyncValueTasksBuilder_ObjectsPooled(string poolingEnvVar, string limitEnvVar)
        {
            // Use RemoteExecutor to launch a process with the right environment variables set
            var psi = new ProcessStartInfo();
            psi.Environment.Add("DOTNET_SYSTEM_THREADING_POOLASYNCVALUETASKS", poolingEnvVar);
            if (limitEnvVar != null)
            {
                psi.Environment.Add("DOTNET_SYSTEM_THREADING_POOLASYNCVALUETASKSLIMIT", limitEnvVar);
            }

            RemoteExecutor.Invoke(async expectReuse =>
            {
                var boxes = new ConcurrentQueue<object>();
                var valueTasks = new ValueTask<int>[10];
                int total = 0;

                // Invoke a bunch of ValueTask methods, some in parallel,
                // and track a) their results and b) what boxing object is used.
                for (int rep = 0; rep < 3; rep++)
                {
                    for (int i = 0; i < valueTasks.Length; i++)
                    {
                        valueTasks[i] = ComputeAsync(i + 1, boxes);
                    }
                    foreach (ValueTask<int> vt in valueTasks)
                    {
                        total += await vt;
                    }
                }

                // Make sure we got the right total, and that if we expected pooling,
                // we at least pooled one object.
                Assert.Equal(330, total);
                if (expectReuse == "1" || expectReuse == "true")
                {
                    Assert.InRange(boxes.Distinct().Count(), 1, boxes.Count - 1);
                }
            }, (poolingEnvVar == "1" || poolingEnvVar == "true").ToString(), new RemoteInvokeOptions() { StartInfo = psi }).Dispose();

            static async ValueTask<int> ComputeAsync(int input, ConcurrentQueue<object> boxes)
            {
                await RecursiveValueTaskAsync(3, boxes);
                return input * 2;
            }

            static async ValueTask RecursiveValueTaskAsync(int depth, ConcurrentQueue<object> boxes)
            {
                boxes.Enqueue(await GetStateMachineData.FetchAsync());
                if (depth > 0)
                {
                    await Task.Delay(1);
                    await RecursiveValueTaskAsync(depth - 1, boxes);
                }
            }
        }

        private struct DelegateStateMachine : IAsyncStateMachine
        {
            internal Action MoveNextDelegate;
            public void MoveNext() => MoveNextDelegate?.Invoke();

            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }
    }
}
