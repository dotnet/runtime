// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new ZstandardStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new ZstandardStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new ZstandardStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new ZstandardStream(stream, level, leaveOpen);
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) =>
            new ZstandardStream(stream, options == null ? null : new ZstandardCompressionOptions(options.CompressionLevel), leaveOpen);

        public override Stream BaseStream(Stream stream) => ((ZstandardStream)stream).BaseStream;

        // The tests are relying on an implementation detail of BrotliStream, using knowledge of its internal buffer size
        // in various test calculations.  Currently the implementation is using the ArrayPool, which will round up to a
        // power-of-2. If the buffer size employed changes (which could also mean that ArrayPool<byte>.Shared starts giving
        // out different array sizes), the tests will need to be tweaked.
        public override int BufferSize => 1 << 16;

        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("ZstandardTestData", Path.GetFileName(uncompressedPath) + ".zst");
    }
}
