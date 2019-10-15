// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    // Tests that are valid for File, FileInfo, and DirectoryInfo
    public abstract class AllGetSetAttributes : BaseGetSetAttributes
    {
        [Fact]
        public void NullParameters()
        {
            Assert.Throws<ArgumentNullException>(() => GetAttributes(null));
            Assert.Throws<ArgumentNullException>(() => SetAttributes(null, FileAttributes.Normal));
        }

        [Fact]
        public void InvalidParameters()
        {
            Assert.Throws<ArgumentException>(() => GetAttributes(string.Empty));
            Assert.Throws<ArgumentException>(() => SetAttributes(string.Empty, FileAttributes.Normal));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void SetAttributes_MissingFile(char trailingChar)
        {
            Assert.Throws<FileNotFoundException>(() => SetAttributes(GetTestFilePath() + trailingChar, FileAttributes.ReadOnly));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void SetAttributes_MissingDirectory(char trailingChar)
        {
            Assert.Throws<DirectoryNotFoundException>(() => SetAttributes(Path.Combine(GetTestFilePath(), "file" + trailingChar), FileAttributes.ReadOnly));
        }


        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymLinksAreReparsePoints()
        {
            string path = CreateItem();
            string linkPath = GetTestFilePath();

            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: IsDirectory));

            Assert.NotEqual(FileAttributes.ReparsePoint, FileAttributes.ReparsePoint & GetAttributes(path));
            Assert.Equal(FileAttributes.ReparsePoint, FileAttributes.ReparsePoint & GetAttributes(linkPath));
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymLinksReflectSymLinkAttributes()
        {
            string path = CreateItem();
            string linkPath = GetTestFilePath();

            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: IsDirectory));

            SetAttributes(path, FileAttributes.ReadOnly);
            try
            {
                Assert.Equal(FileAttributes.ReadOnly, FileAttributes.ReadOnly & GetAttributes(path));
                Assert.NotEqual(FileAttributes.ReadOnly, FileAttributes.ReadOnly & GetAttributes(linkPath));
            }
            finally
            {
                SetAttributes(path, GetAttributes(path) & ~FileAttributes.ReadOnly);
            }
        }
    }
}
