// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_GetNextEntryAsync_Tests : TarTestsBase
    {
        [Fact]
        public async Task GetNextEntryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            await using (MemoryStream archiveStream = new MemoryStream())
            await using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
            {
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await reader.GetNextEntryAsync(copyData: false, cs.Token));
            }
        }


        [Fact]
        public async Task MalformedArchive_TooSmall_Async()
        {
            await using (MemoryStream malformed = new MemoryStream())
            {
                byte[] buffer = new byte[] { 0x1 };
                malformed.Write(buffer);
                malformed.Seek(0, SeekOrigin.Begin);

                await using (TarReader reader = new TarReader(malformed))
                {
                    await Assert.ThrowsAsync<EndOfStreamException>(async () => await reader.GetNextEntryAsync());
                }
            }
        }

        [Fact]
        public async Task MalformedArchive_HeaderSize_Async()
        {
            await using (MemoryStream malformed = new MemoryStream())
            {
                byte[] buffer = new byte[512]; // Minimum length of any header
                Array.Fill<byte>(buffer, 0x1);
                malformed.Write(buffer);
                malformed.Seek(0, SeekOrigin.Begin);

                await using (TarReader reader = new TarReader(malformed))
                {
                    await Assert.ThrowsAsync<FormatException>(async () => await reader.GetNextEntryAsync());
                }
            }
        }

        [Fact]
        public async Task EmptyArchive_Async()
        {
            await using (MemoryStream empty = new MemoryStream())
            await using (TarReader reader = new TarReader(empty))
            {
                Assert.Null(await reader.GetNextEntryAsync());
            }
        }


        [Fact]
        public async Task LongEndMarkers_DoNotAdvanceStream_Async()
        {
            await using (MemoryStream archive = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    UstarTarEntry entry = new UstarTarEntry(TarEntryType.Directory, "dir");
                    await writer.WriteEntryAsync(entry);
                }

                byte[] buffer = new byte[2048]; // Four additional end markers (512 each)
                Array.Fill<byte>(buffer, 0x0);
                archive.Write(buffer);
                archive.Seek(0, SeekOrigin.Begin);

                await using (TarReader reader = new TarReader(archive))
                {
                    Assert.NotNull(await reader.GetNextEntryAsync());
                    Assert.Null(await reader.GetNextEntryAsync());
                    long expectedPosition = archive.Position; // After reading the first null entry, should not advance more
                    Assert.Null(await reader.GetNextEntryAsync());
                    Assert.Equal(expectedPosition, archive.Position);
                }
            }
        }

        [Fact]
        public async Task GetNextEntry_CopyDataTrue_SeekableArchive_Async()
        {
            string expectedText = "Hello world!";
            await using (MemoryStream archive = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                    entry1.DataStream = new MemoryStream();
                    using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                    {
                        streamWriter.WriteLine(expectedText);
                    }
                    entry1.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning
                    await writer.WriteEntryAsync(entry1);

                    UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                    await writer.WriteEntryAsync(entry2);
                }

                archive.Seek(0, SeekOrigin.Begin);

                UstarTarEntry entry;
                await using (TarReader reader = new TarReader(archive)) // Seekable
                {
                    entry = await reader.GetNextEntryAsync(copyData: true) as UstarTarEntry;
                    Assert.NotNull(entry);
                    Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                    // Force reading the next entry to advance the underlying stream position
                    Assert.NotNull(await reader.GetNextEntryAsync());
                    Assert.Null(await reader.GetNextEntryAsync());

                    entry.DataStream.Seek(0, SeekOrigin.Begin); // Should not throw: This is a new stream, not the archive's disposed stream
                    using (StreamReader streamReader = new StreamReader(entry.DataStream))
                    {
                        string actualText = streamReader.ReadLine();
                        Assert.Equal(expectedText, actualText);
                    }

                }

                // The reader must stay alive because it's in charge of disposing all the entries it collected
                Assert.Throws<ObjectDisposedException>(() => entry.DataStream.Read(new byte[1]));
            }
        }

        [Fact]
        public async Task GetNextEntry_CopyDataTrue_UnseekableArchive_Async()
        {
            string expectedText = "Hello world!";
            await using (MemoryStream archive = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                    entry1.DataStream = new MemoryStream();
                    using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                    {
                        streamWriter.WriteLine(expectedText);
                    }
                    entry1.DataStream.Seek(0, SeekOrigin.Begin);
                    await writer.WriteEntryAsync(entry1);

                    UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                    await writer.WriteEntryAsync(entry2);
                }

                archive.Seek(0, SeekOrigin.Begin);
                await using (WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false))
                {
                    UstarTarEntry entry;
                    await using (TarReader reader = new TarReader(wrapped, leaveOpen: true)) // Unseekable
                    {
                        entry = await reader.GetNextEntryAsync(copyData: true) as UstarTarEntry;
                        Assert.NotNull(entry);
                        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                        // Force reading the next entry to advance the underlying stream position
                        Assert.NotNull(await reader.GetNextEntryAsync());
                        Assert.Null(await reader.GetNextEntryAsync());

                        Assert.NotNull(entry.DataStream);
                        entry.DataStream.Seek(0, SeekOrigin.Begin); // Should not throw: This is a new stream, not the archive's disposed stream
                        using (StreamReader streamReader = new StreamReader(entry.DataStream))
                        {
                            string actualText = streamReader.ReadLine();
                            Assert.Equal(expectedText, actualText);
                        }
                    }
                    // The reader must stay alive because it's in charge of disposing all the entries it collected
                    Assert.Throws<ObjectDisposedException>(() => entry.DataStream.Read(new byte[1]));
                }
            }
        }

        [Fact]
        public async Task GetNextEntry_CopyDataFalse_UnseekableArchive_Exceptions_Async()
        {
            await using (MemoryStream archive = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                    entry1.DataStream = new MemoryStream();
                    using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                    {
                        streamWriter.WriteLine("Hello world!");
                    }
                    entry1.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning
                    await writer.WriteEntryAsync(entry1);

                    UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                    await writer.WriteEntryAsync(entry2);
                }

                archive.Seek(0, SeekOrigin.Begin);
                await using (WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false))
                {
                    UstarTarEntry entry;
                    await using (TarReader reader = new TarReader(wrapped)) // Unseekable
                    {
                        entry = await reader.GetNextEntryAsync(copyData: false) as UstarTarEntry;
                        Assert.NotNull(entry);
                        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
                        entry.DataStream.ReadByte(); // Reading is possible as long as we don't move to the next entry

                        // Attempting to read the next entry should automatically move the position pointer to the beginning of the next header
                        Assert.NotNull(await reader.GetNextEntryAsync());
                        Assert.Null(await reader.GetNextEntryAsync());

                        // This is not possible because the position of the main stream is already past the data
                        Assert.Throws<EndOfStreamException>(() => entry.DataStream.Read(new byte[1]));
                    }

                    // The reader must stay alive because it's in charge of disposing all the entries it collected
                    Assert.Throws<ObjectDisposedException>(() => entry.DataStream.Read(new byte[1]));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetNextEntry_UnseekableArchive_ReplaceDataStream_ExcludeFromDisposing_Async(bool copyData)
        {
            await using (MemoryStream archive = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                    entry1.DataStream = new MemoryStream();
                    using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                    {
                        streamWriter.WriteLine("Hello world!");
                    }
                    entry1.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning
                    await writer.WriteEntryAsync(entry1);

                    UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                    await writer.WriteEntryAsync(entry2);
                }

                archive.Seek(0, SeekOrigin.Begin);
                await using (WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false))
                {
                    UstarTarEntry entry;
                    Stream oldStream;
                    await using (TarReader reader = new TarReader(wrapped)) // Unseekable
                    {
                        entry = await reader.GetNextEntryAsync(copyData) as UstarTarEntry;
                        Assert.NotNull(entry);
                        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                        oldStream = entry.DataStream;

                        entry.DataStream = new MemoryStream(); // Substitution, setter should dispose the previous stream
                        using (StreamWriter streamWriter = new StreamWriter(entry.DataStream, leaveOpen: true))
                        {
                            streamWriter.WriteLine("Substituted");
                        }
                    } // Disposing reader should not dispose the substituted DataStream

                    Assert.Throws<ObjectDisposedException>(() => oldStream.Read(new byte[1]));

                    entry.DataStream.Seek(0, SeekOrigin.Begin);
                    using (StreamReader streamReader = new StreamReader(entry.DataStream))
                    {
                        Assert.Equal("Substituted", streamReader.ReadLine());
                    }
                }
            }
        }
    }
}
