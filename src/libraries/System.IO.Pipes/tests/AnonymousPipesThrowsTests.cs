// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class AnonymousPipesThrowsTests : BaseCommonTests
    {
        // Server parameter validation tests
        [Fact]
        public static void ServerBadPipeDirectionThrows()
        {
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeServerStream(PipeDirection.InOut));
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeServerStream(PipeDirection.InOut, HandleInheritability.None));
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeServerStream(PipeDirection.InOut, HandleInheritability.None, 500));

            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<NotSupportedException>(() => new AnonymousPipeServerStream(PipeDirection.InOut, dummyserver.SafePipeHandle, null));
            }
        }

        [Fact]
        public static void ServerBadInheritabilityThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("inheritability", () =>new AnonymousPipeServerStream(PipeDirection.Out, (System.IO.HandleInheritability)999));
            Assert.Throws<ArgumentOutOfRangeException>("inheritability", () => new AnonymousPipeServerStream(PipeDirection.Out, (System.IO.HandleInheritability)999, 500));
            Assert.Throws<ArgumentOutOfRangeException>("inheritability", () => new AnonymousPipeServerStream(PipeDirection.In, (System.IO.HandleInheritability)999));
            Assert.Throws<ArgumentOutOfRangeException>("inheritability", () => new AnonymousPipeServerStream(PipeDirection.In, (System.IO.HandleInheritability)999, 500));
        }

        [Fact]
        public static void ServerBadBufferThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () => new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, -500));
            Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () => new AnonymousPipeServerStream(PipeDirection.InOut, HandleInheritability.None, -500));
            Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () => new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None, -500));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new AnonymousPipeServerStream((PipeDirection)123, HandleInheritability.None, 500));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new AnonymousPipeServerStream((PipeDirection)123, HandleInheritability.None, -500));
        }

        [Fact]
        public static void ServerBadPipeHandleThrows()
        {
            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<ArgumentNullException>("serverSafePipeHandle", () =>new AnonymousPipeServerStream(PipeDirection.Out, null, dummyserver.ClientSafePipeHandle));

                Assert.Throws<ArgumentNullException>("clientSafePipeHandle", () =>new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, null));

                SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
                Assert.Throws<ArgumentException>("serverSafePipeHandle", () =>new AnonymousPipeServerStream(PipeDirection.Out, pipeHandle, dummyserver.ClientSafePipeHandle));

                Assert.Throws<ArgumentException>("clientSafePipeHandle", () =>new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, pipeHandle));
            }
        }

        [Fact]
        public static void ServerReadModeThrows()
        {
            // force different constructor path
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => server.ReadMode = (PipeTransmissionMode)999);

                Assert.Throws<NotSupportedException>(() => server.ReadMode = PipeTransmissionMode.Message);
            }
        }

        // the following set of tests exercise the error checking for both anonymous and named classes
        [Fact]
        public static void ServerWriteBufferNullThrows()
        {
            // force different constructor path
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None))
            {
                ConnectedPipeWriteBufferNullThrows(server);
            }
        }

        [Fact]
        public static void ServerWriteNegativeOffsetThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                ConnectedPipeWriteNegativeOffsetThrows(server);
            }
        }

        [Fact]
        public static void ServerWriteNegativeCountThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                ConnectedPipeWriteNegativeCountThrows(server);
            }
        }

        [Fact]
        public static void ServerWriteArrayOutOfBoundsThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                ConnectedPipeWriteArrayOutOfBoundsThrows(server);
            }
        }

        [Fact]
        public static void ServerReadOnlyThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                ConnectedPipeReadOnlyThrows(server);
            }
        }

        [Fact]
        public static void ServerReadBufferNullThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                ConnectedPipeReadBufferNullThrows(server);
            }
        }

        [Fact]
        public static void ServerReadNegativeOffsetThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                ConnectedPipeReadNegativeOffsetThrows(server);
            }
        }

        [Fact]
        public static void ServerReadNegativeCountThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                ConnectedPipeReadNegativeCountThrows(server);
            }
        }

        [Fact]
        public static void ServerReadArrayOutOfBoundsThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                ConnectedPipeReadArrayOutOfBoundsThrows(server);
            }
        }

        [Fact]
        public static void ServerWriteOnlyThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                ConnectedPipeWriteOnlyThrows(server);
            }
        }

        [Fact]
        public static void ServerUnsupportedOperationThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                ConnectedPipeUnsupportedOperationThrows(server);
            }
        }

        [Fact]
        public static void ServerReadOnlyDisconnectedPipeThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                server.Dispose();

                OtherSidePipeDisconnectWriteThrows(client);
            }
        }

        [Fact]
        public static void ServerWriteOnlyDisconnectedPipeThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                server.Dispose();

                OtherSidePipeDisconnectVerifyRead(client);
            }
        }

        [Fact]
        public static void ServerReadOnlyCancelReadTokenThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {

                ConnectedPipeCancelReadTokenThrows(server);
            }
        }

        // Client parameter validation tests
        [Fact]
        public static void ClientPipeHandleStringAsNullThrows()
        {
            Assert.Throws<ArgumentNullException>("pipeHandleAsString", () => new AnonymousPipeClientStream((string)null));
            Assert.Throws<ArgumentNullException>("pipeHandleAsString", () => new AnonymousPipeClientStream(PipeDirection.Out, (string)null));
        }
        
        [Fact]
        public static void ClientPipeHandleStringAsNotNumericThrows()
        {
            Assert.Throws<ArgumentException>("pipeHandleAsString", () => new AnonymousPipeClientStream("abc"));
            Assert.Throws<ArgumentException>("pipeHandleAsString", () => new AnonymousPipeClientStream(PipeDirection.Out, "abc"));
        }

        [Fact]
        public static void ClientPipeHandleStringAsNotValidThrows()
        {
            Assert.Throws<ArgumentException>("pipeHandleAsString", () => new AnonymousPipeClientStream("-1"));
            Assert.Throws<ArgumentException>("pipeHandleAsString", () => new AnonymousPipeClientStream(PipeDirection.Out, "-1"));
        }

        [Fact]
        public static void ClientPipeHandleAsNullThrows()
        {
            Assert.Throws<ArgumentNullException>("safePipeHandle", () => new AnonymousPipeClientStream(PipeDirection.In, (SafePipeHandle)null));
        }

        [Fact]
        public static void ClientBadPipeHandleAsInvalidThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);

            Assert.Throws<ArgumentException>("safePipeHandle", () => new AnonymousPipeClientStream(PipeDirection.In, pipeHandle));
        }

        [Fact]
        public static void ClientPipeDirectionThrows()
        {
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeClientStream(PipeDirection.InOut, "123"));

            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeClientStream(PipeDirection.InOut, pipeHandle));
        }

        [Fact]
        public static void ClientReadModeThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => client.ReadMode = (PipeTransmissionMode)999);

                Assert.Throws<NotSupportedException>(() => client.ReadMode = PipeTransmissionMode.Message);
            }
        }

        [Fact]
        public static void ClientWriteBufferNullThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                ConnectedPipeWriteBufferNullThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteNegativeOffsetThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                ConnectedPipeWriteNegativeOffsetThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteNegativeCountThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                ConnectedPipeWriteNegativeCountThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteArrayOutOfBoundsThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                ConnectedPipeWriteArrayOutOfBoundsThrows(client);
            }
        }

        [Fact]
        public static void ClientReadOnlyThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                ConnectedPipeReadOnlyThrows(client);
            }
        }

        [Fact]
        public static void ClientReadBufferNullThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                ConnectedPipeReadBufferNullThrows(client);
            }
        }

        [Fact]
        public static void ClientReadNegativeOffsetThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                ConnectedPipeReadNegativeOffsetThrows(client);
            }
        }

        [Fact]
        public static void ClientReadNegativeCountThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                ConnectedPipeReadNegativeCountThrows(client);
            }
        }

        [Fact]
        public static void ClientReadArrayOutOfBoundsThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                ConnectedPipeReadArrayOutOfBoundsThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteOnlyThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                ConnectedPipeWriteOnlyThrows(client);
            }
        }

        [Fact]
        public static void ClientUnsupportedOperationThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                ConnectedPipeUnsupportedOperationThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteOnlyDisconnectedPipeThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                client.Dispose();

                OtherSidePipeDisconnectVerifyRead(server);
            }
        }

        [Fact]
        public static void ClientReadOnlyDisconnectedPipeThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                client.Dispose();

                OtherSidePipeDisconnectWriteThrows(server);
            }
        }

        [Fact]
        public static void ClientReadOnlyCancelReadTokenThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {

                ConnectedPipeCancelReadTokenThrows(client);
            }
        }
    }
}
