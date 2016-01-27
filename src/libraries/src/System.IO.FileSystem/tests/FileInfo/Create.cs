// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
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
