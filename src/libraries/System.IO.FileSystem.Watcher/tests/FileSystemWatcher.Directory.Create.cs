// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class Directory_Create_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_Directory_EmptyPath()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                using (var watcher = new FileSystemWatcher(""))
                {
                }
            });
        }

        [Fact]
        public void FileSystemWatcher_Directory_PathNotExists()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                using (var watcher = new FileSystemWatcher(GetTestFilePath()))
                {
                }
            });
        }

        [Fact]
        public void FileSystemWatcher_Directory_Create()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string dirName = Path.Combine(testDirectory.Path, "dir");
                watcher.Filter = Path.GetFileName(dirName);

                Action action = () => Directory.CreateDirectory(dirName);
                Action cleanup = () => Directory.Delete(dirName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, dirName);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Create_InNestedDirectory()
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var nestedDir = new TempDirectory(Path.Combine(firstDir.Path, "nested")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                string dirName = Path.Combine(nestedDir.Path, "dir");
                Action action = () => Directory.CreateDirectory(dirName);
                Action cleanup = () => Directory.Delete(dirName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, dirName);
            }
        }

        [Fact]
        [OuterLoop("This test has a longer than average timeout and may fail intermittently")]
        public void FileSystemWatcher_Directory_Create_DeepDirectoryStructure()
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var deepDir = new TempDirectory(Path.Combine(dir.Path, "dir", "dir", "dir", "dir", "dir", "dir", "dir")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                // Put a directory at the very bottom and expect it to raise an event
                string dirPath = Path.Combine(deepDir.Path, "leafdir");
                Action action = () => Directory.CreateDirectory(dirPath);
                Action cleanup = () => Directory.Delete(dirPath);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, dirPath, LongWaitTimeout);
            }
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void FileSystemWatcher_Directory_Create_SymLink()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var temp = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir.Path), "*"))
            {
                // Make the symlink in our path (to the temp folder) and make sure an event is raised
                string symLinkPath = Path.Combine(dir.Path, Path.GetFileName(temp.Path));
                Action action = () => Assert.True(CreateSymLink(temp.Path, symLinkPath, true));
                Action cleanup = () => Directory.Delete(symLinkPath);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, symLinkPath);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Create_SynchronizingObject()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                string dirName = Path.Combine(testDirectory.Path, "dir");
                watcher.Filter = Path.GetFileName(dirName);

                Action action = () => Directory.CreateDirectory(dirName);
                Action cleanup = () => Directory.Delete(dirName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, dirName);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }
    }
}
