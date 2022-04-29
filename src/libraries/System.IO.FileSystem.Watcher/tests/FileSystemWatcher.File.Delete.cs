// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Xunit;
using Xunit.Sdk;

namespace System.IO.Tests
{
    public class File_Delete_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_File_Delete()
        {
            using (var watcher = new FileSystemWatcher(TestDirectory))
            {
                string fileName = Path.Combine(TestDirectory, "file");
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
            using (var watcher = new FileSystemWatcher(TestDirectory))
            {
                string fileName = Path.Combine(TestDirectory, "file");
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
            string nestedDir = CreateTestDirectory(TestDirectory, "dir1", "nested");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.FileName;

                string fileName = Path.Combine(nestedDir, "file");
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
            string deepDir = CreateTestDirectory(TestDirectory, "dir", "dir", "dir", "dir", "dir", "dir", "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.FileName;

                // Put a file at the very bottom and expect it to raise an event
                string fileName = Path.Combine(deepDir, "file");
                Action action = () => File.Delete(fileName);
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName, LongWaitTimeout);
            }
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void FileSystemWatcher_File_Delete_SymLink()
        {
            FileSystemWatcherTest.Execute(() =>
            {
                string dir = CreateTestDirectory(TestDirectory, "dir");
                string temp = CreateTestFile();
                using (var watcher = new FileSystemWatcher(dir, "*"))
                {
                    // Make the symlink in our path (to the temp file) and make sure an event is raised
                    string symLinkPath = Path.Combine(dir, GetRandomLinkName());
                    Action action = () => File.Delete(symLinkPath);
                    Action cleanup = () => Assert.True(MountHelper.CreateSymbolicLink(symLinkPath, temp, false));
                    cleanup();

                    ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, symLinkPath);
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [Fact]
        public void FileSystemWatcher_File_Delete_SynchronizingObject()
        {
            using (var watcher = new FileSystemWatcher(TestDirectory))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                string fileName = Path.Combine(TestDirectory, "file");
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
