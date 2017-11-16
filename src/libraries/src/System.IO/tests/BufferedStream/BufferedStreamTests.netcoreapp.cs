// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class BufferedStreamTests
    {
        [Fact]
        public void UnderlyingStream()
        {
            var underlyingStream = new MemoryStream();
            var bufferedStream = new BufferedStream(underlyingStream);
            Assert.Same(underlyingStream, bufferedStream.UnderlyingStream);
        }

        [Fact]
        public void BufferSize()
        {
            var bufferedStream = new BufferedStream(new MemoryStream(), 1234);
            Assert.Equal(1234, bufferedStream.BufferSize);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1024, 4096)]
        [InlineData(4096, 4097)]
        [InlineData(4096, 1)]
        [InlineData(2047, 4096)]
        public void ReadSpan_WriteSpan_AllDataCopied(int spanSize, int bufferSize)
        {
            byte[] data = new byte[80000];
            new Random(42).NextBytes(data);

            var result = new MemoryStream();
            using (var output = new BufferedStream(result, bufferSize))
            using (var input = new BufferedStream(new MemoryStream(data), bufferSize))
            {
                Span<byte> span = new byte[spanSize];
                int bytesRead;
                while ((bytesRead = input.Read(span)) != 0)
                {
                    output.Write(span.Slice(0, bytesRead));
                }
            }
            Assert.Equal(data, result.ToArray());
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1024, 4096)]
        [InlineData(4096, 4097)]
        [InlineData(4096, 1)]
        [InlineData(2047, 4096)]
        public async Task ReadMemory_WriteMemory_AllDataCopied(int spanSize, int bufferSize)
        {
            byte[] data = new byte[80000];
            new Random(42).NextBytes(data);

            var result = new MemoryStream();
            using (var output = new BufferedStream(result, bufferSize))
            using (var input = new BufferedStream(new MemoryStream(data), bufferSize))
            {
                Memory<byte> memory = new byte[spanSize];
                int bytesRead;
                while ((bytesRead = await input.ReadAsync(memory)) != 0)
                {
                    await output.WriteAsync(memory.Slice(0, bytesRead));
                }
            }
            Assert.Equal(data, result.ToArray());
        }

        [Fact]
        public void ReadWriteMemory_Precanceled_Throws()
        {
            using (var bs = new BufferedStream(new MemoryStream()))
            {
                Assert.Equal(TaskStatus.Canceled, bs.ReadAsync(new byte[1], new CancellationToken(true)).AsTask().Status);
                Assert.Equal(TaskStatus.Canceled, bs.WriteAsync(new byte[1], new CancellationToken(true)).Status);
            }
        }
    }
}
