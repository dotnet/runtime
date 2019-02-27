// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

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

        public byte[] ReadFrame(bool server) =>
            ReadFrameCoreAsync(server, sync: true).GetAwaiter().GetResult();

        public Task<byte[]> ReadFrameAsync(bool server) =>
            ReadFrameCoreAsync(server, sync: false);

        private async Task<byte[]> ReadFrameCoreAsync(bool server, bool sync)
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

            bool successfulWait = sync ?
                semaphore.Wait(WaitForReadDataTimeoutMilliseconds) :
                await semaphore.WaitAsync(WaitForReadDataTimeoutMilliseconds).ConfigureAwait(false);
            if (!successfulWait)
            {
                throw new TimeoutException("VirtualNetwork: Timeout reading the next frame.");
            }

            if (_connectionBroken)
            {
                throw new VirtualNetworkConnectionBroken();
            }

            int remainingTries = 3;
            int backOffDelayMilliseconds = 2;

            do
            {
                if (packetQueue.TryDequeue(out byte[] buffer))
                {
                    return buffer;
                }

                remainingTries--;
                backOffDelayMilliseconds *= backOffDelayMilliseconds;
                if (sync)
                {
                    Thread.Sleep(backOffDelayMilliseconds);
                }
                else
                {
                    await Task.Delay(backOffDelayMilliseconds).ConfigureAwait(false);
                }
            }
            while (remainingTries > 0);

            throw new InvalidOperationException("Packet queue: TryDequeue failed.");
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

            packetQueue.Enqueue(buffer.AsSpan().ToArray());
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
