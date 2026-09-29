// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests;

public abstract class TarFile_ExtractToDirectory_Tests : TarTestsBase
{
    protected abstract Task ExtractArchive(MemoryStream archive, string destinationDirectoryName, bool overwriteFiles, bool useOptions, CancellationToken cancellationToken = default);

    public static IEnumerable<object[]> DestinationDirectory_TestData()
    {
        foreach (bool overwriteFiles in new[] { false, true })
        {
            foreach (bool useOptions in new[] { false, true })
            {
                foreach (int missingDirectoryCount in new[] { 0, 1, 3 })
                {
                    foreach (string entryName in new[] { null, "", "file.txt", "nested/child/file.txt", "nested/empty/", "./" })
                    {
                        yield return new object[] { overwriteFiles, useOptions, missingDirectoryCount, entryName };
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(DestinationDirectory_TestData))]
    public async Task DestinationDirectory_CreatedAsNeeded(bool overwriteFiles, bool useOptions, int missingDirectoryCount, string entryName)
    {
        using TempDirectory root = new TempDirectory();
        string destination = root.Path;
        for (int i = 0; i < missingDirectoryCount; i++)
        {
            destination = Path.Join(destination, "missing");
        }

        using MemoryStream archive = CreateArchive(entryName);
        await ExtractArchive(archive, destination, overwriteFiles, useOptions);

        Assert.True(archive.CanRead);
        if (string.IsNullOrEmpty(entryName))
        {
            Assert.Equal(missingDirectoryCount == 0, Directory.Exists(destination));
            Assert.Empty(Directory.GetFileSystemEntries(root.Path));
        }
        else if (entryName.EndsWith('/'))
        {
            Assert.True(Directory.Exists(Path.Join(destination, entryName)));
        }
        else
        {
            Assert.Equal("archive contents", File.ReadAllText(Path.Join(destination, entryName)));
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DestinationDirectory_FileCollision(bool overwriteFiles, bool useOptions)
    {
        using TempDirectory root = new TempDirectory();
        string filePath = Path.Join(root.Path, "file.txt");
        File.WriteAllText(filePath, "original contents");
        using MemoryStream archive = CreateArchive("file.txt");

        if (overwriteFiles)
        {
            await ExtractArchive(archive, root.Path, overwriteFiles, useOptions);
            Assert.Equal("archive contents", File.ReadAllText(filePath));
        }
        else
        {
            await Assert.ThrowsAsync<IOException>(() => ExtractArchive(archive, root.Path, overwriteFiles, useOptions));
            Assert.Equal("original contents", File.ReadAllText(filePath));
        }

        Assert.True(archive.CanRead);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DestinationDirectory_FileInRequiredPath_Throws(bool overwriteFiles, bool useOptions)
    {
        using TempDirectory root = new TempDirectory();
        string filePath = Path.Join(root.Path, "file");
        File.WriteAllText(filePath, "original contents");

        foreach (string destination in new[] { filePath, Path.Join(filePath, "missing") })
        {
            using MemoryStream archive = CreateArchive("file.txt");
            await Assert.ThrowsAnyAsync<IOException>(() => ExtractArchive(archive, destination, overwriteFiles, useOptions));
            Assert.True(archive.CanRead);
            Assert.Equal("original contents", File.ReadAllText(filePath));
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DestinationDirectory_Traversal_Throws(bool overwriteFiles, bool useOptions)
    {
        using TempDirectory root = new TempDirectory();
        string destination = Path.Join(root.Path, "missing");

        foreach (TarEntryType entryType in new[] { TarEntryType.RegularFile, TarEntryType.SymbolicLink, TarEntryType.HardLink })
        {
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, leaveOpen: true))
            {
                PaxTarEntry entry = new PaxTarEntry(entryType, entryType == TarEntryType.RegularFile ? "../outside.txt" : "link");
                if (entryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
                {
                    entry.LinkName = "../outside.txt";
                }
                writer.WriteEntry(entry);
            }
            archive.Position = 0;

            await Assert.ThrowsAsync<IOException>(() => ExtractArchive(archive, destination, overwriteFiles, useOptions));
            Assert.True(archive.CanRead);
            Assert.Empty(Directory.GetFileSystemEntries(root.Path));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DestinationDirectory_InvalidPath_Throws(bool useOptions)
    {
        using MemoryStream archive = CreateArchive(entryName: null);
        await Assert.ThrowsAsync<ArgumentException>(() => ExtractArchive(archive, "\0", overwriteFiles: false, useOptions));
        Assert.True(archive.CanRead);
    }

    private static MemoryStream CreateArchive(string entryName)
    {
        MemoryStream archive = new MemoryStream();
        if (entryName == "")
        {
            // An empty archive may contain just the two end-of-archive records.
            archive.SetLength(1024);
        }
        else if (entryName is not null)
        {
            using TarWriter writer = new TarWriter(archive, leaveOpen: true);
            bool isDirectory = entryName.EndsWith('/');
            PaxTarEntry entry = new PaxTarEntry(isDirectory ? TarEntryType.Directory : TarEntryType.RegularFile, entryName);
            using MemoryStream contents = new MemoryStream(Encoding.UTF8.GetBytes("archive contents"));
            if (!isDirectory)
            {
                entry.DataStream = contents;
            }
            writer.WriteEntry(entry);
        }
        archive.Position = 0;
        return archive;
    }

    // TarEntryFormat, TarEntryType, string fileName
    public static IEnumerable<object[]> GetExactRootDirMatchCases()
    {
        var allValidFormats = new TarEntryFormat[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu };

        foreach (TarEntryFormat format in allValidFormats)
        {
            yield return new object[]
            {
                    format,
                    TarEntryType.Directory,
                    "" // Root directory
            };
            yield return new object[]
            {
                    format,
                    TarEntryType.Directory,
                    "./" // Slash dot root directory
            };
            yield return new object[]
            {
                    format,
                    TarEntryType.Directory,
                    "directory",
            };
            yield return new object[]
            {
                    format,
                    GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format),
                    "file.txt"
            };
        }

        var formatsThatHandleLongFileNames = new TarEntryFormat[] { TarEntryFormat.Pax, TarEntryFormat.Gnu };
        var longFileNames = new string[]
        {
            // Long path with many short segment names  and a filename
            "folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/file.txt",
            // Long path with single long segment name and a filename
            "veryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryverylongfoldername/file.txt",
            // Long path with single long leaf filename
            "veryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryverylongfilename.txt",
        };

        foreach (TarEntryFormat format in formatsThatHandleLongFileNames)
        {
            foreach (string filePath in longFileNames)
            {
                yield return new object[] { format, TarEntryType.RegularFile, filePath };
            }
        }

        var longFolderNames = new string[]
        {
            // Long path with many short segment names  and a filename
            "folderfolder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder/folder",
            // Long path with single long segment name and a filename
            "veryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryverylongfoldername/folder",
            // Long path with single long leaf filename
            "veryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryveryverylongfoldername"
        };

        foreach (TarEntryFormat format in formatsThatHandleLongFileNames)
        {
            foreach (string folderPath in longFolderNames)
            {
                yield return new object[] { format, TarEntryType.Directory, folderPath };
            }
        }
    }
}
