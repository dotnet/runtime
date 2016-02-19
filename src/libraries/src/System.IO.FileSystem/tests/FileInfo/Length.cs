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

        // In some cases (such as when running without elevated privileges,
        // the symbolic link may fail to create. Only run this test if it creates
        // links successfully.
        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymLinkLength()
        {
            var path = GetTestFilePath();
            var linkPath = GetTestFilePath();
            using (var tempFile = new TempFile(path, 2000))
            {
                Assert.True(MountHelper.CreateSymbolicLink(linkPath, path));

                var info = new FileInfo(path);
                Assert.Equal(2000, info.Length);

                // On Windows, symlinks have length 0.  
                // On Unix, we follow to the target and report on the target's size.
                var linkInfo = new FileInfo(linkPath);
                Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 0 : info.Length, linkInfo.Length);
            }
        }
    }
}
