// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            FileMove_SameDirectory(WatcherChangeTypes.Created | WatcherChangeTypes.Deleted);
        }

        [Fact]
        public void File_Move_From_Watched_To_Unwatched()
        {
            FileMove_FromWatchedToUnwatched(WatcherChangeTypes.Deleted);
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
            FileMove_DifferentWatchedDirectory(WatcherChangeTypes.Changed);
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
            FileMove_NestedDirectory(includeSubdirectories ? WatcherChangeTypes.Created | WatcherChangeTypes.Deleted : 0, includeSubdirectories);
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
            FileMove_WithNotifyFilter(WatcherChangeTypes.Deleted);
        }

        #region Test Helpers

        private void FileMove_SameDirectory(WatcherChangeTypes eventType)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var testFile = new TempFile(Path.Combine(dir.Path, "file")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                string sourcePath = testFile.Path;
                string targetPath = testFile.Path + "_" + eventType.ToString();

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
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var dir_adjacent = new TempDirectory(Path.Combine(testDirectory.Path, "dir_adj")))
            using (var testFile = new TempFile(Path.Combine(dir.Path, "file")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, "*"))
            {
                string sourcePath = testFile.Path;
                string targetPath = Path.Combine(dir_adjacent.Path, Path.GetFileName(testFile.Path) + "_" + eventType.ToString());

                // Move the testFile to a different directory under the Watcher
                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, new string[] { dir.Path, dir_adjacent.Path });
            }
        }
        
        private void FileMove_FromWatchedToUnwatched(WatcherChangeTypes eventType)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir_watched = new TempDirectory(Path.Combine(testDirectory.Path, "dir_watched")))
            using (var dir_unwatched = new TempDirectory(Path.Combine(testDirectory.Path, "dir_unwatched")))
            using (var testFile = new TempFile(Path.Combine(dir_watched.Path, "file")))
            using (var watcher = new FileSystemWatcher(dir_watched.Path, "*"))
            {
                string sourcePath = testFile.Path; // watched
                string targetPath = Path.Combine(dir_unwatched.Path, "file");

                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, sourcePath);
            }
        }

        private void FileMove_FromUnwatchedToWatched(WatcherChangeTypes eventType)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir_watched = new TempDirectory(Path.Combine(testDirectory.Path, "dir_watched")))
            using (var dir_unwatched = new TempDirectory(Path.Combine(testDirectory.Path, "dir_unwatched")))
            using (var testFile = new TempFile(Path.Combine(dir_unwatched.Path, "file")))
            using (var watcher = new FileSystemWatcher(dir_watched.Path, "*"))
            {
                string sourcePath = testFile.Path; // unwatched
                string targetPath = Path.Combine(dir_watched.Path, "file");

                Action action = () => File.Move(sourcePath, targetPath);
                Action cleanup = () => File.Move(targetPath, sourcePath);

                ExpectEvent(watcher, eventType, action, cleanup, targetPath);
            }
        }

        private void FileMove_NestedDirectory(WatcherChangeTypes eventType, bool includeSubdirectories)
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var nestedDir = new TempDirectory(Path.Combine(firstDir.Path, "nested")))
            using (var nestedFile = new TempFile(Path.Combine(nestedDir.Path, "nestedFile" + eventType.ToString())))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.IncludeSubdirectories = includeSubdirectories;

                string sourcePath = nestedFile.Path;
                string targetPath = nestedFile.Path + "_2";

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
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, "file")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, Path.GetFileName(file.Path)))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                string sourcePath = file.Path;
                string targetPath = Path.Combine(testDirectory.Path, "target");

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