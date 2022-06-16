// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests that are independent of the archive format.
    public class TarWriter_WriteEntry_Tests : TarTestsBase
    {
        [Fact]
        public void WriteEntry_AfterDispose_Throws()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream);
            writer.Dispose();

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Throws<ObjectDisposedException>(() => writer.WriteEntry(entry));
        }

        [Fact]
        public void WriteEntry_FromUnseekableStream_AdvanceDataStream_WriteFromThatPosition()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
            using WrappedStream unseekable = new WrappedStream(source, canRead: true, canWrite: true, canSeek: false);

            using MemoryStream destination = new MemoryStream();

            using (TarReader reader = new TarReader(unseekable))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);
                entry.DataStream.ReadByte(); // Advance one byte, now the expected string would be "ello file"

                using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    writer.WriteEntry(entry);
                }
            }

            destination.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(destination))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);

                using (StreamReader streamReader = new StreamReader(entry.DataStream, leaveOpen: true))
                {
                    string contents = streamReader.ReadLine();
                    Assert.Equal("ello file", contents);
                }
            }
        }
    }
}
