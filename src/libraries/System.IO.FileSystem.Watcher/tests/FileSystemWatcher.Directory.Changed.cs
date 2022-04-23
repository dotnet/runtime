// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;
using Xunit.Sdk;

namespace System.IO.Tests
{
    public class Directory_Changed_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_Directory_Changed_LastWrite()
        {
            FileSystemWatcherTest.Execute(() =>
            {
                string testDirectory = TestDirectory;
                string dir = Path.Combine(testDirectory, "dir");
                Directory.CreateDirectory(dir);
                using (var watcher = new FileSystemWatcher(testDirectory, Path.GetFileName(dir)))
                {
                    Action action = () => Directory.SetLastWriteTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));

                    WatcherChangeTypes expected = WatcherChangeTypes.Changed;
                    ExpectEvent(watcher, expected, action, expectedPath: dir);
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [Fact]
        public void FileSystemWatcher_Directory_Changed_WatchedFolder()
        {
            string testDirectory = TestDirectory;
            string dir = Path.Combine(testDirectory, "dir");
            Directory.CreateDirectory(dir);
            using (var watcher = new FileSystemWatcher(dir, "*"))
            {
                Action action = () => Directory.SetLastWriteTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(watcher, 0, action, expectedPath: dir);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileSystemWatcher_Directory_Changed_Nested(bool includeSubdirectories)
        {
            string dir = TestDirectory;
            string firstDir = Path.Combine(dir, "dir1");
            string nestedDir = Path.Combine(firstDir, "nested");
            Directory.CreateDirectory(firstDir);
            Directory.CreateDirectory(nestedDir);

            using (var watcher = new FileSystemWatcher(dir, "*"))
            {
                watcher.IncludeSubdirectories = includeSubdirectories;
                watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.Attributes;

                var attributes = File.GetAttributes(nestedDir);
                Action action = () => File.SetAttributes(nestedDir, attributes | FileAttributes.ReadOnly);
                Action cleanup = () => File.SetAttributes(nestedDir, attributes);

                WatcherChangeTypes expected = includeSubdirectories ? WatcherChangeTypes.Changed : 0;
                ExpectEvent(watcher, expected, action, cleanup, nestedDir);
            }
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void FileSystemWatcher_Directory_Changed_SymLink()
        {
            string testDirectory = TestDirectory;
            string dir = Path.Combine(testDirectory, "dir");
            string tempDir = Path.Combine(dir, "tempDir");
            string file = Path.Combine(tempDir, "test");
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(tempDir);
            File.Create(file).Dispose();
            using (var watcher = new FileSystemWatcher(dir, "*"))
            {
                // Setup the watcher
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
                watcher.IncludeSubdirectories = true;
                Assert.True(MountHelper.CreateSymbolicLink(Path.Combine(dir, GetRandomLinkName()), tempDir, true));

                Action action = () => File.AppendAllText(file, "longtext");
                Action cleanup = () => File.AppendAllText(file, "short");

                ExpectEvent(watcher, 0, action, cleanup, dir);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Changed_SynchronizingObject()
        {
            string testDirectory = TestDirectory;
            string dir = Path.Combine(testDirectory, "dir");
            Directory.CreateDirectory(dir);
            using (var watcher = new FileSystemWatcher(testDirectory, Path.GetFileName(dir)))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                Action action = () => Directory.SetLastWriteTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(watcher, WatcherChangeTypes.Changed, action, expectedPath: dir);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }
    }
}
