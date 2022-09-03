// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarFile_ExtractToDirectoryAsync_File_Tests : TarTestsBase
    {
        [ConditionalFact(nameof(IsUnixButNotSuperUser))]
        public async Task Extract_SpecialFiles_Unix_Unelevated_ThrowsUnauthorizedAccess_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                string originalFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");

                string archive = Path.Join(root.Path, "input.tar");
                string destination = Path.Join(root.Path, "dir");

                // Copying the tar to reduce the chance of other tests failing due to being used by another process
                File.Copy(originalFileName, archive);

                Directory.CreateDirectory(destination);

                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => TarFile.ExtractToDirectoryAsync(archive, destination, overwriteFiles: false));

                Assert.Equal(0, Directory.GetFileSystemEntries(destination).Count());
            }
        }
    }
}
