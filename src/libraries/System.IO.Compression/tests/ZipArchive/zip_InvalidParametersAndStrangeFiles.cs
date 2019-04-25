// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_InvalidParametersAndStrangeFiles : ZipFileTestBase
    {
        private static void ConstructorThrows<TException>(Func<ZipArchive> constructor, string Message) where TException : Exception
        {
            try
            {
                Assert.Throws<TException>(() =>
                {
                    using (ZipArchive archive = constructor()) { }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("{0}: {1}", Message, e.ToString()));
                throw;
            }
        }

        [Fact]
        public static async Task InvalidInstanceMethods()
        {
            Stream zipFile = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Update))
            {
                //non-existent entry
                Assert.True(null == archive.GetEntry("nonExistentEntry")); //"Should return null on non-existent entry name"
                //null/empty string
                Assert.Throws<ArgumentNullException>(() => archive.GetEntry(null)); //"Should throw on null entry name"

                ZipArchiveEntry entry = archive.GetEntry("first.txt");

                //null/empty string
                AssertExtensions.Throws<ArgumentException>("entryName", () => archive.CreateEntry("")); //"Should throw on empty entry name"
                Assert.Throws<ArgumentNullException>(() => archive.CreateEntry(null)); //"should throw on null entry name"
            }
        }

        [Fact]
        public static void InvalidConstructors()
        {
            //out of range enum values
            ConstructorThrows<ArgumentOutOfRangeException>(() =>
                new ZipArchive(new MemoryStream(), (ZipArchiveMode)(-1)), "Out of range enum");
            ConstructorThrows<ArgumentOutOfRangeException>(() =>
                new ZipArchive(new MemoryStream(), (ZipArchiveMode)(4)), "out of range enum");
            ConstructorThrows<ArgumentOutOfRangeException>(() =>
                new ZipArchive(new MemoryStream(), (ZipArchiveMode)(10)), "Out of range enum");

            //null/closed stream
            ConstructorThrows<ArgumentNullException>(() =>
                new ZipArchive((Stream)null, ZipArchiveMode.Read), "Null/closed stream");
            ConstructorThrows<ArgumentNullException>(() =>
                new ZipArchive((Stream)null, ZipArchiveMode.Create), "Null/closed stream");
            ConstructorThrows<ArgumentNullException>(() =>
                new ZipArchive((Stream)null, ZipArchiveMode.Update), "Null/closed stream");

            MemoryStream ms = new MemoryStream();
            ms.Dispose();

            ConstructorThrows<ArgumentException>(() =>
                new ZipArchive(ms, ZipArchiveMode.Read), "Disposed Base Stream");
            ConstructorThrows<ArgumentException>(() =>
                new ZipArchive(ms, ZipArchiveMode.Create), "Disposed Base Stream");
            ConstructorThrows<ArgumentException>(() =>
                new ZipArchive(ms, ZipArchiveMode.Update), "Disposed Base Stream");

            //non-seekable to update
            using (LocalMemoryStream nonReadable = new LocalMemoryStream(),
                nonWriteable = new LocalMemoryStream(),
                nonSeekable = new LocalMemoryStream())
            {
                nonReadable.SetCanRead(false);
                nonWriteable.SetCanWrite(false);
                nonSeekable.SetCanSeek(false);

                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonReadable, ZipArchiveMode.Read), "Non readable stream");

                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonWriteable, ZipArchiveMode.Create), "Non-writable stream");

                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonReadable, ZipArchiveMode.Update), "Non-readable stream");
                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonWriteable, ZipArchiveMode.Update), "Non-writable stream");
                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonSeekable, ZipArchiveMode.Update), "Non-seekable stream");
            }
        }

        [Theory]
        [InlineData("LZMA.zip")]
        [InlineData("invalidDeflate.zip")]
        public static async Task ZipArchiveEntry_InvalidUpdate(string zipname)
        {
            string filename = bad(zipname);
            Stream updatedCopy = await StreamHelpers.CreateTempCopyStream(filename);
            string name;
            long length, compressedLength;
            DateTimeOffset lastWriteTime;
            using (ZipArchive archive = new ZipArchive(updatedCopy, ZipArchiveMode.Update, true))
            {
                ZipArchiveEntry e = archive.Entries[0];
                name = e.FullName;
                lastWriteTime = e.LastWriteTime;
                length = e.Length;
                compressedLength = e.CompressedLength;
                Assert.Throws<InvalidDataException>(() => e.Open()); //"Should throw on open"
            }

            //make sure that update mode preserves that unreadable file
            using (ZipArchive archive = new ZipArchive(updatedCopy, ZipArchiveMode.Update))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Equal(name, e.FullName); //"Name isn't the same"
                Assert.Equal(lastWriteTime, e.LastWriteTime); //"LastWriteTime not the same"
                Assert.Equal(length, e.Length); //"Length isn't the same"
                Assert.Equal(compressedLength, e.CompressedLength); //"CompressedLength isn't the same"
                Assert.Throws<InvalidDataException>(() => e.Open()); //"Should throw on open"
            }
        }

        [Theory]
        [InlineData("CDoffsetOutOfBounds.zip")]
        [InlineData("EOCDmissing.zip")]
        public static async Task ZipArchive_InvalidStream(string zipname)
        {
            string filename = bad(zipname);
            using (var stream = await StreamHelpers.CreateTempCopyStream(filename))
                Assert.Throws<InvalidDataException>(() => new ZipArchive(stream, ZipArchiveMode.Read));
        }

        [Theory]
        [InlineData("CDoffsetInBoundsWrong.zip")]
        [InlineData("numberOfEntriesDifferent.zip")]
        public static async Task ZipArchive_InvalidEntryTable(string zipname)
        {
            string filename = bad(zipname);
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(filename), ZipArchiveMode.Read))
                Assert.Throws<InvalidDataException>(() => archive.Entries[0]);
        }

        [Theory]
        [InlineData("compressedSizeOutOfBounds.zip", true)]
        [InlineData("localFileHeaderSignatureWrong.zip", true)]
        [InlineData("localFileOffsetOutOfBounds.zip", true)]
        [InlineData("LZMA.zip", true)]
        [InlineData("invalidDeflate.zip", false)]
        public static async Task ZipArchive_InvalidEntry(string zipname, bool throwsOnOpen)
        {
            string filename = bad(zipname);
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(filename), ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.Entries[0];
                if (throwsOnOpen)
                {
                    Assert.Throws<InvalidDataException>(() => e.Open()); //"should throw on open"
                }
                else
                {
                    using (Stream s = e.Open())
                    {
                        Assert.Throws<InvalidDataException>(() => s.ReadByte()); //"Unreadable stream"
                    }
                }
            }
        }

        [Fact]
        public static async Task ZipArchiveEntry_InvalidLastWriteTime_Read()
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(
                 bad("invaliddate.zip")), ZipArchiveMode.Read))
            {
                Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime); //"Date isn't correct on invalid date"
            }
        }

        [Fact]
        public static void ZipArchiveEntry_InvalidLastWriteTime_Write()
        {
            using (ZipArchive archive = new ZipArchive(new MemoryStream(), ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry("test");
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    //"should throw on bad date"
                    entry.LastWriteTime = new DateTimeOffset(1979, 12, 3, 5, 6, 2, new TimeSpan());
                });
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    //"Should throw on bad date"
                    entry.LastWriteTime = new DateTimeOffset(2980, 12, 3, 5, 6, 2, new TimeSpan());
                });
            }
        }

        [Theory]
        [InlineData("extradata/extraDataLHandCDentryAndArchiveComments.zip", "verysmall", true)]
        [InlineData("extradata/extraDataThenZip64.zip", "verysmall", true)]
        [InlineData("extradata/zip64ThenExtraData.zip", "verysmall", true)]
        [InlineData("dataDescriptor.zip", "normalWithoutBinary", false)]
        [InlineData("filenameTimeAndSizesDifferentInLH.zip", "verysmall", false)]
        public static async Task StrangeFiles(string zipFile, string zipFolder, bool requireExplicit)
        {
            IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(strange(zipFile)), zfolder(zipFolder), ZipArchiveMode.Update, requireExplicit, checkTimes: true);
        }

        /// <summary>
        /// This test tiptoes the buffer boundaries to ensure that the size of a read buffer doesn't
        /// cause any bytes to be left in ZLib's buffer.
        /// </summary>
        [Fact]
        public static void ZipWithLargeSparseFile()
        {
            string zipname = strange("largetrailingwhitespacedeflation.zip");
            string entryname = "A/B/C/D";
            using (FileStream stream = File.Open(zipname, FileMode.Open, FileAccess.Read))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryname);
                long size = entry.Length;

                for (int bufferSize = 1; bufferSize <= size; bufferSize++)
                {
                    using (Stream entryStream = entry.Open())
                    {
                        byte[] b = new byte[bufferSize];
                        int read = 0, count = 0;
                        while ((read = entryStream.Read(b, 0, bufferSize)) > 0)
                        {
                            count += read;
                        }
                        Assert.Equal(size, count);
                    }
                }
            }
        }

        private static readonly byte[] s_emptyFileCompressedWithEtx =
        {
            0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00, 0x08, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x78, 0x6C,
            0x2F, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x6D, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32,
            0x2E, 0x62, 0x69, 0x6E, 0x03, 0x00, 0x50, 0x4B, 0x01, 0x02, 0x14, 0x00, 0x14, 0x00, 0x06, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x78, 0x6C, 0x2F, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x6D, 0x50, 0x72, 0x6F,
            0x70, 0x65, 0x72, 0x74, 0x79, 0x32, 0x2E, 0x62, 0x69, 0x6E, 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x44, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        private static readonly byte[] s_emptyFileCompressedWrongSize =
        {
            0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00, 0x08, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x78, 0x6C,
            0x2F, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x6D, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32,
            0x2E, 0x62, 0x69, 0x6E, 0xBA, 0xAD, 0x03, 0x00, 0x50, 0x4B, 0x01, 0x02, 0x14, 0x00, 0x14, 0x00,
            0x06, 0x00, 0x08, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x78, 0x6C, 0x2F, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x6D, 0x50,
            0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32, 0x2E, 0x62, 0x69, 0x6E, 0x50, 0x4B, 0x05, 0x06,
            0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x44, 0x00, 0x00, 0x00, 0x38, 0x00, 0x00, 0x00,
            0x00, 0x00
        };
        public static IEnumerable<object[]> EmptyFiles = new List<object[]>()
        {
            new object[] { s_emptyFileCompressedWithEtx },
            new object[] { s_emptyFileCompressedWrongSize }
        };

        /// <summary>
        /// This test checks behavior of ZipArchive with unexpected zip files:
        /// 1. EmptyFileCompressedWithEOT has 
        /// Deflate 0x08, _uncompressedSize 0, _compressedSize 2, compressed data: 0x0300 (\u0003 ETX)
        /// 2. EmptyFileCompressedWrongSize has 
        /// Deflate 0x08, _uncompressedSize 0, _compressedSize 4, compressed data: 0xBAAD0300 (just bad data)
        /// ZipArchive is expected to change compression method to Stored (0x00) and ignore "bad" compressed size
        /// </summary>
        [Theory]
        [MemberData(nameof(EmptyFiles))]
        public void ReadArchive_WithEmptyDeflatedFile(byte[] fileBytes)
        {
            using (var testStream = new MemoryStream(fileBytes))
            {
                const string ExpectedFileName = "xl/customProperty2.bin";
                // open archive with zero-length file that is compressed (Deflate = 0x8)
                using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true))
                {
                    // dispose without making any changes will rewrite the archive
                }

                byte[] fileContent = testStream.ToArray();

                // compression method should change to "uncompressed" (Stored = 0x0)
                Assert.Equal(0, fileContent[8]);

                // extract and check the file. should stay empty.
                using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry entry = zip.GetEntry(ExpectedFileName);
                    Assert.Equal(0, entry.Length);
                    Assert.Equal(0, entry.CompressedLength);
                    using (Stream entryStream = entry.Open())
                    {
                        Assert.Equal(0, entryStream.Length);
                    }
                }
            }
        }
    }
}

