// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_GetNextEntry_Tests : TarTestsBase
    {
        [Fact]
        public void MalformedArchive_TooSmall()
        {
            using MemoryStream malformed = new MemoryStream();
            byte[] buffer = new byte[] { 0x1 };
            malformed.Write(buffer);
            malformed.Seek(0, SeekOrigin.Begin);

            using TarReader reader = new TarReader(malformed);
            Assert.Throws<EndOfStreamException>(() => reader.GetNextEntry());
        }

        [Fact]
        public void MalformedArchive_HeaderSize()
        {
            using MemoryStream malformed = new MemoryStream();
            byte[] buffer = new byte[512]; // Minimum length of any header
            Array.Fill<byte>(buffer, 0x1);
            malformed.Write(buffer);
            malformed.Seek(0, SeekOrigin.Begin);

            using TarReader reader = new TarReader(malformed);
            Assert.Throws<InvalidDataException>(() => reader.GetNextEntry());
        }

        [Fact]
        public void EmptyArchive()
        {
            using MemoryStream empty = new MemoryStream();

            using TarReader reader = new TarReader(empty);
            Assert.Null(reader.GetNextEntry());
        }


        [Fact]
        public void LongEndMarkers_DoNotAdvanceStream()
        {
            using MemoryStream archive = new MemoryStream();

            using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry entry = new UstarTarEntry(TarEntryType.Directory, "dir");
                writer.WriteEntry(entry);
            }

            byte[] buffer = new byte[2048]; // Four additional end markers (512 each)
            Array.Fill<byte>(buffer, 0x0);
            archive.Write(buffer);
            archive.Seek(0, SeekOrigin.Begin);

            using TarReader reader = new TarReader(archive);
            Assert.NotNull(reader.GetNextEntry());
            Assert.Null(reader.GetNextEntry());
            long expectedPosition = archive.Position; // After reading the first null entry, should not advance more
            Assert.Null(reader.GetNextEntry());
            Assert.Equal(expectedPosition, archive.Position);
        }

        [Fact]
        public void GetNextEntry_CopyDataTrue_SeekableArchive()
        {
            string expectedText = "Hello world!";
            MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
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
            using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
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

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void GetNextEntry_CopyDataFalse_UnseekableArchive_Exceptions(TarEntryFormat format)
        {
            TarEntryType fileEntryType = GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format);
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                TarEntry entry1 = InvokeTarEntryCreationConstructor(format, fileEntryType, "file.txt");
                entry1.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine("Hello world!");
                }
                entry1.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning
                writer.WriteEntry(entry1);

                TarEntry entry2 = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");
                writer.WriteEntry(entry2);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false);
            TarEntry entry;
            byte[] b = new byte[1];
            using (TarReader reader = new TarReader(wrapped)) // Unseekable
            {
                entry = reader.GetNextEntry(copyData: false);
                Assert.NotNull(entry);
                Assert.Equal(fileEntryType, entry.EntryType);
                entry.DataStream.ReadByte(); // Reading is possible as long as we don't move to the next entry

                // Attempting to read the next entry should automatically move the position pointer to the beginning of the next header
                TarEntry entry2 = reader.GetNextEntry();
                Assert.NotNull(entry2);
                Assert.Equal(format, entry2.Format);
                Assert.Equal(TarEntryType.Directory, entry2.EntryType);
                Assert.Null(reader.GetNextEntry());

                // This is not possible because the position of the main stream is already past the data
                Assert.Throws<EndOfStreamException>(() => entry.DataStream.Read(b));
            }

            // The reader must stay alive because it's in charge of disposing all the entries it collected
            Assert.Throws<ObjectDisposedException>(() => entry.DataStream.Read(b));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetNextEntry_UnseekableArchive_ReplaceDataStream_ExcludeFromDisposing(bool copyData)
        {
            MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
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

        [Theory]
        [InlineData(512, false)]
        [InlineData(512, true)]
        [InlineData(512 + 1, false)]
        [InlineData(512 + 1, true)]
        [InlineData(512 + 512 - 1, false)]
        [InlineData(512 + 512 - 1, true)]
        public void BlockAlignmentPadding_DoesNotAffectNextEntries(int contentSize, bool copyData)
        {
            byte[] fileContents = new byte[contentSize];
            Array.Fill<byte>(fileContents, 0x1);

            using var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, leaveOpen: true))
            {
                var entry1 = new PaxTarEntry(TarEntryType.RegularFile, "file");
                entry1.DataStream = new MemoryStream(fileContents);
                writer.WriteEntry(entry1);

                var entry2 = new PaxTarEntry(TarEntryType.RegularFile, "next-file");
                writer.WriteEntry(entry2);
            }

            archive.Position = 0;
            using var unseekable = new WrappedStream(archive, archive.CanRead, archive.CanWrite, canSeek: false);
            using var reader = new TarReader(unseekable);

            TarEntry e = reader.GetNextEntry(copyData);
            Assert.Equal(contentSize, e.Length);

            byte[] buffer = new byte[contentSize];
            while (e.DataStream.Read(buffer) > 0) ;
            AssertExtensions.SequenceEqual(fileContents, buffer);

            e = reader.GetNextEntry(copyData);
            Assert.Equal(0, e.Length);

            e = reader.GetNextEntry(copyData);
            Assert.Null(e);
        }
    }
}
