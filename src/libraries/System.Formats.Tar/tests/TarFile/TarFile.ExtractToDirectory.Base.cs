// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Formats.Tar.Tests;

public abstract class TarFile_ExtractToDirectory_Tests : TarTestsBase
{
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
