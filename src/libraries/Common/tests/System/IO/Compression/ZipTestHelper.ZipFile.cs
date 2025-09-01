// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Compression.Tests;

public partial class ZipFileTestBase : FileCleanupTestBase
{
    protected Task<ZipArchive> CallZipFileOpen(bool async, string archiveFileName, ZipArchiveMode mode)
    {
        return async ?
            ZipFile.OpenAsync(archiveFileName, mode) :
            Task.FromResult(ZipFile.Open(archiveFileName, mode));
    }

    protected Task<ZipArchive> CallZipFileOpen(bool async, string archiveFileName, ZipArchiveMode mode, Encoding? entryNameEncoding)
    {
        return async ?
            ZipFile.OpenAsync(archiveFileName, mode, entryNameEncoding) :
            Task.FromResult(ZipFile.Open(archiveFileName, mode, entryNameEncoding));
    }

    protected Task<ZipArchive> CallZipFileOpenRead(bool async, string archiveFileName)
    {
        return async ?
            ZipFile.OpenReadAsync(archiveFileName) :
            Task.FromResult(ZipFile.OpenRead(archiveFileName));
    }

    protected Task<ZipArchiveEntry> CallZipFileExtensionsCreateEntryFromFile(bool async, ZipArchive archive, string fileName, string entryName)
    {
        return async ?
            archive.CreateEntryFromFileAsync(fileName, entryName) :
            Task.FromResult(archive.CreateEntryFromFile(fileName, entryName));
    }

    protected Task<ZipArchiveEntry> CallZipFileExtensionsCreateEntryFromFile(bool async, ZipArchive archive, string fileName, string entryName, CompressionLevel compressionLevel)
    {
        return async ?
            archive.CreateEntryFromFileAsync(fileName, entryName, compressionLevel) :
            Task.FromResult(archive.CreateEntryFromFile(fileName, entryName, compressionLevel));
    }

    protected Task CallExtractToFile(bool async, ZipArchiveEntry entry, string destinationFileName)
    {
        if (async)
        {
            return entry.ExtractToFileAsync(destinationFileName, overwrite: false);
        }
        else
        {
            entry.ExtractToFile(destinationFileName);
            return Task.CompletedTask;
        }
    }

