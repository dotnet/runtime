// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_Create : File_Create_str
    {
        #region Utilities

        public override FileStream Create(string path)
        {
            return new FileInfo(path).Create();
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void FullNameUpdatesOnCreate()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDir.FullName, GetTestFileName());
            FileInfo info = new FileInfo(testFile);
            using (FileStream stream = info.Create())
            {
                Assert.True(File.Exists(testFile));
                Assert.Equal(testFile, info.FullName);
            }
        }

        #endregion
    }
}
