// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public partial class IncludeSubdirectoriesTests
{
    [Fact]
    [ActiveIssue(1657)]
    public static void FileSystemWatcher_IncludeSubDirectories_File()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir.Path)))
        {
            string dirPath = Path.GetFullPath(dir.Path);
            string subDirPath = Path.Combine(dirPath, "subdir");

            watcher.Path = dirPath;
            watcher.IncludeSubdirectories = true;
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            Directory.CreateDirectory(subDirPath);

            watcher.EnableRaisingEvents = true;

            File.WriteAllText(Path.Combine(subDirPath, "1.txt"), "testcontent");
            Utility.ExpectEvent(eventOccured, "created");

            watcher.IncludeSubdirectories = false;
            File.WriteAllText(Path.Combine(subDirPath, "2.txt"), "testcontent");
            Utility.ExpectNoEvent(eventOccured, "created");

            watcher.IncludeSubdirectories = true;
            File.WriteAllText(Path.Combine(subDirPath, "3.txt"), "testcontent");
            Utility.ExpectEvent(eventOccured, "created");
        }
    }

    [Fact]
    [ActiveIssue(1657)]
    public static void FileSystemWatcher_IncludeSubDirectories_Directory()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir.Path)))
        {
            string dirPath = Path.GetFullPath(dir.Path);
            string subDirPath = Path.Combine(dirPath, "subdir");

            watcher.Path = dirPath;
            watcher.IncludeSubdirectories = true;
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created);

            Directory.CreateDirectory(subDirPath);

            watcher.EnableRaisingEvents = true;

            Directory.CreateDirectory(Path.Combine(subDirPath, "1"));
            Utility.ExpectEvent(eventOccured, "created");

            watcher.IncludeSubdirectories = false;
            Directory.CreateDirectory(Path.Combine(subDirPath, "2"));
            Utility.ExpectNoEvent(eventOccured, "created");

            watcher.IncludeSubdirectories = true;
            Directory.CreateDirectory(Path.Combine(subDirPath, "3"));
            Utility.ExpectEvent(eventOccured, "created");
        }
    }
}
