// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.IO.Compression.Tests
{
    public partial class ZipFileTest_ConvenienceMethods : ZipFileTestBase
    {
        [Fact]
        public void ExtractToDirectoryOverwrite()
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, overwriteFiles: false);
                Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path /* default false */));
                Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, overwriteFiles: false));
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, overwriteFiles: true);

                DirsEqual(tempFolder.Path, folderName);
            }
        }

        [Fact]
        public void ExtractToDirectoryOverwriteEncoding()
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: false);
                Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, Encoding.UTF8 /* default false */));
                Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: false));
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: true);

                DirsEqual(tempFolder.Path, folderName);
            }
        }

        [Fact]
        public void ExtractToDirectoryZipArchiveOverwrite()
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(zipFileName, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(tempFolder.Path);
                    Assert.Throws<IOException>(() => archive.ExtractToDirectory(tempFolder.Path /* default false */));
                    Assert.Throws<IOException>(() => archive.ExtractToDirectory(tempFolder.Path, overwriteFiles: false));
                    archive.ExtractToDirectory(tempFolder.Path, overwriteFiles: true);

                    DirsEqual(tempFolder.Path, folderName);
                }
            }
        }
    }
}
