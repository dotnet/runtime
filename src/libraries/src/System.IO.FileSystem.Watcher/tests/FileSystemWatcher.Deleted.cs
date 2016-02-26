// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public class DeletedTests
{
    [Fact]
    public static void FileSystemWatcher_Deleted_File()
    {
        using (var watcher = new FileSystemWatcher("."))
        {
            string fileName = Guid.NewGuid().ToString();
            watcher.Filter = fileName;
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);


            using (var file = new TemporaryTestFile(fileName))
            {
                watcher.EnableRaisingEvents = true;
            }

            Utility.ExpectEvent(eventOccurred, "deleted");
        }
    }

    [Fact]
    public static void FileSystemWatcher_Deleted_Directory()
    {
        using (var watcher = new FileSystemWatcher("."))
        {
            string dirName = Guid.NewGuid().ToString();
            watcher.Filter = dirName;
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);

            using (var dir = new TemporaryTestDirectory(dirName))
            {
                watcher.EnableRaisingEvents = true;
            }

            Utility.ExpectEvent(eventOccurred, "deleted");
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
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);

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

                // renaming a directory
                //
                // We don't do this on Linux and OSX because depending on the timing of MOVED_FROM and MOVED_TO events,
                // a rename can trigger delete + create as a deliberate handling of an edge case, and this
                // test is checking that no delete events are raised.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    testDir.Move(testDir.Path + "_rename");
                }

                Utility.ExpectNoEvent(eventOccurred, "deleted");
            }
        }
    }

    [Fact]
    public static void FileSystemWatcher_Deleted_NestedDirectories()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent createOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);

            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            using (var firstDir = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir1")))
            {
                // Wait for the created event
                Utility.ExpectEvent(createOccurred, "create");

                using (var secondDir = new TemporaryTestDirectory(Path.Combine(firstDir.Path, "dir2")))
                {
                    // Wait for the created event
                    Utility.ExpectEvent(createOccurred, "create");

                    using (var nestedDir = new TemporaryTestDirectory(Path.Combine(secondDir.Path, "nested")))
                    {
                        // Wait for the created event
                        Utility.ExpectEvent(createOccurred, "create");
                    }

                    Utility.ExpectEvent(eventOccurred, "deleted");
                }

                Utility.ExpectEvent(eventOccurred, "deleted");
            }

            Utility.ExpectEvent(eventOccurred, "deleted");
        }
    }

    [Fact]
    public static void FileSystemWatcher_Deleted_FileDeletedInNestedDirectory()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent createOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Deleted);

            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            using (var firstDir = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir1")))
            {
                // Wait for the created event
                Utility.ExpectEvent(createOccurred, "create");

                using (var secondDir = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir2")))
                {
                    // Wait for the created event
                    Utility.ExpectEvent(createOccurred, "create");

                    using (var nestedDir = new TemporaryTestFile(Path.Combine(secondDir.Path, "nestedFile"))) { }

                    Utility.ExpectEvent(eventOccurred, "deleted");
                }

                Utility.ExpectEvent(eventOccurred, "deleted");
            }

            Utility.ExpectEvent(eventOccurred, "deleted");
        }
    }
}
