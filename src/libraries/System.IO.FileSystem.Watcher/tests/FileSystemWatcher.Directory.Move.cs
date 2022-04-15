// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
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
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Directory_Move_Multiple_From_Watched_To_Unwatched(int filesCount)
        {
            using var watchedTestDirectory = new TempDirectory(GetTestFilePath());
            using var unwatchedTestDirectory = new TempDirectory(GetTestFilePath());

            var dirs = Enumerable.Range(0, filesCount)
                .Select(i => new
                {
                    DirecoryInWatchedDir = Path.Combine(watchedTestDirectory.Path, $"dir{i}"),
                    DirecoryInUnwatchedDir = Path.Combine(unwatchedTestDirectory.Path, $"dir{i}")
                }).ToArray();

            Array.ForEach(dirs, (dir) => Directory.CreateDirectory(dir.DirecoryInWatchedDir));

            Action action = () => Array.ForEach(dirs, dir => Directory.Move(dir.DirecoryInWatchedDir, dir.DirecoryInUnwatchedDir));
            Action cleanup = () =>
            {
                Array.ForEach(dirs, dir =>
                {
                    TryDeleteDirectory(dir.DirecoryInWatchedDir);
                    TryDeleteDirectory(dir.DirecoryInUnwatchedDir);
                });

                Array.ForEach(dirs, (dir) => Directory.CreateDirectory(dir.DirecoryInWatchedDir));
            };

            using var watcher = new FileSystemWatcher(watchedTestDirectory.Path, "*");
            IEnumerable<FiredEvent> expectedEvents = dirs.Select(dir => new FiredEvent(WatcherChangeTypes.Deleted, dir.DirecoryInWatchedDir));

            int expectedEventCount = filesCount;
            if (PlatformDetection.IsOSXLike) // On macOS, for each directory we receive two events as described in comment below.
            {
                expectedEventCount *= 2;
            }

            WatcherChangeTypes eventTypesToIgnore = 0;
            if (PlatformDetection.IsOSXLike)
            {
                // Remove events as there is a racecondition on macOS.
                // When creating directory and then observe parent folder, watcher receives a Create event altought it is not registered yet.
                eventTypesToIgnore = WatcherChangeTypes.Created;
            }

            ExpectEvents(watcher, action, cleanup, expectedEvents, expectedEventCount, eventTypesToIgnore);
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.FreeBSD, "Not supported on FreeBSD.")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Directory_Move_Multiple_From_Unatched_To_Watched(int filesCount)
        {
            using var watchedTestDirectory = new TempDirectory(GetTestFilePath());
            using var unwatchedTestDirectory = new TempDirectory(GetTestFilePath());

            var dirs = Enumerable.Range(0, filesCount)
                .Select(i => new
                {
                    DirecoryInWatchedDir = Path.Combine(watchedTestDirectory.Path, $"dir{i}"),
                    DirecoryInUnwatchedDir = Path.Combine(unwatchedTestDirectory.Path, $"dir{i}")
                }).ToArray();

            Array.ForEach(dirs, (dir) => Directory.CreateDirectory(dir.DirecoryInUnwatchedDir));


            Action action = () => Array.ForEach(dirs, dir => Directory.Move(dir.DirecoryInUnwatchedDir, dir.DirecoryInWatchedDir));
            Action cleanup = () =>
            {
                Array.ForEach(dirs, dir =>
                {
                    TryDeleteDirectory(dir.DirecoryInWatchedDir);
                    TryDeleteDirectory(dir.DirecoryInUnwatchedDir);
                });

                Array.ForEach(dirs, (dir) => Directory.CreateDirectory(dir.DirecoryInUnwatchedDir));
            };

            using var watcher = new FileSystemWatcher(watchedTestDirectory.Path, "*");
            IEnumerable<FiredEvent> expectedEvents = dirs.Select(dir => new FiredEvent(WatcherChangeTypes.Created, dir.DirecoryInWatchedDir));

            ExpectEvents(watcher, action, cleanup, expectedEvents, filesCount, eventTypesToIgnore: 0);
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

        [Fact]
        public void Directory_Move_SynchronizingObject()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                string sourcePath = dir.Path;
                string targetPath = Path.Combine(testDirectory.Path, "target");

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, WatcherChangeTypes.Renamed, action, cleanup, targetPath);
                Assert.True(invoker.BeginInvoke_Called);
            }
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
