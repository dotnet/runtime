// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class ZipTest
    {
        private static void ConstructorThrows<TException>(Func<ZipArchive> constructor, string Message = "") where TException : Exception
        {
            Assert.Throws<TException>(() =>
            {
                using (ZipArchive archive = constructor()) { }
            });
        }

        [Fact]
        public void InvalidInstanceMethods()
        {
            string zipFileName = CreateTempCopyFile(zfile("normal.zip"));
            using (ZipArchive archive = ZipFile.Open(zipFileName, ZipArchiveMode.Update))
            {
                //non-existent entry
                Assert.True(null == archive.GetEntry("nonExistentEntry"));
                //null/empty string
                Assert.Throws<ArgumentNullException>(() => archive.GetEntry(null));

                ZipArchiveEntry entry = archive.GetEntry("first.txt");

                //null/empty string
                Assert.Throws<ArgumentException>(() => archive.CreateEntry(""));
                Assert.Throws<ArgumentNullException>(() => archive.CreateEntry(null));
            }
        }

        [Fact]
        public void InvalidConstructors()
        {
            //out of range enum values
            ConstructorThrows<ArgumentOutOfRangeException>(() =>
              ZipFile.Open("bad file", (ZipArchiveMode)(10)));
        }

        [Fact]
        public void InvalidFiles()
        {
            ConstructorThrows<InvalidDataException>(() => ZipFile.OpenRead(bad("EOCDmissing.zip")));
            ConstructorThrows<InvalidDataException>(() => ZipFile.Open(bad("EOCDmissing.zip"), ZipArchiveMode.Update));
            ConstructorThrows<InvalidDataException>(() => ZipFile.OpenRead(bad("CDoffsetOutOfBounds.zip")));
            ConstructorThrows<InvalidDataException>(() => ZipFile.Open(bad("CDoffsetOutOfBounds.zip"), ZipArchiveMode.Update));

            using (ZipArchive archive = ZipFile.OpenRead(bad("CDoffsetInBoundsWrong.zip")))
            {
                Assert.Throws<InvalidDataException>(() => { var x = archive.Entries; });
            }
            ConstructorThrows<InvalidDataException>(() => ZipFile.Open(bad("CDoffsetInBoundsWrong.zip"), ZipArchiveMode.Update));

            using (ZipArchive archive = ZipFile.OpenRead(bad("numberOfEntriesDifferent.zip")))
            {
                Assert.Throws<InvalidDataException>(() => { var x = archive.Entries; });
            }
            ConstructorThrows<InvalidDataException>(() => ZipFile.Open(bad("numberOfEntriesDifferent.zip"), ZipArchiveMode.Update));

            //read mode on empty file
            ConstructorThrows<InvalidDataException>(() => new ZipArchive(new MemoryStream()));

            //offset out of bounds
            using (ZipArchive archive = ZipFile.OpenRead(bad("localFileOffsetOutOfBounds.zip")))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Throws<InvalidDataException>(() => e.Open());
            }

            ConstructorThrows<InvalidDataException>(() =>
                ZipFile.Open(bad("localFileOffsetOutOfBounds.zip"), ZipArchiveMode.Update));

            //compressed data offset + compressed size out of bounds
            using (ZipArchive archive = ZipFile.OpenRead(bad("compressedSizeOutOfBounds.zip")))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Throws<InvalidDataException>(() => e.Open());
            }

            ConstructorThrows<InvalidDataException>(() =>
                ZipFile.Open(bad("compressedSizeOutOfBounds.zip"), ZipArchiveMode.Update));

            //signature wrong
            using (ZipArchive archive = ZipFile.OpenRead(bad("localFileHeaderSignatureWrong.zip")))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Throws<InvalidDataException>(() => e.Open());
            }

            ConstructorThrows<InvalidDataException>(() =>
                ZipFile.Open(bad("localFileHeaderSignatureWrong.zip"), ZipArchiveMode.Update));
        }

        [Fact]
        public void UnsupportedCompression()
        {
            //lzma compression method
            UnsupportedCompressionRoutine(bad("LZMA.zip"), true);

            UnsupportedCompressionRoutine(bad("invalidDeflate.zip"), false);
        }

        private void UnsupportedCompressionRoutine(string filename, Boolean throwsOnOpen)
        {
            using (ZipArchive archive = ZipFile.OpenRead(filename))
            {
                ZipArchiveEntry e = archive.Entries[0];
                if (throwsOnOpen)
                {
                    Assert.Throws<InvalidDataException>(() => e.Open());
                }
                else
                {
                    using (Stream s = e.Open())
                    {
                        Assert.Throws<InvalidDataException>(() => s.ReadByte());
                    }
                }
            }

            string updatedCopyName = CreateTempCopyFile(filename);
            string name;
            Int64 length, compressedLength;
            DateTimeOffset lastWriteTime;
            using (ZipArchive archive = ZipFile.Open(updatedCopyName, ZipArchiveMode.Update))
            {
                ZipArchiveEntry e = archive.Entries[0];
                name = e.FullName;
                lastWriteTime = e.LastWriteTime;
                length = e.Length;
                compressedLength = e.CompressedLength;
                Assert.Throws<InvalidDataException>(() => e.Open());
            }

            //make sure that update mode preserves that unreadable file
            using (ZipArchive archive = ZipFile.Open(updatedCopyName, ZipArchiveMode.Update))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Equal(name, e.FullName);
                Assert.Equal(lastWriteTime, e.LastWriteTime);
                Assert.Equal(length, e.Length);
                Assert.Equal(compressedLength, e.CompressedLength);
                Assert.Throws<InvalidDataException>(() => e.Open());
            }
        }

        [Fact]
        public void InvalidDates()
        {
            using (ZipArchive archive = ZipFile.OpenRead(bad("invaliddate.zip")))
            {
                Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime);
            }

            FileInfo fileWithBadDate = new FileInfo(GetTmpFilePath());
            fileWithBadDate.Create().Dispose();
            fileWithBadDate.LastWriteTimeUtc = new DateTime(1970, 1, 1, 1, 1, 1);

            string archivePath = GetTmpFilePath();
            using (FileStream output = File.Open(archivePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(fileWithBadDate.FullName, "SomeEntryName");
            }
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime);
            }
        }

        [Fact]
        public void FilesOutsideDirectory()
        {
            string archivePath = GetTmpFilePath();
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            using (StreamWriter writer = new StreamWriter(archive.CreateEntry(Path.Combine("..", "entry1"), CompressionLevel.Optimal).Open()))
            {
                writer.Write("This is a test.");
            }
            Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(archivePath, GetTmpDirPath()));
        }

        [Fact]
        public void DirectoryEntryWithData()
        {
            string archivePath = GetTmpFilePath();
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            using (StreamWriter writer = new StreamWriter(archive.CreateEntry("testdir" + Path.DirectorySeparatorChar, CompressionLevel.Optimal).Open()))
            {
                writer.Write("This is a test.");
            }
            Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(archivePath, GetTmpDirPath()));
        }

    }
}
