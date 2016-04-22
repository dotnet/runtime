// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class CreatedTests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_Created_File()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string fileName = Path.Combine(testDirectory.Path, GetTestFileName());
                watcher.Filter = Path.GetFileName(fileName);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                watcher.EnableRaisingEvents = true;

                using (var file = new TempFile(fileName))
                {
                    ExpectEvent(eventOccurred, "created");
                }
            }
        }

        [Fact]
        public void FileSystemWatcher_Created_File_ForceRestart()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string fileName = Path.Combine(testDirectory.Path, GetTestFileName());
                watcher.Filter = Path.GetFileName(fileName);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                watcher.EnableRaisingEvents = true;

                watcher.NotifyFilter = NotifyFilters.FileName; // change filter to force restart

                using (var file = new TempFile(fileName))
                {
                    ExpectEvent(eventOccurred, "created");
                }
            }
        }

        [Fact]
        public void FileSystemWatcher_Created_Directory()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string dirName = Path.Combine(testDirectory.Path, GetTestFileName());
                watcher.Filter = Path.GetFileName(dirName);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                watcher.EnableRaisingEvents = true;

                using (var dir = new TempDirectory(dirName))
                {
                    ExpectEvent(eventOccurred, "created");
                }
            }
        }

        [Fact]
        [ActiveIssue(2011, PlatformID.OSX)]
        public void FileSystemWatcher_Created_MoveDirectory()
        {
            // create two test directories
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (TempDirectory originalDir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (TempDirectory targetDir = new TempDirectory(originalDir.Path + "_target"))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string testFileName = GetTestFileName();

                // watch the target dir
                watcher.Path = Path.GetFullPath(targetDir.Path);
                watcher.Filter = testFileName;
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                string sourceFile = Path.Combine(originalDir.Path, testFileName);
                string targetFile = Path.Combine(targetDir.Path, testFileName);

                // create a test file in source
                File.WriteAllText(sourceFile, "test content");

                watcher.EnableRaisingEvents = true;

                // move the test file from source to target directory
                File.Move(sourceFile, targetFile);

                ExpectEvent(eventOccurred, "created");
            }
        }

        [Fact]
        public void FileSystemWatcher_Created_Negative()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*.*";
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                // run all scenarios together to avoid unnecessary waits, 
                // assert information is verbose enough to trace to failure cause

                using (var testFile = new TempFile(Path.Combine(dir.Path, "file")))
                using (var testDir = new TempDirectory(Path.Combine(dir.Path, "dir")))
                {
                    // start listening after we've created these
                    watcher.EnableRaisingEvents = true;

                    // change a file
                    File.WriteAllText(testFile.Path, "change");

                    // renaming a directory
                    //
                    // We don't do this on Linux because depending on the timing of MOVED_FROM and MOVED_TO events,
                    // a rename can trigger delete + create as a deliberate handling of an edge case, and this
                    // test is checking that no create events are raised.
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Directory.Move(testDir.Path, testDir.Path + "_rename");
                    }

                    // deleting a file & directory by leaving the using block
                }

                ExpectNoEvent(eventOccurred, "created");
            }
        }

        [Fact]
        public void FileSystemWatcher_Created_FileCreatedInNestedDirectory()
        {
            TestNestedDirectoriesHelper(GetTestFilePath(), WatcherChangeTypes.Created, (AutoResetEvent are, TempDirectory ttd) =>
            {
                using (var nestedFile = new TempFile(Path.Combine(ttd.Path, "nestedFile")))
                {
                    ExpectEvent(are, "nested file created");
                }
            });
        }

        // This can potentially fail, depending on where the test is run from, due to 
        // the MAX_PATH limitation. When issue 645 is closed, this shouldn't be a problem
        [Fact, ActiveIssue(645, PlatformID.Any)]
        public void FileSystemWatcher_Created_DeepDirectoryStructure()
        {
            // List of created directories
            List<TempDirectory> lst = new List<TempDirectory>();

            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                // Priming directory
                TempDirectory priming = new TempDirectory(Path.Combine(dir.Path, "dir"));
                lst.Add(priming);
                ExpectEvent(eventOccurred, "priming create");

                // Create a deep directory structure and expect things to work
                for (int i = 1; i < 20; i++)
                {
                    lst.Add(new TempDirectory(Path.Combine(lst[i - 1].Path, String.Format("dir{0}", i))));
                    ExpectEvent(eventOccurred, lst[i].Path + " create");
                }

                // Put a file at the very bottom and expect it to raise an event
                using (var file = new TempFile(Path.Combine(lst[lst.Count - 1].Path, "temp file")))
                {
                    ExpectEvent(eventOccurred, "temp file create");
                }
            }
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        [ActiveIssue(3215, PlatformID.OSX)]
        public void FileSystemWatcher_Created_WatcherDoesntFollowSymLinkToFile()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var temp = new TempFile(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.EnableRaisingEvents = true;

                // Make the symlink in our path (to the temp file) and make sure an event is raised
                string symLinkPath = Path.Combine(dir.Path, Path.GetFileName(temp.Path));
                Assert.True(CreateSymLink(temp.Path, symLinkPath, false));
                Assert.True(File.Exists(symLinkPath));
                ExpectEvent(eventOccurred, "symlink created");
            }
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void FileSystemWatcher_Created_WatcherDoesntFollowSymLinkToFolder()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var temp = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.EnableRaisingEvents = true;

                // Make the symlink in our path (to the temp folder) and make sure an event is raised
                string symLinkPath = Path.Combine(dir.Path, Path.GetFileName(temp.Path));
                Assert.True(CreateSymLink(temp.Path, symLinkPath, true));
                Assert.True(Directory.Exists(symLinkPath));
                ExpectEvent(eventOccurred, "symlink created");
            }
        }
    }
}