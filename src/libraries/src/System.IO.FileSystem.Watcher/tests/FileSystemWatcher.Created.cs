// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public class CreatedTests
{
    [Fact]
    public static void FileSystemWatcher_Created_File()
    {
        using (var watcher = new FileSystemWatcher("."))
        {
            string fileName = Guid.NewGuid().ToString();
            watcher.Filter = fileName;
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            watcher.EnableRaisingEvents = true;

            using (var file = new TemporaryTestFile(fileName))
            {
                Utility.ExpectEvent(eventOccurred, "created");
            }
        }
    }

    [Fact]
    public static void FileSystemWatcher_Created_File_ForceRestart()
    {
        using (var watcher = new FileSystemWatcher("."))
        {
            string fileName = Guid.NewGuid().ToString();
            watcher.Filter = fileName;
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            watcher.EnableRaisingEvents = true;

            watcher.NotifyFilter = NotifyFilters.FileName; // change filter to force restart

            using (var file = new TemporaryTestFile(fileName))
            {
                Utility.ExpectEvent(eventOccurred, "created");
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
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            watcher.EnableRaisingEvents = true;

            using (var dir = new TemporaryTestDirectory(dirName))
            {
                Utility.ExpectEvent(eventOccurred, "created");
            }
        }
    }

    [Fact]
    [ActiveIssue(2011, PlatformID.OSX)]
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
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            string sourceFile = Path.Combine(dir.Path, testFileName);
            string targetFile = Path.Combine(targetDir.Path, testFileName);

            // create a test file in source
            File.WriteAllText(sourceFile, "test content");

            watcher.EnableRaisingEvents = true;

            // move the test file from source to target directory
            File.Move(sourceFile, targetFile);

            Utility.ExpectEvent(eventOccurred, "created");
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
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

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

                // renaming a directory
                //
                // We don't do this on Linux because depending on the timing of MOVED_FROM and MOVED_TO events,
                // a rename can trigger delete + create as a deliberate handling of an edge case, and this
                // test is checking that no create events are raised.
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    testDir.Move(testDir.Path + "_rename");
                }

                // deleting a file & directory by leaving the using block
            }

            Utility.ExpectNoEvent(eventOccurred, "created");
        }
    }

    [Fact]
    public static void FileSystemWatcher_Created_FileCreatedInNestedDirectory()
    {
        Utility.TestNestedDirectoriesHelper(WatcherChangeTypes.Created, (AutoResetEvent are, TemporaryTestDirectory ttd) =>
        {
            using (var nestedFile = new TemporaryTestFile(Path.Combine(ttd.Path, "nestedFile")))
            {
                Utility.ExpectEvent(are, "nested file created");
            }
        });
    }

    // This can potentially fail, depending on where the test is run from, due to 
    // the MAX_PATH limitation. When issue 645 is closed, this shouldn't be a problem
    [Fact, ActiveIssue(645, PlatformID.Any)]
    public static void FileSystemWatcher_Created_DeepDirectoryStructure()
    {
        // List of created directories
        List<TemporaryTestDirectory> lst = new List<TemporaryTestDirectory>();

        try
        {
            using (var dir = Utility.CreateTestDirectory())
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                // Priming directory
                TemporaryTestDirectory priming = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir"));
                lst.Add(priming);
                Utility.ExpectEvent(eventOccurred, "priming create");

                // Create a deep directory structure and expect things to work
                for (int i = 1; i < 20; i++)
                {
                    lst.Add(new TemporaryTestDirectory(Path.Combine(lst[i - 1].Path, String.Format("dir{0}", i))));
                    Utility.ExpectEvent(eventOccurred, lst[i].Path + " create");
                }

                // Put a file at the very bottom and expect it to raise an event
                using (var file = new TemporaryTestFile(Path.Combine(lst[lst.Count - 1].Path, "temp file")))
                {
                    Utility.ExpectEvent(eventOccurred, "temp file create");
                }
            }
        }
        finally
        {
            // Cleanup
            foreach (TemporaryTestDirectory d in lst)
                d.Dispose();
        }
    }

    [Fact, OuterLoop]
    [ActiveIssue(1477, PlatformID.Windows)]
    [ActiveIssue(3215, PlatformID.OSX)]
    public static void FileSystemWatcher_Created_WatcherDoesntFollowSymLinkToFile()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var temp = Utility.CreateTestFile(Path.GetTempFileName()))
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            // put everything in our own directory to avoid collisions
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.EnableRaisingEvents = true;

            // Make the symlink in our path (to the temp file) and make sure an event is raised
            Utility.CreateSymLink(Path.GetFullPath(temp.Path), Path.Combine(dir.Path, Path.GetFileName(temp.Path)), false);
            Utility.ExpectEvent(eventOccurred, "symlink created");
        }
    }

    [Fact, ActiveIssue(1477, PlatformID.Windows)]
    public static void FileSystemWatcher_Created_WatcherDoesntFollowSymLinkToFolder()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var temp = Utility.CreateTestDirectory(Path.Combine(Path.GetTempPath(), "FooBar")))
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent eventOccurred = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            // put everything in our own directory to avoid collisions
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.EnableRaisingEvents = true;

            // Make the symlink in our path (to the temp folder) and make sure an event is raised
            Utility.CreateSymLink(Path.GetFullPath(temp.Path), Path.Combine(dir.Path, Path.GetFileName(temp.Path)), true);
            Utility.ExpectEvent(eventOccurred, "symlink created");
        }
    }
}
