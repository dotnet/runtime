// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_ZipArchive_Extract : ZipFileTestBase
    {
        [Fact]
        public void ExtractToDirectoryExtension()
        {
            using (ZipArchive archive = ZipFile.Open(zfile("normal.zip"), ZipArchiveMode.Read))
            {
                string tempFolder = GetTestFilePath();
                Assert.Throws<ArgumentNullException>(() => ((ZipArchive)null).ExtractToDirectory(tempFolder));
                Assert.Throws<ArgumentNullException>(() => archive.ExtractToDirectory(null));
                archive.ExtractToDirectory(tempFolder);

                DirsEqual(tempFolder, zfolder("normal"));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72951", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void ExtractToDirectoryExtension_Unicode()
        {
            using (ZipArchive archive = ZipFile.OpenRead(zfile("unicode.zip")))
            {
                string tempFolder = GetTestFilePath();
                archive.ExtractToDirectory(tempFolder);
                DirFileNamesEqual(tempFolder, zfolder("unicode"));
            }
        }

    }
}
