// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketSendReceivePerfTest
    {
        [Benchmark(InnerIterationCount = 10_000), MeasureGCAllocations]
        public async Task SendAsyncThenReceiveAsync_Task()
        {
            await OpenLoopbackConnectionAsync(async (client, server) =>
            {
                byte[] clientBuffer = new byte[1];
                byte[] serverBuffer = new byte[1];
                foreach (BenchmarkIteration iteration in Benchmark.Iterations)
                {
                    long iters = Benchmark.InnerIterationCount;
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iters; i++)
                        {
                            await client.SendAsync(clientBuffer, SocketFlags.None);
                            await server.ReceiveAsync(serverBuffer, SocketFlags.None);
                        }
                    }
                }
            });
        }

        [Benchmark(InnerIterationCount = 10_000), MeasureGCAllocations]
        public async Task ReceiveAsyncThenSendAsync_Task()
        {
            await OpenLoopbackConnectionAsync(async (client, server) =>
            {
                byte[] clientBuffer = new byte[1];
                byte[] serverBuffer = new byte[1];
                foreach (BenchmarkIteration iteration in Benchmark.Iterations)
                {
                    long iters = Benchmark.InnerIterationCount;
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iters; i++)
                        {
                            Task r = server.ReceiveAsync(serverBuffer, SocketFlags.None);
                            await client.SendAsync(clientBuffer, SocketFlags.None);
                            await r;
                        }
                    }
                }
            });
        }

        [Benchmark(InnerIterationCount = 1_000_000)]
        [InlineData(16)]
        public async Task ReceiveAsyncThenSendAsync_Task_Parallel(int numConnections)
        {
            byte[] clientBuffer = new byte[1];
            byte[] serverBuffer = new byte[1];
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                await OpenLoopbackConnectionAsync(async (client, server) =>
                {
                    long iters = Benchmark.InnerIterationCount;
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iters; i++)
                        {
                            Task r = server.ReceiveAsync(serverBuffer, SocketFlags.None);
                            await client.SendAsync(clientBuffer, SocketFlags.None);
                            await r;
                        }
                    }
                });
            }
        }

        [Benchmark(InnerIterationCount = 10_000), MeasureGCAllocations]
        public async Task SendAsyncThenReceiveAsync_SocketAsyncEventArgs()
        {
            await OpenLoopbackConnectionAsync(async (client, server) =>
            {
                var clientSaea = new AwaitableSocketAsyncEventArgs();
                var serverSaea = new AwaitableSocketAsyncEventArgs();

                clientSaea.SetBuffer(new byte[1], 0, 1);
                serverSaea.SetBuffer(new byte[1], 0, 1);

                foreach (BenchmarkIteration iteration in Benchmark.Iterations)
                {
                    long iters = Benchmark.InnerIterationCount;
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iters; i++)
                        {
                            if (client.SendAsync(clientSaea))
                            {
                                await clientSaea;
                            }
                            if (server.ReceiveAsync(serverSaea))
                            {
                                await serverSaea;
                            }
                        }
                    }
                }
            });
        }

        [Benchmark(InnerIterationCount = 10_000), MeasureGCAllocations]
        public async Task ReceiveAsyncThenSendAsync_SocketAsyncEventArgs()
        {
            await OpenLoopbackConnectionAsync(async (client, server) =>
            {
                var clientSaea = new AwaitableSocketAsyncEventArgs();
                var serverSaea = new AwaitableSocketAsyncEventArgs();

                clientSaea.SetBuffer(new byte[1], 0, 1);
                serverSaea.SetBuffer(new byte[1], 0, 1);

                foreach (BenchmarkIteration iteration in Benchmark.Iterations)
                {
                    long iters = Benchmark.InnerIterationCount;
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iters; i++)
                        {
                            bool pendingServer = server.ReceiveAsync(serverSaea);

                            if (client.SendAsync(clientSaea))
                            {
                                await clientSaea;
                            }

                            if (pendingServer)
                            {
                                await serverSaea;
                            }
                        }
                    }
                }
            });
        }

        private static async Task OpenLoopbackConnectionAsync(Func<Socket, Socket, Task> func)
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                Task connectTask = client.ConnectAsync(listener.LocalEndPoint);

                await await Task.WhenAny(acceptTask, connectTask);
                await Task.WhenAll(acceptTask, connectTask);

                using (Socket server = await acceptTask)
                {
                    await func(client, server);
                }
            }
        }

        internal sealed class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
        {
            private static readonly Action s_completedSentinel = () => { };
            private Action _continuation;

            public AwaitableSocketAsyncEventArgs()
            {
                Completed += delegate
                {
                    Action c = _continuation;
                    if (c != null)
                    {
                        c();
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null)?.Invoke();
                    }
                };
            }

            public AwaitableSocketAsyncEventArgs GetAwaiter() => this;

            public bool IsCompleted => _continuation != null;

            public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

            public void OnCompleted(Action continuation)
            {
                if (ReferenceEquals(_continuation, s_completedSentinel) ||
                    ReferenceEquals(Interlocked.CompareExchange(ref _continuation, continuation, null), s_completedSentinel))
                {
                    Task.Run(continuation);
                }
            }

            public int GetResult()
            {
                _continuation = null;
                if (SocketError != SocketError.Success)
                {
                    throw new SocketException((int)SocketError);
                }
                return BytesTransferred;
            }
        }
    }
}
