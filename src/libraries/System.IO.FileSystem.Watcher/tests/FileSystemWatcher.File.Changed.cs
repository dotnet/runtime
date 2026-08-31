// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public class File_Changed_Tests : FileSystemWatcherTest
    {
        [Fact]
        public void FileSystemWatcher_File_Changed_LastWrite()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                Action action = () => Directory.SetLastWriteTime(file, DateTime.Now + TimeSpan.FromSeconds(10));

                WatcherChangeTypes expected = WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, expectedPath: file);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileSystemWatcher_File_Changed_Nested(bool includeSubdirectories)
        {
            string nestedFile = CreateTestFile(TestDirectory, "dir1", "nested", "nestedFile");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.IncludeSubdirectories = includeSubdirectories;
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Attributes;

                var attributes = File.GetAttributes(nestedFile);
                Action action = () => File.SetAttributes(nestedFile, attributes | FileAttributes.ReadOnly);
                Action cleanup = () => File.SetAttributes(nestedFile, attributes);

                WatcherChangeTypes expected = includeSubdirectories ? WatcherChangeTypes.Changed : 0;
                ExpectEvent(watcher, expected, action, cleanup, nestedFile);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Changed_DataModification()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(Path.GetFullPath(dir), "*"))
            {
                string fileName = Path.Combine(dir, "testFile.txt");
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;

                Action action = () => File.AppendAllText(fileName, "longlonglong!");
                Action cleanup = () => File.WriteAllText(fileName, "short");
                cleanup(); // Initially create the short file.

                WatcherChangeTypes expected = WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, expectedPath: fileName);
            }
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void FileSystemWatcher_File_Changed_SymLink()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            string file = CreateTestFile();
            using (var watcher = new FileSystemWatcher(dir, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
                Assert.True(MountHelper.CreateSymbolicLink(Path.Combine(dir, GetRandomLinkName()), file, false));

                Action action = () => File.AppendAllText(file, "longtext");
                Action cleanup = () => File.AppendAllText(file, "short");

                ExpectEvent(watcher, 0, action, cleanup, expectedPath: file);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Changed_SynchronizingObject()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                TestISynchronizeInvoke invoker = new TestISynchronizeInvoke();
                watcher.SynchronizingObject = invoker;

                Action action = () => Directory.SetLastWriteTime(file, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(watcher, WatcherChangeTypes.Changed, action, expectedPath: file);
                Assert.True(invoker.BeginInvoke_Called);
            }
        }
    }
}
