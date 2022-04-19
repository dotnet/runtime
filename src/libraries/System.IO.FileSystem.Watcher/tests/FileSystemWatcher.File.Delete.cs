// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class File_Delete_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_File_Delete()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string fileName = Path.Combine(testDirectory.Path, "file");
                watcher.Filter = Path.GetFileName(fileName);

                Action action = () => File.Delete(fileName);
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Delete_ForcedRestart()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string fileName = Path.Combine(testDirectory.Path, "file");
                watcher.Filter = Path.GetFileName(fileName);

                Action action = () =>
                {
                    watcher.NotifyFilter = NotifyFilters.FileName; // change filter to force restart
                    File.Delete(fileName);
                };
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Delete_InNestedDirectory()
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var nestedDir = new TempDirectory(Path.Combine(firstDir.Path, "nested")))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.FileName;

                string fileName = Path.Combine(nestedDir.Path, "file");
                Action action = () => File.Delete(fileName);
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName);
            }
        }

        [Fact]
        [OuterLoop("This test has a longer than average timeout and may fail intermittently")]
        public void FileSystemWatcher_File_Delete_DeepDirectoryStructure()
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var deepDir = new TempDirectory(Path.Combine(dir.Path, "dir", "dir", "dir", "dir", "dir", "dir", "dir")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.FileName;

                // Put a file at the very bottom and expect it to raise an event
                string fileName = Path.Combine(deepDir.Path, "file");
                Action action = () => File.Delete(fileName);
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName, LongWaitTimeout);
            }
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void FileSystemWatcher_File_Delete_SymLink()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var temp = new TempFile(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                // Make the symlink in our path (to the temp file) and make sure an event is raised
                string symLinkPath = Path.Combine(dir.Path, GetRandomLinkName());
                Action action = () => File.Delete(symLinkPath);
                Action cleanup = () => Assert.True(MountHelper.CreateSymbolicLink(symLinkPath, temp.Path, false));
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, symLinkPath);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Delete_SynchronizingObject()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                string fileName = Path.Combine(testDirectory.Path, "file");
                watcher.Filter = Path.GetFileName(fileName);

                Action action = () => File.Delete(fileName);
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }
    }
}
