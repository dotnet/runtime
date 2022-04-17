// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarFile_ExtractToDirectory_File_Tests : TarTestsBase
    {

        [Fact]
        public void Extract_SpecialFiles_Unix_Unelevated_ThrowsUnauthorizedAccess()
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");

            using TempDirectory destination = new TempDirectory();

            Assert.Throws<UnauthorizedAccessException>(() => TarFile.ExtractToDirectory(sourceArchiveFileName, destination.Path, overwriteFiles: false));

            Assert.Equal(0, Directory.GetFiles(destination.Path).Count());
        }
    }
}