// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            if (!CanBeReadOnly) return;
            Assert.Throws<FileNotFoundException>(() => SetAttributes(GetTestFilePath() + trailingChar, FileAttributes.ReadOnly));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void SetAttributes_MissingDirectory(char trailingChar)
        {
            if (!CanBeReadOnly) return;
            Assert.Throws<DirectoryNotFoundException>(() => SetAttributes(Path.Combine(GetTestFilePath(), "file" + trailingChar), FileAttributes.ReadOnly));
        }


        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void SymLinksAreReparsePoints()
        {
            string path = CreateItem();
            string linkPath = GetRandomLinkPath();

            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: IsDirectory));

            Assert.NotEqual(FileAttributes.ReparsePoint, FileAttributes.ReparsePoint & GetAttributes(path));
            Assert.Equal(FileAttributes.ReparsePoint, FileAttributes.ReparsePoint & GetAttributes(linkPath));
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void SymLinksReflectSymLinkAttributes()
        {
            string path = CreateItem();
            string linkPath = GetRandomLinkPath();

            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: IsDirectory));

            SetAttributes(path, FileAttributes.ReadOnly);
            try
            {
                Assert.Equal(FileAttributes.ReadOnly, FileAttributes.ReadOnly & GetAttributes(path));
                if (OperatingSystem.IsWindows())
                {
                    Assert.NotEqual(FileAttributes.ReadOnly, FileAttributes.ReadOnly & GetAttributes(linkPath));   
                }
                else
                {
                    // On Unix, Get/SetAttributes FileAttributes.ReadOnly operates on the target of the link.
                    Assert.Equal(FileAttributes.ReadOnly, FileAttributes.ReadOnly & GetAttributes(linkPath));   
                }
            }
            finally
            {
                SetAttributes(path, GetAttributes(path) & ~FileAttributes.ReadOnly);
            }
        }
    }
}
