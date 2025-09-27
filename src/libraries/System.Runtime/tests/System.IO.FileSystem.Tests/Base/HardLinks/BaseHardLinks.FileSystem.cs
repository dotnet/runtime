// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    // Contains test methods that can be used for FileInfo or File.
    public abstract class BaseHardLinks_FileSystem : FileSystemTest
    {
        public BaseHardLinks_FileSystem()
        {
            Assert.True(MountHelper.CanCreateHardLinks);
        }

        /// <summary>Creates a new file depending on the implementing class.</summary>
        protected abstract void CreateFile(string path);

        protected abstract void AssertLinkExists(FileSystemInfo linkInfo);

        /// <summary>Calls the actual public API for creating a hard link.</summary>
        protected abstract FileSystemInfo CreateHardLink(string path, string pathToTarget);

        [Fact]
        public void CreateHardLink_NullPathToTarget()
        {
            Assert.Throws<ArgumentNullException>(() => CreateHardLink(GetRandomFilePath(), pathToTarget: null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0")]
        public void CreateHardLink_InvalidPathToTarget(string pathToTarget)
        {
            Assert.Throws<ArgumentException>(() => CreateHardLink(GetRandomFilePath(), pathToTarget));
        }

        [Fact]
        public void CreateHardLink_TargetDoesNotExist_Throws()
        {
            string linkPath = GetRandomFilePath();
            string nonExistentTarget = GetRandomFilePath();
            Assert.Throws<FileNotFoundException>(() => CreateHardLink(linkPath, nonExistentTarget));
        }

        [Fact]
        public void CreateHardLink_TargetExists_Succeeds()
        {
            string targetPath = GetRandomFilePath();
            string linkPath = GetRandomFilePath();

            CreateFile(targetPath);

            FileSystemInfo linkInfo = CreateHardLink(linkPath, targetPath);
            AssertLinkExists(linkInfo);

            // Both files should have the same content
            Assert.Equal(File.ReadAllText(targetPath), File.ReadAllText(linkPath));
        }

        [Fact]
        public void CreateHardLink_ModifyViaOneLink_VisibleViaOther()
        {
            string targetPath = GetRandomFilePath();
            string linkPath = GetRandomFilePath();

            File.WriteAllText(targetPath, "original");
            CreateHardLink(linkPath, targetPath);

            // Modify via link
            File.WriteAllText(linkPath, "changed");

            // Read via target
            Assert.Equal("changed", File.ReadAllText(targetPath));
        }

        [Fact]
        public void CreateHardLink_DeleteOneLink_FileStillAccessible()
        {
            string targetPath = GetRandomFilePath();
            string linkPath = GetRandomFilePath();

            File.WriteAllText(targetPath, "data");
            CreateHardLink(linkPath, targetPath);

            // Delete the original file
            File.Delete(targetPath);

            // The link should still exist and have the data
            Assert.Equal("data", File.ReadAllText(linkPath));
        }
    }
}
