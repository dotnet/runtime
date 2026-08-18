// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

#nullable enable

namespace Microsoft.Extensions.FileSystemGlobbing.Tests
{
    public class FileAbstractionsTests
    {
        [Fact]
        public void TempFolderStartsInitiallyEmpty()
        {
            using (var scenario = new DisposableFileSystem())
            {
                var contents = scenario.DirectoryInfo!.EnumerateFileSystemInfos();

                Assert.Equal(Path.GetFileName(scenario.RootPath), scenario.DirectoryInfo.Name);
                Assert.Equal(scenario.RootPath, scenario.DirectoryInfo.FullName);
                Assert.Empty(contents);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FilesAreEnumerated(bool useInMemory)
        {
            using (var scenario = new DisposableFileSystem(useInMemory)
                .CreateFile("alpha.txt"))
            {
                var contents = scenario.GetDirectoryInfoBase().EnumerateFileSystemInfos();
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
                var contents1 = scenario.GetDirectoryInfoBase().EnumerateFileSystemInfos();
                var beta = contents1.OfType<DirectoryInfoBase>().Single();
                var contents2 = beta.EnumerateFileSystemInfos();

                Assert.Single(contents1);
                Assert.Equal("beta", beta.Name);
                Assert.Empty(contents2);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SubFoldersAreEnumerated(bool useInMemory)
        {
            using (var scenario = new DisposableFileSystem(useInMemory)
                .CreateFolder("beta")
                .CreateFile(Path.Combine("beta", "alpha.txt")))
            {
                var contents1 = scenario.GetDirectoryInfoBase().EnumerateFileSystemInfos();
                var beta = contents1.OfType<DirectoryInfoBase>().Single();
                var contents2 = beta.EnumerateFileSystemInfos();
                var alphaTxt = contents2.OfType<FileInfoBase>().Single();

                Assert.Single(contents1);
                Assert.Equal("beta", beta.Name);
                Assert.Single(contents2);
                Assert.Equal("alpha.txt", alphaTxt.Name);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetDirectoryCanTakeDotDot(bool useInMemory)
        {
            using (var scenario = new DisposableFileSystem(useInMemory)
                .CreateFolder("gamma")
                .CreateFile(Path.Combine("gamma", "delta.txt"))
                .CreateFolder("beta")
                .CreateFile(Path.Combine("beta", "alpha.txt")))
            {
                var directoryInfoBase = scenario.GetDirectoryInfoBase();

                var gamma = directoryInfoBase.GetDirectory("gamma");
                var dotdot = gamma!.GetDirectory("..");
                var contents1 = dotdot!.EnumerateFileSystemInfos();
                var beta = dotdot.GetDirectory("beta");
                var contents2 = beta!.EnumerateFileSystemInfos();
                var alphaTxt = contents2.OfType<FileInfoBase>().Single();
                var beta2 = directoryInfoBase.GetDirectory("beta");

                Assert.Equal("..", dotdot.Name);
                Assert.Equal(2, contents1.Count());
                Assert.Equal("beta", beta.Name);
                Assert.Single(contents2);
                Assert.Equal("alpha.txt", alphaTxt.Name);
                Assert.Equal(beta.FullName, beta2!.FullName);
            }
        }
    }
}
