// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets.Tests;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Test.Common
{
    public class VirtualNetworkTest
    {
        [Fact]
        public void VirtualNetwork_SingleThreadIntegrityTest_Ok()
        {
            var rnd = new Random();

            var network = new VirtualNetwork();

            for (int i = 0; i < 100000; i++)
            {
                int bufferSize = rnd.Next(1, 2048);

                byte [] writeFrame = new byte[bufferSize];
                rnd.NextBytes(writeFrame);
                uint writeChecksum = Fletcher32.Checksum(writeFrame, 0, writeFrame.Length);

                network.WriteFrame(i % 2 == 0, writeFrame);

                byte [] readFrame;
                network.ReadFrame(i % 2 == 1, out readFrame);

                uint readChecksum = Fletcher32.Checksum(readFrame, 0, readFrame.Length);

                Assert.Equal(writeChecksum, readChecksum);
            }
        }

        [Fact]
        public void VirtualNetwork_MultiThreadIntegrityTest_Ok()
        {
            var rnd = new Random();

            var network = new VirtualNetwork();
            var checksums = new ConcurrentDictionary<int, uint>();

            Parallel.For(0, 100000, async (int i) =>
            {
                int bufferSize = rnd.Next(5, 2048);

                byte[] writeFrame = new byte[bufferSize];
                rnd.NextBytes(writeFrame);
                
                // First 4 bytes represent the sequence number.
                byte [] sequenceNo = BitConverter.GetBytes(i);
                sequenceNo.CopyTo(writeFrame, 0);

                uint writeChecksum = Fletcher32.Checksum(writeFrame, 0, writeFrame.Length);
                checksums.AddOrUpdate(i, writeChecksum, (seq, checkSum) => { Debug.Fail("Attempt to update checksum."); return 0; });

                network.WriteFrame(i % 2 == 0, writeFrame);

                int delayMilliseconds = rnd.Next(0, 1000);
                await Task.Delay(delayMilliseconds);

                byte[] readFrame;
                network.ReadFrame(i % 2 == 1, out readFrame);

                uint readChecksum = Fletcher32.Checksum(readFrame, 0, readFrame.Length);

                int idx = BitConverter.ToInt32(readFrame, 0);
                Assert.Equal(checksums[idx], readChecksum);
            });
        }
    }
}
