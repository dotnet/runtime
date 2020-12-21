// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests
{
    public class FileAbstractionsTests
    {
        [Fact]
        public void TempFolderStartsInitiallyEmpty()
        {
            using (var scenario = new DisposableFileSystem())
            {
                var contents = scenario.DirectoryInfo.EnumerateFileSystemInfos();

                Assert.Equal(Path.GetFileName(scenario.RootPath), scenario.DirectoryInfo.Name);
                Assert.Equal(scenario.RootPath, scenario.DirectoryInfo.FullName);
                Assert.Empty(contents);
            }
        }

        [Fact]
        public void FilesAreEnumerated()
        {
            using (var scenario = new DisposableFileSystem()
                .CreateFile("alpha.txt"))
            {
                var contents = new DirectoryInfoWrapper(scenario.DirectoryInfo).EnumerateFileSystemInfos();
                var alphaTxt = contents.OfType<FileInfoBase>().Single();

                Assert.Single(contents);
                Assert.Equal("alpha.txt", alphaTxt.Name);
            }
        }

        [Fact]
        public void FoldersAreEnumerated()
        {
            using (var scenario = new DisposableFileSystem()
                .CreateFolder("beta"))
            {
                var contents1 = new DirectoryInfoWrapper(scenario.DirectoryInfo).EnumerateFileSystemInfos();
                var beta = contents1.OfType<DirectoryInfoBase>().Single();
                var contents2 = beta.EnumerateFileSystemInfos();

                Assert.Single(contents1);
                Assert.Equal("beta", beta.Name);
                Assert.Empty(contents2);
            }
        }

        [Fact]
        public void SubFoldersAreEnumerated()
        {
            using (var scenario = new DisposableFileSystem()
                .CreateFolder("beta")
                .CreateFile(Path.Combine("beta", "alpha.txt")))
            {
                var contents1 = new DirectoryInfoWrapper(scenario.DirectoryInfo).EnumerateFileSystemInfos();
                var beta = contents1.OfType<DirectoryInfoBase>().Single();
                var contents2 = beta.EnumerateFileSystemInfos();
                var alphaTxt = contents2.OfType<FileInfoBase>().Single();

                Assert.Single(contents1);
                Assert.Equal("beta", beta.Name);
                Assert.Single(contents2);
                Assert.Equal("alpha.txt", alphaTxt.Name);
            }
        }

        [Fact]
        public void GetDirectoryCanTakeDotDot()
        {
            using (var scenario = new DisposableFileSystem()
                .CreateFolder("gamma")
                .CreateFolder("beta")
                .CreateFile(Path.Combine("beta", "alpha.txt")))
            {
                var directoryInfoBase = new DirectoryInfoWrapper(scenario.DirectoryInfo);
                var gamma = directoryInfoBase.GetDirectory("gamma");
                var dotdot = gamma.GetDirectory("..");
                var contents1 = dotdot.EnumerateFileSystemInfos();
                var beta = dotdot.GetDirectory("beta");
                var contents2 = beta.EnumerateFileSystemInfos();
                var alphaTxt = contents2.OfType<FileInfoBase>().Single();

                Assert.Equal("..", dotdot.Name);
                Assert.Equal(2, contents1.Count());
                Assert.Equal("beta", beta.Name);
                Assert.Single(contents2);
                Assert.Equal("alpha.txt", alphaTxt.Name);
            }
        }
    }
}
