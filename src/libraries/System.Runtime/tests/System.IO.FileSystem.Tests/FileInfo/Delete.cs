// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_Delete : File_Delete
    {
        protected override void Delete(string path)
        {
            new FileInfo(path).Delete();
        }

        [Fact]
        public void FileInfoInvalidatedOnDelete()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFilePath = Path.Combine(testDir.FullName, GetTestFileName());
            FileInfo info = new FileInfo(testFilePath);
            using (FileStream fileStream = info.Create())
            {
                Assert.True(info.Exists);
            }

            info.Delete();
            Assert.False(info.Exists);
        }
    }
}
