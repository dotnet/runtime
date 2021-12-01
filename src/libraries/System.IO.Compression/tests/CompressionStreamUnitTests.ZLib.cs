// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class ZLibStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new ZLibStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new ZLibStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new ZLibStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new ZLibStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((ZLibStream)stream).BaseStream;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("ZLibTestData", Path.GetFileName(uncompressedPath) + ".z");
    }
}
