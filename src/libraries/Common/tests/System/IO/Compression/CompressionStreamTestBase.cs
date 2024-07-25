// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Tests;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public abstract class CompressionTestBase : WrappingConnectedStreamConformanceTests
    {
        public static IEnumerable<object[]> UncompressedTestFiles()
        {
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.doc") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.docx") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.pdf") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "alice29.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "asyoulik.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "cp.html") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "fields.c") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "grammar.lsp") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "kennedy.xls") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "lcet10.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "plrabn12.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "ptt5") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "sum") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "xargs.1") };
        }
        public static IEnumerable<object[]> UncompressedTestFilesZLib()
        {
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.doc") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.docx") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.pdf") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "sum") };
        }
        public static IEnumerable<object[]> ZLibOptionsRoundTripTestData()
        {
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.doc"), new ZLibCompressionOptions() { CompressionLevel = 0, CompressionStrategy = ZLibCompressionStrategy.Default } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.docx"), new ZLibCompressionOptions() { CompressionLevel = 3, CompressionStrategy = ZLibCompressionStrategy.Filtered } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.pdf"), new ZLibCompressionOptions() { CompressionLevel = 5, CompressionStrategy = ZLibCompressionStrategy.RunLengthEncoding } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.txt"), new ZLibCompressionOptions() { CompressionLevel = 7, CompressionStrategy = ZLibCompressionStrategy.HuffmanOnly } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "alice29.txt"), new ZLibCompressionOptions() { CompressionLevel = 9, CompressionStrategy = ZLibCompressionStrategy.Fixed } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "asyoulik.txt"), new ZLibCompressionOptions() { CompressionLevel = 2, CompressionStrategy = ZLibCompressionStrategy.RunLengthEncoding } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "cp.html"), new ZLibCompressionOptions() { CompressionLevel = 4, CompressionStrategy = ZLibCompressionStrategy.Default } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "fields.c"), new ZLibCompressionOptions() { CompressionLevel = 6, CompressionStrategy = ZLibCompressionStrategy.HuffmanOnly } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "grammar.lsp"), new ZLibCompressionOptions() { CompressionLevel = 8, CompressionStrategy = ZLibCompressionStrategy.Default } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "kennedy.xls"), new ZLibCompressionOptions() { CompressionLevel = 1, CompressionStrategy = ZLibCompressionStrategy.Fixed } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "lcet10.txt"), new ZLibCompressionOptions() { CompressionLevel = 1, CompressionStrategy = ZLibCompressionStrategy.Filtered } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "plrabn12.txt"), new ZLibCompressionOptions() { CompressionLevel = 2, CompressionStrategy = ZLibCompressionStrategy.RunLengthEncoding } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "ptt5"), new ZLibCompressionOptions() { CompressionLevel = 3, CompressionStrategy = ZLibCompressionStrategy.Default } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "sum"), new ZLibCompressionOptions() { CompressionLevel = 4, CompressionStrategy = ZLibCompressionStrategy.HuffmanOnly } };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "xargs.1"), new ZLibCompressionOptions() { CompressionLevel = 5, CompressionStrategy = ZLibCompressionStrategy.Filtered } };
        }
        protected virtual string UncompressedTestFile() => Path.Combine("UncompressedTestFiles", "TestDocument.pdf");
        protected abstract string CompressedTestFile(string uncompressedPath);
    }

    public abstract class CompressionStreamTestBase : CompressionTestBase
    {
        public abstract Stream CreateStream(Stream stream, CompressionMode mode);
        public abstract Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen);
        public abstract Stream CreateStream(Stream stream, CompressionLevel level);
        public abstract Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen);
        public abstract Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen);
        public abstract Stream BaseStream(Stream stream);
        public virtual int BufferSize { get => 8192; }

        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional(4 * 1024, 16 * 1024);
            return Task.FromResult<StreamPair>((CreateStream(stream1, CompressionMode.Compress), CreateStream(stream2, CompressionMode.Decompress)));
        }

        protected override Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen) =>
            Task.FromResult<StreamPair>((CreateStream(wrapped.Stream1, CompressionMode.Compress, leaveOpen), CreateStream(wrapped.Stream2, CompressionMode.Decompress, leaveOpen)));

        protected override int BufferedSize => 16 * 1024 + BufferSize;
        protected override bool UsableAfterCanceledReads => false;
        protected override Type UnsupportedReadWriteExceptionType => typeof(InvalidOperationException);
        protected override bool WrappedUsableAfterClose => false;
        protected override bool FlushRequiredToWriteData => true;
        protected override bool BlocksOnZeroByteReads => true;
    }
}
