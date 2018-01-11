// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    public class GzipStreamPerfTests : CompressionStreamPerfTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new GZipStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new GZipStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new GZipStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((GZipStream)stream).BaseStream;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("GZipTestData", Path.GetFileName(uncompressedPath) + ".gz");
    }
}
