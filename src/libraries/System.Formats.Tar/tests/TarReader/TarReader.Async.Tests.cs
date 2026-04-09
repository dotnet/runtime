// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarReader_Tests : TarTestsBase
    {
        [Fact]
        public async Task TarReader_LeaveOpen_False_Async()
        {
            await using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            await using (TarReader reader = new TarReader(ms, leaveOpen: false))
            {
                TarEntry entry;
                while ((entry = await reader.GetNextEntryAsync()) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }

            Assert.Throws<ObjectDisposedException>(() => ms.ReadByte());

            Assert.True(dataStreams.Any());
            foreach (Stream ds in dataStreams)
            {
                Assert.Throws<ObjectDisposedException>(() => ds.ReadByte());
            }
        }

        [Fact]
        public async Task TarReader_LeaveOpen_True_Async()
        {
            await using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            await using (TarReader reader = new TarReader(ms, leaveOpen: true))
            {
                TarEntry entry;
                while ((entry = await reader.GetNextEntryAsync()) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }

            ms.ReadByte(); // Should not throw

            Assert.True(dataStreams.Any());
            foreach (Stream ds in dataStreams)
            {
                ds.ReadByte(); // Should not throw
                ds.Dispose();
            }
        }
    }
}
