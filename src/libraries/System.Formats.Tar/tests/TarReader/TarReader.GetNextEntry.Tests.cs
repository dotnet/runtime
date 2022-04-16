// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_GetNextEntry_Tests : TarTestsBase
    {
        [Fact]
        public void GetNextEntry_CopyDataTrue_SeekableArchive()
        {
            string expectedText = "Hello world!";
            MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                entry1.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine(expectedText);
                }
                entry1.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning
                writer.WriteEntry(entry1);

                UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                writer.WriteEntry(entry2);
            }

            archive.Seek(0, SeekOrigin.Begin);

            UstarTarEntry entry;
            using (TarReader reader = new TarReader(archive)) // Seekable
            {
                entry = reader.GetNextEntry(copyData: true) as UstarTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                // Force reading the next entry to advance the underlying stream position
                Assert.NotNull(reader.GetNextEntry());
                Assert.Null(reader.GetNextEntry());

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

        [Fact]
        public void GetNextEntry_CopyDataTrue_UnseekableArchive()
        {
            string expectedText = "Hello world!";
            MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                entry1.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine(expectedText);
                }
                entry1.DataStream.Seek(0, SeekOrigin.Begin);
                writer.WriteEntry(entry1);

                UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                writer.WriteEntry(entry2);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false);

            UstarTarEntry entry;
            using (TarReader reader = new TarReader(wrapped, leaveOpen: true)) // Unseekable
            {
                entry = reader.GetNextEntry(copyData: true) as UstarTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                // Force reading the next entry to advance the underlying stream position
                Assert.NotNull(reader.GetNextEntry());
                Assert.Null(reader.GetNextEntry());

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

        [Fact]
        public void GetNextEntry_CopyDataFalse_UnseekableArchive_Exceptions()
        {
            MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                entry1.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine("Hello world!");
                }
                entry1.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning
                writer.WriteEntry(entry1);

                UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                writer.WriteEntry(entry2);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false);
            UstarTarEntry entry;
            using (TarReader reader = new TarReader(wrapped)) // Unseekable
            {
                entry = reader.GetNextEntry(copyData: false) as UstarTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
                entry.DataStream.ReadByte(); // Reading is possible as long as we don't move to the next entry

                // Attempting to read the next entry should automatically move the position pointer to the beginning of the next header
                Assert.NotNull(reader.GetNextEntry());
                Assert.Null(reader.GetNextEntry());

                // This is not possible because the position of the main stream is already past the data
                Assert.Throws<EndOfStreamException>(() => entry.DataStream.Read(new byte[1]));
            }

            // The reader must stay alive because it's in charge of disposing all the entries it collected
            Assert.Throws<ObjectDisposedException>(() => entry.DataStream.Read(new byte[1]));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetNextEntry_UnseekableArchive_ReplaceDataStream_ExcludeFromDisposing(bool copyData)
        {
            MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                entry1.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine("Hello world!");
                }
                entry1.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning
                writer.WriteEntry(entry1);

                UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                writer.WriteEntry(entry2);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false);
            UstarTarEntry entry;
            Stream oldStream;
            using (TarReader reader = new TarReader(wrapped)) // Unseekable
            {
                entry = reader.GetNextEntry(copyData) as UstarTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                oldStream = entry.DataStream;

                entry.DataStream = new MemoryStream(); // Substitution, setter should dispose the previous stream
                using(StreamWriter streamWriter = new StreamWriter(entry.DataStream, leaveOpen: true))
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
