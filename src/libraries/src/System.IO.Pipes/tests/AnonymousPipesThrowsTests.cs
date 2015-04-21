// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes.Tests
{
    public class AnonymousPipesThrowsTests
    {
        // Server parameter validation tests
        [Fact]
        public static void ServerBadPipeDirectionThrows()
        {
            Assert.Throws<NotSupportedException>(delegate
            {
                AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.InOut, System.IO.HandleInheritability.None);
            });
        }

        [Fact]
        public static void ServerBadHandleInheritabilityThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out, (System.IO.HandleInheritability)999);
            });
        }

        [Fact]
        public static void ServerNullServerPipeHandleThrows()
        {
            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<ArgumentNullException>(delegate
                {
                    AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out, null, dummyserver.ClientSafePipeHandle);
                });
            }
        }

        [Fact]
        public static void ServerNullClientPipeHandleThrows()
        {
            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<ArgumentNullException>(delegate
                {
                    AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, null);
                });
            }
        }

        [Fact]
        public static void ServerServerPipeHandleThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle((System.IntPtr)0, true);
            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<ArgumentException>(delegate
                {
                    AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out, pipeHandle, dummyserver.ClientSafePipeHandle);
                });
            }
        }

        [Fact]
        public static void ServerClientPipeHandleThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle((System.IntPtr)0, true);
            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<ArgumentException>(delegate
                {
                    AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, pipeHandle);
                });
            }
        }

        [Fact]
        public static void ServerPipeDirectionThrows()
        {
            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<NotSupportedException>(delegate
                {
                    AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.InOut, dummyserver.SafePipeHandle, null);
                });
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

        [Fact]
        public static void ServerWriteBufferNullThrows()
        {
            // force different constructor path
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None))
            {
                Assert.Throws<ArgumentNullException>(() => server.Write(null, 0, 1));
            }
        }

        [Fact]
        public async static void ServerWriteAsyncBufferNullThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => server.WriteAsync(null, 0, 1));
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
            }
        }

        [Fact]
        public async static void ServerWriteAsyncNegativeOffsetThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => server.WriteAsync(new byte[5], -1, 1));

                // array is checked first
                await Assert.ThrowsAsync<ArgumentNullException>(() => server.WriteAsync(null, -1, 1));
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
            }
        }

        [Fact]
        public async static void ServerWriteAsyncNegativeCountThrows()
        {

            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => server.WriteAsync(new byte[5], 0, -1));

                // offset is checked before count
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => server.WriteAsync(new byte[1], -1, -1));

                // array is checked first
                await Assert.ThrowsAsync<ArgumentNullException>(() => server.WriteAsync(null, -1, -1));
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
            }
        }

        [Fact]
        public async static void ServerWriteAsyncArrayOutOfBoundsThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                // offset out of bounds
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.WriteAsync(new byte[1], 1, 1));

                // offset out of bounds for 0 count read
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.WriteAsync(new byte[1], 2, 0));

                // offset out of bounds even for 0 length buffer
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.WriteAsync(new byte[0], 1, 0));

                // combination offset and count out of bounds
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.WriteAsync(new byte[2], 1, 2));

                // edges
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.WriteAsync(new byte[0], int.MaxValue, 0));
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.WriteAsync(new byte[0], int.MaxValue, int.MaxValue));

                await Assert.ThrowsAsync<ArgumentException>(() => server.WriteAsync(new byte[5], 3, 4));
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
            }
        }

        [Fact]
        public async static void ServerReadOnlyAsyncThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => server.WriteAsync(new byte[5], 0, 5));

            }
        }

        [Fact]
        public static void ServerReadBufferNullThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                Assert.Throws<ArgumentNullException>(() => server.Read(null, 0, 1));
            }
        }

        [Fact]
        public async static void ServerReadAsyncBufferNullThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => server.ReadAsync(null, 0, 1));
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
            }
        }

        [Fact]
        public async static void ServerReadAsyncNegativeOffsetThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => server.ReadAsync(new byte[5], -1, 1));

                // array is checked first
                await Assert.ThrowsAsync<ArgumentNullException>(() => server.ReadAsync(null, -1, 1));
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
            }
        }

        [Fact]
        public async static void ServerReadAsyncNegativeCountThrows()
        {

            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                await Assert.ThrowsAsync<System.ArgumentOutOfRangeException>(() => server.ReadAsync(new byte[5], 0, -1));

                // offset is checked before count
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => server.ReadAsync(new byte[1], -1, -1));

                // array is checked first
                await Assert.ThrowsAsync<ArgumentNullException>(() => server.ReadAsync(null, -1, -1));
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
            }
        }

        [Fact]
        public async static void ServerReadAsyncArrayOutOfBoundsThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                // offset out of bounds
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.ReadAsync(new byte[1], 1, 1));

                // offset out of bounds for 0 count read
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.ReadAsync(new byte[1], 2, 0));

                // offset out of bounds even for 0 length buffer
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.ReadAsync(new byte[0], 1, 0));

                // combination offset and count out of bounds
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.ReadAsync(new byte[2], 1, 2));

                // edges
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.ReadAsync(new byte[0], int.MaxValue, 0));
                await Assert.ThrowsAsync<ArgumentException>(null, () => server.ReadAsync(new byte[0], int.MaxValue, int.MaxValue));

                await Assert.ThrowsAsync<ArgumentException>(() => server.ReadAsync(new byte[5], 3, 4));
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
            }
        }

        [Fact]
        public async static void ServerWriteOnlyAsyncThrows()
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => server.ReadAsync(new byte[5], 0, 5));

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
            Assert.Throws<ArgumentNullException>(delegate
            {
                AnonymousPipeClientStream client = new AnonymousPipeClientStream(null);
            });
        }

        [Fact]
        public static void ClientPipeDirectionThrows()
        {
            Assert.Throws<NotSupportedException>(delegate
            {
                AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.InOut, "123");
            });
        }

        [Fact]
        public static void ClientPipeHandleStringAsNotNumericThrows()
        {
            Assert.Throws<ArgumentException>(delegate
            {
                AnonymousPipeClientStream client = new AnonymousPipeClientStream("abc");
            });
        }

        [Fact]
        public static void ClientPipeHandleStringAsNotValidThrows()
        {
            Assert.Throws<ArgumentException>(delegate
            {
                AnonymousPipeClientStream client = new AnonymousPipeClientStream("-1");
            });
        }

        [Fact]
        public static void ClientPipeHandleThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle((System.IntPtr)0, true);

            Assert.Throws<NotSupportedException>(delegate
            {
                AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.InOut, pipeHandle);
            });
        }

        [Fact]
        public static void ClientPipeHandleAsNullThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle((System.IntPtr)0, true);

            Assert.Throws<ArgumentNullException>(delegate
            {
                AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, (SafePipeHandle)null);
            });
        }

        [Fact]
        public static void ClientBadPipeHandleAsInvalidThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle((System.IntPtr)0, true);

            Assert.Throws<ArgumentException>(delegate
            {
                AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, pipeHandle);
            });
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
