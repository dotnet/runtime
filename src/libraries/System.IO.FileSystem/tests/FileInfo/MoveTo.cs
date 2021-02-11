// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_MoveTo : File_Move
    {
        protected override void Move(string sourceFile, string destFile)
        {
            new FileInfo(sourceFile).MoveTo(destFile);
        }

        protected override void Move(string sourceFile, string destFile, bool overwrite)
        {
            new FileInfo(sourceFile).MoveTo(destFile, overwrite);
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
