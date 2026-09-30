// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Compression
{
    // Regression tests for https://github.com/dotnet/runtime/issues/134700: DeflateStream, GZipStream, and
    // ZLibStream all share the same pool of native deflate states, keyed by compression level/strategy/window
    // bits/mem level. GZipStream and ZLibStream each use a distinct window-bits value (see ZLibNative.GZip_DefaultWindowBits
    // and ZLibNative.Deflate_DefaultWindowBits), so interleaving them must never let one format's pooled state
    // leak into another's stream.
    public class DeflaterStatePoolTests
    {
        [Fact]
        public void InterleavedFormats_ReusingPool_DoNotCrossContaminate()
        {
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 8192 * 4) + Guid.NewGuid());

            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(data, RoundTrip(data, static (s, mode) => new DeflateStream(s, mode, leaveOpen: true)));
                Assert.Equal(data, RoundTrip(data, static (s, mode) => new GZipStream(s, mode, leaveOpen: true)));
                Assert.Equal(data, RoundTrip(data, static (s, mode) => new ZLibStream(s, mode, leaveOpen: true)));
            }
        }

        private static byte[] RoundTrip(byte[] data, Func<Stream, CompressionMode, Stream> create)
        {
            var compressed = new MemoryStream();
            using (Stream compressor = create(compressed, CompressionMode.Compress))
            {
                compressor.Write(data, 0, data.Length);
            }
            compressed.Position = 0;

            var decompressed = new MemoryStream();
            using (Stream decompressor = create(compressed, CompressionMode.Decompress))
            {
                decompressor.CopyTo(decompressed);
            }
            return decompressed.ToArray();
        }
    }
}
