// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using Xunit;
namespace System.IO.Tests
{
    public partial class IncludeSubdirectoriesTests : FileSystemWatcherTest
    {
        [Fact]
        [ActiveIssue(1657)]
        public void FileSystemWatcher_IncludeSubDirectories_File()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir.Path)))
            {
                string dirPath = Path.GetFullPath(dir.Path);
                string subDirPath = Path.Combine(dirPath, "subdir");

                watcher.Path = dirPath;
                watcher.IncludeSubdirectories = true;
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                Directory.CreateDirectory(subDirPath);

                watcher.EnableRaisingEvents = true;

                File.WriteAllText(Path.Combine(subDirPath, "1.txt"), "testcontent");
                ExpectEvent(eventOccurred, "created");

                watcher.IncludeSubdirectories = false;
                File.WriteAllText(Path.Combine(subDirPath, "2.txt"), "testcontent");
                ExpectNoEvent(eventOccurred, "created");

                watcher.IncludeSubdirectories = true;
                File.WriteAllText(Path.Combine(subDirPath, "3.txt"), "testcontent");
                ExpectEvent(eventOccurred, "created");
            }
        }

        [Fact]
        [ActiveIssue(1657)]
        public void FileSystemWatcher_IncludeSubDirectories_Directory()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir.Path)))
            {
                string dirPath = Path.GetFullPath(dir.Path);
                string subDirPath = Path.Combine(dirPath, "subdir");

                watcher.Path = dirPath;
                watcher.IncludeSubdirectories = true;
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created);

                Directory.CreateDirectory(subDirPath);

                watcher.EnableRaisingEvents = true;

                Directory.CreateDirectory(Path.Combine(subDirPath, "1"));
                ExpectEvent(eventOccurred, "created");

                watcher.IncludeSubdirectories = false;
                Directory.CreateDirectory(Path.Combine(subDirPath, "2"));
                ExpectNoEvent(eventOccurred, "created");

                watcher.IncludeSubdirectories = true;
                Directory.CreateDirectory(Path.Combine(subDirPath, "3"));
                ExpectEvent(eventOccurred, "created");
            }
        }
    }
}