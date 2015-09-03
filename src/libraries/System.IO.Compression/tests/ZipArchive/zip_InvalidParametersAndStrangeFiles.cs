// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_InvalidParametersAndStrangeFiles
    {
        private static void ConstructorThrows<TException>(Func<ZipArchive> constructor, String Message) where TException : Exception
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
            Stream zipFile = await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("normal.zip"));
            using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Update))
            {
                //non-existent entry
                Assert.True(null == archive.GetEntry("nonExistentEntry")); //"Should return null on non-existent entry name"
                //null/empty string
                Assert.Throws<ArgumentNullException>(() => archive.GetEntry(null)); //"Should throw on null entry name"

                ZipArchiveEntry entry = archive.GetEntry("first.txt");

                //null/empty string
                Assert.Throws<ArgumentException>(() => archive.CreateEntry("")); //"Should throw on empty entry name"
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

                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonWriteable, ZipArchiveMode.Create), "Nonwritable stream");

                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonReadable, ZipArchiveMode.Update), "Non-readable stream");
                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonWriteable, ZipArchiveMode.Update), "Nonwritable stream");
                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonSeekable, ZipArchiveMode.Update), "Non-seekable stream");
            }
        }

        [Fact]
        public static async Task UnsupportedCompression()
        {
            //lzma compression method
            await UnsupportedCompressionRoutine(ZipTest.bad("LZMA.zip"), true);

            await UnsupportedCompressionRoutine(ZipTest.bad("invalidDeflate.zip"), false);
        }

        private static async Task UnsupportedCompressionRoutine(String filename, Boolean throwsOnOpen)
        {
            // using (ZipArchive archive = ZipFile.OpenRead(filename))
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

            Stream updatedCopy = await StreamHelpers.CreateTempCopyStream(filename);
            String name;
            Int64 length, compressedLength;
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
        [Fact]
        public static async Task InvalidDates()
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(
                 ZipTest.bad("invaliddate.zip")), ZipArchiveMode.Read))
            {
                Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime); //"Date isn't correct on invalid date"
            }

            using (ZipArchive archive = new ZipArchive(new MemoryStream(), ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry("test");
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                { entry.LastWriteTime = new DateTimeOffset(1979, 12, 3, 5, 6, 2, new TimeSpan()); }); //"should throw on bad date"
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                { entry.LastWriteTime = new DateTimeOffset(2980, 12, 3, 5, 6, 2, new TimeSpan()); }); //"Should throw on bad date"
            }
        }

        [Fact]
        public static async Task StrangeFiles1()
        {
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                 ZipTest.strange(Path.Combine("extradata", "extraDataLHandCDentryAndArchiveComments.zip"))), ZipTest.zfolder("verysmall"), ZipArchiveMode.Update, false, false);
        }

        [Fact]
        public static async Task StrangeFiles2()
        {
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                 ZipTest.strange(Path.Combine("extradata", "extraDataThenZip64.zip"))), ZipTest.zfolder("verysmall"), ZipArchiveMode.Update, false, false);
        }

        [Fact]
        public static async Task StrangeFiles3()
        {
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                 ZipTest.strange(Path.Combine("extradata", "zip64ThenExtraData.zip"))), ZipTest.zfolder("verysmall"), ZipArchiveMode.Update, false, false);
        }

        [Fact]
        [ActiveIssue(1904, PlatformID.AnyUnix)]
        public static async Task StrangeFiles4()
        {
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                 ZipTest.strange("dataDescriptor.zip")), ZipTest.zfolder("normalWithoutBinary"), ZipArchiveMode.Update, true, false);
	    }

        [Fact]
        public static async Task StrangeFiles5()
        {
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                 ZipTest.strange("filenameTimeAndSizesDifferentInLH.zip")), ZipTest.zfolder("verysmall"), ZipArchiveMode.Update, true, false);
        }
    }
}

