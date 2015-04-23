// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes.Tests
{
    public class NamedPipesThrowsTests
    {
        // Server Parameter Checking throws
        [Fact]
        public static void ServerNullPipeNameThrows()
        {
            Assert.Throws<ArgumentNullException>(delegate
            {
                NamedPipeServerStream server = new NamedPipeServerStream(null);
            });
        }

        [Fact]
        public static void ServerZeroLengthPipeNameThrows()
        {
            Assert.Throws<ArgumentException>(delegate
            {
                NamedPipeServerStream server = new NamedPipeServerStream("", PipeDirection.Out, 1, PipeTransmissionMode.Byte);
            });
        }

        [Fact]
        public static void ServerPipeOptionsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                NamedPipeServerStream server = new NamedPipeServerStream("temp1", PipeDirection.Out, 1, PipeTransmissionMode.Byte, (PipeOptions)255);
            });
        }

        [Fact]
        public static void ServerInBufferSizeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                NamedPipeServerStream server = new NamedPipeServerStream("temp2", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, -1, 0);
            });
        }

        [Fact]
        public static void ServerServerInstancesThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                NamedPipeServerStream server = new NamedPipeServerStream("temp3", PipeDirection.Out, 0, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0);
            });
        }

        [Fact]
        public static void ServerNullPipeHandleThrows()
        {
            Assert.Throws<ArgumentNullException>(delegate
            {
                NamedPipeServerStream server = new NamedPipeServerStream(PipeDirection.InOut, false, true, null);
            });
        }

        [Fact]
        public static void ClientInvalidPipeHandleThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            Assert.Throws<ArgumentException>(delegate
            {
                NamedPipeServerStream server = new NamedPipeServerStream(PipeDirection.InOut, false, true, pipeHandle);
            });
        }

        // Client Parameter Checking throws
        [Fact]
        public static void ClientNullPipeNameParameterThrows()
        {
            Assert.Throws<ArgumentNullException>(delegate
            {
                NamedPipeClientStream client = new NamedPipeClientStream(null);
            });
        }

        [Fact]
        public static void ClientZeroLengthPipeNameThrows()
        {
            Assert.Throws<ArgumentException>(delegate
            {
                NamedPipeClientStream client = new NamedPipeClientStream("");
            });
        }

        [Fact]
        public static void ClientNullServerNameParameterThrows()
        {
            Assert.Throws<ArgumentNullException>(delegate
            {
                NamedPipeClientStream client = new NamedPipeClientStream(null, "client1");
            });
        }

        [Fact]
        public static void ClientZeroLengthServerNameThrows()
        {
            Assert.Throws<ArgumentException>(delegate
            {
                NamedPipeClientStream client = new NamedPipeClientStream("", "client1");
            });
        }

        [Fact]
        public static void ClientPipeOptionsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                NamedPipeClientStream client = new NamedPipeClientStream(".", "client1", PipeDirection.In, (PipeOptions)255);
            });
        }

        [Fact]
        public static void ClientImpersonationLevelThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                NamedPipeClientStream client = new NamedPipeClientStream(".", "client1", PipeDirection.In, PipeOptions.None, (System.Security.Principal.TokenImpersonationLevel)999);
            });
        }

        [Fact]
        public static void ClientConnectTimeoutThrows()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream("client1"))
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() => client.Connect(-111));
            }
        }

        [Fact]
        public async static void ClientConnectAsyncBadTimeoutThrows()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream("client1"))
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.ConnectAsync(-111));
            }
        }

        [Fact]
        public static void ClientNullServerHandleThrows()
        {
            Assert.Throws<ArgumentNullException>(delegate
            {
                NamedPipeClientStream client = new NamedPipeClientStream(PipeDirection.InOut, false, true, null);
            });
        }

        [Fact]
        public static void ClientInvalidServerHandleThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            Assert.Throws<ArgumentException>(delegate
            {
                NamedPipeClientStream client = new NamedPipeClientStream(PipeDirection.InOut, false, true, pipeHandle);
            });
        }
    }
}
