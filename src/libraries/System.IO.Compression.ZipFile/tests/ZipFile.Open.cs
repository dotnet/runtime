// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests;

public class ZipFile_Open : ZipFileTestBase
{
    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public Task InvalidConstructors(bool async)
    {
        //out of range enum values
        return Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CallZipFileOpen(async, "bad file", (ZipArchiveMode)(10)));
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
    public async Task InvalidFilesAsync()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => ZipFile.OpenReadAsync(bad("EOCDmissing.zip"), default));
        using (TempFile testArchive = CreateTempCopyFile(bad("EOCDmissing.zip"), GetTestFilePath()))
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => ZipFile.OpenAsync(testArchive.Path, ZipArchiveMode.Update, default));
        }

        await Assert.ThrowsAsync<InvalidDataException>(() => ZipFile.OpenReadAsync(bad("CDoffsetOutOfBounds.zip"), default));
        using (TempFile testArchive = CreateTempCopyFile(bad("CDoffsetOutOfBounds.zip"), GetTestFilePath()))
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => ZipFile.OpenAsync(testArchive.Path, ZipArchiveMode.Update, default));
        }

        await using (ZipArchive archive = await ZipFile.OpenReadAsync(bad("CDoffsetInBoundsWrong.zip"), default))
        {
            Assert.Throws<InvalidDataException>(() => { var x = archive.Entries; });
        }

        using (TempFile testArchive = CreateTempCopyFile(bad("CDoffsetInBoundsWrong.zip"), GetTestFilePath()))
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => ZipFile.OpenAsync(testArchive.Path, ZipArchiveMode.Update, default));
        }

        await using (ZipArchive archive = await ZipFile.OpenReadAsync(bad("numberOfEntriesDifferent.zip"), default))
        {
            Assert.Throws<InvalidDataException>(() => { var x = archive.Entries; });
        }
        using (TempFile testArchive = CreateTempCopyFile(bad("numberOfEntriesDifferent.zip"), GetTestFilePath()))
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => ZipFile.OpenAsync(testArchive.Path, ZipArchiveMode.Update, default));
        }

        //read mode on empty file
        await using (var memoryStream = new MemoryStream())
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => ZipArchive.CreateAsync(memoryStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null));
        }

        //offset out of bounds
        await using (ZipArchive archive = await ZipFile.OpenReadAsync(bad("localFileOffsetOutOfBounds.zip"), default))
        {
            ZipArchiveEntry e = archive.Entries[0];
            await Assert.ThrowsAsync<InvalidDataException>(() => e.OpenAsync(default));
        }

        using (TempFile testArchive = CreateTempCopyFile(bad("localFileOffsetOutOfBounds.zip"), GetTestFilePath()))
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => ZipFile.OpenAsync(testArchive.Path, ZipArchiveMode.Update, default));
        }

        //compressed data offset + compressed size out of bounds
        await using (ZipArchive archive = await ZipFile.OpenReadAsync(bad("compressedSizeOutOfBounds.zip"), default))
        {
            ZipArchiveEntry e = archive.Entries[0];
            await Assert.ThrowsAsync<InvalidDataException>(() => e.OpenAsync(default));
        }

        using (TempFile testArchive = CreateTempCopyFile(bad("compressedSizeOutOfBounds.zip"), GetTestFilePath()))
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => ZipFile.OpenAsync(testArchive.Path, ZipArchiveMode.Update, default));
        }

        //signature wrong
        await using (ZipArchive archive = await ZipFile.OpenReadAsync(bad("localFileHeaderSignatureWrong.zip"), default))
        {
            ZipArchiveEntry e = archive.Entries[0];
            await Assert.ThrowsAsync<InvalidDataException>(() => e.OpenAsync(default));
        }

        using (TempFile testArchive = CreateTempCopyFile(bad("localFileHeaderSignatureWrong.zip"), GetTestFilePath()))
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => ZipFile.OpenAsync(testArchive.Path, ZipArchiveMode.Update, default));
        }
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task InvalidInstanceMethods(bool async)
    {
        using TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath());

        ZipArchive archive = await CallZipFileOpen(async, testArchive.Path, ZipArchiveMode.Update);

        //non-existent entry
        Assert.True(null == archive.GetEntry("nonExistentEntry"));
        //null/empty string
        Assert.Throws<ArgumentNullException>(() => archive.GetEntry(null));

        ZipArchiveEntry entry = archive.GetEntry("first.txt");

        //null/empty string
        AssertExtensions.Throws<ArgumentException>("entryName", () => archive.CreateEntry(""));
        Assert.Throws<ArgumentNullException>(() => archive.CreateEntry(null));

        await DisposeZipArchive(async, archive);
    }

    public static IEnumerable<object[]> Get_UnsupportedCompressionRoutine_Data()
    {
        foreach (bool b in _bools)
        {
            yield return new object[] { "LZMA.zip", true, b};
            yield return new object[] { "invalidDeflate.zip", false, b};
        }
    }

    [Theory]
    [MemberData(nameof(Get_UnsupportedCompressionRoutine_Data))]
    public async Task UnsupportedCompressionRoutine(string zipName, bool throwsOnOpen, bool async)
    {
        string filename = bad(zipName);
        ZipArchive archive = await CallZipFileOpenRead(async, filename);

        ZipArchiveEntry e = archive.Entries[0];
        if (throwsOnOpen)
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => OpenEntryStream(async, e));
        }
        else
        {
            Stream s = await OpenEntryStream(async, e);
            Assert.Throws<InvalidDataException>(() => s.ReadByte());
            await DisposeStream(async, s);
        }

        await DisposeZipArchive(async, archive);

        using (TempFile updatedCopy = CreateTempCopyFile(filename, GetTestFilePath()))
        {
            string name;
            long length, compressedLength;
            DateTimeOffset lastWriteTime;
            archive = await CallZipFileOpen(async, updatedCopy.Path, ZipArchiveMode.Update);

            e = archive.Entries[0];
            name = e.FullName;
            lastWriteTime = e.LastWriteTime;
            length = e.Length;
            compressedLength = e.CompressedLength;
            await Assert.ThrowsAsync<InvalidDataException>(() => OpenEntryStream(async, e));

            await DisposeZipArchive(async, archive);

            //make sure that update mode preserves that unreadable file
            archive = await CallZipFileOpen(async, updatedCopy.Path, ZipArchiveMode.Update);

            e = archive.Entries[0];
            Assert.Equal(name, e.FullName);
            Assert.Equal(lastWriteTime, e.LastWriteTime);
            Assert.Equal(length, e.Length);
            Assert.Equal(compressedLength, e.CompressedLength);
            await Assert.ThrowsAsync<InvalidDataException>(() => OpenEntryStream(async, e));

            await DisposeZipArchive(async, archive);
        }
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task InvalidDates(bool async)
    {
        ZipArchive archive = await CallZipFileOpenRead(async, bad("invaliddate.zip"));
        Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime);
        await DisposeZipArchive(async, archive);

        // Browser VFS does not support saving file attributes, so skip
        if (!PlatformDetection.IsBrowser)
        {
            FileInfo fileWithBadDate = new FileInfo(GetTestFilePath());
            fileWithBadDate.Create().Dispose();
            fileWithBadDate.LastWriteTimeUtc = new DateTime(1970, 1, 1, 1, 1, 1);
            string archivePath = GetTestFilePath();
            using (FileStream output = File.Open(archivePath, FileMode.Create))
            {
                archive = await CreateZipArchive(async, output, ZipArchiveMode.Create, leaveOpen: false, entryNameEncoding: null);
                archive.CreateEntryFromFile(fileWithBadDate.FullName, "SomeEntryName");
                await DisposeZipArchive(async, archive);
            }

            archive = await CallZipFileOpenRead(async, archivePath);
            Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime);
            await DisposeZipArchive(async, archive);
        }
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task ReadStreamOps(bool async)
    {
        using (ZipArchive archive = await CallZipFileOpenRead(async, zfile("normal.zip")))
        {
            foreach (ZipArchiveEntry e in archive.Entries)
            {
                Stream s = await OpenEntryStream(async, e);
                Assert.True(s.CanRead, "Can read to read archive");
                Assert.False(s.CanWrite, "Can't write to read archive");
                
                if (s.CanSeek)
                {
                    // If the stream is seekable, verify that seeking works correctly
                    // Test seeking to beginning
                    long beginResult = s.Seek(0, SeekOrigin.Begin);
                    Assert.Equal(0, beginResult);
                    Assert.Equal(0, s.Position);
                    
                    // Test seeking to end
                    long endResult = s.Seek(0, SeekOrigin.End);
                    Assert.Equal(e.Length, endResult);
                    Assert.Equal(e.Length, s.Position);
                    
                    // Test Position setter
                    s.Position = 0;
                    Assert.Equal(0, s.Position);
                    
                    // Reset to beginning for length check
                    s.Seek(0, SeekOrigin.Begin);
                }
                else
                {
                    // If the stream is not seekable, verify that seeking throws
                    Assert.Throws<NotSupportedException>(() => s.Seek(0, SeekOrigin.Begin));
                    Assert.Throws<NotSupportedException>(() => s.Position = 0);
                }
                
                Assert.Equal(await LengthOfUnseekableStream(s), e.Length);
                await DisposeStream(async, s);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task UpdateReadTwice(bool async)
    {
        using TempFile testArchive = CreateTempCopyFile(zfile("small.zip"), GetTestFilePath());

        ZipArchive archive = await CallZipFileOpen(async, testArchive.Path, ZipArchiveMode.Update);

        ZipArchiveEntry entry = archive.Entries[0];
        string contents1, contents2;
        using (StreamReader s = new StreamReader(await OpenEntryStream(async, entry)))
        {
            contents1 = await s.ReadToEndAsync();
        }
        using (StreamReader s = new StreamReader(await OpenEntryStream(async, entry)))
        {
            contents2 = await s.ReadToEndAsync();
        }
        Assert.Equal(contents1, contents2);

        await DisposeZipArchive(async, archive);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task UpdateAddFile(bool async)
    {
        //add file
        using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
        {
            ZipArchive archive = await CallZipFileOpen(async, testArchive.Path, ZipArchiveMode.Update);
            await UpdateArchive(async, archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
            await DisposeZipArchive(async, archive);

            await IsZipSameAsDir(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read, async);
        }

        //add file and read entries before
        using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
        {
            ZipArchive archive = await CallZipFileOpen(async, testArchive.Path, ZipArchiveMode.Update);
            var x = archive.Entries;
            await UpdateArchive(async, archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
            await DisposeZipArchive(async, archive);

            await IsZipSameAsDir(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read, async);
        }

        //add file and read entries after
        using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
        {
            ZipArchive archive = await CallZipFileOpen(async, testArchive.Path, ZipArchiveMode.Update);
            await UpdateArchive(async, archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
            var x = archive.Entries;
            await DisposeZipArchive(async, archive);

            await IsZipSameAsDir(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read, async);
        }
    }

    private static async Task UpdateArchive(bool async, ZipArchive archive, string installFile, string entryName)
    {
        string fileName = installFile;
        ZipArchiveEntry e = archive.CreateEntry(entryName);

        var file = FileData.GetFile(fileName);
        e.LastWriteTime = file.LastModifiedDate;

        using (var stream = await StreamHelpers.CreateTempCopyStream(fileName))
        {
            Stream es = await OpenEntryStream(async, e);
            es.SetLength(0);
            if (async)
            {
                await stream.CopyToAsync(es);
            }
            else
            {
                stream.CopyTo(es);
            }
            await DisposeStream(async, es);
        }
    }
}
