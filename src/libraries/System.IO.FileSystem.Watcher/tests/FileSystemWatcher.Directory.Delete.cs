// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_Delete_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_Directory_Delete()
        {
            string testDirectory = TestDirectory;
            using (var watcher = new FileSystemWatcher(testDirectory))
            {
                string dirName = Path.Combine(testDirectory, "dir");
                watcher.Filter = Path.GetFileName(dirName);

                Action action = () => Directory.Delete(dirName);
                Action cleanup = () => Directory.CreateDirectory(dirName);
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, expectedPath: dirName);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Delete_InNestedDirectory()
        {
            string dir = TestDirectory;
            string firstDir = Path.Combine(dir, "dir1");
            string nestedDir = Path.Combine(firstDir, "nested");
            Directory.CreateDirectory(firstDir);
            Directory.CreateDirectory(nestedDir);
            using (var watcher = new FileSystemWatcher(dir, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                string dirName = Path.Combine(nestedDir, "dir");
                Action action = () => Directory.Delete(dirName);
                Action cleanup = () => Directory.CreateDirectory(dirName);
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, dirName);
            }
        }

        [Fact]
        [OuterLoop("This test has a longer than average timeout and may fail intermittently")]
        public void FileSystemWatcher_Directory_Delete_DeepDirectoryStructure()
        {
            string dir = TestDirectory;
            string deepDir = Path.Combine(dir, "dir", "dir", "dir", "dir", "dir", "dir", "dir");
            Directory.CreateDirectory(deepDir);
            using (var watcher = new FileSystemWatcher(dir, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                // Put a directory at the very bottom and expect it to raise an event
                string dirPath = Path.Combine(deepDir, "leafdir");
                Action action = () => Directory.Delete(dirPath);
                Action cleanup = () => Directory.CreateDirectory(dirPath);
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, dirPath, LongWaitTimeout);
            }
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void FileSystemWatcher_Directory_Delete_SymLink()
        {
            string testDirectory = TestDirectory;
            string dir = Path.Combine(testDirectory, "dir");
            string tempDir = GetTestFilePath();
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(tempDir);
            using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir), "*"))
            {
                // Make the symlink in our path (to the temp folder) and make sure an event is raised
                string symLinkPath = Path.Combine(dir, GetRandomLinkName());
                Action action = () => Directory.Delete(symLinkPath);
                Action cleanup = () => Assert.True(MountHelper.CreateSymbolicLink(symLinkPath, tempDir, true));
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, expectedPath: symLinkPath);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Delete_SynchronizingObject()
        {
            string testDirectory = TestDirectory;
            using (var watcher = new FileSystemWatcher(testDirectory))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                string dirName = Path.Combine(testDirectory, "dir");
                watcher.Filter = Path.GetFileName(dirName);

                Action action = () => Directory.Delete(dirName);
                Action cleanup = () => Directory.CreateDirectory(dirName);
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, dirName);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }
    }
}
