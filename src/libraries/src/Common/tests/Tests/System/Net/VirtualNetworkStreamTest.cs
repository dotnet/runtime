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
    public class VirtualNetworkStreamTest
    {
        [Fact]
        public void VirtualNetworkStream_SingleThreadIntegrityTest_Ok()
        {
            var rnd = new Random();
            var network = new VirtualNetwork();

            using (var client = new VirtualNetworkStream(network, isServer: false))
            using (var server = new VirtualNetworkStream(network, isServer: true))
            {
                for (int i = 0; i < 100000; i++)
                {
                    int bufferSize;
                    byte[] writeFrame;
                    
                    bufferSize = rnd.Next(1, 2048);
                    writeFrame = new byte[bufferSize];
                    rnd.NextBytes(writeFrame);
                    
                    uint writeChecksum = Fletcher32.Checksum(writeFrame, 0, writeFrame.Length);
                    client.Write(writeFrame, 0, writeFrame.Length);

                    var readFrame = new byte[writeFrame.Length];
                    server.Read(readFrame, 0, readFrame.Length);
                    uint readChecksum = Fletcher32.Checksum(readFrame, 0, readFrame.Length);

                    Assert.Equal(writeChecksum, readChecksum);
                }
            }
        }

        [Fact]
        public void VirtualNetworkStream_WriteMoreThanRead_Ok()
        {
            var network = new VirtualNetwork();

            using (var client = new VirtualNetworkStream(network, isServer: false))
            using (var server = new VirtualNetworkStream(network, isServer: true))
            {
                var writeFrame = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
                int expectedReceivedBytes = writeFrame.Length / 2;

                client.Write(writeFrame, 0, expectedReceivedBytes);

                var readFrame = new byte [expectedReceivedBytes];
                server.Read(readFrame, 0, expectedReceivedBytes);

                var expectedFrame = new byte[expectedReceivedBytes];
                Array.Copy(writeFrame, expectedFrame, expectedReceivedBytes);

                Assert.Equal(expectedFrame, readFrame);
            }
        }

        [Fact]
        public void VirtualNetworkStream_ReadMoreThanWrite_Ok()
        {
            var network = new VirtualNetwork();

            using (var client = new VirtualNetworkStream(network, isServer: false))
            using (var server = new VirtualNetworkStream(network, isServer: true))
            {
                var writeFrame = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                int frame1Len = writeFrame.Length / 2;

                client.Write(writeFrame, 0, frame1Len);
                client.Write(writeFrame, frame1Len, writeFrame.Length - frame1Len);

                int expectedReceivedBytes = writeFrame.Length;
                var readFrame = new byte[expectedReceivedBytes];

                int bytesRead = 0;

                do
                {
                    bytesRead += server.Read(readFrame, bytesRead, expectedReceivedBytes - bytesRead);
                }
                while (bytesRead < expectedReceivedBytes); 

                Assert.Equal(writeFrame, readFrame);
            }
        }

        [Fact]
        public void VirtualNetworkStream_MultiThreadIntegrityTest_Ok()
        {
            int maxFrameSize = 2048;
            Assert.True(maxFrameSize > sizeof(int) + 1);

            var rnd = new Random();
            var rndLock = new object();
            
            var network = new VirtualNetwork();
            var checksumAndLengths = new ConcurrentDictionary<int, Tuple<uint, int>>();

            object readLock = new object();

            using (var client = new VirtualNetworkStream(network, isServer: false))
            using (var server = new VirtualNetworkStream(network, isServer: true))
            {
                Parallel.For(0, 100, (int i) =>
                {
                    int bufferSize;
                    int delayMilliseconds;
                    byte[] writeFrame;
                    
                    lock (rndLock)
                    {
                        bufferSize = rnd.Next(sizeof(int) + 1, maxFrameSize);
                        delayMilliseconds = rnd.Next(0, 10);
                        
                        writeFrame = new byte[bufferSize];
                        rnd.NextBytes(writeFrame);
                    }


                    // First 4 bytes represent the sequence number.
                    byte[] sequenceNo = BitConverter.GetBytes(i);
                    sequenceNo.CopyTo(writeFrame, 0);

                    uint writeChecksum = Fletcher32.Checksum(writeFrame, 0, writeFrame.Length);
                    var writeFrameInfo = new Tuple<uint, int>(writeChecksum, writeFrame.Length);

                    checksumAndLengths.AddOrUpdate(i, writeFrameInfo, (seq, checkSum) => { Debug.Fail("Attempt to update checksum."); return new Tuple<uint, int>(0, 0); });

                    client.WriteAsync(writeFrame, 0, writeFrame.Length).GetAwaiter().GetResult();
                    Task.Delay(delayMilliseconds).GetAwaiter().GetResult();

                    // First read the index to know how much data to read from this frame.
                    var readFrame = new byte[maxFrameSize];
                    int readLen = server.ReadAsync(readFrame, 0, maxFrameSize).GetAwaiter().GetResult();

                    int idx = BitConverter.ToInt32(readFrame, 0);
                    Tuple<uint, int> expectedFrameInfo = checksumAndLengths[idx];

                    Assert.Equal(expectedFrameInfo.Item2, readLen);
                    uint readChecksum = Fletcher32.Checksum(readFrame, 0, readLen);
                    Assert.Equal(expectedFrameInfo.Item1, readChecksum);
                });
            }
        }
    }
}
