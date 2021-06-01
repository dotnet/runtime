// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class Directory_Move_Tests : FileSystemWatcherTest
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected WatcherChangeTypes are different based on OS
        public void Directory_Move_To_Same_Directory()
        {
            DirectoryMove_SameDirectory(WatcherChangeTypes.Renamed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void Directory_Move_From_Watched_To_Unwatched()
        {
            DirectoryMove_FromWatchedToUnwatched(WatcherChangeTypes.Deleted);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.OSX)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Directory_Move_Multiple_From_Watched_To_Unwatched_Mac(int filesCount)
        {
            // On Mac, the FSStream aggregate old events caused by the test setup.
            // There is no option how to get rid of it but skip it.
            DirectoryMove_Multiple_FromWatchedToUnwatched(filesCount, skipOldEvents: true);
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.OSX, "Not supported on OSX.")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Directory_Move_Multiple_From_Watched_To_Unwatched(int filesCount)
        {
            DirectoryMove_Multiple_FromWatchedToUnwatched(filesCount, skipOldEvents: false);
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.FreeBSD, "Not supported on FreeBSD.")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Directory_Move_Multiple_From_Unatched_To_Watched(int filesCount)
        {
            DirectoryMove_Multiple_FromUnwatchedToWatched(filesCount);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected WatcherChangeTypes are different based on OS
        public void Windows_Directory_Move_To_Different_Watched_Directory()
        {
            DirectoryMove_DifferentWatchedDirectory(WatcherChangeTypes.Changed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected WatcherChangeTypes are different based on OS
        public void Unix_Directory_Move_To_Different_Watched_Directory()
        {
            DirectoryMove_DifferentWatchedDirectory(0);
        }

        [Fact]
        public void Directory_Move_From_Unwatched_To_Watched()
        {
            DirectoryMove_FromUnwatchedToWatched(WatcherChangeTypes.Created);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Directory_Move_In_Nested_Directory(bool includeSubdirectories)
        {
            DirectoryMove_NestedDirectory(includeSubdirectories ? WatcherChangeTypes.Renamed : 0, includeSubdirectories);
        }

        [Fact]
        public void Directory_Move_With_Set_NotifyFilter()
        {
            DirectoryMove_WithNotifyFilter(WatcherChangeTypes.Renamed);
        }

        #region Test Helpers

        private void DirectoryMove_SameDirectory(WatcherChangeTypes eventType)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, "*"))
            {
                string sourcePath = dir.Path;
                string targetPath = Path.Combine(testDirectory.Path, "target");

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void DirectoryMove_DifferentWatchedDirectory(WatcherChangeTypes eventType)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var sourceDir = new TempDirectory(Path.Combine(testDirectory.Path, "source")))
            using (var adjacentDir = new TempDirectory(Path.Combine(testDirectory.Path, "adj")))
            using (var dir = new TempDirectory(Path.Combine(sourceDir.Path, "dir")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, "*"))
            {
                string sourcePath = dir.Path;
                string targetPath = Path.Combine(adjacentDir.Path, "target");

                // Move the dir to a different directory under the Watcher
                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, new string[] { sourceDir.Path, adjacentDir.Path });
            }
        }

        private void DirectoryMove_Multiple_FromWatchedToUnwatched(int filesCount, bool skipOldEvents)
        {
            Assert.InRange(filesCount, 0, int.MaxValue);

            using var watchedTestDirectory = new TempDirectory(GetTestFilePath());
            using var unwatchedTestDirectory = new TempDirectory(GetTestFilePath());

            var dirs = Enumerable.Range(0, filesCount)
                            .Select(i => new
                            {
                                DirecoryInWatchedDir = Path.Combine(watchedTestDirectory.Path, $"dir{i}"),
                                DirecoryInUnwatchedDir = Path.Combine(unwatchedTestDirectory.Path, $"dir{i}")
                            }).ToArray();

            Array.ForEach(dirs, (dir) => Directory.CreateDirectory(dir.DirecoryInWatchedDir));

            using var watcher = new FileSystemWatcher(watchedTestDirectory.Path, "*");

            Action action = () => Array.ForEach(dirs, dir => Directory.Move(dir.DirecoryInWatchedDir, dir.DirecoryInUnwatchedDir));

            // On macOS, for each file we receive two events as describe in comment below.
            int expectEvents = filesCount;
            if (skipOldEvents)
                expectEvents = expectEvents * 2;

            IEnumerable<FiredEvent> events = ExpectEvents(watcher, expectEvents, action);

            if (skipOldEvents)
                events = events.Where(x => x.EventType != WatcherChangeTypes.Created);

            var expectedEvents = dirs.Select(dir => new FiredEvent(WatcherChangeTypes.Deleted, dir.DirecoryInWatchedDir));

            // Remove Created events as there is racecondition when create dir and then observe parent folder. It receives Create event altought Watcher is not registered yet.
            Assert.Equal(expectedEvents, events.Where(x => x.EventType != WatcherChangeTypes.Created));


        }

        private void DirectoryMove_Multiple_FromUnwatchedToWatched(int filesCount)
        {
            Assert.InRange(filesCount, 0, int.MaxValue);

            using var watchedTestDirectory = new TempDirectory(GetTestFilePath());
            using var unwatchedTestDirectory = new TempDirectory(GetTestFilePath());


            var dirs = Enumerable.Range(0, filesCount)
                            .Select(i => new
                            {
                                DirecoryInWatchedDir = Path.Combine(watchedTestDirectory.Path, $"dir{i}"),
                                DirecoryInUnwatchedDir = Path.Combine(unwatchedTestDirectory.Path, $"dir{i}")
                            }).ToArray();

            Array.ForEach(dirs, (dir) => Directory.CreateDirectory(dir.DirecoryInUnwatchedDir));

            using var watcher = new FileSystemWatcher(watchedTestDirectory.Path, "*");

            Action action = () => Array.ForEach(dirs, dir => Directory.Move(dir.DirecoryInUnwatchedDir, dir.DirecoryInWatchedDir));

            List<FiredEvent> events = ExpectEvents(watcher, filesCount, action);
            var expectedEvents = dirs.Select(dir => new FiredEvent(WatcherChangeTypes.Created, dir.DirecoryInWatchedDir));

            Assert.Equal(expectedEvents, events);
        }

        private void DirectoryMove_FromWatchedToUnwatched(WatcherChangeTypes eventType)
        {
            using (var watchedTestDirectory = new TempDirectory(GetTestFilePath()))
            using (var unwatchedTestDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(watchedTestDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(watchedTestDirectory.Path, "*"))
            {
                string sourcePath = dir.Path; // watched
                string targetPath = Path.Combine(unwatchedTestDirectory.Path, "target");

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, sourcePath);
            }
        }

        private void DirectoryMove_FromUnwatchedToWatched(WatcherChangeTypes eventType)
        {
            using (var watchedTestDirectory = new TempDirectory(GetTestFilePath()))
            using (var unwatchedTestDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(unwatchedTestDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(watchedTestDirectory.Path, "*"))
            {
                string sourcePath = dir.Path; // unwatched
                string targetPath = Path.Combine(watchedTestDirectory.Path, "target");

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void DirectoryMove_NestedDirectory(WatcherChangeTypes eventType, bool includeSubdirectories)
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "first")))
            using (var secondDir = new TempDirectory(Path.Combine(firstDir.Path, "second")))
            using (var nestedDir = new TempDirectory(Path.Combine(secondDir.Path, "nested")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = includeSubdirectories;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                string sourcePath = nestedDir.Path;
                string targetPath = nestedDir.Path + "_2";

                // Move the dir to a different directory within the same nested directory
                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void DirectoryMove_WithNotifyFilter(WatcherChangeTypes eventType)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                string sourcePath = dir.Path;
                string targetPath = dir.Path + "_2";

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        #endregion
    }
}
