// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class SocketAsyncEventArgsTest
    {
        [Fact]
        public void SetBuffer_MemoryBuffer_Roundtrips()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                Memory<byte> memory = new byte[42];
                saea.SetBuffer(memory);
                Assert.True(memory.Equals(saea.MemoryBuffer));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(memory.Length, saea.Count);
                Assert.Null(saea.Buffer);
            }
        }

        [Fact]
        public void SetBufferMemory_ThenSetBufferIntInt_Throws()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                Memory<byte> memory = new byte[42];
                saea.SetBuffer(memory);
                Assert.Throws<InvalidOperationException>(() => saea.SetBuffer(0, 42));
                Assert.Throws<InvalidOperationException>(() => saea.SetBuffer(0, 0));
                Assert.Throws<InvalidOperationException>(() => saea.SetBuffer(1, 2));
                Assert.True(memory.Equals(saea.MemoryBuffer));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(memory.Length, saea.Count);
            }
        }

        [Fact]
        public void SetBufferArrayIntInt_AvailableFromMemoryBuffer()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                byte[] array = new byte[42];

                saea.SetBuffer(array, 0, array.Length);
                Assert.True(MemoryMarshal.TryGetArray(saea.MemoryBuffer, out ArraySegment<byte> result));
                Assert.Same(array, result.Array);
                Assert.Same(saea.Buffer, array);
                Assert.Equal(0, result.Offset);
                Assert.Equal(array.Length, result.Count);

                saea.SetBuffer(1, 2);
                Assert.Same(saea.Buffer, array);
                Assert.Equal(1, saea.Offset);
                Assert.Equal(2, saea.Count);

                Assert.True(MemoryMarshal.TryGetArray(saea.MemoryBuffer, out result));
                Assert.Same(array, result.Array);
                Assert.Equal(0, result.Offset);
                Assert.Equal(array.Length, result.Count);
            }
        }

        [Fact]
        public void SetBufferMemory_Default_ResetsCountOffset()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                saea.SetBuffer(42, 84);
                Assert.Equal(0, saea.Offset);
                Assert.Equal(0, saea.Count);

                saea.SetBuffer(new byte[3], 1, 2);
                Assert.Equal(1, saea.Offset);
                Assert.Equal(2, saea.Count);

                saea.SetBuffer(Memory<byte>.Empty);
                Assert.Null(saea.Buffer);
                Assert.Equal(0, saea.Offset);
                Assert.Equal(0, saea.Count);
            }
        }

        [Fact]
        public void SetBufferListWhenMemoryBufferSet_Throws()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                var bufferList = new List<ArraySegment<byte>> { new ArraySegment<byte>(new byte[1]) };
                Memory<byte> buffer = new byte[1];

                saea.SetBuffer(buffer);
                AssertExtensions.Throws<ArgumentException>(null, () => saea.BufferList = bufferList);
                Assert.True(buffer.Equals(saea.MemoryBuffer));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(buffer.Length, saea.Count);
                Assert.Null(saea.BufferList);

                saea.SetBuffer(Memory<byte>.Empty);
                saea.BufferList = bufferList; // works fine when Buffer has been set back to null
            }
        }

        [Fact]
        public void SetBufferMemoryWhenBufferListSet_Throws()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                var bufferList = new List<ArraySegment<byte>> { new ArraySegment<byte>(new byte[1]) };
                saea.BufferList = bufferList;

                saea.SetBuffer(Memory<byte>.Empty); // nop

                Memory<byte> buffer = new byte[2];
                AssertExtensions.Throws<ArgumentException>(null, () => saea.SetBuffer(buffer));
                Assert.Same(bufferList, saea.BufferList);
                Assert.Null(saea.Buffer);
                Assert.True(saea.MemoryBuffer.Equals(default));

                saea.BufferList = null;
                saea.SetBuffer(buffer); // works fine when BufferList has been set back to null
            }
        }

        [Fact]
        public void SetBufferMemoryWhenBufferMemorySet_Succeeds()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                Memory<byte> buffer1 = new byte[1];
                Memory<byte> buffer2 = new byte[2];

                for (int i = 0; i < 2; i++)
                {
                    saea.SetBuffer(buffer1);
                    Assert.Null(saea.Buffer);
                    Assert.True(saea.MemoryBuffer.Equals(buffer1));
                    Assert.Equal(0, saea.Offset);
                    Assert.Equal(buffer1.Length, saea.Count);
                }

                saea.SetBuffer(buffer2);
                Assert.Null(saea.Buffer);
                Assert.True(saea.MemoryBuffer.Equals(buffer2));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(buffer2.Length, saea.Count);
            }
        }

        [Fact]
        public void SetBufferMemoryWhenBufferSet_Succeeds()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                byte[] buffer1 = new byte[3];
                Memory<byte> buffer2 = new byte[4];

                saea.SetBuffer(buffer1, 0, buffer1.Length);
                Assert.Same(buffer1, saea.Buffer);
                Assert.Equal(0, saea.Offset);
                Assert.Equal(buffer1.Length, saea.Count);

                saea.SetBuffer(1, 2);
                Assert.Same(buffer1, saea.Buffer);
                Assert.Equal(1, saea.Offset);
                Assert.Equal(2, saea.Count);

                saea.SetBuffer(buffer2);
                Assert.Null(saea.Buffer);
                Assert.True(saea.MemoryBuffer.Equals(buffer2));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(buffer2.Length, saea.Count);
            }
        }

        [Fact]
        public void SetBufferMemory_NonArray_BufferReturnsNull()
        {
            using (var m = new NativeMemoryManager(42))
            using (var saea = new SocketAsyncEventArgs())
            {
                saea.SetBuffer(m.Memory);
                Assert.True(saea.MemoryBuffer.Equals(m.Memory));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(m.Memory.Length, saea.Count);
                Assert.Null(saea.Buffer);
            }
        }

        [OuterLoop("Involves GC and finalization")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Finalizer_InvokedWhenNoLongerReferenced(bool afterAsyncOperation)
        {
            var cwt = new ConditionalWeakTable<object, object>();

            for (int i = 0; i < 5; i++) // create several SAEA instances, stored into cwt
            {
                CreateSocketAsyncEventArgs();

                void CreateSocketAsyncEventArgs() // separated out so that JIT doesn't extend lifetime of SAEA instances
                {
                    var saea = new SocketAsyncEventArgs();
                    cwt.Add(saea, saea);

                    if (afterAsyncOperation)
                    {
                        using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                            listener.Listen(1);

                            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                            {
                                saea.RemoteEndPoint = listener.LocalEndPoint;
                                using (var mres = new ManualResetEventSlim())
                                {
                                    saea.Completed += (s, e) => mres.Set();
                                    if (client.ConnectAsync(saea))
                                    {
                                        mres.Wait();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Assert.True(SpinWait.SpinUntil(() =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return cwt.Count() == 0; // validate that the cwt becomes empty
            }, 30_000));
        }
    }
}
