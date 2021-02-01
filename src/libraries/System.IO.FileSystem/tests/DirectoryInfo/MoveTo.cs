// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_MoveTo : Directory_Move
    {
        #region Utilities

        protected override void Move(string sourceDir, string destDir)
        {
            new DirectoryInfo(sourceDir).MoveTo(destDir);
        }

        protected virtual void Move(DirectoryInfo sourceDir, string destDir)
        {
            sourceDir.MoveTo(destDir);
        }

        #endregion

        #region UniversalTests

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40536", TestPlatforms.Browser)]
        public void DirectoryPathUpdatesOnMove()
        {
            //NOTE: MoveTo adds a trailing separator character to the FullName of a DirectoryInfo
            string DestName1 = GetTestFileName();
            string DestName2 = GetTestFileName();
            string testDirSource = Path.Combine(TestDirectory, GetTestFileName());
            string testDirDest1 = Path.Combine(TestDirectory, DestName1);
            string testDirDest2 = Path.Combine(TestDirectory, DestName2);

            DirectoryInfo sourceDir = Directory.CreateDirectory(testDirSource);
            Move(sourceDir, testDirDest1);
            Assert.True(Directory.Exists(testDirDest1));
            Assert.False(Directory.Exists(testDirSource));
            Assert.Equal(testDirDest1 + Path.DirectorySeparatorChar, sourceDir.FullName);
            Assert.Equal(DestName1, sourceDir.Name);

            Move(sourceDir, testDirDest2);
            Assert.True(Directory.Exists(testDirDest2));
            Assert.Equal(testDirDest2 + Path.DirectorySeparatorChar, sourceDir.FullName);
            Assert.Equal(DestName2, sourceDir.Name);
        }

        #endregion
    }
}
