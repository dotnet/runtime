// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
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
