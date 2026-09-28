// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    /// <summary>
    /// Tests for writing and reading large file entries (>= 8 GB) using simulated streams
    /// to avoid requiring large amounts of disk space or memory.
    /// </summary>
    public class TarWriter_WriteEntry_LargeFiles_Tests : TarTestsBase
    {
        public static IEnumerable<object[]> WriteEntry_LargeFile_TheoryData()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.Pax, TarEntryFormat.Gnu })
            {
                foreach (bool isAsync in new[] { false, true })
                {
                    yield return new object[] { format, isAsync };
                }
            }
        }

        [Theory]
        [OuterLoop("Runs for several seconds")]
        [MemberData(nameof(WriteEntry_LargeFile_TheoryData))]
        public async Task WriteEntry_LargeFile_RoundTrip(TarEntryFormat entryFormat, bool isAsync)
        {
            // Test just above the legacy max file size (8GB - 1 byte)
            long size = LegacyMaxFileSize + 1;
            const string entryName = "large-file.bin";

            // Use ConnectedStreams to avoid storing the entire tar archive in memory.
            (Stream writer, Stream reader) = ConnectedStreams.CreateUnidirectional(initialBufferSize: 4_096, maxBufferSize: 1_024 * 1_024);

            Task writeTask = Task.Run(async () =>
            {
                await using (writer)
                {
                    await using TarWriter tarWriter = new(writer, leaveOpen: true);
                    TarEntry writeEntry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, entryName);
                    writeEntry.DataStream = new SimulatedDataStream(size);

                    if (isAsync)
                    {
                        await tarWriter.WriteEntryAsync(writeEntry);
                    }
                    else
                    {
                        tarWriter.WriteEntry(writeEntry);
                    }
                }
            });

            await using (reader)
            {
                await using TarReader tarReader = new(reader, leaveOpen: true);

                TarEntry entry = isAsync ? await tarReader.GetNextEntryAsync() : tarReader.GetNextEntry();
                Assert.Equal(size, entry.Length);
                Assert.Equal(entryName, entry.Name);

                Stream dataStream = entry.DataStream;
                Assert.Equal(size, dataStream.Length);
                Assert.Equal(0, dataStream.Position);

                ReadOnlyMemory<byte> dummyData = SimulatedDataStream.DummyData;

                // Read and verify the first bytes.
                Memory<byte> buffer = new byte[dummyData.Length];
                Assert.Equal(buffer.Length, isAsync ? await dataStream.ReadAsync(buffer) : dataStream.Read(buffer.Span));
                AssertExtensions.SequenceEqual(dummyData.Span, buffer.Span);
                Assert.Equal(0, dataStream.ReadByte());

                // Skip to near the end and verify the last bytes.
                long dummyDataOffset = size - dummyData.Length - 1;
                Memory<byte> skipBuffer = new byte[4_096];
                while (dataStream.Position < dummyDataOffset)
                {
                    int bufSize = (int)Math.Min(skipBuffer.Length, dummyDataOffset - dataStream.Position);
                    int bytesRead = isAsync ? await dataStream.ReadAsync(skipBuffer.Slice(0, bufSize)) : dataStream.Read(skipBuffer.Span.Slice(0, bufSize));
                    Assert.True(bytesRead > 0, "Stream ended before expected");
                }

                Assert.Equal(0, dataStream.ReadByte());
                buffer.Span.Clear();
                Assert.Equal(buffer.Length, isAsync ? await dataStream.ReadAsync(buffer) : dataStream.Read(buffer.Span));
                AssertExtensions.SequenceEqual(dummyData.Span, buffer.Span);
                Assert.Equal(size, dataStream.Position);

                Assert.Null(isAsync ? await tarReader.GetNextEntryAsync() : tarReader.GetNextEntry());
            }

            await writeTask;
        }
    }
}