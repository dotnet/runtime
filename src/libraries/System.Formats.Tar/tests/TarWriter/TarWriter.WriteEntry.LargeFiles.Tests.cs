// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    /// <summary>
    /// Tests for writing and reading large file entries (>8GB) using simulated streams
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
        public async Task WriteEntryAsync_LargeFile_RoundTrip(TarEntryFormat entryFormat, bool isAsync)
        {
            // Test just above the legacy max file size (8GB - 1 byte), as well as
            long size = LegacyMaxFileSize + 1;

            // Use ConnectedStreams to avoid storing the entire tar archive in memory
            (Stream writer, Stream reader) = ConnectedStreams.CreateUnidirectional(initialBufferSize: 4096, maxBufferSize: 1024 * 1024);

            Exception writeException = null;
            Exception readException = null;

            // Write the tar archive in a separate task
            Task writeTask = Task.Run(async () =>
            {
                try
                {
                    await using (writer)
                    {
                        await using (TarWriter tarWriter = new(writer, leaveOpen: false))
                        {
                            TarEntry writeEntry = InvokeTarEntryCreationConstructor(
                                entryFormat,
                                entryFormat is TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile,
                                "large-file-async.bin");
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
                    }
                }
                catch (Exception ex)
                {
                    writeException = ex;
                }
            });

            // Read the tar archive in the current thread
            try
            {
                await using (reader)
                {
                    await using (TarReader tarReader = new(reader))
                    {
                        TarEntry entry = isAsync ? await tarReader.GetNextEntryAsync() : tarReader.GetNextEntry();
                        Assert.NotNull(entry);
                        Assert.Equal(size, entry.Length);
                        Assert.Equal("large-file-async.bin", entry.Name);

                        Stream dataStream = entry.DataStream;
                        Assert.NotNull(dataStream);
                        Assert.Equal(size, dataStream.Length);
                        Assert.Equal(0, dataStream.Position);

                        ReadOnlyMemory<byte> dummyData = SimulatedDataStream.DummyData;

                        // Read and verify the first bytes
                        Memory<byte> buffer = new byte[dummyData.Length];
                        Assert.Equal(buffer.Length, isAsync ? await dataStream.ReadAsync(buffer) : dataStream.Read(buffer.Span));
                        AssertExtensions.SequenceEqual(dummyData.Span, buffer.Span);

                        // check next byte is correct
                        if (isAsync)
                        {
                            buffer.Span.Clear();
                            Assert.Equal(1, await dataStream.ReadAsync(buffer.Slice(0, 1)));
                        }
                        else
                        {
                            buffer.Span.Clear();
                            Assert.Equal(1, dataStream.Read(buffer.Span.Slice(0, 1)));
                        }
                        Assert.Equal(0, buffer.Span[0]);
                        buffer.Span.Clear();

                        // Skip to near the end and verify the last bytes
                        long dummyDataOffset = size - dummyData.Length - 1;
                        Memory<byte> skipBuffer = new byte[4096];
                        while (dataStream.Position < dummyDataOffset)
                        {
                            int bufSize = (int)Math.Min(skipBuffer.Length, dummyDataOffset - dataStream.Position);
                            int bytesRead = isAsync ? await dataStream.ReadAsync(skipBuffer.Slice(0, bufSize)) : dataStream.Read(skipBuffer.Span.Slice(0, bufSize));
                            Assert.True(bytesRead > 0, "Stream ended before expected");
                        }

                        Assert.Equal(0, dataStream.ReadByte()); // check previous byte is correct
                        Assert.Equal(buffer.Length, isAsync ? await dataStream.ReadAsync(buffer) : dataStream.Read(buffer.Span));
                        AssertExtensions.SequenceEqual(dummyData.Span, buffer.Span);
                        Assert.Equal(size, dataStream.Position);

                        Assert.Null(isAsync ? await tarReader.GetNextEntryAsync() : tarReader.GetNextEntry());
                    }
                }
            }
            catch (Exception ex)
            {
                readException = ex;
            }

            // Wait for the write task to complete
            await writeTask;

            // Check for exceptions
            if (writeException != null)
            {
                throw new Exception("Write task failed", writeException);
            }
            if (readException != null)
            {
                throw readException;
            }
        }
    }
}
