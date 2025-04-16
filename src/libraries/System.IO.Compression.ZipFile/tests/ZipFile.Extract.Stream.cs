// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests;

public class ZipFile_Extract_Stream : ZipFileTestBase
{
    [Fact]
    public void ExtractToDirectory_NullStream_Throws()
    {
        Assert.Throws<ArgumentNullException>("source", () => ZipFile.ExtractToDirectory(source: null, GetTestFilePath()));
    }

    [Fact]
    public void ExtractToDirectory_UnreadableStream_Throws()
    {
        using MemoryStream ms = new();
        using WrappedStream source = new(ms, canRead: false, canWrite: true, canSeek: true);
        Assert.Throws<ArgumentException>("source", () => ZipFile.ExtractToDirectory(source, GetTestFilePath()));
    }

    public static IEnumerable<object[]> Get_ExtractToDirectoryNormal_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { "normal.zip", "normal", async };
            yield return new object[] { "empty.zip", "empty", async };
            yield return new object[] { "explicitdir1.zip", "explicitdir", async };
            yield return new object[] { "explicitdir2.zip", "explicitdir", async };
            yield return new object[] { "appended.zip", "small", async };
            yield return new object[] { "prepended.zip", "small", async };
            yield return new object[] { "noexplicitdir.zip", "explicitdir", async };
        }
    }

    [Theory]
    [MemberData(nameof(Get_ExtractToDirectoryNormal_Data))]
    public async Task ExtractToDirectoryNormal(string file, string folder, bool async)
    {
        FileStream source = CreateFileStreamRead(async, zfile(file));
        string folderName = zfolder(folder);
        using TempDirectory tempFolder = new(GetTestFilePath());
        await CallZipFileExtractToDirectory(async, source, tempFolder.Path);
        DirsEqual(tempFolder.Path, folderName);
        await DisposeStream(async, source);
    }

    public static IEnumerable<object[]> Get_ExtractToDirectoryNormal_Unwritable_Unseekable_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { "normal.zip", "normal", async };
            yield return new object[] { "empty.zip", "empty", async };
            yield return new object[] { "explicitdir1.zip", "explicitdir", async };
            yield return new object[] { "explicitdir2.zip", "explicitdir", async };
            yield return new object[] { "appended.zip", "small", async };
            yield return new object[] { "prepended.zip", "small", async };
            yield return new object[] { "noexplicitdir.zip", "explicitdir", async };
        }
    }

    [Theory]
    [MemberData(nameof(Get_ExtractToDirectoryNormal_Unwritable_Unseekable_Data))]
    public async Task ExtractToDirectoryNormal_Unwritable_Unseekable(string file, string folder, bool async)
    {
        FileStream fs = CreateFileStreamRead(async, zfile(file));
        using WrappedStream source = new(fs, canRead: true, canWrite: false, canSeek: false);
        string folderName = zfolder(folder);
        using TempDirectory tempFolder = new(GetTestFilePath());
        await CallZipFileExtractToDirectory(async, source, tempFolder.Path);
        DirsEqual(tempFolder.Path, folderName);
        await DisposeStream(async, fs);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/72951", TestPlatforms.iOS | TestPlatforms.tvOS)]
    public async Task ExtractToDirectoryUnicode(bool async)
    {
        FileStream source = CreateFileStreamRead(async, zfile("unicode.zip"));
        string folderName = zfolder("unicode");
        using TempDirectory tempFolder = new TempDirectory(GetTestFilePath());
        await CallZipFileExtractToDirectory(async, source, tempFolder.Path);
        DirFileNamesEqual(tempFolder.Path, folderName);
        await DisposeStream(async, source);
    }

    public static IEnumerable<object[]> GEt_ExtractOutOfRoot_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { "../Foo", async };
            yield return new object[] { "../Barbell", async };
        }
    }

    [Theory]
    [MemberData(nameof(GEt_ExtractOutOfRoot_Data))]
    public async Task ExtractOutOfRoot(string entryName, bool async)
    {
        FileStream source = new(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, bufferSize: 4096, useAsync: async);

        ZipArchive archive = await CreateZipArchive(async, source, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null);
        ZipArchiveEntry entry = archive.CreateEntry(entryName);
        await DisposeZipArchive(async, archive);

        DirectoryInfo destination = Directory.CreateDirectory(Path.Combine(GetTestFilePath(), "Bar"));
        source.Position = 0;
        await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, source, destination.FullName));
        await DisposeStream(async, source);
    }

    public static IEnumerable<object[]> Get_Unix_ZipWithInvalidFileNames_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { "NullCharFileName_FromWindows", async };
            yield return new object[] { "NullCharFileName_FromUnix", async };
        }
    }

    /// <summary>
    /// This test ensures that a zipfile with path names that are invalid to this OS will throw errors
    /// when an attempt is made to extract them.
    /// </summary>
    [Theory]
    [MemberData(nameof(Get_Unix_ZipWithInvalidFileNames_Data))]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
    public async Task Unix_ZipWithInvalidFileNames(string zipName, bool async)
    {
        string testDirectory = GetTestFilePath();
        FileStream source = CreateFileStreamRead(async, compat(zipName) + ".zip");
        await CallZipFileExtractToDirectory(async, source, testDirectory);
        Assert.True(File.Exists(Path.Combine(testDirectory, "a_6b6d")));
        await DisposeStream(async, source);
    }

    public static IEnumerable<object[]> Get_Unix_ZipWithOSSpecificFileNames_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { "backslashes_FromUnix", "aa\\bb\\cc\\dd", async };
            yield return new object[] { "backslashes_FromWindows", "aa\\bb\\cc\\dd", async };
            yield return new object[] { "WindowsInvalid_FromUnix", "aa<b>d", async };
            yield return new object[] { "WindowsInvalid_FromWindows", "aa<b>d", async };
        }
    }

    [Theory]
    [MemberData(nameof(Get_Unix_ZipWithOSSpecificFileNames_Data))]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
    public async Task Unix_ZipWithOSSpecificFileNames(string zipName, string fileName, bool async)
    {
        string tempDir = GetTestFilePath();
        Stream source = CreateFileStreamRead(async, compat(zipName) + ".zip");
        await CallZipFileExtractToDirectory(async, source, tempDir);
        string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
        Assert.Equal(1, results.Length);
        Assert.Equal(fileName, Path.GetFileName(results[0]));
        await DisposeStream(async, source);
    }

    /// <summary>
    /// This test checks whether or not ZipFile.ExtractToDirectory() is capable of handling filenames
		/// which contain invalid path characters in Windows.
    ///  Archive:  InvalidWindowsFileNameChars.zip
    ///  Test/
    ///  Test/normalText.txt
    ///  Test"<>|^A^B^C^D^E^F^G^H^I^J^K^L^M^N^O^P^Q^R^S^T^U^V^W^X^Y^Z^[^\^]^^^_/
    ///  Test"<>|^A^B^C^D^E^F^G^H^I^J^K^L^M^N^O^P^Q^R^S^T^U^V^W^X^Y^Z^[^\^]^^^_/TestText1"<>|^A^B^C^D^E^F^G^H^I^J^K^L^M^N^O^P^Q^R^S^T^U^V^W^X^Y^Z^[^\^]^^^_.txt
    ///  TestEmpty/
    ///  TestText"<>|^A^B^C^D^E^F^G^H^I^J^K^L^M^N^O^P^Q^R^S^T^U^V^W^X^Y^Z^[^\^]^^^_.txt
    /// </summary>
    public static IEnumerable<object[]> Get_Windows_ZipWithInvalidFileNames_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { "InvalidWindowsFileNameChars.zip", new string[] { "TestText______________________________________.txt", "Test______________________________________/TestText1______________________________________.txt", "Test/normalText.txt" }, async };
            yield return new object[] { "NullCharFileName_FromWindows.zip", new string[] { "a_6b6d" }, async };
            yield return new object[] { "NullCharFileName_FromUnix.zip", new string[] { "a_6b6d" }, async };
            yield return new object[] { "WindowsInvalid_FromUnix.zip", new string[] { "aa_b_d" }, async };
            yield return new object[] { "WindowsInvalid_FromWindows.zip", new string[] { "aa_b_d" }, async };
        }
    }

    [Theory]
    [MemberData(nameof(Get_Windows_ZipWithInvalidFileNames_Data))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public async Task Windows_ZipWithInvalidFileNames(string zipFileName, string[] expectedFiles, bool async)
    {
        string testDirectory = GetTestFilePath();

        FileStream source = CreateFileStreamRead(async, compat(zipFileName));
        await CallZipFileExtractToDirectory(async, source, testDirectory);
        foreach (string expectedFile in expectedFiles)
        {
            string path = Path.Combine(testDirectory, expectedFile);
            Assert.True(File.Exists(path));
            File.Delete(path);
        }
        await DisposeStream(async, source);
    }

    public static IEnumerable<object[]> Get_Windows_ZipWithOSSpecificFileNames_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { "backslashes_FromUnix", "dd", async };
            yield return new object[] { "backslashes_FromWindows", "dd", async };
        }
    }

    [Theory]
    [MemberData(nameof(Get_Windows_ZipWithOSSpecificFileNames_Data))]
    [PlatformSpecific(TestPlatforms.Windows)]  // Checks Windows-specific invalid file path
    public async Task Windows_ZipWithOSSpecificFileNames(string zipName, string fileName, bool async)
    {
        string tempDir = GetTestFilePath();
        using Stream source = CreateFileStreamRead(async, compat(zipName) + ".zip");
        await CallZipFileExtractToDirectory(async, source, tempDir);
        string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
        Assert.Equal(1, results.Length);
        Assert.Equal(fileName, Path.GetFileName(results[0]));
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task ExtractToDirectoryOverwrite(bool async)
    {
        string folderName = zfolder("normal");

        using TempDirectory tempFolder = new(GetTestFilePath());
        using FileStream source = CreateFileStreamRead(async, zfile("normal.zip"));
        await CallZipFileExtractToDirectory(async, source, tempFolder.Path, overwriteFiles: false);
        source.Position = 0;
        await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, source, tempFolder.Path /* default false */));
        source.Position = 0;
        await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, source, tempFolder.Path, overwriteFiles: false));
        source.Position = 0;
        await CallZipFileExtractToDirectory(async, source, tempFolder.Path, overwriteFiles: true);

        DirsEqual(tempFolder.Path, folderName);

        await DisposeStream(async, source);
    }

    [Theory]
    [MemberData(nameof(Get_Booleans_Data))]
    public async Task ExtractToDirectoryOverwriteEncoding(bool async)
    {
        string folderName = zfolder("normal");

        using TempDirectory tempFolder = new TempDirectory(GetTestFilePath());
        using FileStream source = CreateFileStreamRead(async, zfile("normal.zip"));

        await CallZipFileExtractToDirectory(async, source, tempFolder.Path, Encoding.UTF8, overwriteFiles: false);
        source.Position = 0;
        await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, source, tempFolder.Path, Encoding.UTF8 /* default false */));
        source.Position = 0;
        await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, source, tempFolder.Path, Encoding.UTF8, overwriteFiles: false));
        source.Position = 0;
        await CallZipFileExtractToDirectory(async, source, tempFolder.Path, Encoding.UTF8, overwriteFiles: true);

        DirsEqual(tempFolder.Path, folderName);
    }

    [Fact]
    public void FilesOutsideDirectory()
    {
        using MemoryStream source = new();
        using (ZipArchive archive = new(source, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (StreamWriter writer = new(archive.CreateEntry(Path.Combine("..", "entry1"), CompressionLevel.Optimal).Open()))
            {
                writer.Write("This is a test.");
            }
        }
        source.Position = 0;
        Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(source, GetTestFilePath()));
    }

    [Fact]
    public void DirectoryEntryWithData()
    {
        using MemoryStream source = new();
        using (ZipArchive archive = new(source, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (StreamWriter writer = new(archive.CreateEntry("testdir" + Path.DirectorySeparatorChar, CompressionLevel.Optimal).Open()))
            {
                writer.Write("This is a test.");
            }
        }
        source.Position = 0;
        Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(source, GetTestFilePath()));
    }

    [Fact]
    public void ExtractToDirectoryRoundTrip()
    {
        string folderName = zfolder("normal");
        MemoryStream source = new();
        using TempDirectory tempFolder = new();

        ZipFile.CreateFromDirectory(folderName, source);
        source.Position = 0;
        ZipFile.ExtractToDirectory(source, tempFolder.Path, overwriteFiles: false);

        DirFileNamesEqual(tempFolder.Path, folderName);
    }
}
