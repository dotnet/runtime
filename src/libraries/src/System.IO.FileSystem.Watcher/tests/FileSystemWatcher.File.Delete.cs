// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class File_Delete_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_File_Delete()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string fileName = Path.Combine(testDirectory.Path, "file");
                watcher.Filter = Path.GetFileName(fileName);

                Action action = () => File.Delete(fileName);
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Delete_ForcedRestart()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                string fileName = Path.Combine(testDirectory.Path, "file");
                watcher.Filter = Path.GetFileName(fileName);

                Action action = () =>
                {
                    watcher.NotifyFilter = NotifyFilters.FileName; // change filter to force restart
                    File.Delete(fileName);
                };
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Delete_InNestedDirectory()
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var nestedDir = new TempDirectory(Path.Combine(firstDir.Path, "nested")))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.FileName;

                string fileName = Path.Combine(nestedDir.Path, "file");
                Action action = () => File.Delete(fileName);
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName);
            }
        }

        [Fact]
        [ActiveIssue(8410, PlatformID.Linux)]
        public void FileSystemWatcher_File_Delete_DeepDirectoryStructure()
        {
            // List of created directories
            List<TempDirectory> lst = new List<TempDirectory>();

            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.FileName;

                // Create a deep directory structure
                lst.Add(new TempDirectory(Path.Combine(dir.Path, "dir")));
                for (int i = 1; i < 20; i++)
                {
                    string dirPath = Path.Combine(lst[i - 1].Path, String.Format("dir{0}", i));
                    lst.Add(new TempDirectory(dirPath));
                }

                // Put a file at the very bottom and expect it to raise an event
                string fileName = Path.Combine(lst[lst.Count - 1].Path, "file");
                Action action = () => File.Delete(fileName);
                Action cleanup = () => File.Create(fileName).Dispose();
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, fileName);
            }
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void FileSystemWatcher_File_Delete_SymLink()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var temp = new TempFile(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                // Make the symlink in our path (to the temp file) and make sure an event is raised
                string symLinkPath = Path.Combine(dir.Path, Path.GetFileName(temp.Path));
                Action action = () => File.Delete(symLinkPath);
                Action cleanup = () => Assert.True(CreateSymLink(temp.Path, symLinkPath, false));
                cleanup();

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, action, cleanup, symLinkPath);
            }
        }
    }
}