// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class File_Changed_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_File_Changed_LastWrite()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, "file")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, Path.GetFileName(file.Path)))
            {
                Action action = () => Directory.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                WatcherChangeTypes expected = WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, expectedPath: file.Path);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileSystemWatcher_File_Changed_Nested(bool includeSubdirectories)
        {
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
            using (var nestedDir = new TempDirectory(Path.Combine(firstDir.Path, "nested")))
            using (var nestedFile = new TempFile(Path.Combine(nestedDir.Path, "nestedFile")))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.IncludeSubdirectories = includeSubdirectories;
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Attributes;

                var attributes = File.GetAttributes(nestedFile.Path);
                Action action = () => File.SetAttributes(nestedFile.Path, attributes | FileAttributes.ReadOnly);
                Action cleanup = () => File.SetAttributes(nestedFile.Path, attributes);

                WatcherChangeTypes expected = includeSubdirectories ? WatcherChangeTypes.Changed : 0;
                ExpectEvent(watcher, expected, action, cleanup, nestedFile.Path);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Changed_DataModification()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir.Path), "*"))
            {
                string fileName = Path.Combine(dir.Path, "testFile.txt");
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;

                Action action = () => File.AppendAllText(fileName, "longlonglong!");
                Action cleanup = () => File.WriteAllText(fileName, "short");
                cleanup(); // Initially create the short file.

                WatcherChangeTypes expected = WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, expectedPath: fileName);
            }
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void FileSystemWatcher_File_Changed_SymLink()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var file = new TempFile(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(dir.Path, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
                Assert.True(CreateSymLink(file.Path, Path.Combine(dir.Path, "link"), false));

                Action action = () => File.AppendAllText(file.Path, "longtext");
                Action cleanup = () => File.AppendAllText(file.Path, "short");

                ExpectEvent(watcher, 0, action, cleanup, expectedPath: file.Path);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Changed_SynchronizingObject()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, "file")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, Path.GetFileName(file.Path)))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                Action action = () => Directory.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(watcher, WatcherChangeTypes.Changed, action, expectedPath: file.Path);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }
    }
}
