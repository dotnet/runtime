// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamCopyToTests
    {
        [Fact]
        public void CopyToAsync_StreamToken_InvalidArgsThrows()
        {
            Stream s = new MemoryStream();
            AssertExtensions.Throws<ArgumentNullException>("destination", () => { s.CopyToAsync(null, default(CancellationToken)); });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(42)]
        [InlineData(100000)] // greater than 81920, the DefaultCopyBufferSize
        public void CopyToAsync_StreamToken_ExpectedBufferSizePropagated(int length)
        {
            Stream s = new CustomMemoryStream();
            s.Write(new byte[length], 0, length);
            s.Position = 0;

            const int DefaultCopyBufferSize = 81920;
            Assert.Equal(Math.Max(1, Math.Min(length, DefaultCopyBufferSize)), ((Task<int>)s.CopyToAsync(Stream.Null, default(CancellationToken))).Result);
        }

        private sealed class CustomMemoryStream : MemoryStream
        {
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
                Task.FromResult(bufferSize);
        }

        [Fact]
        public void CopyToAsync_StreamToken_PrecanceledToken_Cancels()
        {
            var src = new MemoryStream();
            Assert.Equal(TaskStatus.Canceled, src.CopyToAsync(Stream.Null, new CancellationToken(true)).Status);
        }

        [Fact]
        public async Task CopyToAsync_StreamToken_AllDataCopied()
        {
            var src = new MemoryStream();
            src.Write(Enumerable.Range(0, 10000).Select(i => (byte)i).ToArray(), 0, 256);
            src.Position = 0;

            var dst = new MemoryStream();
            await src.CopyToAsync(dst, default(CancellationToken));

            Assert.Equal<byte>(src.ToArray(), dst.ToArray());
        }
    }
}
