// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class AnonymousPipeTest_CrossProcess : RemoteExecutorTestBase
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/21275", TargetFrameworkMonikers.Uap)]
        public void PingPong()
        {
            // Create two anonymous pipes, one for each direction of communication.
            // Then spawn another process to communicate with.
            using (var outbound = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            using (var inbound = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
            using (var remote = RemoteInvoke(PingPong_OtherProcess, outbound.GetClientHandleAsString(), inbound.GetClientHandleAsString()))
            {
                // Close our local copies of the handles now that we've passed them of to the other process
                outbound.DisposeLocalCopyOfClientHandle();
                inbound.DisposeLocalCopyOfClientHandle();

                // Ping-pong back and forth by writing then reading bytes one at a time.
                for (byte i = 0; i < 10; i++)
                {
                    outbound.WriteByte(i);
                    int received = inbound.ReadByte();
                    Assert.Equal(i, received);
                }
            }
        }

        private static int PingPong_OtherProcess(string inHandle, string outHandle)
        {
            // Create the clients associated with the supplied handles
            using (var inbound = new AnonymousPipeClientStream(PipeDirection.In, inHandle))
            using (var outbound = new AnonymousPipeClientStream(PipeDirection.Out, outHandle))
            {
                // Repeatedly read then write a byte from and to the server
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
