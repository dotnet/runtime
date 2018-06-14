// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMacOsHighSierraOrHigher))]
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
