// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_ZipArchive_Extract : ZipFileTestBase
    {
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectoryExtension(bool async)
        {
            ZipArchive archive = await CallZipFileOpen(async, zfile("normal.zip"), ZipArchiveMode.Read);

            string tempFolder = GetTestFilePath();
            await Assert.ThrowsAsync<ArgumentNullException>(() => CallZipFileExtensionsExtractToDirectory(async, (ZipArchive)null, tempFolder));
            await Assert.ThrowsAsync<ArgumentNullException>(() => CallZipFileExtensionsExtractToDirectory(async, archive, null));
            await CallZipFileExtensionsExtractToDirectory(async, archive, tempFolder);

            await DirsEqual(tempFolder, zfolder("normal"));

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectoryExtension_Unicode(bool async)
        {
            ZipArchive archive = await CallZipFileOpenRead(async, zfile("unicode.zip"));

            string tempFolder = GetTestFilePath();
            await CallZipFileExtensionsExtractToDirectory(async, archive, tempFolder);
            DirFileNamesEqual(tempFolder, zfolder("unicode"));

            await DisposeZipArchive(async, archive);

        }

    }
}
