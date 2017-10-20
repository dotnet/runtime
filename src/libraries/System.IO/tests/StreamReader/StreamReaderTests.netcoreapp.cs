// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamReaderTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public async Task Read_EmptySpan_ReadsNothing(int length)
        {
            using (var r = new StreamReader(new MemoryStream(Enumerable.Repeat((byte)'s', length).ToArray())))
            {
                Assert.Equal(0, r.Read(Span<char>.Empty));
                Assert.Equal(0, r.ReadBlock(Span<char>.Empty));
                Assert.Equal(0, await r.ReadAsync(Memory<char>.Empty));
                Assert.Equal(0, await r.ReadBlockAsync(Memory<char>.Empty));
            }
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(1, 100, 101)]
        [InlineData(100, 50, 1)]
        [InlineData(100, 50, 101)]
        public void Read_ReadsExpectedData(int readLength, int totalLength, int bufferSize)
        {
            var data = new char[totalLength];
            var r = new Random(42);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (char)('a' + r.Next(0, 26));
            }

            var result = new char[data.Length];
            Span<char> dst = result;

            using (var sr = new StreamReader(new MemoryStream(data.Select(i => (byte)i).ToArray()), Encoding.ASCII, false, bufferSize))
            {
                while (dst.Length > 0)
                {
                    int read = sr.Read(dst);
                    Assert.InRange(read, 1, dst.Length);
                    dst = dst.Slice(read);
                }
            }

            Assert.Equal<char>(data, result);
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(1, 100, 101)]
        [InlineData(100, 50, 1)]
        [InlineData(100, 50, 101)]
        public void ReadBlock_ReadsExpectedData(int readLength, int totalLength, int bufferSize)
        {
            var data = new char[totalLength];
            var r = new Random(42);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (char)('a' + r.Next(0, 26));
            }

            var result = new char[data.Length];
            Span<char> dst = result;

            using (var sr = new StreamReader(new MemoryStream(data.Select(i => (byte)i).ToArray()), Encoding.ASCII, false, bufferSize))
            {
                while (dst.Length > 0)
                {
                    int read = sr.ReadBlock(dst);
                    Assert.InRange(read, 1, dst.Length);
                    dst = dst.Slice(read);
                }
            }

            Assert.Equal<char>(data, result);
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(1, 100, 101)]
        [InlineData(100, 50, 1)]
        [InlineData(100, 50, 101)]
        public async Task ReadAsync_ReadsExpectedData(int readLength, int totalLength, int bufferSize)
        {
            var data = new char[totalLength];
            var r = new Random(42);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (char)('a' + r.Next(0, 26));
            }

            var result = new char[data.Length];
            Memory<char> dst = result;

            using (var sr = new StreamReader(new MemoryStream(data.Select(i => (byte)i).ToArray()), Encoding.ASCII, false, bufferSize))
            {
                while (dst.Length > 0)
                {
                    int read = await sr.ReadAsync(dst);
                    Assert.InRange(read, 1, dst.Length);
                    dst = dst.Slice(read);
                }
            }

            Assert.Equal<char>(data, result);
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(1, 100, 101)]
        [InlineData(100, 50, 1)]
        [InlineData(100, 50, 101)]
        public async Task ReadBlockAsync_ReadsExpectedData(int readLength, int totalLength, int bufferSize)
        {
            var data = new char[totalLength];
            var r = new Random(42);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (char)('a' + r.Next(0, 26));
            }

            var result = new char[data.Length];
            Memory<char> dst = result;

            using (var sr = new StreamReader(new MemoryStream(data.Select(i => (byte)i).ToArray()), Encoding.ASCII, false, bufferSize))
            {
                while (dst.Length > 0)
                {
                    int read = await sr.ReadBlockAsync(dst);
                    Assert.InRange(read, 1, dst.Length);
                    dst = dst.Slice(read);
                }
            }

            Assert.Equal<char>(data, result);
        }

        [Fact]
        public void ReadBlock_RepeatsReadsUntilReadDesiredAmount()
        {
            char[] data = "hello world".ToCharArray();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (buffer, offset, count) => ms.Read(buffer, offset, 1)); // do actual reads a byte at a time
            using (var r = new StreamReader(s, Encoding.UTF8, false, 2))
            {
                var result = new char[data.Length];
                Assert.Equal(data.Length, r.ReadBlock((Span<char>)result));
                Assert.Equal<char>(data, result);
            }
        }

        [Fact]
        public async Task ReadBlockAsync_RepeatsReadsUntilReadDesiredAmount()
        {
            char[] data = "hello world".ToCharArray();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var s = new DelegateStream(
                canReadFunc: () => true,
                readAsyncFunc: (buffer, offset, count, cancellationToken) => ms.ReadAsync(buffer, offset, 1)); // do actual reads a byte at a time
            using (var r = new StreamReader(s, Encoding.UTF8, false, 2))
            {
                var result = new char[data.Length];
                Assert.Equal(data.Length, await r.ReadBlockAsync((Memory<char>)result));
                Assert.Equal<char>(data, result);
            }
        }

        [Fact]
        public async Task ReadAsync_Precanceled_ThrowsException()
        {
            using (var sr = new StreamReader(new MemoryStream()))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sr.ReadAsync(Memory<char>.Empty, new CancellationToken(true)).AsTask());
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sr.ReadBlockAsync(Memory<char>.Empty, new CancellationToken(true)).AsTask());
            }
        }

        [Fact]
        public async Task Read_SpanMemory_DisposedStream_ThrowsException()
        {
            var sr = new StreamReader(new MemoryStream());
            sr.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sr.Read(Span<char>.Empty));
            Assert.Throws<ObjectDisposedException>(() => sr.ReadBlock(Span<char>.Empty));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => sr.ReadAsync(Memory<char>.Empty).AsTask());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => sr.ReadBlockAsync(Memory<char>.Empty).AsTask());
        }
    }
}
