// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class Stream_ReadWriteSpan
    {
        [Fact]
        public void ReadSpan_DelegatesToRead_Success()
        {
            bool readInvoked = false;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvoked = true;
                    Assert.NotNull(array);
                    Assert.Equal(0, offset);
                    Assert.Equal(20, count);

                    for (int i = 0; i < 10; i++) array[offset + i] = (byte)i;
                    return 10;
                });

            Span<byte> totalSpan = new byte[30];
            Span<byte> targetSpan = totalSpan.Slice(5, 20);

            Assert.Equal(10, s.Read(targetSpan));
            Assert.True(readInvoked);
            for (int i = 0; i < 10; i++) Assert.Equal(i, targetSpan[i]);
            for (int i = 10; i < 20; i++) Assert.Equal(0, targetSpan[i]);
            readInvoked = false;
        }

        [Fact]
        public void WriteSpan_DelegatesToWrite_Success()
        {
            bool writeInvoked = false;
            var s = new DelegateStream(
                canWriteFunc: () => true,
                writeFunc: (array, offset, count) =>
                {
                    writeInvoked = true;
                    Assert.NotNull(array);
                    Assert.Equal(0, offset);
                    Assert.Equal(3, count);

                    for (int i = 0; i < count; i++) Assert.Equal(i, array[offset + i]);
                });

            Span<byte> span = new byte[10];
            span[3] = 1;
            span[4] = 2;
            s.Write(span.Slice(2, 3));
            Assert.True(writeInvoked);
            writeInvoked = false;
        }

        [Fact]
        public async Task ReadAsyncMemory_WrapsArray_DelegatesToReadAsyncArray_Success()
        {
            bool readInvoked = false;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readAsyncFunc: (array, offset, count, cancellationToken) =>
                {
                    readInvoked = true;
                    Assert.NotNull(array);
                    Assert.Equal(5, offset);
                    Assert.Equal(20, count);

                    for (int i = 0; i < 10; i++)
                    {
                        array[offset + i] = (byte)i;
                    }
                    return Task.FromResult(10);
                });

            Memory<byte> totalMemory = new byte[30];
            Memory<byte> targetMemory = totalMemory.Slice(5, 20);

            Assert.Equal(10, await s.ReadAsync(targetMemory));
            Assert.True(readInvoked);
            for (int i = 0; i < 10; i++)
                Assert.Equal(i, targetMemory.Span[i]);
            for (int i = 10; i < 20; i++)
                Assert.Equal(0, targetMemory.Span[i]);
            readInvoked = false;
        }

        [Fact]
        public async Task ReadAsyncMemory_WrapsNative_DelegatesToReadAsyncArrayWithPool_Success()
        {
            bool readInvoked = false;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readAsyncFunc: (array, offset, count, cancellationToken) =>
                {
                    readInvoked = true;
                    Assert.NotNull(array);
                    Assert.Equal(0, offset);
                    Assert.Equal(20, count);

                    for (int i = 0; i < 10; i++)
                    {
                        array[offset + i] = (byte)i;
                    }
                    return Task.FromResult(10);
                });

            using (var totalNativeMemory = new NativeOwnedMemory(30))
            {
                Memory<byte> totalMemory = totalNativeMemory.Memory;
                Memory<byte> targetMemory = totalMemory.Slice(5, 20);

                Assert.Equal(10, await s.ReadAsync(targetMemory));
                Assert.True(readInvoked);
                for (int i = 0; i < 10; i++)
                    Assert.Equal(i, targetMemory.Span[i]);
                readInvoked = false;
            }
        }

        [Fact]
        public async Task WriteAsyncMemory_WrapsArray_DelegatesToWrite_Success()
        {
            bool writeInvoked = false;
            var s = new DelegateStream(
                canWriteFunc: () => true,
                writeAsyncFunc: (array, offset, count, cancellationToken) =>
                {
                    writeInvoked = true;
                    Assert.NotNull(array);
                    Assert.Equal(2, offset);
                    Assert.Equal(3, count);

                    for (int i = 0; i < count; i++)
                        Assert.Equal(i, array[offset + i]);

                    return Task.CompletedTask;
                });

            Memory<byte> memory = new byte[10];
            memory.Span[3] = 1;
            memory.Span[4] = 2;
            await s.WriteAsync(memory.Slice(2, 3));
            Assert.True(writeInvoked);
            writeInvoked = false;
        }

        [Fact]
        public async Task WriteAsyncMemory_WrapsNative_DelegatesToWrite_Success()
        {
            bool writeInvoked = false;
            var s = new DelegateStream(
                canWriteFunc: () => true,
                writeAsyncFunc: (array, offset, count, cancellationToken) =>
                {
                    writeInvoked = true;
                    Assert.NotNull(array);
                    Assert.Equal(0, offset);
                    Assert.Equal(3, count);

                    for (int i = 0; i < count; i++)
                        Assert.Equal(i, array[i]);

                    return Task.CompletedTask;
                });

            using (var nativeMemory = new NativeOwnedMemory(10))
            {
                Memory<byte> memory = nativeMemory.Memory;
                memory.Span[2] = 0;
                memory.Span[3] = 1;
                memory.Span[4] = 2;
                await s.WriteAsync(memory.Slice(2, 3));
                Assert.True(writeInvoked);
                writeInvoked = false;
            }
        }
    }
}
