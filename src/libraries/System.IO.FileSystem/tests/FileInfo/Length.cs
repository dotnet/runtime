// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_Length : FileSystemTest
    {
        [Fact]
        public void ZeroLength()
        {
            var testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.Equal(0, testFile.Length);
        }

        [Fact]
        public void SetPositionThenWrite()
        {
            string path = GetTestFilePath();
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                fileStream.SetLength(100);
                fileStream.Position = 100;
                var writer = new StreamWriter(fileStream);
                writer.Write("four");
                writer.Flush();
            }
            var testFile = new FileInfo(path);
            Assert.Equal(104, testFile.Length);
        }

        [Fact]
        public void Length_Of_Directory_Throws_FileNotFoundException()
        {
            string path = GetTestFilePath();
            Directory.CreateDirectory(path);
            FileInfo info = new FileInfo(path);
            Assert.Throws<FileNotFoundException>(() => info.Length);
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymLinkLength()
        {
            string path = GetTestFilePath();
            string linkPath = GetTestFilePath();

            const int FileSize = 2000;
            using (var tempFile = new TempFile(path, FileSize))
            {
                Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: false));

                var info = new FileInfo(path);
                Assert.Equal(FileSize, info.Length);

                // On Windows, symlinks have length 0.  
                // On Unix, a symlink contains the path to the target, and thus has that length
                var linkInfo = new FileInfo(linkPath);
                Assert.Equal(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 0 : path.Length, 
                    linkInfo.Length);

                // On both, FileStream should however open the target such that its length is the target length
                using (FileStream linkFs = File.OpenRead(linkPath))
                {
                    Assert.Equal(FileSize, linkFs.Length);
                }
            }
        }
    }
}
