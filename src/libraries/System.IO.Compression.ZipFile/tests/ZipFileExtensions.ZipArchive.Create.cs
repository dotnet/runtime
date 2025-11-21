// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_ZipArchive_Create : ZipFileTestBase
    {
        public static IEnumerable<object[]> Get_CreateEntryFromFileExtension_Data()
        {
            foreach (bool withCompressionLevel in _bools)
            {
                foreach (bool async in _bools)
                {
                    yield return new object[] { withCompressionLevel, async };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_CreateEntryFromFileExtension_Data))]
        public async Task CreateEntryFromFileExtension(bool withCompressionLevel, bool async)
        {
            //add file
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            {
                ZipArchive archive = await CallZipFileOpen(async, testArchive.Path, ZipArchiveMode.Update);

                    string entryName = "added.txt";
                    string sourceFilePath = zmodified(Path.Combine("addFile", entryName));

                    await Assert.ThrowsAsync<ArgumentNullException>(() => CallZipFileExtensionsCreateEntryFromFile(async, (ZipArchive)null, sourceFilePath, entryName));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => CallZipFileExtensionsCreateEntryFromFile(async, archive, null, entryName));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => CallZipFileExtensionsCreateEntryFromFile(async, archive, sourceFilePath, null));

                    ZipArchiveEntry e = withCompressionLevel ?
                        await CallZipFileExtensionsCreateEntryFromFile(async, archive, sourceFilePath, entryName) :
                        await CallZipFileExtensionsCreateEntryFromFile(async, archive, sourceFilePath, entryName, CompressionLevel.Fastest);
                    Assert.NotNull(e);

                await DisposeZipArchive(async, archive);

                await IsZipSameAsDir(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read, requireExplicit: false, checkTimes: false, async);
            }
        }
    }
}
