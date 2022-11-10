// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    [OuterLoop]
    [Collection(nameof(DisableParallelization))] // don't create multiple large files at the same time
    public class TarWriter_WriteEntryAsync_LongFile_Tests : TarTestsBase
    {
        public static IEnumerable<object[]> WriteEntry_LongFileSize_TheoryDataAsync()
            => TarWriter_WriteEntry_LongFile_Tests.WriteEntry_LongFileSize_TheoryData();

        [Theory]
        [MemberData(nameof(WriteEntry_LongFileSize_TheoryDataAsync))]
        public async Task WriteEntry_LongFileSizeAsync(TarEntryFormat entryFormat, long size, bool unseekableStream)
        {
            // Write archive with a 8 Gb long entry.
            FileStream tarFile = File.Open(GetTestFilePath(), new FileStreamOptions { Access = FileAccess.ReadWrite, Mode = FileMode.Create, Options = FileOptions.DeleteOnClose });
            Stream s = unseekableStream ? new WrappedStream(tarFile, tarFile.CanRead, tarFile.CanWrite, canSeek: false) : tarFile;

            await using (TarWriter writer = new(s, leaveOpen: true))
            {
                TarEntry writeEntry = InvokeTarEntryCreationConstructor(entryFormat, entryFormat is TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile, "foo");
                writeEntry.DataStream = new SimulatedDataStream(size);
                await writer.WriteEntryAsync(writeEntry);
            }

            tarFile.Position = 0;

            // Read the archive back.
            await using TarReader reader = new TarReader(s);
            TarEntry entry = await reader.GetNextEntryAsync();
            Assert.Equal(size, entry.Length);

            Stream dataStream = entry.DataStream;
            Assert.Equal(size, dataStream.Length);
            Assert.Equal(0, dataStream.Position);

            ReadOnlyMemory<byte> dummyData = SimulatedDataStream.DummyData;

            // Read the first bytes.
            byte[] buffer = new byte[dummyData.Length];
            Assert.Equal(buffer.Length, dataStream.Read(buffer));
            AssertExtensions.SequenceEqual(dummyData.Span, buffer);
            Assert.Equal(0, dataStream.ReadByte()); // check next byte is correct.
            buffer.AsSpan().Clear();

            // Read the last bytes.
            long dummyDataOffset = size - dummyData.Length - 1;
            if (dataStream.CanSeek)
            {
                Assert.False(unseekableStream);
                dataStream.Seek(dummyDataOffset, SeekOrigin.Begin);
            }
            else
            {
                Assert.True(unseekableStream);
                Memory<byte> seekBuffer = new byte[4_096];

                while (dataStream.Position < dummyDataOffset)
                {
                    int bufSize = (int)Math.Min(seekBuffer.Length, dummyDataOffset - dataStream.Position);
                    int res = await dataStream.ReadAsync(seekBuffer.Slice(0, bufSize));
                    Assert.True(res > 0, "Unseekable stream finished before expected - Something went very wrong");
                }
            }

            Assert.Equal(0, dataStream.ReadByte()); // check previous byte is correct.
            Assert.Equal(buffer.Length, dataStream.Read(buffer));
            AssertExtensions.SequenceEqual(dummyData.Span, buffer);
            Assert.Equal(size, dataStream.Position);

            Assert.Null(await reader.GetNextEntryAsync());
        }
    }
}
