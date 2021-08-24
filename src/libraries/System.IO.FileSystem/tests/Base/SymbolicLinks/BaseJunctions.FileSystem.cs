// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public abstract class BaseJunctions_FileSystem : BaseSymbolicLinks
    {
        protected DirectoryInfo CreateJunction(string junctionPath, string targetPath)
        {
            Assert.True(MountHelper.CreateJunction(junctionPath, targetPath));
            DirectoryInfo junctionInfo = new(junctionPath);
            return junctionInfo;
        }

        protected abstract DirectoryInfo CreateDirectory(string path);

        protected abstract FileSystemInfo ResolveLinkTarget(string junctionPath, bool returnFinalTarget);

        protected abstract void VerifyEnumerateMethods(string junctionPath, string[] expectedFiles, string[] expectedDirectories, string[] expectedEntries);

        protected void VerifyEnumeration(IEnumerable<string> expectedEnumeration, IEnumerable<string> actualEnumeration)
        {
            foreach (string expectedItem in expectedEnumeration)
            {
                Assert.True(actualEnumeration.Contains(expectedItem));
            }
        }

        [Fact]
        public void Junction_ResolveLinkTarget()
        {
            string junctionPath = GetRandomLinkPath();
            string targetPath = GetRandomDirPath();

            CreateDirectory(targetPath);
            DirectoryInfo junctionInfo = CreateJunction(junctionPath, targetPath);

            FileSystemInfo? actualTargetInfo = ResolveLinkTarget(junctionPath, returnFinalTarget: false);
            Assert.True(actualTargetInfo is DirectoryInfo);
            Assert.Equal(targetPath, actualTargetInfo.FullName);
            Assert.Equal(targetPath, junctionInfo.LinkTarget);
        }

        [Fact]
        public void Junction_EnumerateFileSystemEntries()
        {
            // Root
            string targetPath = GetRandomDirPath();
            Directory.CreateDirectory(targetPath);

            string fileName = GetRandomFileName();
            string subDirName = GetRandomDirName();
            string subFileName = Path.Join(subDirName, GetRandomFileName());

            string filePath = Path.Join(targetPath, fileName);
            string subDirPath = Path.Join(targetPath, subDirName);
            string subFilePath = Path.Join(targetPath, subFileName);

            File.Create(filePath).Dispose();
            Directory.CreateDirectory(subDirPath);
            File.Create(subFilePath).Dispose();

            string junctionPath = GetRandomLinkPath();
            CreateJunction(junctionPath, targetPath);

            string jFilePath = Path.Join(junctionPath, fileName);
            string jSubDirPath = Path.Join(junctionPath, subDirName);
            string jSubFilePath = Path.Join(junctionPath, subFileName);

            string[] expectedFiles = new[] { jFilePath, jSubFilePath };
            string[] expectedDirectories = new[] { jSubDirPath };
            string[] expectedEntries = new[] { jFilePath, jSubDirPath, jSubFilePath };

            VerifyEnumerateMethods(junctionPath, expectedFiles, expectedDirectories, expectedEntries);
        }
    }
}
