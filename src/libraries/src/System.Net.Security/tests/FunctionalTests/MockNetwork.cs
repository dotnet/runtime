// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;

namespace System.Net.Security.Tests
{
    internal class MockNetwork
    {
        private readonly Queue<byte[]> _clientWriteQueue = new Queue<byte[]>();
        private readonly Queue<byte[]> _serverWriteQueue = new Queue<byte[]>();

        private readonly SemaphoreSlim _clientDataAvailable = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _serverDataAvailable = new SemaphoreSlim(0);

        public void ReadFrame(bool server, out byte[] buffer)
        {
            SemaphoreSlim semaphore;
            Queue<byte[]> packetQueue;

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

            semaphore.Wait(TestConfiguration.PassingTestTimeoutMilliseconds);
            buffer = packetQueue.Dequeue();
        }

        public void WriteFrame(bool server, byte[] buffer)
        {
            SemaphoreSlim semaphore;
            Queue<byte[]> packetQueue;

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
    }
}
