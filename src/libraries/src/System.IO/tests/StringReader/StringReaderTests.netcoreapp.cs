// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class StringReaderTests
    {
        [Fact]
        public void ReadSpan_Success()
        {
            string input = "abcdef";
            var reader = new StringReader(input);
            Span<char> s = new char[2];

            Assert.Equal(2, reader.Read(s));
            Assert.Equal("ab", new string(s.ToArray()));

            Assert.Equal(1, reader.Read(s.Slice(0, 1)));
            Assert.Equal("cb", new string(s.ToArray()));

            Assert.Equal(2, reader.Read(s));
            Assert.Equal("de", new string(s.ToArray()));

            Assert.Equal(1, reader.Read(s));
            Assert.Equal("f", new string(s.Slice(0, 1).ToArray()));

            Assert.Equal(0, reader.Read(s));
        }

        [Fact]
        public void ReadBlockSpan_Success()
        {
            string input = "abcdef";
            var reader = new StringReader(input);
            Span<char> s = new char[2];

            Assert.Equal(2, reader.ReadBlock(s));
            Assert.Equal("ab", new string(s.ToArray()));

            Assert.Equal(1, reader.ReadBlock(s.Slice(0, 1)));
            Assert.Equal("cb", new string(s.ToArray()));

            Assert.Equal(2, reader.ReadBlock(s));
            Assert.Equal("de", new string(s.ToArray()));

            Assert.Equal(1, reader.ReadBlock(s));
            Assert.Equal("f", new string(s.Slice(0, 1).ToArray()));

            Assert.Equal(0, reader.ReadBlock(s));
        }

        [Fact]
        public async Task ReadMemoryAsync_Success()
        {
            string input = "abcdef";
            var reader = new StringReader(input);
            Memory<char> m = new char[2];

            Assert.Equal(2, await reader.ReadAsync(m));
            Assert.Equal("ab", new string(m.ToArray()));

            Assert.Equal(1, await reader.ReadAsync(m.Slice(0, 1)));
            Assert.Equal("cb", new string(m.ToArray()));

            Assert.Equal(2, await reader.ReadAsync(m));
            Assert.Equal("de", new string(m.ToArray()));

            Assert.Equal(1, await reader.ReadAsync(m));
            Assert.Equal("f", new string(m.Slice(0, 1).ToArray()));

            Assert.Equal(0, await reader.ReadAsync(m));
        }

        [Fact]
        public async Task ReadBlockMemoryAsync_Success()
        {
            string input = "abcdef";
            var reader = new StringReader(input);
            Memory<char> m = new char[2];

            Assert.Equal(2, await reader.ReadBlockAsync(m));
            Assert.Equal("ab", new string(m.ToArray()));

            Assert.Equal(1, await reader.ReadBlockAsync(m.Slice(0, 1)));
            Assert.Equal("cb", new string(m.ToArray()));

            Assert.Equal(2, await reader.ReadBlockAsync(m));
            Assert.Equal("de", new string(m.ToArray()));

            Assert.Equal(1, await reader.ReadBlockAsync(m));
            Assert.Equal("f", new string(m.Slice(0, 1).ToArray()));

            Assert.Equal(0, await reader.ReadBlockAsync(m));
        }

        [Fact]
        public void Disposed_ThrowsException()
        {
            var reader = new StringReader("abc");
            reader.Dispose();

            Assert.Throws<ObjectDisposedException>(() => reader.Read(Span<char>.Empty));
            Assert.Throws<ObjectDisposedException>(() => reader.ReadBlock(Span<char>.Empty));
            Assert.Throws<ObjectDisposedException>(() => { reader.ReadAsync(Memory<char>.Empty); });
            Assert.Throws<ObjectDisposedException>(() => { reader.ReadBlockAsync(Memory<char>.Empty); });
        }

        [Fact]
        public async Task Precanceled_ThrowsException()
        {
            var reader = new StringReader("abc");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadAsync(Memory<char>.Empty, new CancellationToken(true)).AsTask());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadBlockAsync(Memory<char>.Empty, new CancellationToken(true)).AsTask());
        }
    }
}
