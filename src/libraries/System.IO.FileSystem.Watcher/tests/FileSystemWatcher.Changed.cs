// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class ChangedTests : FileSystemWatcherTest
    {
        [Fact]
        [ActiveIssue(2011, PlatformID.OSX)]
        public void FileSystemWatcher_Changed_LastWrite_File()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.Filter = Path.GetFileName(file.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                watcher.EnableRaisingEvents = true;

                File.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(eventOccurred, "changed");
            }
        }

        [Fact]
        [ActiveIssue(2011, PlatformID.OSX)]
        public void FileSystemWatcher_Changed_LastWrite_Directory()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.Filter = Path.GetFileName(dir.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                watcher.EnableRaisingEvents = true;
                Directory.SetLastWriteTime(dir.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(eventOccurred, "changed");
            }
        }

        [Fact]
        public void FileSystemWatcher_Changed_Negative()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*.*";
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                // run all scenarios together to avoid unnecessary waits, 
                // assert information is verbose enough to trace to failure cause

                watcher.EnableRaisingEvents = true;

                // create a file
                using (var testFile = new TempFile(Path.Combine(dir.Path, "file")))
                using (var testDir = new TempDirectory(Path.Combine(dir.Path, "dir")))
                {
                    // rename a file in the same directory
                    File.Move(testFile.Path, testFile.Path + "_rename");

                    // renaming a directory
                    Directory.Move(testDir.Path, testDir.Path + "_rename");
                    // deleting a file & directory by leaving the using block
                }

                ExpectNoEvent(eventOccurred, "changed");
            }
        }

        [Fact, ActiveIssue(2279)]
        public void FileSystemWatcher_Changed_WatchedFolder()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                watcher.EnableRaisingEvents = true;

                Directory.SetLastAccessTime(watcher.Path, DateTime.Now);

                ExpectEvent(eventOccurred, "changed");
            }
        }

        [Fact]
        public void FileSystemWatcher_Changed_NestedDirectories()
        {
            TestNestedDirectoriesHelper(GetTestFilePath(), WatcherChangeTypes.Changed, (AutoResetEvent are, TempDirectory ttd) =>
            {
                Directory.SetLastAccessTime(ttd.Path, DateTime.Now);
                ExpectEvent(are, "changed");
            },
            NotifyFilters.DirectoryName | NotifyFilters.LastAccess);
        }

        [Fact]
        public void FileSystemWatcher_Changed_FileInNestedDirectory()
        {
            TestNestedDirectoriesHelper(GetTestFilePath(), WatcherChangeTypes.Changed, (AutoResetEvent are, TempDirectory ttd) =>
            {
                using (var nestedFile = new TempFile(Path.Combine(ttd.Path, "nestedFile")))
                {
                    Directory.SetLastAccessTime(nestedFile.Path, DateTime.Now);
                    ExpectEvent(are, "changed");
                }
            },
            NotifyFilters.DirectoryName | NotifyFilters.LastAccess | NotifyFilters.FileName);
        }

        [Fact]
        public void FileSystemWatcher_Changed_FileDataChange()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

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

                ExpectEvent(eventOccurred, "file changed");
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FileSystemWatcher_Changed_PreSeededNestedStructure(bool includeSubdirectories)
        {
            // Make a nested structure before the FSW is setup
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            using (var dir1 = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var dir2 = new TempDirectory(Path.Combine(dir1.Path, "dir2")))
            using (var dir3 = new TempDirectory(Path.Combine(dir2.Path, "dir3")))
            {
                string filePath = Path.Combine(dir3.Path, "testfile.txt");
                File.WriteAllBytes(filePath, new byte[4096]);

                // Attach the FSW to the existing structure
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.NotifyFilter = NotifyFilters.Attributes;
                watcher.IncludeSubdirectories = includeSubdirectories;
                watcher.EnableRaisingEvents = true;

                File.SetAttributes(filePath, FileAttributes.ReadOnly);
                File.SetAttributes(filePath, FileAttributes.Normal);

                if (includeSubdirectories)
                    ExpectEvent(eventOccurred, "file changed");
                else
                    ExpectNoEvent(eventOccurred, "file changed");

                // Restart the FSW
                watcher.EnableRaisingEvents = false;
                watcher.EnableRaisingEvents = true;

                File.SetAttributes(filePath, FileAttributes.ReadOnly);
                File.SetAttributes(filePath, FileAttributes.Normal);

                if (includeSubdirectories)
                    ExpectEvent(eventOccurred, "second file change");
                else
                    ExpectNoEvent(eventOccurred, "second file change");
            }
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void FileSystemWatcher_Changed_SymLinkFileDoesntFireEvent()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent are = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                // Setup the watcher
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;

                using (var file = new TempFile(GetTestFilePath()))
                {
                    CreateSymLink(file.Path, Path.Combine(dir.Path, "link"), false);
                    watcher.EnableRaisingEvents = true;

                    // Changing the temp file should not fire an event through the symlink
                    byte[] bt = new byte[4096];
                    File.WriteAllBytes(file.Path, bt);
                }

                ExpectNoEvent(are, "symlink'd file change");
            }
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void FileSystemWatcher_Changed_SymLinkFolderDoesntFireEvent()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var tempDir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFilePath())))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent are = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                // Setup the watcher
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
                watcher.IncludeSubdirectories = true;

                using (var file = new TempFile(Path.Combine(tempDir.Path, "test")))
                {
                    // Create the symlink first
                    CreateSymLink(tempDir.Path, Path.Combine(dir.Path, "link"), true);
                    watcher.EnableRaisingEvents = true;

                    // Changing the temp file should not fire an event through the symlink
                    byte[] bt = new byte[4096];
                    File.WriteAllBytes(file.Path, bt);
                }

                ExpectNoEvent(are, "symlink'd file change");
            }
        }

        [Fact]
        public void FileSystemWatcher_Changed_RootFolderChangeDoesNotFireEvent()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent are = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                // Setup the watcher
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.EnableRaisingEvents = true;

                Directory.SetLastWriteTime(dir.Path, DateTime.Now.AddSeconds(10));
                ExpectNoEvent(are, "Root Directory Change");
            }
        }
    }
}