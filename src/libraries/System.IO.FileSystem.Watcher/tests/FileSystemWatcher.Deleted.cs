// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class DeletedTests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_Deleted_File()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string fileName = Path.Combine(testDirectory.Path, GetTestFileName());
                watcher.Filter = Path.GetFileName(fileName);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Deleted);

                using (var file = new TempFile(fileName))
                {
                    watcher.EnableRaisingEvents = true;
                }
                Assert.False(File.Exists(fileName));

                ExpectEvent(eventOccurred, "deleted");
            }
        }

        [Fact]
        public void FileSystemWatcher_Deleted_Directory()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string dirName = Path.Combine(testDirectory.Path, GetTestFileName());
                watcher.Filter = Path.GetFileName(dirName);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Deleted);

                using (var dir = new TempDirectory(dirName))
                {
                    watcher.EnableRaisingEvents = true;
                }

                ExpectEvent(eventOccurred, "deleted");
            }
        }

        [Fact]
        public void FileSystemWatcher_Deleted_Negative()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*.*";
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Deleted);

                // run all scenarios together to avoid unnecessary waits, 
                // assert information is verbose enough to trace to failure cause

                watcher.EnableRaisingEvents = true;

                // create a file
                using (var testFile = new TempFile(Path.Combine(dir.Path, "file")))
                using (var testDir = new TempDirectory(Path.Combine(dir.Path, "dir")))
                {
                    // change a file
                    File.WriteAllText(testFile.Path, "change");

                    // renaming a directory
                    //
                    // We don't do this on Linux and OSX because depending on the timing of MOVED_FROM and MOVED_TO events,
                    // a rename can trigger delete + create as a deliberate handling of an edge case, and this
                    // test is checking that no delete events are raised.
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Directory.Move(testDir.Path, testDir.Path + "_rename");
                    }

                    ExpectNoEvent(eventOccurred, "deleted");
                }
            }
        }

        [Fact]
        public void FileSystemWatcher_Deleted_NestedDirectories()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent createOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Deleted);

                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
                {
                    // Wait for the created event
                    ExpectEvent(createOccurred, "create");

                    using (var secondDir = new TempDirectory(Path.Combine(firstDir.Path, "dir2")))
                    {
                        // Wait for the created event
                        ExpectEvent(createOccurred, "create");

                        using (var nestedDir = new TempDirectory(Path.Combine(secondDir.Path, "nested")))
                        {
                            // Wait for the created event
                            ExpectEvent(createOccurred, "create");
                        }

                        ExpectEvent(eventOccurred, "deleted");
                    }

                    ExpectEvent(eventOccurred, "deleted");
                }

                ExpectEvent(eventOccurred, "deleted");
            }
        }

        [Fact]
        [ActiveIssue(8023)]
        public void FileSystemWatcher_Deleted_FileDeletedInNestedDirectory()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent createOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Deleted);

                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
                {
                    // Wait for the created event
                    ExpectEvent(createOccurred, "create");

                    using (var secondDir = new TempDirectory(Path.Combine(dir.Path, "dir2")))
                    {
                        // Wait for the created event
                        ExpectEvent(createOccurred, "create");

                        using (var nesteddir = new TempDirectory(Path.Combine(secondDir.Path, "nestedFile"))) { }

                        ExpectEvent(eventOccurred, "deleted");
                    }

                    ExpectEvent(eventOccurred, "deleted");
                }

                ExpectEvent(eventOccurred, "deleted");
            }
        }
    }
}