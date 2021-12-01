// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class File_Create_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_File_Create()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string fileName = Path.Combine(testDirectory.Path, "file");
                watcher.Filter = Path.GetFileName(fileName);

                Action action = () => File.Create(fileName).Dispose();
                Action cleanup = () => File.Delete(fileName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, fileName);
            }
        }

        [Fact]
        [OuterLoop]
        public void FileSystemWatcher_File_Create_EnablingDisablingNotAffectRaisingEvent()
        {
            ExecuteWithRetry(() =>
            {
                using (var testDirectory = new TempDirectory(GetTestFilePath()))
                using (var watcher = new FileSystemWatcher(testDirectory.Path))
                {
                    string fileName = Path.Combine(testDirectory.Path, "file");
                    watcher.Filter = Path.GetFileName(fileName);

                    int numberOfRaisedEvents = 0;
                    AutoResetEvent autoResetEvent = new AutoResetEvent(false);
                    FileSystemEventHandler handler = (o, e) =>
                    {
                        Interlocked.Increment(ref numberOfRaisedEvents);
                        autoResetEvent.Set();
                    };

                    watcher.Created += handler;

                    for (int i = 0; i < 100; i++)
                    {
                        watcher.EnableRaisingEvents = true;
                        watcher.EnableRaisingEvents = false;
                    }

                    watcher.EnableRaisingEvents = true;

                    // this should raise one and only one event
                    File.Create(fileName).Dispose();
                    Assert.True(autoResetEvent.WaitOne(WaitForExpectedEventTimeout_NoRetry));
                    Assert.False(autoResetEvent.WaitOne(SubsequentExpectedWait));
                    Assert.True(numberOfRaisedEvents == 1);
                }
            });
        }

        [Fact]
        public void FileSystemWatcher_File_Create_ForcedRestart()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string fileName = Path.Combine(testDirectory.Path, "file");
                watcher.Filter = Path.GetFileName(fileName);

                Action action = () =>
                {
                    watcher.NotifyFilter = NotifyFilters.FileName; // change filter to force restart
                    File.Create(fileName).Dispose();
                };
                Action cleanup = () => File.Delete(fileName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, fileName);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Create_InNestedDirectory()
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var nestedDir = new TempDirectory(Path.Combine(firstDir.Path, "nested")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.FileName;

                string fileName = Path.Combine(nestedDir.Path, "file");
                Action action = () => File.Create(fileName).Dispose();
                Action cleanup = () => File.Delete(fileName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, fileName);
            }
        }

        [Fact]
        [OuterLoop("This test has a longer than average timeout and may fail intermittently")]
        public void FileSystemWatcher_File_Create_DeepDirectoryStructure()
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var deepDir = new TempDirectory(Path.Combine(dir.Path, "dir", "dir", "dir", "dir", "dir", "dir", "dir")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.FileName;

                // Put a file at the very bottom and expect it to raise an event
                string fileName = Path.Combine(deepDir.Path, "file");
                Action action = () => File.Create(fileName).Dispose();
                Action cleanup = () => File.Delete(fileName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, fileName, LongWaitTimeout);
            }
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void FileSystemWatcher_File_Create_SymLink()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var temp = new TempFile(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                // Make the symlink in our path (to the temp file) and make sure an event is raised
                string symLinkPath = Path.Combine(dir.Path, Path.GetFileName(temp.Path));
                Action action = () => Assert.True(CreateSymLink(temp.Path, symLinkPath, false));
                Action cleanup = () => File.Delete(symLinkPath);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, symLinkPath);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Create_SynchronizingObject()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                string fileName = Path.Combine(testDirectory.Path, "file");
                watcher.Filter = Path.GetFileName(fileName);

                Action action = () => File.Create(fileName).Dispose();
                Action cleanup = () => File.Delete(fileName);

                ExpectEvent(watcher, WatcherChangeTypes.Created, action, cleanup, fileName);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }
    }
}
