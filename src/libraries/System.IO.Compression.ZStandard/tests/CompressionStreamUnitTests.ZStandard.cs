// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class ZStandardStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new ZStandardStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new ZStandardStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new ZStandardStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new ZStandardStream(stream, level, leaveOpen);
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) =>
            new ZStandardStream(stream, options == null ? null : new ZStandardCompressionOptions(options.CompressionLevel), leaveOpen);

        public override Stream BaseStream(Stream stream) => ((ZStandardStream)stream).BaseStream;

        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("ZStandardTestData", Path.GetFileName(uncompressedPath) + ".zst");
    }
}
