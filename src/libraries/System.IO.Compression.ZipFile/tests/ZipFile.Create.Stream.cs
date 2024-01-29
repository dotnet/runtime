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
    [Fact]
    public void CreateFromDirectory_NullSourceDirectory_Throws()
    {
        using MemoryStream ms = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => ZipFile.CreateFromDirectory(sourceDirectoryName: null, ms));
        Assert.Throws<ArgumentNullException>(() => ZipFile.CreateFromDirectory(sourceDirectoryName: null, ms, CompressionLevel.NoCompression, includeBaseDirectory: false));
        Assert.Throws<ArgumentNullException>(() => ZipFile.CreateFromDirectory(sourceDirectoryName: null, ms, CompressionLevel.NoCompression, includeBaseDirectory: false, Encoding.UTF8));
    }

    [Theory]
    [InlineData((CompressionLevel)int.MinValue)]
    [InlineData((CompressionLevel)(-1))]
    [InlineData((CompressionLevel)4)]
    [InlineData((CompressionLevel)int.MaxValue)]
    public void CreateFromDirectory_CompressionLevel_OutOfRange_Throws(CompressionLevel invalidCompressionLevel)
    {
        using MemoryStream ms = new MemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => ZipFile.CreateFromDirectory("sourceDirectory", ms, invalidCompressionLevel, includeBaseDirectory: false));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZipFile.CreateFromDirectory("sourceDirectory", ms, invalidCompressionLevel, includeBaseDirectory: false, Encoding.UTF8));
    }
       
    [Fact]
    public void CreateFromDirectory_UnwritableStream_Throws()
    {
        using MemoryStream ms = new();
        using WrappedStream destination = new(ms, canRead: true, canWrite: false, canSeek: true);
        Assert.Throws<ArgumentException>("destination", () => ZipFile.CreateFromDirectory(GetTestFilePath(), destination));
    }

    [Fact]
    public void CreateFromDirectoryNormal()
    {
        string folderName = zfolder("normal");
        using MemoryStream destination = new();
        ZipFile.CreateFromDirectory(folderName, destination);
        destination.Position = 0;
        IsZipSameAsDir(destination, folderName, ZipArchiveMode.Read, requireExplicit: false, checkTimes: false);
    }

    [Fact]
    public void CreateFromDirectoryNormal_Unreadable_Unseekable()
    {
        string folderName = zfolder("normal");
        using MemoryStream ms = new();
        using WrappedStream destination = new(ms, canRead: false, canWrite: true, canSeek: false);
        ZipFile.CreateFromDirectory(folderName, destination);
        ms.Position = 0;
        IsZipSameAsDir(ms, folderName, ZipArchiveMode.Read, requireExplicit: false, checkTimes: false);
    }

    [Fact]
    public void CreateFromDirectory_IncludeBaseDirectory()
    {
        string folderName = zfolder("normal");
        using MemoryStream destination = new();
        ZipFile.CreateFromDirectory(folderName, destination, CompressionLevel.Optimal, true);

        IEnumerable<string> expected = Directory.EnumerateFiles(zfolder("normal"), "*", SearchOption.AllDirectories);
        destination.Position = 0;
        using ZipArchive archive = new(destination);
        foreach (ZipArchiveEntry actualEntry in archive.Entries)
        {
            string expectedFile = expected.Single(i => Path.GetFileName(i).Equals(actualEntry.Name));
            Assert.StartsWith("normal", actualEntry.FullName);
            Assert.Equal(new FileInfo(expectedFile).Length, actualEntry.Length);
            using Stream expectedStream = File.OpenRead(expectedFile);
            using Stream actualStream = actualEntry.Open();
            StreamsEqual(expectedStream, actualStream);
        }
    }

    [Fact]
    public void CreateFromDirectoryUnicode()
    {
        string folderName = zfolder("unicode");
        using MemoryStream destination = new();
        ZipFile.CreateFromDirectory(folderName, destination);

        using ZipArchive archive = new(destination);
        IEnumerable<string> actual = archive.Entries.Select(entry => entry.Name);
        IEnumerable<string> expected = Directory.EnumerateFileSystemEntries(zfolder("unicode"), "*", SearchOption.AllDirectories).ToList();
        Assert.True(Enumerable.SequenceEqual(expected.Select(i => Path.GetFileName(i)), actual.Select(i => i)));
    }

    [Fact]
    public void CreatedEmptyDirectoriesRoundtrip()
    {
        using TempDirectory tempFolder = new(GetTestFilePath());
        
        DirectoryInfo rootDir = new(tempFolder.Path);
        rootDir.CreateSubdirectory("empty1");

        using MemoryStream destination = new();
        ZipFile.CreateFromDirectory(
            rootDir.FullName, destination,
            CompressionLevel.Optimal, false, Encoding.UTF8);

        using ZipArchive archive = new(destination);

        Assert.Equal(1, archive.Entries.Count);
        Assert.StartsWith("empty1", archive.Entries[0].FullName);
    }

    [Fact]
    public void CreatedEmptyUtf32DirectoriesRoundtrip()
    {
        using TempDirectory tempFolder = new(GetTestFilePath());

        Encoding entryEncoding = Encoding.UTF32;
        DirectoryInfo rootDir = new(tempFolder.Path);
        rootDir.CreateSubdirectory("empty1");

        using MemoryStream destination = new();
        ZipFile.CreateFromDirectory(
            rootDir.FullName, destination,
            CompressionLevel.Optimal, false, entryEncoding);

        using ZipArchive archive = new(destination, ZipArchiveMode.Read, leaveOpen: false, entryEncoding);
        Assert.Equal(1, archive.Entries.Count);
        Assert.StartsWith("empty1", archive.Entries[0].FullName);
    }

    [Fact]
    public void CreatedEmptyRootDirectoryRoundtrips()
    {
        using TempDirectory tempFolder = new(GetTestFilePath());

        DirectoryInfo emptyRoot = new(tempFolder.Path);
        using MemoryStream destination = new();
        ZipFile.CreateFromDirectory(
            emptyRoot.FullName, destination,
            CompressionLevel.Optimal, true);

        using ZipArchive archive = new(destination);
        Assert.Equal(1, archive.Entries.Count);
    }

    [Fact]
    public void CreateSetsExternalAttributesCorrectly()
    {
        string folderName = zfolder("normal");
        using MemoryStream destination = new();
        ZipFile.CreateFromDirectory(folderName, destination);
        destination.Position = 0;
        using ZipArchive archive = new(destination);

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
    }
}
