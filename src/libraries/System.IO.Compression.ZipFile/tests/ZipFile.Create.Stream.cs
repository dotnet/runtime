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
    public void CreateFromDirectoryNormal()
    {
        string folderName = zfolder("normal");
        using MemoryStream destination = new();
        ZipFile.CreateFromDirectory(folderName, destination);

        IsZipSameAsDir(destination, folderName, ZipArchiveMode.Read, requireExplicit: false, checkTimes: false);
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
