// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    [ConditionalClass(typeof(SymbolicLink_Changed_Tests), nameof(CanCreateSymbolicLinks))]
    public class SymbolicLink_Changed_Tests : FileSystemWatcherTest
    {
        private string CreateSymbolicLinkToTarget(string targetPath, bool isDirectory, string linkPath = null)
        {
            linkPath ??= GetTestFilePath();
            Assert.True(CreateSymLink(targetPath, linkPath, isDirectory));

            return linkPath;
        }

        [Fact]
        public void FileSystemWatcher_FileSymbolicLink_TargetsFile_Fails()
        {
            // Arrange
            using var tempFile = new TempFile(GetTestFilePath());
            string linkPath = CreateSymbolicLinkToTarget(tempFile.Path, isDirectory: false);

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
            using var tempFile = new TempFile(GetTestFilePath());
            string linkPath = CreateSymbolicLinkToTarget(tempFile.Path, isDirectory: true);
            using var watcher = new FileSystemWatcher(linkPath);

            // Act - Assert
            Assert.Throws<FileNotFoundException>(() => watcher.EnableRaisingEvents = true);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FileSystemWatcher_DirectorySymbolicLink_TargetsSelf_Fails()
        {
            // Arrange
            string linkPath = GetTestFilePath();
            CreateSymbolicLinkToTarget(targetPath: linkPath, isDirectory: true, linkPath: linkPath);
            using var watcher = new FileSystemWatcher(linkPath);

            // Act - Assert
            Assert.Throws<FileNotFoundException>(() => watcher.EnableRaisingEvents = true);
        }

        [Fact]
        public void FileSystemWatcher_SymbolicLink_TargetsDirectory_Create()
        {
            // Arrange
            using var tempDir = new TempDirectory(GetTestFilePath());
            string linkPath = CreateSymbolicLinkToTarget(tempDir.Path, isDirectory: true);

            using var watcher = new FileSystemWatcher(linkPath);
            watcher.NotifyFilter = NotifyFilters.DirectoryName;

            string subDirName = GetTestFileName();
            string subDirPath = Path.Combine(tempDir.Path, subDirName);

            // Act - Assert
            ExpectEvent(watcher, WatcherChangeTypes.Created,
                action: () => Directory.CreateDirectory(subDirPath),
                cleanup: () => Directory.Delete(subDirPath),
                expectedPath: Path.Combine(linkPath, subDirName));
        }

        [Fact]
        public void FileSystemWatcher_SymbolicLink_TargetsDirectory_Create_IncludeSubdirectories()
        {
            // Arrange
            const string subDir = "subDir";
            const string subDirLv2 = "subDirLv2";
            using var tempDir = new TempDirectory(GetTestFilePath());
            using var tempSubDir = new TempDirectory(Path.Combine(tempDir.Path, subDir));

            string linkPath = CreateSymbolicLinkToTarget(tempDir.Path, isDirectory: true);
            using var watcher = new FileSystemWatcher(linkPath);
            watcher.NotifyFilter = NotifyFilters.DirectoryName;

            string subDirLv2Path = Path.Combine(tempSubDir.Path, subDirLv2);

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
        }

        [Fact]
        public void FileSystemWatcher_SymbolicLink_IncludeSubdirectories_DoNotDereferenceChildLink()
        {
            // Arrange
            using var dirA = new TempDirectory(GetTestFilePath());
            using var dirB = new TempDirectory(GetTestFilePath());

            string linkPath = Path.Combine(dirA.Path, "linkToDirB");
            CreateSymbolicLinkToTarget(dirB.Path, isDirectory: true, linkPath);

            using var watcher = new FileSystemWatcher(dirA.Path);
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
