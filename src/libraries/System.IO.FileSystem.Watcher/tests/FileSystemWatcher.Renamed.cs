// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public partial class RenamedTests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_Renamed_Directory()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.Filter = Path.GetFileName(dir.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Renamed);

                string newName = Path.Combine(testDirectory.Path, GetTestFileName());

                watcher.EnableRaisingEvents = true;

                Directory.Move(dir.Path, newName);

                ExpectEvent(eventOccurred, "renamed");
            }
        }

        [Fact]
        public void FileSystemWatcher_Renamed_Negative()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*.*";
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Renamed);

                watcher.EnableRaisingEvents = true;

                // run all scenarios together to avoid unnecessary waits, 
                // assert information is verbose enough to trace to failure cause

                // create a file
                using (var testFile = new TempFile(Path.Combine(dir.Path, "file")))
                using (var testDir = new TempDirectory(Path.Combine(dir.Path, "dir")))
                {
                    // change a file
                    File.WriteAllText(testFile.Path, "changed");

                    // deleting a file & directory by leaving the using block
                }

                ExpectNoEvent(eventOccurred, "created");
            }
        }

        [Fact]
        public void FileSystemWatcher_Renamed_NestedDirectory()
        {
            TestNestedDirectoriesHelper(GetTestFilePath(), WatcherChangeTypes.Renamed, (AutoResetEvent are, TempDirectory ttd) =>
            {
                Directory.Move(ttd.Path, ttd.Path + "_2");
                ExpectEvent(are, "renamed");
            });
        }

        [Fact]
        public void FileSystemWatcher_Renamed_FileInNestedDirectory()
        {
            TestNestedDirectoriesHelper(GetTestFilePath(), WatcherChangeTypes.Renamed | WatcherChangeTypes.Created, (AutoResetEvent are, TempDirectory ttd) =>
            {
                using (var nestedFile = new TempFile(Path.Combine(ttd.Path, "nestedFile")))
                {
                    ExpectEvent(are, "file created");
                    File.Move(nestedFile.Path, nestedFile.Path + "_2");
                    ExpectEvent(are, "renamed");
                }
            });
        }

        [Fact, OuterLoop]
        // Note: Can't use the TestNestedDirectoriesHelper since we need access to the root
        public void FileSystemWatcher_Moved_NestedDirectoryRoot()
        {
            // Create a test root with our watch dir and a temp directory since, on the default Ubuntu install, the system
            // temp directory is on a different mount point and Directory.Move does not work across mount points.
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(root.Path, "test_root")))
            using (var temp = new TempDirectory(Path.Combine(root.Path, "temp")))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent createdOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
                AutoResetEvent deletedOccurred = WatchForEvents(watcher, WatcherChangeTypes.Deleted);

                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                using (var dir1 = new TempDirectory(Path.Combine(dir.Path, "dir1")))
                {
                    ExpectEvent(createdOccurred, "dir1 created");

                    using (var dir2 = new TempDirectory(Path.Combine(dir1.Path, "dir2")))
                    {
                        ExpectEvent(createdOccurred, "dir2 created");

                        using (var file = new TempFile(Path.Combine(dir2.Path, "test file"))) { };

                        // Move the directory out of the watched folder and expect that we get a deleted event
                        string original = dir1.Path;
                        string target = Path.Combine(temp.Path, Path.GetFileName(dir1.Path));
                        Directory.Move(dir1.Path, target);
                        ExpectEvent(deletedOccurred, "dir1 moved out");

                        // Move the directory back and expect a created event
                        Directory.Move(target, original);
                        ExpectEvent(createdOccurred, "dir1 moved back");
                    }
                }
            }
        }

        [Fact]
        // Note: Can't use the TestNestedDirectoriesHelper since we need access to the root
        public void FileSystemWatcher_Moved_NestedDirectoryRootWithoutSubdirectoriesFlag()
        {
            // Create a test root with our watch dir and a temp directory since, on the default Ubuntu install, the system
            // temp directory is on a different mount point and Directory.Move does not work across mount points.
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(root.Path, "test_root")))
            using (var temp = new TempDirectory(Path.Combine(root.Path, "temp")))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent createdOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
                AutoResetEvent deletedOccurred = WatchForEvents(watcher, WatcherChangeTypes.Deleted);

                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.IncludeSubdirectories = false;
                watcher.EnableRaisingEvents = true;

                using (var dir1 = new TempDirectory(Path.Combine(dir.Path, "dir1")))
                {
                    ExpectEvent(createdOccurred, "dir1 created");

                    using (var dir2 = new TempDirectory(Path.Combine(dir1.Path, "dir2")))
                    {
                        ExpectNoEvent(createdOccurred, "dir2 created");

                        using (var file = new TempFile(Path.Combine(dir2.Path, "test file"))) { };

                        // Move the directory out of the watched folder and expect that we get a deleted event
                        string original = dir1.Path;
                        string target = Path.Combine(temp.Path, Path.GetFileName(dir1.Path));
                        Directory.Move(dir1.Path, target);
                        ExpectEvent(deletedOccurred, "dir1 moved out");

                        // Move the directory back and expect a created event
                        Directory.Move(target, original);
                        ExpectEvent(createdOccurred, "dir1 moved back");
                    }
                }
            }
        }

        [Fact]
        // Note: Can't use the TestNestedDirectoriesHelper since we need access to the root
        public void FileSystemWatcher_Moved_NestedDirectoryTreeMoveFile()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(root.Path, "test_root")))
            using (var temp = new TempDirectory(Path.Combine(root.Path, "temp")))
            using (var dir1 = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created | WatcherChangeTypes.Deleted | WatcherChangeTypes.Changed);

                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                string filePath = Path.Combine(dir1.Path, "test_file");
                using (var file = File.Create(filePath))
                {
                    // Wait for the file to be created then make a change to validate that we get a change
                    ExpectEvent(eventOccurred, "test file created");
                    byte[] buffer = new byte[4096];
                    file.Write(buffer, 0, buffer.Length);
                    file.Flush();
                }
                ExpectEvent(eventOccurred, "test file changed");

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                {
                    byte[] buffer = new byte[4096];
                    fs.Write(buffer, 0, buffer.Length);
                    fs.Flush();
                }
                ExpectEvent(eventOccurred, "test file changed");
            }
        }


        [Fact]
        // Note: Can't use the TestNestedDirectoriesHelper since we need access to the root
        public void FileSystemWatcher_Moved_NestedDirectoryTreeMoveFolder()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(root.Path, "test_root")))
            using (var temp = new TempDirectory(Path.Combine(root.Path, "temp")))
            using (var dir1 = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created | WatcherChangeTypes.Deleted | WatcherChangeTypes.Changed);

                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                // Move the nested dir out of scope and validate that we get a single deleted event
                string original = dir1.Path;
                string target = Path.Combine(temp.Path, "dir1");
                Directory.Move(dir1.Path, target);
                ExpectEvent(eventOccurred, "nested dir deleted");

                // Move the dir (and child file) back into scope and validate that we get a created event
                Directory.Move(target, original);
                ExpectEvent(eventOccurred, "nested dir created");
            }
        }
    }
}