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
        public void Extract_SpecialFiles_Windows_ThrowsInvalidOperation()
        {
            string originalFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");
            using TempDirectory root = new TempDirectory();

            string archive = Path.Join(root.Path, "input.tar");
            string destination = Path.Join(root.Path, "dir");

            // Copying the tar to reduce the chance of other tests failing due to being used by another process
            File.Copy(originalFileName, archive);

            Directory.CreateDirectory(destination);

            Assert.Throws<InvalidOperationException>(() => TarFile.ExtractToDirectory(archive, destination, overwriteFiles: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(destination).Count());
        }
    }
}
