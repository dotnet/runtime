// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests;

public class ZipFile_Create_Stream : ZipFileTestBase
{
    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreateFromDirectory_NullSourceDirectory_Throws(bool async)
    {
        using MemoryStream ms = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentNullException>(() => CallZipFileCreateFromDirectory(async, sourceDirectoryName: null, ms));
        await Assert.ThrowsAsync<ArgumentNullException>(() => CallZipFileCreateFromDirectory(async, sourceDirectoryName: null, ms, CompressionLevel.NoCompression, includeBaseDirectory: false));
        await Assert.ThrowsAsync<ArgumentNullException>(() => CallZipFileCreateFromDirectory(async, sourceDirectoryName: null, ms, CompressionLevel.NoCompression, includeBaseDirectory: false, Encoding.UTF8));
    }

    public static IEnumerable<object[]> Get_CreateFromDirectory_CompressionLevel_OutOfRange_Throws_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { (CompressionLevel)int.MinValue, async };
            yield return new object[] { (CompressionLevel)(-1), async };
            yield return new object[] { (CompressionLevel)4, async };
            yield return new object[] { (CompressionLevel)int.MaxValue, async };
        }
    }

    [Theory]
    [MemberData(nameof(Get_CreateFromDirectory_CompressionLevel_OutOfRange_Throws_Data))]
    public async Task CreateFromDirectory_CompressionLevel_OutOfRange_Throws(CompressionLevel invalidCompressionLevel, bool async)
    {
        using MemoryStream ms = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CallZipFileCreateFromDirectory(async, "sourceDirectory", ms, invalidCompressionLevel, includeBaseDirectory: false));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CallZipFileCreateFromDirectory(async, "sourceDirectory", ms, invalidCompressionLevel, includeBaseDirectory: false, Encoding.UTF8));
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreateFromDirectory_UnwritableStream_Throws(bool async)
    {
        using MemoryStream ms = new();
        using WrappedStream destination = new(ms, canRead: true, canWrite: false, canSeek: true);
        await Assert.ThrowsAsync<ArgumentException>("destination", () => CallZipFileCreateFromDirectory(async, GetTestFilePath(), destination));
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreateFromDirectoryNormal(bool async)
    {
        string folderName = zfolder("normal");
        using MemoryStream destination = new();
        await CallZipFileCreateFromDirectory(async, folderName, destination);
        destination.Position = 0;
        await IsZipSameAsDir(destination, folderName, ZipArchiveMode.Read, requireExplicit: false, checkTimes: false, async);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreateFromDirectoryNormal_Unreadable_Unseekable(bool async)
    {
        string folderName = zfolder("normal");
        using MemoryStream ms = new();
        using WrappedStream destination = new(ms, canRead: false, canWrite: true, canSeek: false);
        await CallZipFileCreateFromDirectory(async, folderName, destination);
        ms.Position = 0;
        await IsZipSameAsDir(ms, folderName, ZipArchiveMode.Read, requireExplicit: false, checkTimes: false, async);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreateFromDirectory_IncludeBaseDirectory(bool async)
    {
        string folderName = zfolder("normal");
        using MemoryStream destination = new();
        await CallZipFileCreateFromDirectory(async, folderName, destination, CompressionLevel.Optimal, true);

        IEnumerable<string> expected = Directory.EnumerateFiles(zfolder("normal"), "*", SearchOption.AllDirectories);
        destination.Position = 0;
        ZipArchive archive = await CreateZipArchive(async, destination, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);

        foreach (ZipArchiveEntry actualEntry in archive.Entries)
        {
            string expectedFile = expected.Single(i => Path.GetFileName(i).Equals(actualEntry.Name));
            Assert.StartsWith("normal", actualEntry.FullName);
            Assert.Equal(new FileInfo(expectedFile).Length, actualEntry.Length);
            using Stream expectedStream = File.OpenRead(expectedFile);

            Stream actualStream = await OpenEntryStream(async, actualEntry);
            StreamsEqual(expectedStream, actualStream);

        }

        await DisposeZipArchive(async, archive);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreateFromDirectoryUnicode(bool async)
    {
        string folderName = zfolder("unicode");
        using MemoryStream destination = new();
        await CallZipFileCreateFromDirectory(async, folderName, destination);

        ZipArchive archive = await CreateZipArchive(async, destination, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);
        IEnumerable<string> actual = archive.Entries.Select(entry => entry.Name);
        IEnumerable<string> expected = Directory.EnumerateFileSystemEntries(zfolder("unicode"), "*", SearchOption.AllDirectories).ToList();
        Assert.True(Enumerable.SequenceEqual(expected.Select(i => Path.GetFileName(i)), actual.Select(i => i)));
        await DisposeZipArchive(async, archive);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreatedEmptyDirectoriesRoundtrip(bool async)
    {
        using TempDirectory tempFolder = new(GetTestFilePath());

        DirectoryInfo rootDir = new(tempFolder.Path);
        string folderName = "empty1";
        rootDir.CreateSubdirectory(folderName);

        using MemoryStream destination = new();
        await CallZipFileCreateFromDirectory(async, rootDir.FullName, destination,
            CompressionLevel.Optimal, false, Encoding.UTF8);

        ZipArchive archive = await CreateZipArchive(async, destination, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);

        Assert.Equal(1, archive.Entries.Count);
        Assert.StartsWith(folderName, archive.Entries[0].FullName);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreatedEmptyUtf32DirectoriesRoundtrip(bool async)
    {
        using TempDirectory tempFolder = new(GetTestFilePath());

        Encoding entryEncoding = Encoding.UTF32;
        DirectoryInfo rootDir = new(tempFolder.Path);
        string folderName = "empty1";
        rootDir.CreateSubdirectory(folderName);

        using MemoryStream destination = new();
        await CallZipFileCreateFromDirectory(async, rootDir.FullName, destination,
            CompressionLevel.Optimal, false, entryEncoding);

        ZipArchive archive = await CreateZipArchive(async, destination, ZipArchiveMode.Read, leaveOpen: false, entryEncoding);
        Assert.Equal(1, archive.Entries.Count);
        Assert.StartsWith(folderName, archive.Entries[0].FullName);
        await DisposeZipArchive(async, archive);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreatedEmptyRootDirectoryRoundtrips(bool async)
    {
        using TempDirectory tempFolder = new(GetTestFilePath());

        DirectoryInfo emptyRoot = new(tempFolder.Path);
        using MemoryStream destination = new();
        await CallZipFileCreateFromDirectory(async, emptyRoot.FullName, destination,
            CompressionLevel.Optimal, true);

        ZipArchive archive = await CreateZipArchive(async, destination, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);
        Assert.Equal(1, archive.Entries.Count);
        await DisposeZipArchive(async, archive);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task CreateSetsExternalAttributesCorrectly(bool async)
    {
        string folderName = zfolder("normal");
        using MemoryStream destination = new();
        await CallZipFileCreateFromDirectory(async, folderName, destination);
        destination.Position = 0;
        ZipArchive archive = await CreateZipArchive(async, destination, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(0, entry.ExternalAttributes);
            }
            else
            {
                Assert.NotEqual(0, entry.ExternalAttributes);
            }
        }
        await DisposeZipArchive(async, archive);
    }
}
