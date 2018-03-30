// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace System.Net.Test.Common
{
    public class VirtualNetwork
    {
        public class VirtualNetworkConnectionBroken : Exception
        {
            public VirtualNetworkConnectionBroken() : base("Connection broken") { }
        }

        private readonly int WaitForReadDataTimeoutMilliseconds = 30 * 1000;

        private readonly ConcurrentQueue<byte[]> _clientWriteQueue = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<byte[]> _serverWriteQueue = new ConcurrentQueue<byte[]>();

        private readonly SemaphoreSlim _clientDataAvailable = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _serverDataAvailable = new SemaphoreSlim(0);

        public bool DisableConnectionBreaking { get; set; } = false;
        private bool _connectionBroken = false;

        public void ReadFrame(bool server, out byte[] buffer)
        {
            if (_connectionBroken)
            {
                throw new VirtualNetworkConnectionBroken();
            }

            SemaphoreSlim semaphore;
            ConcurrentQueue<byte[]> packetQueue;

            if (server)
            {
                semaphore = _clientDataAvailable;
                packetQueue = _clientWriteQueue;
            }
            else
            {
                semaphore = _serverDataAvailable;
                packetQueue = _serverWriteQueue;
            }

            if (!semaphore.Wait(WaitForReadDataTimeoutMilliseconds))
            {
                throw new TimeoutException("VirtualNetwork: Timeout reading the next frame.");
            }

            if (_connectionBroken)
            {
                throw new VirtualNetworkConnectionBroken();
            }

            bool dequeueSucceeded = false;
            int remainingTries = 3;
            int backOffDelayMilliseconds = 2;

            do
            {
                dequeueSucceeded = packetQueue.TryDequeue(out buffer);
                if (dequeueSucceeded)
                {
                    break;
                }

                remainingTries--;
                backOffDelayMilliseconds *= backOffDelayMilliseconds;
                Thread.Sleep(backOffDelayMilliseconds);
            }
            while (!dequeueSucceeded && (remainingTries > 0));

            Debug.Assert(dequeueSucceeded, "Packet queue: TryDequeue failed.");
        }

        public void WriteFrame(bool server, byte[] buffer)
        {
            if (_connectionBroken)
            {
                throw new VirtualNetworkConnectionBroken();
            }

            SemaphoreSlim semaphore;
            ConcurrentQueue<byte[]> packetQueue;

            if (server)
            {
                semaphore = _serverDataAvailable;
                packetQueue = _serverWriteQueue;
            }
            else
            {
                semaphore = _clientDataAvailable;
                packetQueue = _clientWriteQueue;
            }

            byte[] innerBuffer = new byte[buffer.Length];
            buffer.CopyTo(innerBuffer, 0);

            packetQueue.Enqueue(innerBuffer);
            semaphore.Release();
        }

        public void BreakConnection()
        {
            if (!DisableConnectionBreaking)
            {
                _connectionBroken = true;
                _serverDataAvailable.Release(1_000_000);
                _clientDataAvailable.Release(1_000_000);
            }
        }
    }
}
