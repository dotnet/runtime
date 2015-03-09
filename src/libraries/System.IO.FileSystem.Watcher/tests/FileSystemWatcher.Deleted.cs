// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public partial class FileSystemWatcher_4000_Tests
{
    [Fact]
    public static void FileSystemWatcher_Deleted_File()
    {
        using (var watcher = new FileSystemWatcher("."))
        {
            string fileName = Guid.NewGuid().ToString();
            watcher.Filter = fileName;
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);


            using (var file = new TemporaryTestFile(fileName))
            {
                watcher.EnableRaisingEvents = true;
            }

            Utility.ExpectEvent(eventOccured, "deleted");
        }
    }

    [Fact]
    public static void FileSystemWatcher_Deleted_Directory()
    {
        using (var watcher = new FileSystemWatcher("."))
        {
            string dirName = Guid.NewGuid().ToString();
            watcher.Filter = dirName;
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);

            using (var dir = new TemporaryTestDirectory(dirName))
            {
                watcher.EnableRaisingEvents = true;
            }

            Utility.ExpectEvent(eventOccured, "deleted");
        }
    }


    [Fact]
    public static void FileSystemWatcher_Deleted_Negative()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            // put everything in our own directory to avoid collisions
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*.*";
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);

            // run all scenarios together to avoid unnecessary waits, 
            // assert information is verbose enough to trace to failure cause

            watcher.EnableRaisingEvents = true;

            // create a file
            using (var testFile = new TemporaryTestFile(Path.Combine(dir.Path, "file")))
            using (var testDir = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir")))
            {
                // change a file
                testFile.WriteByte(0xFF);
                testFile.Flush();

                // rename a file in the same directory
                testFile.Move(testFile.Path + "_rename");

                // renaming a directory
                testDir.Move(testDir.Path + "_rename");

                Utility.ExpectNoEvent(eventOccured, "changed");
            }
        }
    }
}
