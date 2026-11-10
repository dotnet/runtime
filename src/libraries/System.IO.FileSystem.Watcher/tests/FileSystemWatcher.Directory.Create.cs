// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
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
            using (var watcher = new FileSystemWatcher(TestDirectory))
            {
                string dirName = Path.Combine(TestDirectory, "dir");
                watcher.Filter = Path.GetFileName(dirName);

                Action action = () => Directory.CreateDirectory(dirName);
                Action cleanup = () => Directory.Delete(dirName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, dirName);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Create_InNestedDirectory()
        {
            string nestedDir = CreateTestDirectory(TestDirectory, "dir1", "nested");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                string dirName = Path.Combine(nestedDir, "dir");
                Action action = () => Directory.CreateDirectory(dirName);
                Action cleanup = () => Directory.Delete(dirName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, dirName);
            }
        }

        [Fact]
        [OuterLoop("This test has a longer than average timeout and may fail intermittently")]
        public void FileSystemWatcher_Directory_Create_DeepDirectoryStructure()
        {
            string deepDir = CreateTestDirectory(TestDirectory, "dir", "dir", "dir", "dir", "dir", "dir", "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                // Put a directory at the very bottom and expect it to raise an event
                string dirPath = Path.Combine(deepDir, "leafdir");
                Action action = () => Directory.CreateDirectory(dirPath);
                Action cleanup = () => Directory.Delete(dirPath);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, dirPath, LongWaitTimeout);
            }
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void FileSystemWatcher_Directory_Create_SymLink()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            string temp = CreateTestDirectory();
            using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir), "*"))
            {
                // Make the symlink in our path (to the temp folder) and make sure an event is raised
                string symLinkPath = Path.Combine(dir, GetRandomLinkName());
                Action action = () => Assert.True(MountHelper.CreateSymbolicLink(symLinkPath, temp, true));
                Action cleanup = () => Directory.Delete(symLinkPath);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, symLinkPath);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Create_SynchronizingObject()
        {
            using (var watcher = new FileSystemWatcher(TestDirectory))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                string dirName = Path.Combine(TestDirectory, "dir");
                watcher.Filter = Path.GetFileName(dirName);

                Action action = () => Directory.CreateDirectory(dirName);
                Action cleanup = () => Directory.Delete(dirName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, dirName);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }
    }
}
