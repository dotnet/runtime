// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Buffers.Tests
{
    public class ArrayBufferWriterTests_Byte : ArrayBufferWriterTests<byte>
    {
        public override void WriteData(IBufferWriter<byte> bufferWriter, int numBytes)
        {
            Span<byte> outputSpan = bufferWriter.GetSpan(numBytes);
            Assert.True(outputSpan.Length >= numBytes);
            var random = new Random(42);

            var data = new byte[numBytes];
            random.NextBytes(data);
            data.CopyTo(outputSpan);

            bufferWriter.Advance(numBytes);
        }

        [Fact]
        public void WriteAndCopyToStream()
        {
            var output = new ArrayBufferWriter<byte>();
            WriteData(output, 100);

            using var memStream = new MemoryStream(100);

            Assert.Equal(100, output.WrittenCount);

            ReadOnlySpan<byte> outputSpan = output.WrittenMemory.ToArray();

            ReadOnlyMemory<byte> transientMemory = output.WrittenMemory;
            ReadOnlySpan<byte> transientSpan = output.WrittenSpan;

            Assert.True(transientSpan.SequenceEqual(transientMemory.Span));

            Assert.True(transientSpan[0] != 0);

            memStream.Write(transientSpan.ToArray(), 0, transientSpan.Length);
            output.Clear();

            Assert.True(transientSpan[0] == 0);
            Assert.True(transientMemory.Span[0] == 0);

            Assert.Equal(0, output.WrittenCount);
            byte[] streamOutput = memStream.ToArray();

            Assert.True(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));

            Assert.Equal(outputSpan.Length, streamOutput.Length);
            Assert.True(outputSpan.SequenceEqual(streamOutput));
        }

        [Fact]
        public async Task WriteAndCopyToStreamAsync()
        {
            var output = new ArrayBufferWriter<byte>();
            WriteData(output, 100);

            using var memStream = new MemoryStream(100);

            Assert.Equal(100, output.WrittenCount);

            ReadOnlyMemory<byte> outputMemory = output.WrittenMemory.ToArray();

            ReadOnlyMemory<byte> transient = output.WrittenMemory;

            Assert.True(transient.Span[0] != 0);

            await memStream.WriteAsync(transient.ToArray(), 0, transient.Length);
            output.Clear();

            Assert.True(transient.Span[0] == 0);

            Assert.Equal(0, output.WrittenCount);
            byte[] streamOutput = memStream.ToArray();

            Assert.True(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenMemory.Span));

            Assert.Equal(outputMemory.Length, streamOutput.Length);
            Assert.True(outputMemory.Span.SequenceEqual(streamOutput));
        }
    }
}
