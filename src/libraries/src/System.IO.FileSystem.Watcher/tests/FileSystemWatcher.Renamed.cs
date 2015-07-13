// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading;
using Xunit;

public partial class FileSystemWatcher_4000_Tests
{
    [Fact]
    public static void FileSystemWatcher_Renamed_File()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Renamed);

            string newName = file.Path + "_rename";
            Utility.EnsureDelete(newName);

            watcher.EnableRaisingEvents = true;

            file.Move(newName);

            Utility.ExpectEvent(eventOccured, "renamed");
        }
    }


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
        Utility.TestNestedDirectoriesHelper(WatcherChangeTypes.Renamed, (AutoResetEvent are, TemporaryTestDirectory ttd) =>
        {
            using (var nestedFile = new TemporaryTestFile(Path.Combine(ttd.Path, "nestedFile")))
            {
                nestedFile.Move(nestedFile.Path + "_2");
                Utility.ExpectEvent(are, "renamed");
            }
        });
    }
}
