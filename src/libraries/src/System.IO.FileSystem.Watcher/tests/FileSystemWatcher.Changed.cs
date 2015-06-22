// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public partial class FileSystemWatcher_4000_Tests
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
    public static void FileSystemWatcher_ChangeWatchedFolder()
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
}
