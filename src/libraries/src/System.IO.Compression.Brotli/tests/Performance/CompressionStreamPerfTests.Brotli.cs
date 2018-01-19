// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    public class BrotliStreamPerfTests : CompressionStreamPerfTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new BrotliStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new BrotliStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new BrotliStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new BrotliStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((BrotliStream)stream).BaseStream;
        public override bool FlushCompletes { get => false; }
        public override int BufferSize { get => 65520; }
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("BrotliTestData", Path.GetFileName(uncompressedPath) + ".br");
    }
}
