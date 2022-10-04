// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_Tests : TarTestsBase
    {
        [Fact]
        public void TarReader_NullArchiveStream() => Assert.Throws<ArgumentNullException>(() => new TarReader(archiveStream: null));

        [Fact]
        public void TarReader_UnreadableStream()
        {
            using MemoryStream ms = new MemoryStream();
            using WrappedStream ws = new WrappedStream(ms, canRead: false, canWrite: true, canSeek: true);
            Assert.Throws<ArgumentException>(() => new TarReader(ws));
        }

        [Fact]
        public void TarReader_LeaveOpen_False()
        {
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            using (TarReader reader = new TarReader(ms, leaveOpen: false))
            {
                TarEntry entry;
                while ((entry = reader.GetNextEntry()) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }

            Assert.True(dataStreams.Any());
            foreach (Stream ds in dataStreams)
            {
                Assert.Throws<ObjectDisposedException>(() => ds.ReadByte());
            }
        }

        [Fact]
        public void TarReader_LeaveOpen_True()
        {
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            using (TarReader reader = new TarReader(ms, leaveOpen: true))
            {
                TarEntry entry;
                while ((entry = reader.GetNextEntry()) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }

            Assert.True(dataStreams.Any());
            foreach (Stream ds in dataStreams)
            {
                ds.ReadByte(); // Should not throw
                ds.Dispose();
            }
        }

        [Fact]
        public void TarReader_LeaveOpen_False_CopiedDataNotDisposed()
        {
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            using (TarReader reader = new TarReader(ms, leaveOpen: false))
            {
                TarEntry entry;
                while ((entry = reader.GetNextEntry(copyData: true)) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }

            Assert.True(dataStreams.Any());
            foreach (Stream ds in dataStreams)
            {
                ds.ReadByte(); // Should not throw, copied streams, user should dispose
                ds.Dispose();
            }
        }
    }
}
