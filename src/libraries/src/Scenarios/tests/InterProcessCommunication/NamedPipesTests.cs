// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

namespace InterProcessCommunication.Tests
{
    public class NamedPipesTests : RemoteExecutorTestBase
    {
        [Fact]
        public void PingPong()
        {
            // Create names for two pipes
            string outName = Guid.NewGuid().ToString("N");
            string inName = Guid.NewGuid().ToString("N");

            // Create the two named pipes, one for each direction, then create
            // another process with which to communicate
            using (var outbound = new NamedPipeServerStream(outName, PipeDirection.Out))
            using (var inbound = new NamedPipeClientStream(".", inName, PipeDirection.In))
            using (var remote = RemoteInvoke(PingPong_OtherProcess, outName, inName))
            {
                // Wait for both pipes to be connected
                Task.WaitAll(outbound.WaitForConnectionAsync(), inbound.ConnectAsync());

                // Repeatedly write then read a byte to and from the other process
                for (byte i = 0; i < 10; i++)
                {
                    outbound.WriteByte(i);
                    int received = inbound.ReadByte();
                    Assert.Equal(i, received);
                }
            }
        }

        private static int PingPong_OtherProcess(string inName, string outName)
        {
            // Create pipes with the supplied names
            using (var inbound = new NamedPipeClientStream(".", inName, PipeDirection.In))
            using (var outbound = new NamedPipeServerStream(outName, PipeDirection.Out))
            {
                // Wait for the connections to be established
                Task.WaitAll(inbound.ConnectAsync(), outbound.WaitForConnectionAsync());

                // Repeatedly read then write bytes from and to the other process
                for (int i = 0; i < 10; i++)
                {
                    int b = inbound.ReadByte();
                    outbound.WriteByte((byte)b);
                }
            }
            return SuccessExitCode;
        }

    }
}
