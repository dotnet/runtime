// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarWriter_Tests : TarTestsBase
    {
        [Fact]
        public void Constructors_NullStream()
        {
            Assert.Throws<ArgumentNullException>(() => new TarWriter(archiveStream: null));
            Assert.Throws<ArgumentNullException>(() => new TarWriter(archiveStream: null, TarFormat.V7));
        }

        [Fact]
        public void Constructors_LeaveOpen()
        {
            using MemoryStream archiveStream = new MemoryStream();

            TarWriter writer1 = new TarWriter(archiveStream, leaveOpen: true);
            writer1.Dispose();
            archiveStream.WriteByte(0); // Should succeed because stream was not closed

            TarWriter writer2 = new TarWriter(archiveStream, leaveOpen: false);
            writer2.Dispose();
            Assert.Throws<ObjectDisposedException>(() => archiveStream.WriteByte(0)); // Should fail because stream was closed
        }

        [Fact]
        public void Constructor_Format()
        {
            using MemoryStream archiveStream = new MemoryStream();

            using TarWriter writerDefault = new TarWriter(archiveStream, leaveOpen: true);
            Assert.Equal(TarFormat.Pax, writerDefault.Format);

            using TarWriter writerV7 = new TarWriter(archiveStream, TarFormat.V7, leaveOpen: true);
            Assert.Equal(TarFormat.V7, writerV7.Format);

            using TarWriter writerUstar = new TarWriter(archiveStream, TarFormat.Ustar, leaveOpen: true);
            Assert.Equal(TarFormat.Ustar, writerUstar.Format);

            using TarWriter writerPax = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true);
            Assert.Equal(TarFormat.Pax, writerPax.Format);

            using TarWriter writerGnu = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true);
            Assert.Equal(TarFormat.Gnu, writerGnu.Format);

            using TarWriter writerNullGeaDefaultPax = new TarWriter(archiveStream, leaveOpen: true, globalExtendedAttributes: null);
            Assert.Equal(TarFormat.Pax, writerNullGeaDefaultPax.Format);

            using TarWriter writerValidGeaDefaultPax = new TarWriter(archiveStream, leaveOpen: true, globalExtendedAttributes: new Dictionary<string, string>());
            Assert.Equal(TarFormat.Pax, writerValidGeaDefaultPax.Format);

            Assert.Throws<ArgumentOutOfRangeException>(() => new TarWriter(archiveStream, TarFormat.Unknown));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TarWriter(archiveStream, (TarFormat)int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TarWriter(archiveStream, (TarFormat)int.MaxValue));
        }

        [Fact]
        public void Constructors_UnwritableStream_Throws()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using WrappedStream wrappedStream = new WrappedStream(archiveStream, canRead: true, canWrite: false, canSeek: false);
            Assert.Throws<IOException>(() => new TarWriter(wrappedStream));
            Assert.Throws<IOException>(() => new TarWriter(wrappedStream, TarFormat.V7));
        }

        [Fact]
        public void Constructor_NoEntryInsertion_WritesNothing()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, leaveOpen: true);
            writer.Dispose(); // No entries inserted, should write no empty records
            Assert.Equal(0, archiveStream.Length);
        }

        [Fact]
        public void VerifyChecksumV7()
        {
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarFormat.V7, leaveOpen: true))
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
                entry.DataStream.Write(buffer); // Data length: decimal 5
                entry.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning

                // Sum so far: 0 + 241 + 351 + 353 + 359 + 571 + 533 = decimal 2408
                // Add 8 spaces to the sum: 2408 + (8 x 32) = octal 5150, decimal 2664 (final)
                // Checksum: '005150\0 '

                writer.WriteEntry(entry);

                Assert.Equal(2664, entry.Checksum);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(2664, entry.Checksum);
            }
        }
    }
}
