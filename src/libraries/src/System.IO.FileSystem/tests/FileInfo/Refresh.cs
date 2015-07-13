// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_Refresh : FileSystemTest
    {
        #region UniversalTests

        [Fact]
        public void DeleteThenRefresh()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.True(testFile.Exists);
            testFile.Delete();
            testFile.Refresh();
            Assert.False(testFile.Exists);
        }

        [Fact]
        public void NameChange()
        {
            string source = GetTestFilePath();
            string dest = GetTestFilePath();
            FileInfo testFile = new FileInfo(source);
            testFile.Create().Dispose();
            Assert.Equal(source, testFile.FullName);
            testFile.MoveTo(dest);
            testFile.Refresh();
            Assert.Equal(dest, testFile.FullName);
        }

        [Fact]
        public void AttributeChange()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.True((testFile.Attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly);
            testFile.Attributes = FileAttributes.ReadOnly;
            testFile.Refresh();
            Assert.True((testFile.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
            testFile.Attributes = new FileAttributes();
            testFile.Refresh();
            Assert.True((testFile.Attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly);
        }

        #endregion
    }
}

