// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class ExecutionContextFlowTest : FileCleanupTestBase
    {
        [OuterLoop("Relies on finalization")]
        [Fact]
        public void ExecutionContext_NotCachedInSocketAsyncEventArgs()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                client.Connect(listener.LocalEndPoint);
                using (Socket server = listener.Accept())
                using (var saea = new SocketAsyncEventArgs())
                {
                    var receiveCompleted = new ManualResetEventSlim();
                    saea.Completed += (_, __) => receiveCompleted.Set();
                    saea.SetBuffer(new byte[1]);

                    var ecDropped = new ManualResetEventSlim();
                    var al = CreateAsyncLocalWithSetWhenFinalized(ecDropped);
                    Assert.True(client.ReceiveAsync(saea));
                    al.Value = null;

                    server.Send(new byte[1]);
                    Assert.True(receiveCompleted.Wait(TestSettings.PassingTestTimeout));

                    for (int i = 0; i < 3; i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    Assert.True(ecDropped.Wait(TestSettings.PassingTestTimeout));
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static AsyncLocal<object> CreateAsyncLocalWithSetWhenFinalized(ManualResetEventSlim ecDropped) =>
            new AsyncLocal<object>() { Value = new SetOnFinalized { _setWhenFinalized = ecDropped } };

        private sealed class SetOnFinalized
        {
            internal ManualResetEventSlim _setWhenFinalized;
            ~SetOnFinalized() => _setWhenFinalized.Set();
        }

        [Fact]
        public Task ExecutionContext_FlowsOnlyOnceAcrossAsyncOperations()
        {
            return Task.Run(async () => // escape xunit's sync ctx
            {
                using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    listener.Listen(1);

                    client.Connect(listener.LocalEndPoint);
                    using (Socket server = listener.Accept())
                    {
                        var stackLog = new StringBuilder();
                        int executionContextChanges = 0;
                        var asyncLocal = new AsyncLocal<int>(_ =>
                        {
                            lock (stackLog)
                            {
                                executionContextChanges++;
                                stackLog.AppendLine($"#{executionContextChanges}: {Environment.StackTrace}");
                            }
                        });
                        Assert.Equal(0, executionContextChanges);

                        int numAwaits = 20;
                        for (int i = 1; i <= numAwaits; i++)
                        {
                            asyncLocal.Value = i;

                            await new AwaitWithOnCompletedInvocation<int>(
                                client.ReceiveAsync(new Memory<byte>(new byte[1]), SocketFlags.None),
                                () => server.Send(new byte[1]));

                            Assert.Equal(i, asyncLocal.Value);
                        }

                        // This doesn't count EC changes where EC.Run is passed the same context
                        // as is current, but it's the best we can track via public API.
                        try
                        {
                            Assert.InRange(executionContextChanges, 1, numAwaits * 3); // at most: 1 / AsyncLocal change + 1 / suspend + 1 / resume
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"{nameof(executionContextChanges)} == {executionContextChanges} with log: {stackLog.ToString()}", e);
                        }
                    }
                }
            });
        }

        private readonly struct AwaitWithOnCompletedInvocation<T> : ICriticalNotifyCompletion
        {
            private readonly ValueTask<T> _valueTask;
            private readonly Action _invokeAfterOnCompleted;

            public AwaitWithOnCompletedInvocation(ValueTask<T> valueTask, Action invokeAfterOnCompleted)
            {
                _valueTask = valueTask;
                _invokeAfterOnCompleted = invokeAfterOnCompleted;
            }

            public AwaitWithOnCompletedInvocation<T> GetAwaiter() => this;

            public bool IsCompleted => false;
            public T GetResult() => _valueTask.GetAwaiter().GetResult();
            public void OnCompleted(Action continuation) => throw new NotSupportedException();
            public void UnsafeOnCompleted(Action continuation)
            {
                _valueTask.GetAwaiter().UnsafeOnCompleted(continuation);
                _invokeAfterOnCompleted();
            }
        }
    }
}
