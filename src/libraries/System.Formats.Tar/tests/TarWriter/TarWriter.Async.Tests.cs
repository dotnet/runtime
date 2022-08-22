// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_Tests : TarTestsBase
    {
        [Fact]
        public async Task Constructors_LeaveOpen_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                TarWriter writer1 = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
                await writer1.DisposeAsync();
                archiveStream.WriteByte(0); // Should succeed because stream was not closed

                TarWriter writer2 = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: false);
                await writer2.DisposeAsync();
                Assert.Throws<ObjectDisposedException>(() => archiveStream.WriteByte(0)); // Should fail because stream was closed
            }
        }

        [Fact]
        public async Task Constructor_NoEntryInsertion_WritesNothing_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
                await writer.DisposeAsync(); // No entries inserted, should write no empty records
                Assert.Equal(0, archiveStream.Length);
            }
        }

        [Fact]
        public async void Write_To_UnseekableStream_Async()
        {
            await using (MemoryStream inner = new MemoryStream())
            {
                await using (WrappedStream wrapped = new WrappedStream(inner, canRead: true, canWrite: true, canSeek: false))
                {
                    await using (TarWriter writer = new TarWriter(wrapped, TarEntryFormat.Pax, leaveOpen: true))
                    {
                        PaxTarEntry paxEntry = new PaxTarEntry(TarEntryType.RegularFile, "file.txt");
                        await writer.WriteEntryAsync(paxEntry);
                    } // The final records should get written, and the length should not be set because position cannot be read

                    inner.Seek(0, SeekOrigin.Begin); // Rewind the base stream (wrapped cannot be rewound)

                    await using (TarReader reader = new TarReader(wrapped))
                    {
                        TarEntry entry = await reader.GetNextEntryAsync();
                        Assert.Equal(TarEntryFormat.Pax, entry.Format);
                        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
                        Assert.Null(await reader.GetNextEntryAsync());
                    }
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task Verify_Checksum_RegularFile_Async(TarEntryFormat format) =>
            Verify_Checksum_Internal_Async(
                format,
                // Convert to V7RegularFile if format is V7
                GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format),
                longPath: false,
                longLink: false);

        [Theory] // V7 does not support BlockDevice
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task Verify_Checksum_BlockDevice_Async(TarEntryFormat format) =>
            Verify_Checksum_Internal_Async(format, TarEntryType.BlockDevice, longPath: false, longLink: false);

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task Verify_Checksum_Directory_LongPath_Async(TarEntryFormat format) =>
            Verify_Checksum_Internal_Async(format, TarEntryType.Directory, longPath: true, longLink: false);

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task Verify_Checksum_SymbolicLink_LongLink_Async(TarEntryFormat format) =>
            Verify_Checksum_Internal_Async(format, TarEntryType.SymbolicLink, longPath: false, longLink: true);

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task Verify_Checksum_SymbolicLink_LongLink_LongPath_Async(TarEntryFormat format) =>
            Verify_Checksum_Internal_Async(format, TarEntryType.SymbolicLink, longPath: true, longLink: true);

        private async Task Verify_Checksum_Internal_Async(TarEntryFormat format, TarEntryType entryType, bool longPath, bool longLink)
        {
            using MemoryStream archive = new MemoryStream();
            int expectedChecksum;
            await using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                TarEntry entry = CreateTarEntryAndGetExpectedChecksum(format, entryType, longPath, longLink, out expectedChecksum);
                await writer.WriteEntryAsync(entry);
                Assert.Equal(expectedChecksum, entry.Checksum);
            }

            archive.Seek(0, SeekOrigin.Begin);
            await using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry = await reader.GetNextEntryAsync();
                Assert.Equal(expectedChecksum, entry.Checksum);
            }
        }
    }
}
