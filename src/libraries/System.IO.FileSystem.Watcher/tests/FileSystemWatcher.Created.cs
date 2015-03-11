// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public partial class FileSystemWatcher_4000_Tests
{
    [Fact]
    public static void FileSystemWatcher_Created_File()
    {
        using (var watcher = new FileSystemWatcher("."))
        {
            string fileName = Guid.NewGuid().ToString();
            watcher.Filter = fileName;
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            watcher.EnableRaisingEvents = true;

            using (var file = new TemporaryTestFile(fileName))
            {
                Utility.ExpectEvent(eventOccured, "created");
            }
        }
    }

    [Fact]
    public static void FileSystemWatcher_Created_Directory()
    {
        using (var watcher = new FileSystemWatcher("."))
        {
            string dirName = Guid.NewGuid().ToString();
            watcher.Filter = dirName;
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            watcher.EnableRaisingEvents = true;

            using (var dir = new TemporaryTestDirectory(dirName))
            {
                Utility.ExpectEvent(eventOccured, "created");
            }
        }
    }

    [Fact]
    public static void FileSystemWatcher_Created_MoveDirectory()
    {
        // create two test directories
        using (TemporaryTestDirectory dir = Utility.CreateTestDirectory(),
            targetDir = new TemporaryTestDirectory(dir.Path + "_target"))
        using (var watcher = new FileSystemWatcher("."))
        {
            string testFileName = "testFile.txt";

            // watch the target dir
            watcher.Path = Path.GetFullPath(targetDir.Path);
            watcher.Filter = testFileName;
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            string sourceFile = Path.Combine(dir.Path, testFileName);
            string targetFile = Path.Combine(targetDir.Path, testFileName);

            // create a test file in source
            File.WriteAllText(sourceFile, "test content");

            watcher.EnableRaisingEvents = true;

            // move the test file from source to target directory
            File.Move(sourceFile, targetFile);

            Utility.ExpectEvent(eventOccured, "created");
        }
    }

    [Fact]
    public static void FileSystemWatcher_Created_Negative()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            // put everything in our own directory to avoid collisions
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*.*";
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            // run all scenarios together to avoid unnecessary waits, 
            // assert information is verbose enough to trace to failure cause

            using (var testFile = new TemporaryTestFile(Path.Combine(dir.Path, "file")))
            using (var testDir = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir")))
            {
                // start listening after we've created these
                watcher.EnableRaisingEvents = true;

                // change a file
                testFile.WriteByte(0xFF);
                testFile.Flush();

                // rename a file in the same directory
                testFile.Move(testFile.Path + "_rename");

                // renaming a directory
                testDir.Move(testDir.Path + "_rename");

                // deleting a file & directory by leaving the using block
            }

            Utility.ExpectNoEvent(eventOccured, "created");
        }
    }
}
