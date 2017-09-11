// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
    public sealed class NamedPipeTest_CrossProcess : RemoteExecutorTestBase
    {
        [Fact]
        public void PingPong_Sync()
        {
            // Create names for two pipes
            string outName = GetUniquePipeName();
            string inName = GetUniquePipeName();

            // Create the two named pipes, one for each direction, then create
            // another process with which to communicate
            using (var outbound = new NamedPipeServerStream(outName, PipeDirection.Out))
            using (var inbound = new NamedPipeClientStream(".", inName, PipeDirection.In))
            using (RemoteInvoke(PingPong_OtherProcess, outName, inName))
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

        [Fact]
        public async Task PingPong_Async()
        {
            // Create names for two pipes
            string outName = GetUniquePipeName();
            string inName = GetUniquePipeName();

            // Create the two named pipes, one for each direction, then create
            // another process with which to communicate
            using (var outbound = new NamedPipeServerStream(outName, PipeDirection.Out))
            using (var inbound = new NamedPipeClientStream(".", inName, PipeDirection.In))
            using (RemoteInvoke(PingPong_OtherProcess, outName, inName))
            {
                // Wait for both pipes to be connected
                await Task.WhenAll(outbound.WaitForConnectionAsync(), inbound.ConnectAsync());

                // Repeatedly write then read a byte to and from the other process
                var data = new byte[1];
                for (byte i = 0; i < 10; i++)
                {
                    data[0] = i;
                    await outbound.WriteAsync(data, 0, data.Length);
                    data[0] = 0;
                    int received = await inbound.ReadAsync(data, 0, data.Length);
                    Assert.Equal(1, received);
                    Assert.Equal(i, data[0]);
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

        private static string GetUniquePipeName()
        {
            if (PlatformDetection.IsInAppContainer)
            {
                return @"LOCAL\" + Path.GetRandomFileName();
            }
            return Path.GetRandomFileName();
        }

    }
}
