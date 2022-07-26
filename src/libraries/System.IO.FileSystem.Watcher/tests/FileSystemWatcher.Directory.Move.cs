// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        [ConditionalTheory]
        [PlatformSpecific(TestPlatforms.OSX)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Directory_Move_Multiple_From_Watched_To_Unwatched_Mac(int filesCount)
        {
            if (Environment.OSVersion.Version.Major == 12)
            {
                throw new SkipTestException("Unreliable on Monterey"); // https://github.com/dotnet/runtime/issues/70164
            }

            // On Mac, the FSStream aggregate old events caused by the test setup.
            // There is no option how to get rid of it but skip it.
            DirectoryMove_Multiple_FromWatchedToUnwatched(filesCount, skipOldEvents: true);
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.OSX, "Not supported on OSX.")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51393", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
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

        [Fact]
        public void Directory_Move_SynchronizingObject()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                string sourcePath = dir;
                string targetPath = Path.Combine(TestDirectory, "target");

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, WatcherChangeTypes.Renamed, action, cleanup, targetPath);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }

        #region Test Helpers

        private void DirectoryMove_SameDirectory(WatcherChangeTypes eventType)
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                string sourcePath = dir;
                string targetPath = Path.Combine(TestDirectory, "target");

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void DirectoryMove_DifferentWatchedDirectory(WatcherChangeTypes eventType)
        {
            string sourceDir = Path.Combine(TestDirectory, "source");
            string adjacentDir = CreateTestDirectory(TestDirectory, "adj");
            string dir = CreateTestDirectory(sourceDir, "dir");

            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                string sourcePath = dir;
                string targetPath = Path.Combine(adjacentDir, "target");

                // Move the dir to a different directory under the Watcher
                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, new string[] { sourceDir, adjacentDir });
            }
        }

        private void DirectoryMove_Multiple_FromWatchedToUnwatched(int filesCount, bool skipOldEvents)
        {
            Assert.InRange(filesCount, 0, int.MaxValue);

            string watchedTestDirectory = CreateTestDirectory();
            string unwatchedTestDirectory = CreateTestDirectory();
            var dirs = Enumerable.Range(0, filesCount)
                            .Select(i => new
                            {
                                DirectoryInWatchedDir = Path.Combine(watchedTestDirectory, $"dir{i}"),
                                DirectoryInUnwatchedDir = Path.Combine(unwatchedTestDirectory, $"dir{i}")
                            }).ToArray();

            Array.ForEach(dirs, (dir) => Directory.CreateDirectory(dir.DirectoryInWatchedDir));

            using var watcher = new FileSystemWatcher(watchedTestDirectory, "*");

            Action action = () => Array.ForEach(dirs, dir => Directory.Move(dir.DirectoryInWatchedDir, dir.DirectoryInUnwatchedDir));

            // On macOS, for each file we receive two events as describe in comment below.
            int expectEvents = filesCount;
            if (skipOldEvents)
                expectEvents = expectEvents * 2;

            IEnumerable<FiredEvent> events = ExpectEvents(watcher, expectEvents, action);

            if (skipOldEvents)
                events = events.Where(x => x.EventType != WatcherChangeTypes.Created);

            var expectedEvents = dirs.Select(dir => new FiredEvent(WatcherChangeTypes.Deleted, dir.DirectoryInWatchedDir));

            // Remove Created events as there is racecondition when create dir and then observe parent folder. It receives Create event altought Watcher is not registered yet.
            Assert.Equal(expectedEvents, events.Where(x => x.EventType != WatcherChangeTypes.Created));


        }

        private void DirectoryMove_Multiple_FromUnwatchedToWatched(int filesCount)
        {
            Assert.InRange(filesCount, 0, int.MaxValue);

            string watchedTestDirectory = CreateTestDirectory();
            string unwatchedTestDirectory = CreateTestDirectory();

            var dirs = Enumerable.Range(0, filesCount)
                            .Select(i => new
                            {
                                DirectoryInWatchedDir = Path.Combine(watchedTestDirectory, $"dir{i}"),
                                DirectoryInUnwatchedDir = Path.Combine(unwatchedTestDirectory, $"dir{i}")
                            }).ToArray();

            Array.ForEach(dirs, (dir) => Directory.CreateDirectory(dir.DirectoryInUnwatchedDir));

            using var watcher = new FileSystemWatcher(watchedTestDirectory, "*");

            Action action = () => Array.ForEach(dirs, dir => Directory.Move(dir.DirectoryInUnwatchedDir, dir.DirectoryInWatchedDir));

            List<FiredEvent> events = ExpectEvents(watcher, filesCount, action);
            var expectedEvents = dirs.Select(dir => new FiredEvent(WatcherChangeTypes.Created, dir.DirectoryInWatchedDir));

            Assert.Equal(expectedEvents, events);
        }

        private void DirectoryMove_FromWatchedToUnwatched(WatcherChangeTypes eventType)
        {
            string watchedTestDirectory = GetTestFilePath();
            string unwatchedTestDirectory = CreateTestDirectory();
            string dir = CreateTestDirectory(watchedTestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(watchedTestDirectory, "*"))
            {
                string sourcePath = dir; // watched
                string targetPath = Path.Combine(unwatchedTestDirectory, "target");

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, sourcePath);
            }
        }

        private void DirectoryMove_FromUnwatchedToWatched(WatcherChangeTypes eventType)
        {
            string watchedTestDirectory = CreateTestDirectory();
            string unwatchedTestDirectory = GetTestFilePath();
            string dir = CreateTestDirectory(unwatchedTestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(watchedTestDirectory, "*"))
            {
                string sourcePath = dir; // unwatched
                string targetPath = Path.Combine(watchedTestDirectory, "target");

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void DirectoryMove_NestedDirectory(WatcherChangeTypes eventType, bool includeSubdirectories)
        {
            string nestedDir = CreateTestDirectory(TestDirectory, "first", "second", "nested");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.IncludeSubdirectories = includeSubdirectories;
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                string sourcePath = nestedDir;
                string targetPath = nestedDir + "_2";

                // Move the dir to a different directory within the same nested directory
                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void DirectoryMove_WithNotifyFilter(WatcherChangeTypes eventType)
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                string sourcePath = dir;
                string targetPath = dir + "_2";

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        #endregion
    }
}
