// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
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

    [Theory]
    [InlineData("normal.zip", "normal")]
    [InlineData("empty.zip", "empty")]
    [InlineData("explicitdir1.zip", "explicitdir")]
    [InlineData("explicitdir2.zip", "explicitdir")]
    [InlineData("appended.zip", "small")]
    [InlineData("prepended.zip", "small")]
    [InlineData("noexplicitdir.zip", "explicitdir")]
    public void ExtractToDirectoryNormal(string file, string folder)
    {
        using FileStream source = File.OpenRead(zfile(file));
        string folderName = zfolder(folder);
        using TempDirectory tempFolder = new(GetTestFilePath());
        ZipFile.ExtractToDirectory(source, tempFolder.Path);
        DirsEqual(tempFolder.Path, folderName);
    }

    [Theory]
    [InlineData("normal.zip", "normal")]
    [InlineData("empty.zip", "empty")]
    [InlineData("explicitdir1.zip", "explicitdir")]
    [InlineData("explicitdir2.zip", "explicitdir")]
    [InlineData("appended.zip", "small")]
    [InlineData("prepended.zip", "small")]
    [InlineData("noexplicitdir.zip", "explicitdir")]
    public void ExtractToDirectoryNormal_Unwritable_Unseekable(string file, string folder)
    {
        using FileStream fs = File.OpenRead(zfile(file));
        using WrappedStream source = new(fs, canRead: true, canWrite: false, canSeek: false);
        string folderName = zfolder(folder);
        using TempDirectory tempFolder = new(GetTestFilePath());
        ZipFile.ExtractToDirectory(source, tempFolder.Path);
        DirsEqual(tempFolder.Path, folderName);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/72951", TestPlatforms.iOS | TestPlatforms.tvOS)]
    public void ExtractToDirectoryUnicode()
    {
        using Stream source = File.OpenRead(zfile("unicode.zip"));
        string folderName = zfolder("unicode");
        using TempDirectory tempFolder = new TempDirectory(GetTestFilePath());
        ZipFile.ExtractToDirectory(source, tempFolder.Path);
        DirFileNamesEqual(tempFolder.Path, folderName);
    }

    [Theory]
    [InlineData("../Foo")]
    [InlineData("../Barbell")]
    public void ExtractOutOfRoot(string entryName)
    {
        using FileStream source = new(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite);
        using (ZipArchive archive = new(source, ZipArchiveMode.Create, leaveOpen: true))
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName);
        }

        DirectoryInfo destination = Directory.CreateDirectory(Path.Combine(GetTestFilePath(), "Bar"));
        source.Position = 0;
        Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(source, destination.FullName));
    }

    /// <summary>
    /// This test ensures that a zipfile with path names that are invalid to this OS will throw errors
    /// when an attempt is made to extract them.
    /// </summary>
    [Theory]
    [InlineData("NullCharFileName_FromWindows")]
    [InlineData("NullCharFileName_FromUnix")]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
    public void Unix_ZipWithInvalidFileNames(string zipName)
    {
        string testDirectory = GetTestFilePath();
        using Stream source = File.OpenRead(compat(zipName) + ".zip");
        ZipFile.ExtractToDirectory(source, testDirectory);

        Assert.True(File.Exists(Path.Combine(testDirectory, "a_6b6d")));
    }

    [Theory]
    [InlineData("backslashes_FromUnix", "aa\\bb\\cc\\dd")]
    [InlineData("backslashes_FromWindows", "aa\\bb\\cc\\dd")]
    [InlineData("WindowsInvalid_FromUnix", "aa<b>d")]
    [InlineData("WindowsInvalid_FromWindows", "aa<b>d")]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
    public void Unix_ZipWithOSSpecificFileNames(string zipName, string fileName)
    {
        string tempDir = GetTestFilePath();
        using Stream source = File.OpenRead(compat(zipName) + ".zip");
        ZipFile.ExtractToDirectory(source, tempDir);
        string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
        Assert.Equal(1, results.Length);
        Assert.Equal(fileName, Path.GetFileName(results[0]));
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
    [Theory]
    [PlatformSpecific(TestPlatforms.Windows)]
    [InlineData("InvalidWindowsFileNameChars.zip",  new string[] { "TestText______________________________________.txt" , "Test______________________________________/TestText1______________________________________.txt" , "Test/normalText.txt" })]
    [InlineData("NullCharFileName_FromWindows.zip", new string[] { "a_6b6d" })]
    [InlineData("NullCharFileName_FromUnix.zip",    new string[] { "a_6b6d" })]
    [InlineData("WindowsInvalid_FromUnix.zip",      new string[] { "aa_b_d" })]
    [InlineData("WindowsInvalid_FromWindows.zip",   new string[] { "aa_b_d" })]
    public void Windows_ZipWithInvalidFileNames(string zipFileName, string[] expectedFiles)
    {
        string testDirectory = GetTestFilePath();

        using Stream source = File.OpenRead(compat(zipFileName));
        ZipFile.ExtractToDirectory(source, testDirectory);
        foreach (string expectedFile in expectedFiles)
        {
            string path = Path.Combine(testDirectory, expectedFile);
            Assert.True(File.Exists(path));
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("backslashes_FromUnix", "dd")]
    [InlineData("backslashes_FromWindows", "dd")]
    [PlatformSpecific(TestPlatforms.Windows)]  // Checks Windows-specific invalid file path
    public void Windows_ZipWithOSSpecificFileNames(string zipName, string fileName)
    {
        string tempDir = GetTestFilePath();
        using Stream source = File.OpenRead(compat(zipName) + ".zip");
        ZipFile.ExtractToDirectory(source, tempDir);
        string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
        Assert.Equal(1, results.Length);
        Assert.Equal(fileName, Path.GetFileName(results[0]));
    }

    [Fact]
    public void ExtractToDirectoryOverwrite()
    {
        string folderName = zfolder("normal");

        using TempDirectory tempFolder = new(GetTestFilePath());
        using Stream source = File.OpenRead(zfile("normal.zip"));
        ZipFile.ExtractToDirectory(source, tempFolder.Path, overwriteFiles: false);
        source.Position = 0;
        Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(source, tempFolder.Path /* default false */));
        source.Position = 0;
        Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(source, tempFolder.Path, overwriteFiles: false));
        source.Position = 0;
        ZipFile.ExtractToDirectory(source, tempFolder.Path, overwriteFiles: true);

        DirsEqual(tempFolder.Path, folderName);
    }

    [Fact]
    public void ExtractToDirectoryOverwriteEncoding()
    {
        string folderName = zfolder("normal");

        using TempDirectory tempFolder = new TempDirectory(GetTestFilePath());
        using Stream source = File.OpenRead(zfile("normal.zip"));
        ZipFile.ExtractToDirectory(source, tempFolder.Path, Encoding.UTF8, overwriteFiles: false);
        source.Position = 0;
        Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(source, tempFolder.Path, Encoding.UTF8 /* default false */));
        source.Position = 0;
        Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(source, tempFolder.Path, Encoding.UTF8, overwriteFiles: false));
        source.Position = 0;
        ZipFile.ExtractToDirectory(source, tempFolder.Path, Encoding.UTF8, overwriteFiles: true);

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
