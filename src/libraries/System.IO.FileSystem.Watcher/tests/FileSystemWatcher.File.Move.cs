// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class File_Move_Tests : FileSystemWatcherTest
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected WatcherChangeTypes are different based on OS
        public void Windows_File_Move_To_Same_Directory()
        {
            FileMove_SameDirectory(WatcherChangeTypes.Renamed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected WatcherChangeTypes are different based on OS
        public void Unix_File_Move_To_Same_Directory()
        {
            FileMove_SameDirectory(WatcherChangeTypes.Renamed);
        }

        [Fact]
        public void File_Move_From_Watched_To_Unwatched()
        {
            FileMove_FromWatchedToUnwatched(WatcherChangeTypes.Deleted);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void File_Move_Multiple_From_Watched_To_Unwatched(int filesCount)
        {
            string watchedTestDirectory = CreateTestDirectory(TestDirectory, "dir_watched");
            string unwatchedTestDirectory = CreateTestDirectory(TestDirectory, "dir_unwatched");

            var files = Enumerable.Range(0, filesCount)
                .Select(i => new
                {
                    FileInWatchedDir = Path.Combine(watchedTestDirectory, $"file{i}"),
                    FileInUnwatchedDir = Path.Combine(unwatchedTestDirectory, $"file{i}")
                }).ToArray();

            Array.ForEach(files, (file) => File.Create(file.FileInWatchedDir).Dispose());

            Action action = () => Array.ForEach(files, file => File.Move(file.FileInWatchedDir, file.FileInUnwatchedDir));
            Action cleanup = () =>
            {
                Array.ForEach(files, file =>
                {
                    File.Delete(file.FileInWatchedDir);
                    File.Delete(file.FileInUnwatchedDir);
                });

                Array.ForEach(files, (file) => File.Create(file.FileInWatchedDir).Dispose());
            };

            using var watcher = new FileSystemWatcher(watchedTestDirectory, "*");

            int expectedEventCount = filesCount;
            if (PlatformDetection.IsOSXLike) // On macOS, for each file we receive three events as described in comment below.
            {
                expectedEventCount = expectedEventCount * 3;
            }

            WatcherChangeTypes eventTypesToIgnore = 0;
            if (PlatformDetection.IsOSXLike)
            {
                // Remove events as there is racecondition on macOS.
                // When creating file and then observe parent folder, watcher receives Create and Changed event although it is not registered yet.
                eventTypesToIgnore = WatcherChangeTypes.Created | WatcherChangeTypes.Changed;
            }

            IEnumerable<FiredEvent> expectedEvents = files.Select(file => new FiredEvent(WatcherChangeTypes.Deleted, file.FileInWatchedDir));

            ExpectEvents(watcher, action, cleanup, expectedEvents, expectedEventCount, eventTypesToIgnore);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void File_Move_Multiple_From_Unwatched_To_Watched(int filesCount)
        {
            string watchedTestDirectory = CreateTestDirectory(TestDirectory, "dir_watched");
            string unwatchedTestDirectory = CreateTestDirectory(TestDirectory, "dir_unwatched");

            var files = Enumerable.Range(0, filesCount)
                .Select(i => new
                {
                    FileInWatchedDir = Path.Combine(watchedTestDirectory, $"file{i}"),
                    FileInUnwatchedDir = Path.Combine(unwatchedTestDirectory, $"file{i}")
                }).ToArray();

            Array.ForEach(files, (file) => File.Create(file.FileInUnwatchedDir).Dispose());

            Action action = () => Array.ForEach(files, file => File.Move(file.FileInUnwatchedDir, file.FileInWatchedDir));
            Action cleanup = () =>
            {
                Array.ForEach(files, file =>
                {
                    File.Delete(file.FileInWatchedDir);
                    File.Delete(file.FileInUnwatchedDir);
                });

                Array.ForEach(files, (file) => File.Create(file.FileInUnwatchedDir).Dispose());
            };

            using var watcher = new FileSystemWatcher(watchedTestDirectory, "*");
            IEnumerable<FiredEvent> expectedEvents = files.Select(file => new FiredEvent(WatcherChangeTypes.Created, file.FileInWatchedDir));

            ExpectEvents(watcher, action, cleanup, expectedEvents, filesCount, eventTypesToIgnore: 0);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected WatcherChangeTypes are different based on OS
        public void Windows_File_Move_To_Different_Watched_Directory()
        {
            FileMove_DifferentWatchedDirectory(WatcherChangeTypes.Changed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // Expected WatcherChangeTypes are different based on OS
        public void OSX_File_Move_To_Different_Watched_Directory()
        {
            FileMove_DifferentWatchedDirectory(0);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]  // Expected WatcherChangeTypes are different based on OS
        public void Linux_File_Move_To_Different_Watched_Directory()
        {
            FileMove_DifferentWatchedDirectory(0);
        }

        [Fact]
        public void File_Move_From_Unwatched_To_Watched()
        {
            FileMove_FromUnwatchedToWatched(WatcherChangeTypes.Created);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected WatcherChangeTypes are different based on OS
        public void Windows_File_Move_In_Nested_Directory(bool includeSubdirectories)
        {
            FileMove_NestedDirectory(includeSubdirectories ? WatcherChangeTypes.Renamed : 0, includeSubdirectories);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected WatcherChangeTypes are different based on OS
        public void Unix_File_Move_In_Nested_Directory(bool includeSubdirectories)
        {
            FileMove_NestedDirectory(includeSubdirectories ? WatcherChangeTypes.Renamed : 0, includeSubdirectories);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected WatcherChangeTypes are different based on OS
        public void Windows_File_Move_With_Set_NotifyFilter()
        {
            FileMove_WithNotifyFilter(WatcherChangeTypes.Renamed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected WatcherChangeTypes are different based on OS
        public void Unix_File_Move_With_Set_NotifyFilter()
        {
            FileMove_WithNotifyFilter(WatcherChangeTypes.Renamed);
        }

        [Fact]
        public void File_Move_SynchronizingObject()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            string testFile = CreateTestFile(dir, "file");
            using (var watcher = new FileSystemWatcher(dir, "*"))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;
                string sourcePath = testFile;
                string targetPath = testFile + "_Renamed";
                // Move the testFile to a different name in the same directory
                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                ExpectEvent(watcher, WatcherChangeTypes.Renamed, action, cleanup, targetPath);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }

        #region Test Helpers

        private void FileMove_SameDirectory(WatcherChangeTypes eventType)
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            string testFile = CreateTestFile(dir, "file");
            using (var watcher = new FileSystemWatcher(dir, "*"))
            {
                string sourcePath = testFile;
                string targetPath = testFile + "_" + eventType.ToString();

                // Move the testFile to a different name in the same directory
                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                if ((eventType & WatcherChangeTypes.Deleted) > 0)
                    ExpectEvent(watcher, eventType, action, cleanup, new string[] { sourcePath, targetPath });
                else
                    ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void FileMove_DifferentWatchedDirectory(WatcherChangeTypes eventType)
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            string dir_adjacent = CreateTestDirectory(TestDirectory, "dir_adj");
            string testFile = CreateTestFile(dir, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                string sourcePath = testFile;
                string targetPath = Path.Combine(dir_adjacent, Path.GetFileName(testFile) + "_" + eventType.ToString());

                // Move the testFile to a different directory under the Watcher
                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, new string[] { dir, dir_adjacent });
            }
        }

        private void FileMove_FromWatchedToUnwatched(WatcherChangeTypes eventType)
        {
            string dir_watched = CreateTestDirectory(TestDirectory, "dir_watched");
            string dir_unwatched = CreateTestDirectory(TestDirectory, "dir_unwatched");
            string testFile = CreateTestFile(dir_watched, "file");
            using (var watcher = new FileSystemWatcher(dir_watched, "*"))
            {
                string sourcePath = testFile; // watched
                string targetPath = Path.Combine(dir_unwatched, "file");

                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, sourcePath);
            }
        }

        private void FileMove_FromUnwatchedToWatched(WatcherChangeTypes eventType)
        {
            string dir_watched = CreateTestDirectory(TestDirectory, "dir_watched");
            string testFile = CreateTestFile(TestDirectory, "dir_unwatched", "file");
            using (var watcher = new FileSystemWatcher(dir_watched, "*"))
            {
                string sourcePath = testFile; // unwatched
                string targetPath = Path.Combine(dir_watched, "file");

                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void FileMove_NestedDirectory(WatcherChangeTypes eventType, bool includeSubdirectories)
        {
            string nestedFile = CreateTestFile(TestDirectory, "dir1", "nested", "nestedFile" + eventType.ToString());
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.IncludeSubdirectories = includeSubdirectories;

                string sourcePath = nestedFile;
                string targetPath = nestedFile + "_2";

                // Move the testFile to a different name within the same nested directory
                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                if ((eventType & WatcherChangeTypes.Deleted) > 0)
                    ExpectEvent(watcher, eventType, action, cleanup, new string[] { targetPath, sourcePath });
                else
                    ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void FileMove_WithNotifyFilter(WatcherChangeTypes eventType)
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                string sourcePath = file;
                string targetPath = Path.Combine(TestDirectory, "target");

                // Move the testFile to a different name under the same directory with active notifyfilters
                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                if ((eventType & WatcherChangeTypes.Deleted) > 0)
                    ExpectEvent(watcher, eventType, action, cleanup, sourcePath);
                else
                    ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        #endregion
    }
}
