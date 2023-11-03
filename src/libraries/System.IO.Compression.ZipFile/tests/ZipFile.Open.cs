// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests;

public class ZipFile_Open : ZipFileTestBase
{
    [Fact]
    public void InvalidConstructors()
    {
        //out of range enum values
        Assert.Throws<ArgumentOutOfRangeException>(() => ZipFile.Open("bad file", (ZipArchiveMode)(10)));
    }

    [Fact]
    public void InvalidFiles()
    {
        Assert.Throws<InvalidDataException>(() => ZipFile.OpenRead(bad("EOCDmissing.zip")));
        using (TempFile testArchive = CreateTempCopyFile(bad("EOCDmissing.zip"), GetTestFilePath()))
        {
            Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
        }

        Assert.Throws<InvalidDataException>(() => ZipFile.OpenRead(bad("CDoffsetOutOfBounds.zip")));
        using (TempFile testArchive = CreateTempCopyFile(bad("CDoffsetOutOfBounds.zip"), GetTestFilePath()))
        {
            Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
        }

        using (ZipArchive archive = ZipFile.OpenRead(bad("CDoffsetInBoundsWrong.zip")))
        {
            Assert.Throws<InvalidDataException>(() => { var x = archive.Entries; });
        }

        using (TempFile testArchive = CreateTempCopyFile(bad("CDoffsetInBoundsWrong.zip"), GetTestFilePath()))
        {
            Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
        }

        using (ZipArchive archive = ZipFile.OpenRead(bad("numberOfEntriesDifferent.zip")))
        {
            Assert.Throws<InvalidDataException>(() => { var x = archive.Entries; });
        }
        using (TempFile testArchive = CreateTempCopyFile(bad("numberOfEntriesDifferent.zip"), GetTestFilePath()))
        {
            Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
        }

        //read mode on empty file
        using (var memoryStream = new MemoryStream())
        {
            Assert.Throws<InvalidDataException>(() => new ZipArchive(memoryStream));
        }

        //offset out of bounds
        using (ZipArchive archive = ZipFile.OpenRead(bad("localFileOffsetOutOfBounds.zip")))
        {
            ZipArchiveEntry e = archive.Entries[0];
            Assert.Throws<InvalidDataException>(() => e.Open());
        }

        using (TempFile testArchive = CreateTempCopyFile(bad("localFileOffsetOutOfBounds.zip"), GetTestFilePath()))
        {
            Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
        }

        //compressed data offset + compressed size out of bounds
        using (ZipArchive archive = ZipFile.OpenRead(bad("compressedSizeOutOfBounds.zip")))
        {
            ZipArchiveEntry e = archive.Entries[0];
            Assert.Throws<InvalidDataException>(() => e.Open());
        }

        using (TempFile testArchive = CreateTempCopyFile(bad("compressedSizeOutOfBounds.zip"), GetTestFilePath()))
        {
            Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
        }

        //signature wrong
        using (ZipArchive archive = ZipFile.OpenRead(bad("localFileHeaderSignatureWrong.zip")))
        {
            ZipArchiveEntry e = archive.Entries[0];
            Assert.Throws<InvalidDataException>(() => e.Open());
        }

        using (TempFile testArchive = CreateTempCopyFile(bad("localFileHeaderSignatureWrong.zip"), GetTestFilePath()))
        {
            Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
        }
    }

    [Fact]
    public void InvalidInstanceMethods()
    {
        using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
        using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
        {
            //non-existent entry
            Assert.True(null == archive.GetEntry("nonExistentEntry"));
            //null/empty string
            Assert.Throws<ArgumentNullException>(() => archive.GetEntry(null));

            ZipArchiveEntry entry = archive.GetEntry("first.txt");

            //null/empty string
            AssertExtensions.Throws<ArgumentException>("entryName", () => archive.CreateEntry(""));
            Assert.Throws<ArgumentNullException>(() => archive.CreateEntry(null));
        }
    }

