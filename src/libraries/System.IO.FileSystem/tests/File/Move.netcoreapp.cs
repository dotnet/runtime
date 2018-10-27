// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Linq;

namespace System.IO.Tests
{
    public partial class File_Move
    {
        #region Utilities

        public virtual void Move(string sourceFile, string destFile, bool overwrite)
        {
            File.Move(sourceFile, destFile, overwrite);
        }

        private void MoveDestinationFileDoesNotExist(bool overwrite)
        {   
            string srcPath = GetTestFilePath();
            string destPath = GetTestFilePath();

            byte[] srcContents = new byte[] { 1, 2, 3, 4, 5 };
            File.WriteAllBytes(srcPath, srcContents);

            Move(srcPath, destPath, overwrite);

            Assert.False(File.Exists(srcPath));
            Assert.Equal(srcContents, File.ReadAllBytes(destPath));
        }

        #endregion

        [Fact]
        public void BasicMoveWithOverwriteFileExists()
        {
            string srcPath = GetTestFilePath();
            string destPath = GetTestFilePath();

            byte[] srcContents = new byte[] { 1, 2, 3, 4, 5 };
            byte[] destContents = new byte[] { 6, 7, 8, 9, 10 };
            File.WriteAllBytes(srcPath, srcContents);
            File.WriteAllBytes(destPath, destContents);

            Move(srcPath, destPath, true);

            Assert.False(File.Exists(srcPath));
            Assert.Equal(srcContents, File.ReadAllBytes(destPath));
        }

        [Fact]
        public void BasicMoveWithOverwriteFileDoesNotExist()
        {
            MoveDestinationFileDoesNotExist(true);
        }

        [Fact]
        public void BasicMoveWithoutOverwriteFileDoesNotExist()
        {
            MoveDestinationFileDoesNotExist(false);
        }

        [Fact]
        public void MoveOntoExistingFileNoOverwrite()
        {
            string srcPath = GetTestFilePath();
            string destPath = GetTestFilePath();

            byte[] srcContents = new byte[] { 1, 2, 3, 4, 5 };
            byte[] destContents = new byte[] { 6, 7, 8, 9, 10 };
            File.WriteAllBytes(srcPath, srcContents);
            File.WriteAllBytes(destPath, destContents);

            Assert.Throws<IOException>(() => Move(srcPath, destPath, false));
            Assert.True(File.Exists(srcPath));
            Assert.True(File.Exists(destPath));
            Assert.Equal(destContents, File.ReadAllBytes(destPath));
        }
    }
}
