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
                using (var testDirectory = new TempDirectory(GetTestFilePath()))
                using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
                using (var watcher = new FileSystemWatcher(testDirectory.Path, Path.GetFileName(dir.Path)))
                {
                    Action action = () => Directory.SetLastWriteTime(dir.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                    WatcherChangeTypes expected = WatcherChangeTypes.Changed;
                    ExpectEvent(watcher, expected, action, expectedPath: dir.Path);
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [Fact]
        public void FileSystemWatcher_Directory_Changed_WatchedFolder()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                Action action = () => Directory.SetLastWriteTime(dir.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(watcher, 0, action, expectedPath: dir.Path);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileSystemWatcher_Directory_Changed_Nested(bool includeSubdirectories)
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var nestedDir = new TempDirectory(Path.Combine(firstDir.Path, "nested")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = includeSubdirectories;
                watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.Attributes;

                var attributes = File.GetAttributes(nestedDir.Path);
                Action action = () => File.SetAttributes(nestedDir.Path, attributes | FileAttributes.ReadOnly);
                Action cleanup = () => File.SetAttributes(nestedDir.Path, attributes);

                WatcherChangeTypes expected = includeSubdirectories ? WatcherChangeTypes.Changed : 0;
                ExpectEvent(watcher, expected, action, cleanup, nestedDir.Path);
            }
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void FileSystemWatcher_Directory_Changed_SymLink()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var tempDir = new TempDirectory(Path.Combine(testDirectory.Path, "tempDir")))
            using (var file = new TempFile(Path.Combine(tempDir.Path, "test")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                // Setup the watcher
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
                watcher.IncludeSubdirectories = true;
                Assert.True(MountHelper.CreateSymbolicLink(Path.Combine(dir.Path, GetRandomLinkName()), tempDir.Path, true));

                Action action = () => File.AppendAllText(file.Path, "longtext");
                Action cleanup = () => File.AppendAllText(file.Path, "short");

                ExpectEvent(watcher, 0, action, cleanup, dir.Path);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Changed_SynchronizingObject()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, Path.GetFileName(dir.Path)))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                Action action = () => Directory.SetLastWriteTime(dir.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(watcher, WatcherChangeTypes.Changed, action, expectedPath: dir.Path);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }
    }
}
