// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_MoveTo : File_Move
    {
        public override void Move(string sourceFile, string destFile)
        {
            new FileInfo(sourceFile).MoveTo(destFile);
        }

        [Fact]
        public override void NonExistentPath()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.Throws<FileNotFoundException>(() => Move(GetTestFilePath(), testFile.FullName));
            Assert.Throws<DirectoryNotFoundException>(() => Move(testFile.FullName, Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName())));
            Assert.Throws<DirectoryNotFoundException>(() => Move(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName()), testFile.FullName));
        }
    }
}