    protected Task CallExtractToFile(bool async, ZipArchiveEntry entry, string destinationFileName, bool overwrite)
    {
        if (async)
        {
            return entry.ExtractToFileAsync(destinationFileName, overwrite);
        }
        else
        {
            entry.ExtractToFile(destinationFileName, overwrite);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileCreateFromDirectory(bool async, string sourceDirectoryName, Stream destination)
    {
        if (async)
        {
            return ZipFile.CreateFromDirectoryAsync(sourceDirectoryName, destination);
        }
        else
        {
            ZipFile.CreateFromDirectory(sourceDirectoryName, destination);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileCreateFromDirectory(bool async, string sourceDirectoryName, string destinationArchiveFileName)
    {
        if (async)
        {
            return ZipFile.CreateFromDirectoryAsync(sourceDirectoryName, destinationArchiveFileName);
        }
        else
        {
            ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileCreateFromDirectory(bool async, string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory)
    {
        if (async)
        {
            return ZipFile.CreateFromDirectoryAsync(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory);
        }
        else
        {
            ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileCreateFromDirectory(bool async, string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding)
    {
        if (async)
        {
            return ZipFile.CreateFromDirectoryAsync(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, entryNameEncoding);
        }
        else
        {
            ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, entryNameEncoding);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileCreateFromDirectory(bool async, string sourceDirectoryName, Stream destination, CompressionLevel compressionLevel, bool includeBaseDirectory)
    {
        if (async)
        {
            return ZipFile.CreateFromDirectoryAsync(sourceDirectoryName, destination, compressionLevel, includeBaseDirectory);
        }
        else
        {
            ZipFile.CreateFromDirectory(sourceDirectoryName, destination, compressionLevel, includeBaseDirectory);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileCreateFromDirectory(bool async, string sourceDirectoryName, Stream destination, CompressionLevel compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding)
    {
        if (async)
        {
            return ZipFile.CreateFromDirectoryAsync(sourceDirectoryName, destination, compressionLevel, includeBaseDirectory, entryNameEncoding);
        }
        else
        {
            ZipFile.CreateFromDirectory(sourceDirectoryName, destination, compressionLevel, includeBaseDirectory, entryNameEncoding);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileExtractToDirectory(bool async, Stream source, string destinationDirectoryName)
    {
        if (async)
        {
            return ZipFile.ExtractToDirectoryAsync(source, destinationDirectoryName);
        }
        else
        {
            ZipFile.ExtractToDirectory(source, destinationDirectoryName);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileExtractToDirectory(bool async, Stream source, string destinationDirectoryName, bool overwriteFiles)
    {
        if (async)
        {
            return ZipFile.ExtractToDirectoryAsync(source, destinationDirectoryName, overwriteFiles);
        }
        else
        {
            ZipFile.ExtractToDirectory(source, destinationDirectoryName, overwriteFiles);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileExtractToDirectory(bool async, Stream source, string destinationDirectoryName, Encoding? entryNameEncoding)
    {
        if (async)
        {
            return ZipFile.ExtractToDirectoryAsync(source, destinationDirectoryName, entryNameEncoding);
        }
        else
        {
            ZipFile.ExtractToDirectory(source, destinationDirectoryName, entryNameEncoding);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileExtractToDirectory(bool async, Stream source, string destinationDirectoryName, Encoding? entryNameEncoding, bool overwriteFiles)
    {
        if (async)
        {
            return ZipFile.ExtractToDirectoryAsync(source, destinationDirectoryName, entryNameEncoding, overwriteFiles);
        }
        else
        {
            ZipFile.ExtractToDirectory(source, destinationDirectoryName, entryNameEncoding, overwriteFiles);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileExtractToDirectory(bool async, string sourceArchiveFileName, string destinationDirectoryName)
    {
        if (async)
        {
            return ZipFile.ExtractToDirectoryAsync(sourceArchiveFileName, destinationDirectoryName);
        }
        else
        {
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileExtractToDirectory(bool async, string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles)
    {
        if (async)
        {
            return ZipFile.ExtractToDirectoryAsync(sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
        }
        else
        {
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileExtractToDirectory(bool async, string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding)
    {
        if (async)
        {
            return ZipFile.ExtractToDirectoryAsync(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding);
        }
        else
        {
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding);
            return Task.CompletedTask;
        }
    }

    protected Task CallZipFileExtractToDirectory(bool async, string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding, bool overwriteFiles)
    {
        if (async)
        {
            return ZipFile.ExtractToDirectoryAsync(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding, overwriteFiles);
        }
        else
        {
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding, overwriteFiles);
            return Task.CompletedTask;
        }
    }

    protected static Task CallZipFileExtensionsExtractToDirectory(bool async, ZipArchive archive, string destinationDirectoryName)
    {
        if (async)
        {
            return archive.ExtractToDirectoryAsync(destinationDirectoryName);
        }
        else
        {
            archive.ExtractToDirectory(destinationDirectoryName);
            return Task.CompletedTask;
        }
    }

    public static IEnumerable<object[]> Get_Unix_ZipWithInvalidFileNames_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { "NullCharFileName_FromWindows", async };
            yield return new object[] { "NullCharFileName_FromUnix", async };
        }
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

    public static IEnumerable<object[]> Get_Windows_ZipWithOSSpecificFileNames_Data()
    {
        foreach (bool async in _bools)
        {
            yield return new object[] { "backslashes_FromUnix", "dd", async };
            yield return new object[] { "backslashes_FromWindows", "dd", async };
        }
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
}
