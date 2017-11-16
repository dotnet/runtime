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
    public partial class StreamWriterTests
    {
        [Fact]
        public void Write_EmptySpan_WritesNothing()
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s))
            {
                writer.Write(ReadOnlySpan<char>.Empty);
                writer.Flush();
                Assert.Equal(0, s.Position);
            }
        }

        [Fact]
        public void WriteLine_EmptySpan_WritesNewLine()
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s))
            {
                writer.WriteLine(ReadOnlySpan<char>.Empty);
                writer.Flush();
                Assert.Equal(Environment.NewLine.Length, s.Position);
            }
        }

        [Fact]
        public async Task WriteAsync_EmptyMemory_WritesNothing()
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s))
            {
                await writer.WriteAsync(ReadOnlyMemory<char>.Empty);
                await writer.FlushAsync();
                Assert.Equal(0, s.Position);
            }
        }

        [Fact]
        public async Task WriteLineAsync_EmptyMemory_WritesNothing()
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s))
            {
                await writer.WriteLineAsync(ReadOnlyMemory<char>.Empty);
                await writer.FlushAsync();
                Assert.Equal(Environment.NewLine.Length, s.Position);
            }
        }

        [Theory]
        [InlineData(1, 1, 1, false)]
        [InlineData(100, 1, 100, false)]
        [InlineData(100, 10, 3, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 10, 3, true)]
        public void Write_Span_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, Encoding.ASCII, writerBufferSize) { AutoFlush = autoFlush })
            {
                var data = new char[length];
                var rand = new Random(42);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (char)(rand.Next(0, 26) + 'a');
                }

                Span<char> source = data;
                while (source.Length > 0)
                {
                    int n = Math.Min(source.Length, writeSize);
                    writer.Write(source.Slice(0, n));
                    source = source.Slice(n);
                }

                writer.Flush();

                Assert.Equal(data, s.ToArray().Select(b => (char)b));
            }
        }

        [Theory]
        [InlineData(1, 1, 1, false)]
        [InlineData(100, 1, 100, false)]
        [InlineData(100, 10, 3, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 10, 3, true)]
        public async Task Write_Memory_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, Encoding.ASCII, writerBufferSize) { AutoFlush = autoFlush })
            {
                var data = new char[length];
                var rand = new Random(42);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (char)(rand.Next(0, 26) + 'a');
                }

                ReadOnlyMemory<char> source = data;
                while (source.Length > 0)
                {
                    int n = Math.Min(source.Length, writeSize);
                    await writer.WriteAsync(source.Slice(0, n));
                    source = source.Slice(n);
                }

                await writer.FlushAsync();

                Assert.Equal(data, s.ToArray().Select(b => (char)b));
            }
        }

        [Theory]
        [InlineData(1, 1, 1, false)]
        [InlineData(100, 1, 100, false)]
        [InlineData(100, 10, 3, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 10, 3, true)]
        public void WriteLine_Span_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, Encoding.ASCII, writerBufferSize) { AutoFlush = autoFlush })
            {
                var data = new char[length];
                var rand = new Random(42);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (char)(rand.Next(0, 26) + 'a');
                }

                Span<char> source = data;
                while (source.Length > 0)
                {
                    int n = Math.Min(source.Length, writeSize);
                    writer.WriteLine(source.Slice(0, n));
                    source = source.Slice(n);
                }

                writer.Flush();

                Assert.Equal(length + (Environment.NewLine.Length * (length / writeSize)), s.Length);
            }
        }

        [Theory]
        [InlineData(1, 1, 1, false)]
        [InlineData(100, 1, 100, false)]
        [InlineData(100, 10, 3, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 10, 3, true)]
        public async Task WriteLineAsync_Memory_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, Encoding.ASCII, writerBufferSize) { AutoFlush = autoFlush })
            {
                var data = new char[length];
                var rand = new Random(42);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (char)(rand.Next(0, 26) + 'a');
                }

                ReadOnlyMemory<char> source = data;
                while (source.Length > 0)
                {
                    int n = Math.Min(source.Length, writeSize);
                    await writer.WriteLineAsync(source.Slice(0, n));
                    source = source.Slice(n);
                }

                await writer.FlushAsync();

                Assert.Equal(length + (Environment.NewLine.Length * (length / writeSize)), s.Length);
            }
        }

        [Fact]
        public async Task WriteAsync_Precanceled_ThrowsCancellationException()
        {
            using (var writer = new StreamWriter(Stream.Null))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteAsync(ReadOnlyMemory<char>.Empty, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteLineAsync(ReadOnlyMemory<char>.Empty, new CancellationToken(true)));
            }
        }
    }
}
