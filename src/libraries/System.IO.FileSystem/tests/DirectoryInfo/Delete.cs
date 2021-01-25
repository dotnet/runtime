// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_Delete : Directory_Delete_str
    {
        protected override void Delete(string path)
        {
            new DirectoryInfo(path).Delete();
        }

        [Fact]
        public void DeleteInvalidatesDirectoryInfo()
        {
            string testDir = Path.Combine(GetTestFilePath(), "DirectoryCreate");
            DirectoryInfo testDirectoryInfo = new DirectoryInfo(testDir);
            testDirectoryInfo.Create();
            Assert.True(testDirectoryInfo.Exists);
            testDirectoryInfo.Delete();
            Assert.False(testDirectoryInfo.Exists);
        }
    }

    public class DirectoryInfo_Delete_bool : Directory_Delete_str_bool
    {
        protected override void Delete(string path)
        {
            new DirectoryInfo(path).Delete(false);
        }

        protected override void Delete(string path, bool recursive)
        {
            new DirectoryInfo(path).Delete(recursive);
        }
    }
}
