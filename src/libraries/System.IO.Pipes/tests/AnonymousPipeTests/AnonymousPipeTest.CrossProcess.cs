// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class AnonymousPipeTest_CrossProcess
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void PingPong()
        {
            // Create two anonymous pipes, one for each direction of communication.
            // Then spawn another process to communicate with.
            using (var outbound = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            using (var inbound = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
            using (var remote = RemoteExecutor.Invoke(new Action<string, string>(ChildFunc), outbound.GetClientHandleAsString(), inbound.GetClientHandleAsString()))
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

            void ChildFunc(string inHandle, string outHandle)
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
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ServerClosesPipe_ClientReceivesEof()
        {
            using (var pipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            using (var remote = RemoteExecutor.Invoke(new Action<string>(ChildFunc), pipe.GetClientHandleAsString()))
            {
                pipe.DisposeLocalCopyOfClientHandle();
                pipe.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

                pipe.Dispose();

                Assert.True(remote.Process.WaitForExit(30_000));
            }

            void ChildFunc(string clientHandle)
            {
                using (var pipe = new AnonymousPipeClientStream(PipeDirection.In, clientHandle))
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        Assert.Equal(i, pipe.ReadByte());
                    }
                    Assert.Equal(-1, pipe.ReadByte());
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ClientClosesPipe_ServerReceivesEof()
        {
            using (var pipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
            using (var remote = RemoteExecutor.Invoke(new Action<string>(ChildFunc), pipe.GetClientHandleAsString(), new RemoteInvokeOptions { CheckExitCode = false }))
            {
                pipe.DisposeLocalCopyOfClientHandle();

                for (int i = 1; i <= 5; i++)
                {
                    Assert.Equal(i, pipe.ReadByte());
                }
                Assert.Equal(-1, pipe.ReadByte());

                remote.Process.Kill();
            }

            void ChildFunc(string clientHandle)
            {
                using (var pipe = new AnonymousPipeClientStream(PipeDirection.Out, clientHandle))
                {
                    pipe.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
                }
                Thread.CurrentThread.Join();
            }
        }
    }
}
