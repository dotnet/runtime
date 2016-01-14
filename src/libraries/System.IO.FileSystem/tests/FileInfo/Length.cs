// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        [ConditionalFact("CanCreateSymbolicLinks")]
        public void SymLinkLength()
        {
            var path = GetTestFilePath();
            var linkPath = GetTestFilePath();
            using (var tempFile = new TempFile(path, 2000))
            {
                Assert.True(MountHelper.CreateSymbolicLink(linkPath, path));

                var info = new FileInfo(path);
                var linkInfo = new FileInfo(linkPath);
                Assert.Equal(2000, info.Length);
                // On Windows, sym links report 0 size; on Linux, their size is the length of the path they point to
                // Confirm that the size we get is not the size of the target file (and that it's less, since our temporary
                // sym links should never exceed 500 bytes.
                Assert.True(2000 > linkInfo.Length);
            }
        }

        private static bool CanCreateSymbolicLinks
        {
            get
            {
                var path = Path.GetTempFileName();
                var linkPath = path + ".link";
                var ret = MountHelper.CreateSymbolicLink(linkPath, path);
                try { File.Delete(path); } catch { }
                try { File.Delete(linkPath); } catch { }
                return ret;
            }
        }
    }
}