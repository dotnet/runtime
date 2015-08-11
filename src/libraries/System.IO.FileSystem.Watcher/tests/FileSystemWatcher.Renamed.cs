// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading;
using Xunit;

public partial class RenamedTests
{
    [Fact]
    public static void FileSystemWatcher_Renamed_Directory()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.Filter = Path.GetFileName(dir.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Renamed);

            string newName = dir.Path + "_rename";
            Utility.EnsureDelete(newName);

            watcher.EnableRaisingEvents = true;

            dir.Move(newName);

            Utility.ExpectEvent(eventOccured, "renamed");
        }
    }

    [Fact]
    public static void FileSystemWatcher_Renamed_Negative()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            // put everything in our own directory to avoid collisions
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*.*";
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Renamed);

            watcher.EnableRaisingEvents = true;

            // run all scenarios together to avoid unnecessary waits, 
            // assert information is verbose enough to trace to failure cause

            // create a file
            using (var testFile = new TemporaryTestFile(Path.Combine(dir.Path, "file")))
            using (var testDir = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir")))
            {
                // change a file
                testFile.WriteByte(0xFF);
                testFile.Flush();

                // deleting a file & directory by leaving the using block
            }

            Utility.ExpectNoEvent(eventOccured, "created");
        }
    }

    [Fact]
    public static void FileSystemWatcher_Renamed_NestedDirectory()
    {
        Utility.TestNestedDirectoriesHelper(WatcherChangeTypes.Renamed, (AutoResetEvent are, TemporaryTestDirectory ttd) =>
        {
            ttd.Move(ttd.Path + "_2");
            Utility.ExpectEvent(are, "renamed");
        });
    }

    [Fact]
    public static void FileSystemWatcher_Renamed_FileInNestedDirectory()
    {
        Utility.TestNestedDirectoriesHelper(WatcherChangeTypes.Renamed | WatcherChangeTypes.Created, (AutoResetEvent are, TemporaryTestDirectory ttd) =>
        {
            using (var nestedFile = new TemporaryTestFile(Path.Combine(ttd.Path, "nestedFile")))
            {
                Utility.ExpectEvent(are, "file created");
                nestedFile.Move(nestedFile.Path + "_2");
                Utility.ExpectEvent(are, "renamed");
            }
        });
    }

    [Fact]
    // Note: Can't use the TestNestedDirectoriesHelper since we need access to the root
    public static void FileSystemWatcher_Moved_NestedDirectoryRoot()
    {
        // Create a test root with our watch dir and a temp directory since, on the default Ubuntu install, the system
        // temp directory is on a different mount point and Directory.Move does not work across mount points.
        using (var root = Utility.CreateTestDirectory())
        using (var dir = Utility.CreateTestDirectory(Path.Combine(root.Path, "test_root")))
        using (var temp = Utility.CreateTestDirectory(Path.Combine(root.Path, "temp")))
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent createdOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
            AutoResetEvent deletedOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);

            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            using (var dir1 = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir1")))
            {
                Utility.ExpectEvent(createdOccured, "dir1 created");

                using (var dir2 = new TemporaryTestDirectory(Path.Combine(dir1.Path, "dir2")))
                {
                    Utility.ExpectEvent(createdOccured, "dir2 created");

                    using (var file = Utility.CreateTestFile(Path.Combine(dir2.Path, "test file"))) { };

                    // Move the directory out of the watched folder and expect that we get a deleted event
                    string original = dir1.Path;
                    string target = Path.Combine(temp.Path, Path.GetFileName(dir1.Path));
                    dir1.Move(target);
                    Utility.ExpectEvent(deletedOccured, "dir1 moved out");

                    // Move the directory back and expect a created event
                    dir1.Move(original);
                    Utility.ExpectEvent(createdOccured, "dir1 moved back");
                }
            }
        }
    }

    [Fact]
    // Note: Can't use the TestNestedDirectoriesHelper since we need access to the root
    public static void FileSystemWatcher_Moved_NestedDirectoryRootWithoutSubdirectoriesFlag()
    {
        // Create a test root with our watch dir and a temp directory since, on the default Ubuntu install, the system
        // temp directory is on a different mount point and Directory.Move does not work across mount points.
        using (var root = Utility.CreateTestDirectory())
        using (var dir = Utility.CreateTestDirectory(Path.Combine(root.Path, "test_root")))
        using (var temp = Utility.CreateTestDirectory(Path.Combine(root.Path, "temp")))
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent createdOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
            AutoResetEvent deletedOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);

            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;

            using (var dir1 = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir1")))
            {
                Utility.ExpectEvent(createdOccured, "dir1 created");

                using (var dir2 = new TemporaryTestDirectory(Path.Combine(dir1.Path, "dir2")))
                {
                    Utility.ExpectNoEvent(createdOccured, "dir2 created");

                    using (var file = Utility.CreateTestFile(Path.Combine(dir2.Path, "test file"))) { };

                    // Move the directory out of the watched folder and expect that we get a deleted event
                    string original = dir1.Path;
                    string target = Path.Combine(temp.Path, Path.GetFileName(dir1.Path));
                    dir1.Move(target);
                    Utility.ExpectEvent(deletedOccured, "dir1 moved out");

                    // Move the directory back and expect a created event
                    dir1.Move(original);
                    Utility.ExpectEvent(createdOccured, "dir1 moved back");
                }
            }
        }
    }

    [Fact]
    // Note: Can't use the TestNestedDirectoriesHelper since we need access to the root
    public static void FileSystemWatcher_Moved_NestedDirectoryTreeMoveFileAndFolder()
    {
        using (var root = Utility.CreateTestDirectory())
        using (var dir = Utility.CreateTestDirectory(Path.Combine(root.Path, "test_root")))
        using (var temp = Utility.CreateTestDirectory(Path.Combine(root.Path, "temp")))
        using (var dir1 = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir1")))
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created | WatcherChangeTypes.Deleted | WatcherChangeTypes.Changed);

            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            string filePath = Path.Combine(dir1.Path, "test_file");
            using (var file = File.Create(filePath))
            {
                // Wait for the file to be created then make a change to validate that we get a change
                Utility.ExpectEvent(eventOccured, "test file created");
                byte[] buffer = new byte[4096];
                file.Write(buffer, 0, buffer.Length);
                file.Flush();
            }
            Utility.ExpectEvent(eventOccured, "test file changed");

            // Move the nested dir out of scope and validate that we get a single deleted event
            string original = dir1.Path;
            string target = Path.Combine(temp.Path, "dir1");
            dir1.Move(target);
            Utility.ExpectEvent(eventOccured, "nested dir deleted");

            // Move the dir (and child file) back into scope and validate that we get a created event
            dir1.Move(original);
            Utility.ExpectEvent(eventOccured, "nested dir created");

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                byte[] buffer = new byte[4096];
                fs.Write(buffer, 0, buffer.Length);
                fs.Flush();
            }
            Utility.ExpectEvent(eventOccured, "test file changed");
        }
    }
}
