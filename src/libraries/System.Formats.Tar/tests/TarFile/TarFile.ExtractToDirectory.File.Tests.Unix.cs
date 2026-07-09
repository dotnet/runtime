// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarFile_ExtractToDirectory_File_Tests : TarTestsBase
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotPrivilegedProcess))]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task Extract_SpecialFiles_Unix_Unelevated_ThrowsUnauthorizedAccess(bool async)
        {
            string originalFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");
            using TempDirectory root = new TempDirectory();

            string archive = Path.Join(root.Path, "input.tar");
            string destination = Path.Join(root.Path, "dir");

            File.Copy(originalFileName, archive);
            Directory.CreateDirectory(destination);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ExtractToDirectory(archive, destination, overwriteFiles: false, async));

            Assert.Equal(0, Directory.GetFileSystemEntries(destination).Count());
        }
    }
}
