// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class StringWriterTests
    {
        [Fact]
        public async Task WriteSpanMemory_Success()
        {
            var sw = new StringWriter();

            sw.Write((Span<char>)new char[0]);
            sw.Write((Span<char>)new char[] { 'a' });
            sw.Write((Span<char>)new char[] { 'b', 'c', 'd' });
            sw.WriteLine((Span<char>)new char[] { 'e' });

            await sw.WriteAsync((ReadOnlyMemory<char>)new char[0]);
            await sw.WriteAsync((ReadOnlyMemory<char>)new char[] { 'f' });
            await sw.WriteAsync((ReadOnlyMemory<char>)new char[] { 'g', 'h', 'i' });
            await sw.WriteLineAsync((ReadOnlyMemory<char>)new char[] { 'j' });

            Assert.Equal("abcde" + Environment.NewLine + "fghij" + Environment.NewLine, sw.ToString());
        }

        [Fact]
        public async Task Precanceled_ThrowsException()
        {
            var writer = new StringWriter();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteAsync(Memory<char>.Empty, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteLineAsync(Memory<char>.Empty, new CancellationToken(true)));
        }
    }
}
