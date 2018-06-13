// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_ZipArchive_Create : ZipFileTestBase
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateEntryFromFileExtension(bool withCompressionLevel)
        {
            //add file
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
                {
                    string entryName = "added.txt";
                    string sourceFilePath = zmodified(Path.Combine("addFile", entryName));

                    Assert.Throws<ArgumentNullException>(() => ((ZipArchive)null).CreateEntryFromFile(sourceFilePath, entryName));
                    Assert.Throws<ArgumentNullException>(() => archive.CreateEntryFromFile(null, entryName));
                    Assert.Throws<ArgumentNullException>(() => archive.CreateEntryFromFile(sourceFilePath, null));

                    ZipArchiveEntry e = withCompressionLevel ?
                        archive.CreateEntryFromFile(sourceFilePath, entryName) :
                        archive.CreateEntryFromFile(sourceFilePath, entryName, CompressionLevel.Fastest);
                    Assert.NotNull(e);
                }
                await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read, requireExplicit: false, checkTimes: false);
            }
        }
    }
}
