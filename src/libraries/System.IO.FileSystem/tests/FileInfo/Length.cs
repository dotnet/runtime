// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
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
    }
}