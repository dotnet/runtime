// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class SelectTest
    {
        private readonly ITestOutputHelper _log;

        public SelectTest(ITestOutputHelper output)
        {
            _log = output;
        }

        private const int SmallTimeoutMicroseconds = 10 * 1000;
        private const int FailTimeoutMicroseconds  = 30 * 1000 * 1000;

        [PlatformSpecific(~TestPlatforms.OSX)] // typical OSX install has very low max open file descriptors value
        [Theory]
        [InlineData(90, 0)]
        [InlineData(0, 90)]
        [InlineData(45, 45)]
        public void Select_ReadWrite_AllReady_ManySockets(int reads, int writes)
        {
            Select_ReadWrite_AllReady(reads, writes);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        [InlineData(2, 2)]
        public void Select_ReadWrite_AllReady(int reads, int writes)
        {
            var readPairs = Enumerable.Range(0, reads).Select(_ => CreateConnectedSockets()).ToArray();
            var writePairs = Enumerable.Range(0, writes).Select(_ => CreateConnectedSockets()).ToArray();
            try
            {
                foreach (var pair in readPairs)
                {
                    pair.Value.Send(new byte[1] { 42 });
                }

                var readList = new List<Socket>(readPairs.Select(p => p.Key).ToArray());
                var writeList = new List<Socket>(writePairs.Select(p => p.Key).ToArray());

                Socket.Select(readList, writeList, null, -1); // using -1 to test wait code path, but should complete instantly

                // Since no buffers are full, all writes should be available.
                Assert.Equal(writePairs.Length, writeList.Count);

                // We could wake up from Select for writes even if reads are about to become available,
                // so there's very little we can assert if writes is non-zero.
                if (writes == 0 && reads > 0)
                {
                    Assert.InRange(readList.Count, 1, readPairs.Length);
                }

                // When we do the select again, the lists shouldn't change at all, as they've already
                // been filtered to ones that were ready.
                int readListCountBefore = readList.Count;
                int writeListCountBefore = writeList.Count;
                Socket.Select(readList, writeList, null, FailTimeoutMicroseconds);
                Assert.Equal(readListCountBefore, readList.Count);
                Assert.Equal(writeListCountBefore, writeList.Count);
            }
            finally
            {
                DisposeSockets(readPairs);
                DisposeSockets(writePairs);
            }
        }

        [PlatformSpecific(~TestPlatforms.OSX)] // typical OSX install has very low max open file descriptors value
        [Fact]
        public void Select_ReadError_NoneReady_ManySockets()
        {
            Select_ReadError_NoneReady(45, 45);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        [InlineData(2, 2)]
        public void Select_ReadError_NoneReady(int reads, int errors)
        {
            var readPairs = Enumerable.Range(0, reads).Select(_ => CreateConnectedSockets()).ToArray();
            var errorPairs = Enumerable.Range(0, errors).Select(_ => CreateConnectedSockets()).ToArray();
            try
            {
                var readList = new List<Socket>(readPairs.Select(p => p.Key).ToArray());
                var errorList = new List<Socket>(errorPairs.Select(p => p.Key).ToArray());

                Socket.Select(readList, null, errorList, SmallTimeoutMicroseconds);

                Assert.Empty(readList);
                Assert.Empty(errorList);
            }
            finally
            {
                DisposeSockets(readPairs);
                DisposeSockets(errorPairs);
            }
        }

        [PlatformSpecific(~TestPlatforms.OSX)] // typical OSX install has very low max open file descriptors value
        public void Select_Read_OneReadyAtATime_ManySockets(int reads)
        {
            Select_Read_OneReadyAtATime(90); // value larger than the internal value in SocketPal.Unix that swaps between stack and heap allocation
        }

        [Theory]
        [InlineData(2)]
        public void Select_Read_OneReadyAtATime(int reads)
        {
            var rand = new Random(42);
            var readPairs = Enumerable.Range(0, reads).Select(_ => CreateConnectedSockets()).ToList();
            try
            {
                while (readPairs.Count > 0)
                {
                    int next = rand.Next(0, readPairs.Count);
                    readPairs[next].Value.Send(new byte[1] { 42 });

                    var readList = new List<Socket>(readPairs.Select(p => p.Key).ToArray());
                    Socket.Select(readList, null, null, FailTimeoutMicroseconds);

                    Assert.Equal(1, readList.Count);
                    Assert.Same(readPairs[next].Key, readList[0]);

                    readPairs.RemoveAt(next);
                }
            }
            finally
            {
                DisposeSockets(readPairs);
            }
        }

        [PlatformSpecific(~TestPlatforms.OSX)] // typical OSX install has very low max open file descriptors value
        [Fact]
        public void Select_Error_OneReadyAtATime()
        {
            const int Errors = 90; // value larger than the internal value in SocketPal.Unix that swaps between stack and heap allocation
            var rand = new Random(42);
            var errorPairs = Enumerable.Range(0, Errors).Select(_ => CreateConnectedSockets()).ToList();
            try
            {
                while (errorPairs.Count > 0)
                {
                    int next = rand.Next(0, errorPairs.Count);
                    errorPairs[next].Value.Send(new byte[1] { 42 }, SocketFlags.OutOfBand);

                    var errorList = new List<Socket>(errorPairs.Select(p => p.Key).ToArray());
                    Socket.Select(null, null, errorList, FailTimeoutMicroseconds);

                    Assert.Equal(1, errorList.Count);
                    Assert.Same(errorPairs[next].Key, errorList[0]);

                    errorPairs.RemoveAt(next);
                }
            }
            finally
            {
                DisposeSockets(errorPairs);
            }
        }

        [Theory]
        [InlineData(SelectMode.SelectRead)]
        [InlineData(SelectMode.SelectError)]
        public void Poll_NotReady(SelectMode mode)
        {
            KeyValuePair<Socket, Socket> pair = CreateConnectedSockets();
            try
            {
                Assert.False(pair.Key.Poll(SmallTimeoutMicroseconds, mode));
            }
            finally
            {
                pair.Key.Dispose();
                pair.Value.Dispose();
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(FailTimeoutMicroseconds)]
        public void Poll_ReadReady_LongTimeouts(int microsecondsTimeout)
        {
            KeyValuePair<Socket, Socket> pair = CreateConnectedSockets();
            try
            {
                Task.Delay(1).ContinueWith(_ => pair.Value.Send(new byte[1] { 42 }),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                Assert.True(pair.Key.Poll(microsecondsTimeout, SelectMode.SelectRead));
            }
            finally
            {
                pair.Key.Dispose();
                pair.Value.Dispose();
            }
        }

        private static KeyValuePair<Socket, Socket> CreateConnectedSockets()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.LingerState = new LingerOption(true, 0);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.LingerState = new LingerOption(true, 0);

                Task<Socket> acceptTask = listener.AcceptAsync();
                client.Connect(listener.LocalEndPoint);
                Socket server = acceptTask.GetAwaiter().GetResult();

                return new KeyValuePair<Socket, Socket>(client, server);
            }
        }

        private static void DisposeSockets(IEnumerable<KeyValuePair<Socket, Socket>> sockets)
        {
            foreach (var pair in sockets)
            {
                pair.Key.Dispose();
                pair.Value.Dispose();
            }
        }

        [OuterLoop]
        [Fact]
        public static async Task Select_AcceptNonBlocking_Success()
        {
            using (Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = listenSocket.BindToAnonymousPort(IPAddress.Loopback);

                listenSocket.Blocking = false;

                listenSocket.Listen(5);

                Task t = Task.Run(() => { DoAccept(listenSocket, 5); });

                // Loop, doing connections and pausing between
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(50);
                    using (Socket connectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        connectSocket.Connect(listenSocket.LocalEndPoint);
                    }
                }

                // Give the task 5 seconds to complete; if not, assume it's hung.
                await t.TimeoutAfter(5000);
            }
        }

        public static void DoAccept(Socket listenSocket, int connectionsToAccept)
        {
            int connectionCount = 0;
            while (true)
            {
                var ls = new List<Socket> { listenSocket };
                Socket.Select(ls, null, null, 1000000);
                if (ls.Count > 0)
                {
                    while (true)
                    {
                        try
                        {
                            Socket s = listenSocket.Accept();
                            s.Close();
                            connectionCount++;
                        }
                        catch (SocketException e)
                        {
                            Assert.Equal(e.SocketErrorCode, SocketError.WouldBlock);

                            //No more requests in queue
                            break;
                        }

                        if (connectionCount == connectionsToAccept)
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}
