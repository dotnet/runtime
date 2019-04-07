// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class NetworkStreamTest
    {
        [Fact]
        public async Task ReadWrite_Span_Success()
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                var clientData = new byte[] { 42 };

                client.Write((ReadOnlySpan<byte>)clientData);

                var serverData = new byte[clientData.Length];
                Assert.Equal(serverData.Length, server.Read((Span<byte>)serverData));

                Assert.Equal(clientData, serverData);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ReadWrite_Memory_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var clientData = new byte[] { 42 };

                await client.WriteAsync((ReadOnlyMemory<byte>)clientData);

                var serverData = new byte[clientData.Length];
                Assert.Equal(serverData.Length, await server.ReadAsync((Memory<byte>)serverData));

                Assert.Equal(clientData, serverData);
            });
        }

        [Fact]
        public async Task ReadWrite_Memory_LargeWrite_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var writeBuffer = new byte[10 * 1024 * 1024];
                var readBuffer = new byte[writeBuffer.Length];
                RandomNumberGenerator.Fill(writeBuffer);

                ValueTask writeTask = client.WriteAsync((ReadOnlyMemory<byte>)writeBuffer);

                int totalRead = 0;
                while (totalRead < readBuffer.Length)
                {
                    int bytesRead = await server.ReadAsync(new Memory<byte>(readBuffer).Slice(totalRead));
                    Assert.InRange(bytesRead, 0, int.MaxValue);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    totalRead += bytesRead;
                }
                Assert.Equal(readBuffer.Length, totalRead);
                Assert.Equal<byte>(writeBuffer, readBuffer);

                await writeTask;
            });
        }

        [Fact]
        public async Task ReadWrite_Precanceled_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.WriteAsync((ArraySegment<byte>)new byte[0], new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.ReadAsync((ArraySegment<byte>)new byte[0], new CancellationToken(true)));

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.WriteAsync((ReadOnlyMemory<byte>)new byte[0], new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.ReadAsync((Memory<byte>)new byte[0], new CancellationToken(true)));
            });
        }

        [Fact]
        public async Task ReadAsync_AwaitMultipleTimes_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var b = new byte[1];
                ValueTask<int> r = server.ReadAsync(b);
                await client.WriteAsync(new byte[] { 42 });
                Assert.Equal(1, await r);
                Assert.Equal(42, b[0]);
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await r);
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().IsCompleted);
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().OnCompleted(() => { }));
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().GetResult());
            });
        }

        [Fact]
        public async Task ReadAsync_MultipleContinuations_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                var b = new byte[1];
                ValueTask<int> r = server.ReadAsync(b);
                r.GetAwaiter().OnCompleted(() => { });
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().OnCompleted(() => { }));
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ReadAsync_MultipleConcurrentValueTaskReads_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                // Technically this isn't supported behavior, but it happens to work because it's supported on socket.
                // So validate it to alert us to any potential future breaks.

                byte[] b1 = new byte[1], b2 = new byte[1], b3 = new byte[1];
                ValueTask<int> r1 = server.ReadAsync(b1);
                ValueTask<int> r2 = server.ReadAsync(b2);
                ValueTask<int> r3 = server.ReadAsync(b3);

                await client.WriteAsync(new byte[] { 42, 43, 44 });

                Assert.Equal(3, await r1 + await r2 + await r3);
                Assert.Equal(42 + 43 + 44, b1[0] + b2[0] + b3[0]);
            });
        }

        [Fact]
        public async Task ReadAsync_MultipleConcurrentValueTaskReads_AsTask_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                // Technically this isn't supported behavior, but it happens to work because it's supported on socket.
                // So validate it to alert us to any potential future breaks.

                byte[] b1 = new byte[1], b2 = new byte[1], b3 = new byte[1];
                Task<int> r1 = server.ReadAsync((Memory<byte>)b1).AsTask();
                Task<int> r2 = server.ReadAsync((Memory<byte>)b2).AsTask();
                Task<int> r3 = server.ReadAsync((Memory<byte>)b3).AsTask();

                await client.WriteAsync(new byte[] { 42, 43, 44 });

                Assert.Equal(3, await r1 + await r2 + await r3);
                Assert.Equal(42 + 43 + 44, b1[0] + b2[0] + b3[0]);
            });
        }

        [Fact]
        public async Task WriteAsync_MultipleConcurrentValueTaskWrites_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                // Technically this isn't supported behavior, but it happens to work because it's supported on socket.
                // So validate it to alert us to any potential future breaks.

                ValueTask s1 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 42 }));
                ValueTask s2 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 43 }));
                ValueTask s3 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 44 }));

                byte[] b1 = new byte[1], b2 = new byte[1], b3 = new byte[1];
                Assert.Equal(3,
                    await client.ReadAsync((Memory<byte>)b1) +
                    await client.ReadAsync((Memory<byte>)b2) +
                    await client.ReadAsync((Memory<byte>)b3));

                await s1;
                await s2;
                await s3;

                Assert.Equal(42 + 43 + 44, b1[0] + b2[0] + b3[0]);
            });
        }

        [Fact]
        public async Task WriteAsync_MultipleConcurrentValueTaskWrites_AsTask_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                // Technically this isn't supported behavior, but it happens to work because it's supported on socket.
                // So validate it to alert us to any potential future breaks.

                Task s1 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 42 })).AsTask();
                Task s2 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 43 })).AsTask();
                Task s3 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 44 })).AsTask();

                byte[] b1 = new byte[1], b2 = new byte[1], b3 = new byte[1];
                Task<int> r1 = client.ReadAsync((Memory<byte>)b1).AsTask();
                Task<int> r2 = client.ReadAsync((Memory<byte>)b2).AsTask();
                Task<int> r3 = client.ReadAsync((Memory<byte>)b3).AsTask();

                await Task.WhenAll(s1, s2, s3, r1, r2, r3);

                Assert.Equal(3, await r1 + await r2 + await r3);
                Assert.Equal(42 + 43 + 44, b1[0] + b2[0] + b3[0]);
            });
        }

        public static IEnumerable<object[]> ReadAsync_ContinuesOnCurrentContextIfDesired_MemberData() =>
            from flowExecutionContext in new[] { true, false }
            from continueOnCapturedContext in new bool?[] { null, false, true }
            select new object[] { flowExecutionContext, continueOnCapturedContext };

        [Theory]
        [MemberData(nameof(ReadAsync_ContinuesOnCurrentContextIfDesired_MemberData))]
        public async Task ReadAsync_ContinuesOnCurrentSynchronizationContextIfDesired(
            bool flowExecutionContext, bool? continueOnCapturedContext)
        {
            await Task.Run(async () => // escape xunit sync ctx
            {
                await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
                {
                    Assert.Null(SynchronizationContext.Current);

                    var continuationRan = new TaskCompletionSource<bool>();
                    var asyncLocal = new AsyncLocal<int>();
                    bool schedulerWasFlowed = false;
                    bool executionContextWasFlowed = false;
                    Action continuation = () =>
                    {
                        schedulerWasFlowed = SynchronizationContext.Current is CustomSynchronizationContext;
                        executionContextWasFlowed = 42 == asyncLocal.Value;
                        continuationRan.SetResult(true);
                    };

                    var readBuffer = new byte[1];
                    ValueTask<int> readValueTask = client.ReadAsync((Memory<byte>)new byte[1]);

                    SynchronizationContext.SetSynchronizationContext(new CustomSynchronizationContext());
                    asyncLocal.Value = 42;
                    switch (continueOnCapturedContext)
                    {
                        case null:
                            if (flowExecutionContext)
                            {
                                readValueTask.GetAwaiter().OnCompleted(continuation);
                            }
                            else
                            {
                                readValueTask.GetAwaiter().UnsafeOnCompleted(continuation);
                            }
                            break;
                        default:
                            if (flowExecutionContext)
                            {
                                readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(continuation);
                            }
                            else
                            {
                                readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().UnsafeOnCompleted(continuation);
                            }
                            break;
                    }
                    asyncLocal.Value = 0;
                    SynchronizationContext.SetSynchronizationContext(null);

                    Assert.False(readValueTask.IsCompleted);
                    Assert.False(readValueTask.IsCompletedSuccessfully);
                    await server.WriteAsync(new byte[] { 42 });

                    await continuationRan.Task;
                    Assert.True(readValueTask.IsCompleted);
                    Assert.True(readValueTask.IsCompletedSuccessfully);

                    Assert.Equal(continueOnCapturedContext != false, schedulerWasFlowed);
                    Assert.Equal(flowExecutionContext, executionContextWasFlowed);
                });
            });
        }

        [Theory]
        [MemberData(nameof(ReadAsync_ContinuesOnCurrentContextIfDesired_MemberData))]
        public async Task ReadAsync_ContinuesOnCurrentTaskSchedulerIfDesired(
            bool flowExecutionContext, bool? continueOnCapturedContext)
        {
            await Task.Run(async () => // escape xunit sync ctx
            {
                await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
                {
                    Assert.Null(SynchronizationContext.Current);

                    var continuationRan = new TaskCompletionSource<bool>();
                    var asyncLocal = new AsyncLocal<int>();
                    bool schedulerWasFlowed = false;
                    bool executionContextWasFlowed = false;
                    Action continuation = () =>
                    {
                        schedulerWasFlowed = TaskScheduler.Current is CustomTaskScheduler;
                        executionContextWasFlowed = 42 == asyncLocal.Value;
                        continuationRan.SetResult(true);
                    };

                    var readBuffer = new byte[1];
                    ValueTask<int> readValueTask = client.ReadAsync((Memory<byte>)new byte[1]);

                    await Task.Factory.StartNew(() =>
                    {
                        Assert.IsType<CustomTaskScheduler>(TaskScheduler.Current);
                        asyncLocal.Value = 42;
                        switch (continueOnCapturedContext)
                        {
                            case null:
                                if (flowExecutionContext)
                                {
                                    readValueTask.GetAwaiter().OnCompleted(continuation);
                                }
                                else
                                {
                                    readValueTask.GetAwaiter().UnsafeOnCompleted(continuation);
                                }
                                break;
                            default:
                                if (flowExecutionContext)
                                {
                                    readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(continuation);
                                }
                                else
                                {
                                    readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().UnsafeOnCompleted(continuation);
                                }
                                break;
                        }
                        asyncLocal.Value = 0;
                    }, CancellationToken.None, TaskCreationOptions.None, new CustomTaskScheduler());

                    Assert.False(readValueTask.IsCompleted);
                    Assert.False(readValueTask.IsCompletedSuccessfully);
                    await server.WriteAsync(new byte[] { 42 });

                    await continuationRan.Task;
                    Assert.True(readValueTask.IsCompleted);
                    Assert.True(readValueTask.IsCompletedSuccessfully);

                    Assert.Equal(continueOnCapturedContext != false, schedulerWasFlowed);
                    Assert.Equal(flowExecutionContext, executionContextWasFlowed);
                });
            });
        }

        [Fact]
        public async Task DisposeAsync_ClosesStream()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            { 
                Assert.True(client.DisposeAsync().IsCompletedSuccessfully);
                Assert.True(server.DisposeAsync().IsCompletedSuccessfully);

                await client.DisposeAsync();
                await server.DisposeAsync();

                Assert.False(server.CanRead);
                Assert.False(server.CanWrite);

                Assert.False(client.CanRead);
                Assert.False(client.CanWrite);
            });
        }

        [Fact]
        public async Task ReadAsync_CancelPendingRead_DoesntImpactSubsequentReads()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ReadAsync(new byte[1], 0, 1, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await client.ReadAsync(new Memory<byte>(new byte[1]), new CancellationToken(true)); });

                CancellationTokenSource cts = new CancellationTokenSource();
                Task<int> t = client.ReadAsync(new byte[1], 0, 1, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);

                cts = new CancellationTokenSource();
                ValueTask<int> vt = client.ReadAsync(new Memory<byte>(new byte[1]), cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);

                byte[] buffer = new byte[1];
                vt = client.ReadAsync(new Memory<byte>(buffer));
                Assert.False(vt.IsCompleted);
                await server.WriteAsync(new ReadOnlyMemory<byte>(new byte[1] { 42 }));
                Assert.Equal(1, await vt);
                Assert.Equal(42, buffer[0]);
            });
        }

        private sealed class CustomSynchronizationContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object state)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SetSynchronizationContext(this);
                    try
                    {
                        d(state);
                    }
                    finally
                    {
                        SetSynchronizationContext(null);
                    }
                }, null);
            }
        }

        private sealed class CustomTaskScheduler : TaskScheduler
        {
            protected override void QueueTask(Task task) => ThreadPool.QueueUserWorkItem(_ => TryExecuteTask(task));
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
            protected override IEnumerable<Task> GetScheduledTasks() => null;
        }
    }
}
