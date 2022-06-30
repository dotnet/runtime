// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarWriter_Async_Tests : TarTestsBase
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

        [Fact]
        public async Task VerifyChecksumV7_Async()
        {
            await using (MemoryStream archive = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archive, TarEntryFormat.V7, leaveOpen: true))
                {
                    V7TarEntry entry = new V7TarEntry(
                        // '\0' = 0
                        TarEntryType.V7RegularFile,
                        // 'a.b' = 97 + 46 + 98 = 241
                        entryName: "a.b");

                    // '0000744\0' = 48 + 48 + 48 + 48 + 55 + 52 + 52 + 0 = 351
                    entry.Mode = AssetMode; // octal 744 = u+rxw, g+r, o+r

                    // '0017351\0' = 48 + 48 + 49 + 55 + 51 + 53 + 49 + 0 = 353
                    entry.Uid = AssetUid; // decimal 7913, octal 17351

                    // '0006773\0' = 48 + 48 + 48 + 54 + 55 + 55 + 51 + 0 = 359
                    entry.Gid = AssetGid; // decimal 3579, octal 6773

                    // '14164217674\0' = 49 + 52 + 49 + 54 + 52 + 50 + 49 + 55 + 54 + 55 + 52 + 0 = 571
                    DateTimeOffset mtime = new DateTimeOffset(2022, 1, 2, 3, 45, 00, TimeSpan.Zero); // ToUnixTimeSeconds() = decimal 1641095100, octal 14164217674
                    entry.ModificationTime = mtime;

                    entry.DataStream = new MemoryStream();
                    byte[] buffer = new byte[] { 72, 101, 108, 108, 111 };

                    // '0000000005\0' = 48 + 48 + 48 + 48 + 48 + 48 + 48 + 48 + 48 + 48 + 53 + 0 = 533
                    await entry.DataStream.WriteAsync(buffer); // Data length: decimal 5
                    entry.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning

                    // Sum so far: 0 + 241 + 351 + 353 + 359 + 571 + 533 = decimal 2408
                    // Add 8 spaces to the sum: 2408 + (8 x 32) = octal 5150, decimal 2664 (final)
                    // Checksum: '005150\0 '

                    await writer.WriteEntryAsync(entry);

                    Assert.Equal(2664, entry.Checksum);
                }

                archive.Seek(0, SeekOrigin.Begin);
                await using (TarReader reader = new TarReader(archive))
                {
                    TarEntry entry = await reader.GetNextEntryAsync();
                    Assert.Equal(2664, entry.Checksum);
                }
            }
        }
    }
}
