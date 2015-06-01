// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class AnonymousPipesThrowsTests
    {
        private static void NotReachable(object obj) { Assert.True(false, "This should not be reached."); }

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
            Assert.Throws<ArgumentOutOfRangeException>(() =>new AnonymousPipeServerStream(PipeDirection.Out, (System.IO.HandleInheritability)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnonymousPipeServerStream(PipeDirection.Out, (System.IO.HandleInheritability)999, 500));
        }

        [Fact]
        public static void ServerBadBufferThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, -500));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnonymousPipeServerStream(PipeDirection.InOut, HandleInheritability.None, -500));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnonymousPipeServerStream((PipeDirection)123, HandleInheritability.None, 500));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnonymousPipeServerStream((PipeDirection)123, HandleInheritability.None, -500));
        }

        [Fact]
        public static void ServerBadPipeHandleThrows()
        {
            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<ArgumentNullException>(() =>new AnonymousPipeServerStream(PipeDirection.Out, null, dummyserver.ClientSafePipeHandle));

                Assert.Throws<ArgumentNullException>(() =>new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, null));

                SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
                Assert.Throws<ArgumentException>(() =>new AnonymousPipeServerStream(PipeDirection.Out, pipeHandle, dummyserver.ClientSafePipeHandle));

                Assert.Throws<ArgumentException>(() =>new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, pipeHandle));
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
                Assert.Throws<ArgumentNullException>(() => server.Write(null, 0, 1));
                Assert.Throws<ArgumentNullException>(() => NotReachable(server.WriteAsync(null, 0, 1)));
            }
        }

        [Fact]
        public static void ServerWriteNegativeOffsetThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Write(new byte[5], -1, 1));

                // array is checked first
                Assert.Throws<ArgumentNullException>(() => server.Write(null, -1, 1));
                Assert.Throws<ArgumentNullException>(() => NotReachable(server.WriteAsync(null, -1, 1)));
            }
        }

        [Fact]
        public static void ServerWriteNegativeCountThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Write(new byte[5], 0, -1));

                // offset is checked before count
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Write(new byte[1], -1, -1));

                // array is checked first
                Assert.Throws<ArgumentNullException>(() => server.Write(null, -1, -1));

                Assert.Throws<ArgumentOutOfRangeException>(() => NotReachable(server.WriteAsync(new byte[5], 0, -1)));

                // offset is checked before count
                Assert.Throws<ArgumentOutOfRangeException>(() => NotReachable(server.WriteAsync(new byte[1], -1, -1)));

                // array is checked first
                Assert.Throws<ArgumentNullException>(() => NotReachable(server.WriteAsync(null, -1, -1)));
            }
        }

        [Fact]
        public static void ServerWriteArrayOutOfBoundsThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () => server.Write(new byte[1], 1, 1));

                // offset out of bounds for 0 count read
                Assert.Throws<ArgumentException>(null, () => server.Write(new byte[1], 2, 0));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => server.Write(new byte[0], 1, 0));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => server.Write(new byte[2], 1, 2));

                // edges
                Assert.Throws<ArgumentException>(null, () => server.Write(new byte[0], int.MaxValue, 0));
                Assert.Throws<ArgumentException>(null, () => server.Write(new byte[0], int.MaxValue, int.MaxValue));

                Assert.Throws<ArgumentException>(() => server.Write(new byte[5], 3, 4));

                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.WriteAsync(new byte[1], 1, 1)));

                // offset out of bounds for 0 count read
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.WriteAsync(new byte[1], 2, 0)));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.WriteAsync(new byte[0], 1, 0)));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.WriteAsync(new byte[2], 1, 2)));

                // edges
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.WriteAsync(new byte[0], int.MaxValue, 0)));
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.WriteAsync(new byte[0], int.MaxValue, int.MaxValue)));

                Assert.Throws<ArgumentException>(() => NotReachable(server.WriteAsync(new byte[5], 3, 4)));
            }
        }

        [Fact]
        public static void ServerReadOnlyThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                Assert.Throws<NotSupportedException>(() => server.Write(new byte[5], 0, 5));

                Assert.Throws<NotSupportedException>(() => server.WriteByte(123));

                Assert.Throws<NotSupportedException>(() => server.Flush());

                Assert.Throws<NotSupportedException>(() => server.OutBufferSize);

                Assert.Throws<NotSupportedException>(() => server.WaitForPipeDrain());

                Assert.Throws<NotSupportedException>(() => NotReachable(server.WriteAsync(new byte[5], 0, 5)));
            }
        }

        [Fact]
        public static void ServerReadBufferNullThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                Assert.Throws<ArgumentNullException>(() => server.Read(null, 0, 1));

                Assert.Throws<ArgumentNullException>(() => NotReachable(server.ReadAsync(null, 0, 1)));
            }
        }

        [Fact]
        public static void ServerReadNegativeOffsetThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Read(new byte[5], -1, 1));

                // array is checked first
                Assert.Throws<ArgumentNullException>(() => server.Read(null, -1, 1));

                Assert.Throws<ArgumentOutOfRangeException>(() => NotReachable(server.ReadAsync(new byte[5], -1, 1)));

                // array is checked first
                Assert.Throws<ArgumentNullException>(() => NotReachable(server.ReadAsync(null, -1, 1)));
            }
        }

        [Fact]
        public static void ServerReadNegativeCountThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() => server.Read(new byte[5], 0, -1));

                // offset is checked before count
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Read(new byte[1], -1, -1));

                // array is checked first
                Assert.Throws<ArgumentNullException>(() => server.Read(null, -1, -1));

                Assert.Throws<System.ArgumentOutOfRangeException>(() => NotReachable(server.ReadAsync(new byte[5], 0, -1)));

                // offset is checked before count
                Assert.Throws<ArgumentOutOfRangeException>(() => NotReachable(server.ReadAsync(new byte[1], -1, -1)));

                // array is checked first
                Assert.Throws<ArgumentNullException>(() => NotReachable(server.ReadAsync(null, -1, -1)));
            }
        }

        [Fact]
        public static void ServerReadArrayOutOfBoundsThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () => server.Read(new byte[1], 1, 1));

                // offset out of bounds for 0 count read
                Assert.Throws<ArgumentException>(null, () => server.Read(new byte[1], 2, 0));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => server.Read(new byte[0], 1, 0));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => server.Read(new byte[2], 1, 2));

                // edges
                Assert.Throws<ArgumentException>(null, () => server.Read(new byte[0], int.MaxValue, 0));
                Assert.Throws<ArgumentException>(null, () => server.Read(new byte[0], int.MaxValue, int.MaxValue));

                Assert.Throws<ArgumentException>(() => server.Read(new byte[5], 3, 4));

                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.ReadAsync(new byte[1], 1, 1)));

                // offset out of bounds for 0 count read
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.ReadAsync(new byte[1], 2, 0)));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.ReadAsync(new byte[0], 1, 0)));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.ReadAsync(new byte[2], 1, 2)));

                // edges
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.ReadAsync(new byte[0], int.MaxValue, 0)));
                Assert.Throws<ArgumentException>(null, () => NotReachable(server.ReadAsync(new byte[0], int.MaxValue, int.MaxValue)));

                Assert.Throws<ArgumentException>(() => NotReachable(server.ReadAsync(new byte[5], 3, 4)));
            }
        }

        [Fact]
        public static void ServerWriteOnlyThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<NotSupportedException>(() => server.Read(new byte[5], 0, 5));

                Assert.Throws<NotSupportedException>(() => server.ReadByte());

                Assert.Throws<NotSupportedException>(() => server.InBufferSize);

                Assert.Throws<NotSupportedException>(() => NotReachable(server.ReadAsync(new byte[5], 0, 5)));
            }
        }

        [Fact]
        public static void ServerUnsupportedOperationThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<NotSupportedException>(() => server.Length);

                Assert.Throws<NotSupportedException>(() => server.SetLength(10L));

                Assert.Throws<NotSupportedException>(() => server.Position);

                Assert.Throws<NotSupportedException>(() => server.Position = 10L);

                Assert.Throws<NotSupportedException>(() => server.Seek(10L, System.IO.SeekOrigin.Begin));
            }
        }

        // Client parameter validation tests
        [Fact]
        public static void ClientPipeHandleStringAsNullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new AnonymousPipeClientStream((string)null));
            Assert.Throws<ArgumentNullException>(() => new AnonymousPipeClientStream(PipeDirection.Out, (string)null));
        }
        
        [Fact]
        public static void ClientPipeHandleStringAsNotNumericThrows()
        {
            Assert.Throws<ArgumentException>(() => new AnonymousPipeClientStream("abc"));
            Assert.Throws<ArgumentException>(() => new AnonymousPipeClientStream(PipeDirection.Out, "abc"));
        }

        [Fact]
        public static void ClientPipeHandleStringAsNotValidThrows()
        {
            Assert.Throws<ArgumentException>(() => new AnonymousPipeClientStream("-1"));
            Assert.Throws<ArgumentException>(() => new AnonymousPipeClientStream(PipeDirection.Out, "-1"));
        }

        [Fact]
        public static void ClientPipeHandleAsNullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new AnonymousPipeClientStream(PipeDirection.In, (SafePipeHandle)null));
        }

        [Fact]
        public static void ClientBadPipeHandleAsInvalidThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);

            Assert.Throws<ArgumentException>(() => new AnonymousPipeClientStream(PipeDirection.In, pipeHandle));
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
            {
                using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => client.ReadMode = (PipeTransmissionMode)999);

                    Assert.Throws<NotSupportedException>(() => client.ReadMode = PipeTransmissionMode.Message);
                }
            }
        }
    }
}
