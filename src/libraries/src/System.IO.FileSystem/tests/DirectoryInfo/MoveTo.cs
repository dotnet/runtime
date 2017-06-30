// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_MoveTo : Directory_Move
    {
        #region Utilities

        public override void Move(string sourceDir, string destDir)
        {
            new DirectoryInfo(sourceDir).MoveTo(destDir);
        }

        public virtual void Move(DirectoryInfo sourceDir, string destDir)
        {
            sourceDir.MoveTo(destDir);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void DirectoryPathUpdatesOnMove()
        {
            //NOTE: MoveTo adds a trailing separator character to the FullName of a DirectoryInfo
            string testDirSource = Path.Combine(TestDirectory, GetTestFileName());
            string testDirDest1 = Path.Combine(TestDirectory, GetTestFileName());
            string testDirDest2 = Path.Combine(TestDirectory, GetTestFileName());

            DirectoryInfo sourceDir = Directory.CreateDirectory(testDirSource);
            Move(sourceDir, testDirDest1);
            Assert.True(Directory.Exists(testDirDest1));
            Assert.False(Directory.Exists(testDirSource));
            Assert.Equal(testDirDest1 + Path.DirectorySeparatorChar, sourceDir.FullName);

            Move(sourceDir, testDirDest2);
            Assert.True(Directory.Exists(testDirDest2));
            Assert.Equal(testDirDest2 + Path.DirectorySeparatorChar, sourceDir.FullName);
        }

        #endregion
    }
}