    [Theory]
    [InlineData("LZMA.zip", true)]
    [InlineData("invalidDeflate.zip", false)]
    public void UnsupportedCompressionRoutine(string zipName, bool throwsOnOpen)
    {
        string filename = bad(zipName);
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

        using (TempFile updatedCopy = CreateTempCopyFile(filename, GetTestFilePath()))
        {
            string name;
            long length, compressedLength;
            DateTimeOffset lastWriteTime;
            using (ZipArchive archive = ZipFile.Open(updatedCopy.Path, ZipArchiveMode.Update))
            {
                ZipArchiveEntry e = archive.Entries[0];
                name = e.FullName;
                lastWriteTime = e.LastWriteTime;
                length = e.Length;
                compressedLength = e.CompressedLength;
                Assert.Throws<InvalidDataException>(() => e.Open());
            }

            //make sure that update mode preserves that unreadable file
            using (ZipArchive archive = ZipFile.Open(updatedCopy.Path, ZipArchiveMode.Update))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Equal(name, e.FullName);
                Assert.Equal(lastWriteTime, e.LastWriteTime);
                Assert.Equal(length, e.Length);
                Assert.Equal(compressedLength, e.CompressedLength);
                Assert.Throws<InvalidDataException>(() => e.Open());
            }
        }
    }

    [Fact]
    public void InvalidDates()
    {
        using (ZipArchive archive = ZipFile.OpenRead(bad("invaliddate.zip")))
        {
            Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime);
        }

        // Browser VFS does not support saving file attributes, so skip
        if (!PlatformDetection.IsBrowser)
        {
            FileInfo fileWithBadDate = new FileInfo(GetTestFilePath());
            fileWithBadDate.Create().Dispose();
            fileWithBadDate.LastWriteTimeUtc = new DateTime(1970, 1, 1, 1, 1, 1);
            string archivePath = GetTestFilePath();
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
    }

    [Fact]
    public void ReadStreamOps()
    {
        using (ZipArchive archive = ZipFile.OpenRead(zfile("normal.zip")))
        {
            foreach (ZipArchiveEntry e in archive.Entries)
            {
                using (Stream s = e.Open())
                {
                    Assert.True(s.CanRead, "Can read to read archive");
                    Assert.False(s.CanWrite, "Can't write to read archive");
                    Assert.False(s.CanSeek, "Can't seek on archive");
                    Assert.Equal(LengthOfUnseekableStream(s), e.Length);
                }
            }
        }
    }

    [Fact]
    public void UpdateReadTwice()
    {
        using (TempFile testArchive = CreateTempCopyFile(zfile("small.zip"), GetTestFilePath()))
        using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
        {
            ZipArchiveEntry entry = archive.Entries[0];
            string contents1, contents2;
            using (StreamReader s = new StreamReader(entry.Open()))
            {
                contents1 = s.ReadToEnd();
            }
            using (StreamReader s = new StreamReader(entry.Open()))
            {
                contents2 = s.ReadToEnd();
            }
            Assert.Equal(contents1, contents2);
        }
    }

    [Fact]
    public async Task UpdateAddFile()
    {
        //add file
        using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
        {
            using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
            {
                await UpdateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
            }
            await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read);
        }

        //add file and read entries before
        using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
        {
            using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
            {
                var x = archive.Entries;

                await UpdateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
            }
            await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read);
        }

        //add file and read entries after
        using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
        {
            using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
            {
                await UpdateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");

                var x = archive.Entries;
            }
            await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read);
        }
    }

    private static async Task UpdateArchive(ZipArchive archive, string installFile, string entryName)
    {
        string fileName = installFile;
        ZipArchiveEntry e = archive.CreateEntry(entryName);

        var file = FileData.GetFile(fileName);
        e.LastWriteTime = file.LastModifiedDate;

        using (var stream = await StreamHelpers.CreateTempCopyStream(fileName))
        {
            using (Stream es = e.Open())
            {
                es.SetLength(0);
                stream.CopyTo(es);
            }
        }
    }
}
