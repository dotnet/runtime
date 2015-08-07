// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public class ChangedTests
{
    [Fact]
    [ActiveIssue(2011, PlatformID.OSX)]
    public static void FileSystemWatcher_Changed_LastWrite_File()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            watcher.EnableRaisingEvents = true;

            File.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

            Utility.ExpectEvent(eventOccured, "changed");
        }
    }

    [Fact]
    [ActiveIssue(2011, PlatformID.OSX)]
    public static void FileSystemWatcher_Changed_LastWrite_Directory()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.Filter = Path.GetFileName(dir.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            watcher.EnableRaisingEvents = true;
            Directory.SetLastWriteTime(dir.Path, DateTime.Now + TimeSpan.FromSeconds(10));

            Utility.ExpectEvent(eventOccured, "changed");
        }
    }


    [Fact]
    public static void FileSystemWatcher_Changed_Negative()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            // put everything in our own directory to avoid collisions
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*.*";
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            // run all scenarios together to avoid unnecessary waits, 
            // assert information is verbose enough to trace to failure cause

            watcher.EnableRaisingEvents = true;

            // create a file
            using (var testFile = new TemporaryTestFile(Path.Combine(dir.Path, "file")))
            using (var testDir = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir")))
            {
                // rename a file in the same directory
                testFile.Move(testFile.Path + "_rename");

                // renaming a directory
                testDir.Move(testDir.Path + "_rename");
                // deleting a file & directory by leaving the using block
            }

            Utility.ExpectNoEvent(eventOccured, "changed");
        }
    }

    [Fact, ActiveIssue(2279)]
    public static void FileSystemWatcher_Changed_WatchedFolder()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            watcher.EnableRaisingEvents = true;

            Directory.SetLastAccessTime(watcher.Path, DateTime.Now);

            Utility.ExpectEvent(eventOccured, "changed");
        }
    }

    [Fact]
    public static void FileSystemWatcher_Changed_NestedDirectories()
    {
        Utility.TestNestedDirectoriesHelper(WatcherChangeTypes.Changed, (AutoResetEvent are, TemporaryTestDirectory ttd) =>
        {
            Directory.SetLastAccessTime(ttd.Path, DateTime.Now);
            Utility.ExpectEvent(are, "changed");
        },
        NotifyFilters.DirectoryName | NotifyFilters.LastAccess);
    }

    [Fact]
    public static void FileSystemWatcher_Changed_FileInNestedDirectory()
    {
        Utility.TestNestedDirectoriesHelper(WatcherChangeTypes.Changed, (AutoResetEvent are, TemporaryTestDirectory ttd) =>
        {
            using (var nestedFile = new TemporaryTestFile(Path.Combine(ttd.Path, "nestedFile")))
            {
                Directory.SetLastAccessTime(nestedFile.Path, DateTime.Now);
                Utility.ExpectEvent(are, "changed");
            }
        },
        NotifyFilters.DirectoryName | NotifyFilters.LastAccess | NotifyFilters.FileName);
    }

    [Fact]
    public static void FileSystemWatcher_Changed_FileDataChange()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            // Attach the FSW to the existing structure
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;

            using (var file = File.Create(Path.Combine(dir.Path, "testfile.txt")))
            {
                watcher.EnableRaisingEvents = true;

                // Change the nested file and verify we get the changed event
                byte[] bt = new byte[4096];
                file.Write(bt, 0, bt.Length);
                file.Flush();
            }

            Utility.ExpectEvent(eventOccured, "file changed");
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void FileSystemWatcher_Changed_PreSeededNestedStructure(bool includeSubdirectories)
    {
        // Make a nested structure before the FSW is setup
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        using (var dir1 = Utility.CreateTestDirectory(Path.Combine(dir.Path, "dir1")))
        using (var dir2 = Utility.CreateTestDirectory(Path.Combine(dir1.Path, "dir2")))
        using (var dir3 = Utility.CreateTestDirectory(Path.Combine(dir2.Path, "dir3")))
        {
            string filePath = Path.Combine(dir3.Path, "testfile.txt");
            File.WriteAllBytes(filePath, new byte[4096]);

            // Attach the FSW to the existing structure
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.NotifyFilter = NotifyFilters.Attributes;
            watcher.IncludeSubdirectories = includeSubdirectories;
            watcher.EnableRaisingEvents = true;

            File.SetAttributes(filePath, FileAttributes.ReadOnly);
            File.SetAttributes(filePath, FileAttributes.Normal);

            if (includeSubdirectories)
                Utility.ExpectEvent(eventOccured, "file changed");
            else
                Utility.ExpectNoEvent(eventOccured, "file changed");

            // Restart the FSW
            watcher.EnableRaisingEvents = false;
            watcher.EnableRaisingEvents = true;

            File.SetAttributes(filePath, FileAttributes.ReadOnly);
            File.SetAttributes(filePath, FileAttributes.Normal);

            if (includeSubdirectories)
                Utility.ExpectEvent(eventOccured, "second file change");
            else
                Utility.ExpectNoEvent(eventOccured, "second file change");
        }
    }

    [Fact, ActiveIssue(1477, PlatformID.Windows)]
    public static void FileSystemWatcher_Changed_SymLinkFileDoesntFireEvent()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent are = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            // Setup the watcher
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;

            using (var file = Utility.CreateTestFile(Path.GetTempFileName()))
            {
                Utility.CreateSymLink(file.Path, Path.Combine(dir.Path, "link"), false);
                watcher.EnableRaisingEvents = true;

                // Changing the temp file should not fire an event through the symlink
                byte[] bt = new byte[4096];
                file.Write(bt, 0, bt.Length);
                file.Flush();
            }

            Utility.ExpectNoEvent(are, "symlink'd file change");
        }
    }

    [Fact, ActiveIssue(1477, PlatformID.Windows)]
    public static void FileSystemWatcher_Changed_SymLinkFolderDoesntFireEvent()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var tempDir = Utility.CreateTestDirectory(Path.Combine(Path.GetTempPath(), "FooBar")))
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent are = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            // Setup the watcher
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
            watcher.IncludeSubdirectories = true;

            using (var file = Utility.CreateTestFile(Path.Combine(tempDir.Path, "test")))
            {
                // Create the symlink first
                Utility.CreateSymLink(tempDir.Path, Path.Combine(dir.Path, "link"), true);
                watcher.EnableRaisingEvents = true;

                // Changing the temp file should not fire an event through the symlink
                byte[] bt = new byte[4096];
                file.Write(bt, 0, bt.Length);
                file.Flush();
            }

            Utility.ExpectNoEvent(are, "symlink'd file change");
        }
    }
}
