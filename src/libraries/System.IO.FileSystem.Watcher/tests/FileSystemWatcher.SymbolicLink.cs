// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using Xunit.Sdk;

namespace System.IO.Tests
{
    [ConditionalClass(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
    public class SymbolicLink_Changed_Tests : FileSystemWatcherTest
    {
        private string CreateSymbolicLinkToTarget(string targetPath, bool isDirectory, string linkPath = null)
        {
            linkPath ??= GetRandomLinkPath();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, targetPath, isDirectory));

            return linkPath;
        }

        [Fact]
        public void FileSystemWatcher_FileSymbolicLink_TargetsFile_Fails()
        {
            // Arrange
            string tempFile = CreateTestFile();
            string linkPath = CreateSymbolicLinkToTarget(tempFile, isDirectory: false);

            // Act - Assert
            Assert.Throws<ArgumentException>(() => new FileSystemWatcher(linkPath));
        }

        // Windows 7 and 8.1 doesn't throw in this case, see https://github.com/dotnet/runtime/issues/53010.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10OrLater))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/53366")]
        public void FileSystemWatcher_DirectorySymbolicLink_TargetsFile_Fails()
        {
            // Arrange
            string tempFile = CreateTestFile();
            string linkPath = CreateSymbolicLinkToTarget(tempFile, isDirectory: true);
            using var watcher = new FileSystemWatcher(linkPath);

            // Act - Assert
            Assert.Throws<FileNotFoundException>(() => watcher.EnableRaisingEvents = true);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FileSystemWatcher_DirectorySymbolicLink_TargetsSelf_Fails()
        {
            // Arrange
            string linkPath = GetRandomLinkPath();
            CreateSymbolicLinkToTarget(targetPath: linkPath, isDirectory: true, linkPath: linkPath);
            using var watcher = new FileSystemWatcher(linkPath);

            // Act - Assert
            Assert.Throws<FileNotFoundException>(() => watcher.EnableRaisingEvents = true);
        }

        [Fact]
        public void FileSystemWatcher_SymbolicLink_TargetsDirectory_Create()
        {
            // Arrange
            string tempDir = CreateTestDirectory();
            string linkPath = CreateSymbolicLinkToTarget(tempDir, isDirectory: true);

            using var watcher = new FileSystemWatcher(linkPath);
            watcher.NotifyFilter = NotifyFilters.DirectoryName;

            string subDirName = GetTestFileName();
            string subDirPath = Path.Combine(tempDir, subDirName);

            // Act - Assert
            ExpectEvent(watcher, WatcherChangeTypes.Created,
                action: () => Directory.CreateDirectory(subDirPath),
                cleanup: () => Directory.Delete(subDirPath),
                expectedPath: Path.Combine(linkPath, subDirName));
        }

        [Fact]
        public void FileSystemWatcher_SymbolicLink_TargetsDirectory_Create_IncludeSubdirectories()
        {
            FileSystemWatcherTest.Execute(() =>
            {
                // Arrange
                const string subDir = "subDir";
                const string subDirLv2 = "subDirLv2";
                string tempDir = GetTestFilePath();
                string tempSubDir = CreateTestDirectory(tempDir, subDir);

                string linkPath = CreateSymbolicLinkToTarget(tempDir, isDirectory: true);
                using var watcher = new FileSystemWatcher(linkPath);
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                string subDirLv2Path = Path.Combine(tempSubDir, subDirLv2);

                // Act - Assert
                ExpectNoEvent(watcher, WatcherChangeTypes.Created,
                    action: () => Directory.CreateDirectory(subDirLv2Path),
                    cleanup: () => Directory.Delete(subDirLv2Path));

                // Turn include subdirectories on.
                watcher.IncludeSubdirectories = true;

                ExpectEvent(watcher, WatcherChangeTypes.Created,
                    action: () => Directory.CreateDirectory(subDirLv2Path),
                    cleanup: () => Directory.Delete(subDirLv2Path),
                    expectedPath: Path.Combine(linkPath, subDir, subDirLv2));
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/70450", TestPlatforms.OSX)]
        public void FileSystemWatcher_SymbolicLink_IncludeSubdirectories_DoNotDereferenceChildLink()
        {
            // Arrange
            string dirA = CreateTestDirectory();
            string dirB = CreateTestDirectory();

            string linkPath = Path.Combine(dirA, GetRandomDirName() + ".link");
            CreateSymbolicLinkToTarget(dirB, isDirectory: true, linkPath);

            using var watcher = new FileSystemWatcher(dirA);
            watcher.NotifyFilter = NotifyFilters.DirectoryName;
            watcher.IncludeSubdirectories = true;

            string subDirPath = Path.Combine(linkPath, GetTestFileName());

            // Act - Assert
            ExpectNoEvent(watcher, WatcherChangeTypes.Created,
                action: () => Directory.CreateDirectory(subDirPath),
                cleanup: () => Directory.Delete(subDirPath));
        }
    }
}
