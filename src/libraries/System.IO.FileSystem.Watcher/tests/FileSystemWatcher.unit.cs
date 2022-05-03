// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tests;

using Microsoft.DotNet.XUnitExtensions;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace System.IO.Tests
{
    public class FileSystemWatcherTests : FileSystemWatcherTest
    {
        private static void ValidateDefaults(FileSystemWatcher watcher, string path, string filter)
        {
            Assert.False(watcher.EnableRaisingEvents);
            Assert.Equal(filter, watcher.Filter);
            Assert.False(watcher.IncludeSubdirectories);
            Assert.Equal(8192, watcher.InternalBufferSize);
            Assert.Equal(NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName, watcher.NotifyFilter);
            Assert.Equal(path, watcher.Path);
        }

        [Fact]
        public void FileSystemWatcher_NewFileInfoAction_TriggersNothing()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                Action action = () => new FileInfo(file);

                ExpectEvent(watcher, 0, action, expectedPath: file);
            }
        }

        [Fact]
        public void FileSystemWatcher_FileInfoGetter_TriggersNothing()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                FileAttributes res;
                Action action = () => res = new FileInfo(file).Attributes;

                ExpectEvent(watcher, 0, action, expectedPath: file);
            }
        }

        [Fact]
        public void FileSystemWatcher_EmptyAction_TriggersNothing()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                Action action = () => { };

                ExpectEvent(watcher, 0, action, expectedPath: file);
            }
        }

        [Fact]
        public void FileSystemWatcher_ctor()
        {
            string path = string.Empty;
            string pattern = "*";
            using (FileSystemWatcher watcher = new FileSystemWatcher())
                ValidateDefaults(watcher, path, pattern);
        }

        [Fact]
        public void FileSystemWatcher_ctor_path()
        {
            string path = @".";
            string pattern = "*";
            using (FileSystemWatcher watcher = new FileSystemWatcher(path))
                ValidateDefaults(watcher, path, pattern);
        }

        [Fact]
        public void FileSystemWatcher_ctor_path_pattern()
        {
            string path = @".";
            string pattern = "honey.jar";
            using (FileSystemWatcher watcher = new FileSystemWatcher(path, pattern))
                ValidateDefaults(watcher, path, pattern);
        }

        [Fact]
        public void FileSystemWatcher_ctor_NullStrings()
        {
            // Null filter
            Assert.Throws<ArgumentNullException>("filter", () => new FileSystemWatcher(TestDirectory, null));

            // Null path
            Assert.Throws<ArgumentNullException>("path", () => new FileSystemWatcher(null, null));
            Assert.Throws<ArgumentNullException>("path", () => new FileSystemWatcher(null));
            Assert.Throws<ArgumentNullException>("path", () => new FileSystemWatcher(null, "*"));
            
        }

        [Fact]
        public void FileSystemWatcher_ctor_InvalidStrings()
        {
            // Empty path
            AssertExtensions.Throws<ArgumentException>("path", () => new FileSystemWatcher(string.Empty));
            AssertExtensions.Throws<ArgumentException>("path", () => new FileSystemWatcher(string.Empty, "*"));

            // Invalid directory
            AssertExtensions.Throws<ArgumentException>("path", () => new FileSystemWatcher(GetTestFilePath()));
            AssertExtensions.Throws<ArgumentException>("path", () => new FileSystemWatcher(GetTestFilePath(), "*"));
        }

        [Fact]
        public void FileSystemWatcher_Changed()
        {
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                var handler = new FileSystemEventHandler((o, e) => { });

                // add / remove
                watcher.Changed += handler;
                watcher.Changed -= handler;

                // shouldn't throw
                watcher.Changed -= handler;
            }
        }

        [Fact]
        public void FileSystemWatcher_Created()
        {
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                var handler = new FileSystemEventHandler((o, e) => { });

                // add / remove
                watcher.Created += handler;
                watcher.Created -= handler;

                // shouldn't throw
                watcher.Created -= handler;
            }
        }

        [Fact]
        public void FileSystemWatcher_Deleted()
        {
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                var handler = new FileSystemEventHandler((o, e) => { });

                // add / remove
                watcher.Deleted += handler;
                watcher.Deleted -= handler;

                // shouldn't throw
                watcher.Deleted -= handler;
            }
        }

        [Fact]
        public void FileSystemWatcher_Disposed()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Dispose();
            watcher.Dispose(); // shouldn't throw

            Assert.Throws<ObjectDisposedException>(() => watcher.EnableRaisingEvents = true);
        }

        [Fact]
        public void FileSystemWatcher_EnableRaisingEvents()
        {

            FileSystemWatcher watcher = new FileSystemWatcher(TestDirectory);
            Assert.False(watcher.EnableRaisingEvents);

            watcher.EnableRaisingEvents = true;
            Assert.True(watcher.EnableRaisingEvents);

            watcher.EnableRaisingEvents = false;
            Assert.False(watcher.EnableRaisingEvents);
            
        }

        [Fact]
        public void FileSystemWatcher_Error()
        {
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                var handler = new ErrorEventHandler((o, e) => { });

                // add / remove
                watcher.Error += handler;
                watcher.Error -= handler;

                // shouldn't throw
                watcher.Error -= handler;
            }
        }

        [Fact]
        public void FileSystemWatcher_Filter()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();

            Assert.Equal("*", watcher.Filter);

            // Null and empty should be mapped to "*"
            watcher.Filter = null;
            Assert.Equal("*", watcher.Filter);

            watcher.Filter = string.Empty;
            Assert.Equal("*", watcher.Filter);

            watcher.Filter = " ";
            Assert.Equal(" ", watcher.Filter);

            watcher.Filter = "\0";
            Assert.Equal("\0", watcher.Filter);

            watcher.Filter = "\n";
            Assert.Equal("\n", watcher.Filter);

            watcher.Filter = "abc.dll";
            Assert.Equal("abc.dll", watcher.Filter);

            if (!PlatformDetection.IsOSXLike)
            {
                watcher.Filter = "ABC.DLL";
                Assert.Equal("ABC.DLL", watcher.Filter);
            }

            // We can make this setting by first changing to another value then back.
            watcher.Filter = null;
            watcher.Filter = "ABC.DLL";
            Assert.Equal("ABC.DLL", watcher.Filter);
        }

        [Fact]
        public void FileSystemWatcher_IncludeSubdirectories()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            Assert.False(watcher.IncludeSubdirectories);

            watcher.IncludeSubdirectories = true;
            Assert.True(watcher.IncludeSubdirectories);

            watcher.IncludeSubdirectories = false;
            Assert.False(watcher.IncludeSubdirectories);
        }

        [Fact]
        public void FileSystemWatcher_InternalBufferSize()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            Assert.Equal(8192, watcher.InternalBufferSize);

            watcher.InternalBufferSize = 20000;
            Assert.Equal(20000, watcher.InternalBufferSize);

            watcher.InternalBufferSize = int.MaxValue;
            Assert.Equal(int.MaxValue, watcher.InternalBufferSize);

            // FSW enforces a minimum value of 4096
            watcher.InternalBufferSize = 0;
            Assert.Equal(4096, watcher.InternalBufferSize);

            watcher.InternalBufferSize = -1;
            Assert.Equal(4096, watcher.InternalBufferSize);

            watcher.InternalBufferSize = int.MinValue;
            Assert.Equal(4096, watcher.InternalBufferSize);

            watcher.InternalBufferSize = 4095;
            Assert.Equal(4096, watcher.InternalBufferSize);
        }

        [Fact]
        public void FileSystemWatcher_NotifyFilter()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            Assert.Equal(NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName, watcher.NotifyFilter);

            var notifyFilters = Enum.GetValues(typeof(NotifyFilters)).Cast<NotifyFilters>();
            foreach (NotifyFilters filterValue in notifyFilters)
            {
                watcher.NotifyFilter = filterValue;
                Assert.Equal(filterValue, watcher.NotifyFilter);
            }

            var allFilters = notifyFilters.Aggregate((mask, flag) => mask | flag);
            watcher.NotifyFilter = allFilters;
            Assert.Equal(allFilters, watcher.NotifyFilter);

            // This doesn't make sense, but it is permitted.
            watcher.NotifyFilter = 0;
            Assert.Equal((NotifyFilters)0, watcher.NotifyFilter);

            // These throw InvalidEnumException on .NET Framework, but ArgumentException on K
            Assert.ThrowsAny<ArgumentException>(() => watcher.NotifyFilter = (NotifyFilters)(-1));
            Assert.ThrowsAny<ArgumentException>(() => watcher.NotifyFilter = (NotifyFilters)int.MinValue);
            Assert.ThrowsAny<ArgumentException>(() => watcher.NotifyFilter = (NotifyFilters)int.MaxValue);
            Assert.ThrowsAny<ArgumentException>(() => watcher.NotifyFilter = allFilters + 1);

            // Simulate a bit added to the flags
            Assert.ThrowsAny<ArgumentException>(() => watcher.NotifyFilter = allFilters | (NotifyFilters)((int)notifyFilters.Max() << 1));
        }

        [Fact]
        public void FileSystemWatcher_OnChanged()
        {
            using (TestFileSystemWatcher watcher = new TestFileSystemWatcher())
            {
                bool eventOccurred = false;
                object obj = null;
                FileSystemEventArgs actualArgs = null, expectedArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, "directory", "file");

                watcher.Changed += (o, e) =>
                {
                    eventOccurred = true;
                    obj = o;
                    actualArgs = e;
                };

                watcher.CallOnChanged(expectedArgs);
                Assert.True(eventOccurred, "Event should be invoked");
                Assert.Equal(watcher, obj);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Fact]
        public void FileSystemWatcher_OnCreated()
        {
            using (TestFileSystemWatcher watcher = new TestFileSystemWatcher())
            {
                bool eventOccurred = false;
                object obj = null;
                FileSystemEventArgs actualArgs = null, expectedArgs = new FileSystemEventArgs(WatcherChangeTypes.Created, "directory", "file");

                watcher.Created += (o, e) =>
                {
                    eventOccurred = true;
                    obj = o;
                    actualArgs = e;
                };

                watcher.CallOnCreated(expectedArgs);
                Assert.True(eventOccurred, "Event should be invoked");
                Assert.Equal(watcher, obj);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.Windows)]  // Casing matters on Linux
        public void FileSystemWatcher_OnCreatedWithMismatchedCasingGivesExpectedFullPath()
        {
            using (var fsw = new FileSystemWatcher(TestDirectory))
            {
                AutoResetEvent are = new AutoResetEvent(false);
                string fullPath = Path.Combine(TestDirectory.ToUpper(), "Foo.txt");

                fsw.Created += (o, e) =>
                {
                    Assert.True(fullPath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));
                    are.Set();
                };

                fsw.EnableRaisingEvents = true;
                using (var file = new TempFile(fullPath))
                {
                    ExpectEvent(are, "created");
                }
            }
        }

        [Fact]
        public void FileSystemWatcher_OnDeleted()
        {
            using (TestFileSystemWatcher watcher = new TestFileSystemWatcher())
            {
                bool eventOccurred = false;
                object obj = null;
                FileSystemEventArgs actualArgs = null, expectedArgs = new FileSystemEventArgs(WatcherChangeTypes.Deleted, "directory", "file");

                watcher.Deleted += (o, e) =>
                {
                    eventOccurred = true;
                    obj = o;
                    actualArgs = e;
                };

                watcher.CallOnDeleted(expectedArgs);
                Assert.True(eventOccurred, "Event should be invoked");
                Assert.Equal(watcher, obj);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Fact]
        public void FileSystemWatcher_OnError()
        {
            using (TestFileSystemWatcher watcher = new TestFileSystemWatcher())
            {
                bool eventOccurred = false;
                object obj = null;
                ErrorEventArgs actualArgs = null, expectedArgs = new ErrorEventArgs(new Exception());

                watcher.Error += (o, e) =>
                {
                    eventOccurred = true;
                    obj = o;
                    actualArgs = e;
                };

                watcher.CallOnError(expectedArgs);
                Assert.True(eventOccurred, "Event should be invoked");
                Assert.Equal(watcher, obj);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Fact]
        public void FileSystemWatcher_OnRenamed()
        {
            using (TestFileSystemWatcher watcher = new TestFileSystemWatcher())
            {
                bool eventOccurred = false;
                object obj = null;
                RenamedEventArgs actualArgs = null, expectedArgs = new RenamedEventArgs(WatcherChangeTypes.Renamed, "directory", "file", "oldFile");

                watcher.Renamed += (o, e) =>
                {
                    eventOccurred = true;
                    obj = o;
                    actualArgs = e;
                };

                watcher.CallOnRenamed(expectedArgs);
                Assert.True(eventOccurred, "Event should be invoked");
                Assert.Equal(watcher, obj);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix FSW don't trigger on a file rename.
        public void FileSystemWatcher_Windows_OnRenameGivesExpectedFullPath()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var fsw = new FileSystemWatcher(TestDirectory))
            {
                AutoResetEvent eventOccurred = WatchRenamed(fsw).EventOccured;

                string newPath = Path.Combine(TestDirectory, "newPath");

                fsw.Renamed += (o, e) =>
                {
                    Assert.Equal(file, e.OldFullPath);
                    Assert.Equal(newPath, e.FullPath);
                };

                fsw.EnableRaisingEvents = true;
                File.Move(file, newPath);
                ExpectEvent(eventOccurred, "renamed");
            }
        }

        [Fact]
        public void FileSystemWatcher_Path()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            Assert.Equal(string.Empty, watcher.Path);

            watcher.Path = null;
            Assert.Equal(string.Empty, watcher.Path);

            watcher.Path = ".";
            Assert.Equal(".", watcher.Path);

            if (!PlatformDetection.IsInAppContainer)
            {
                watcher.Path = "..";
                Assert.Equal("..", watcher.Path);
            }

            string currentDir = Path.GetFullPath(".").TrimEnd('.', Path.DirectorySeparatorChar);
            watcher.Path = currentDir;
            Assert.Equal(currentDir, watcher.Path);

            if (OperatingSystem.IsWindows() || // expect no change for OrdinalIgnoreCase-equal strings
                OperatingSystem.IsMacOS())
            {
                watcher.Path = currentDir.ToUpperInvariant();
                Assert.Equal(currentDir, watcher.Path);

                watcher.Path = currentDir.ToLowerInvariant();
                Assert.Equal(currentDir, watcher.Path);
            }

            // expect a change for same "full-path" but different string path, FSW does not normalize
            string currentDirRelative = currentDir +
                Path.DirectorySeparatorChar + "." +
                Path.DirectorySeparatorChar + "." +
                Path.DirectorySeparatorChar + "." +
                Path.DirectorySeparatorChar + ".";
            watcher.Path = currentDirRelative;
            Assert.Equal(currentDirRelative, watcher.Path);

            // FSW starts with String.Empty and will ignore setting this if it is already set,
            // but if you set it after some other valid string has been set it will throw.
            Assert.Throws<ArgumentException>(() => watcher.Path = string.Empty);
            // Non-existent path
            Assert.Throws<ArgumentException>(() => watcher.Path = GetTestFilePath());
            // Web path
            Assert.Throws<ArgumentException>(() => watcher.Path = "http://localhost");
            // File protocol
            Assert.Throws<ArgumentException>(() => watcher.Path = "file:///" + currentDir.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        [Fact]
        public void FileSystemWatcher_Renamed()
        {
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                var handler = new RenamedEventHandler((o, e) => { });

                // add / remove
                watcher.Renamed += handler;
                watcher.Renamed -= handler;

                // shouldn't throw
                watcher.Renamed -= handler;
            }
        }

        [Fact]
        public void FileSystemWatcher_StopCalledOnBackgroundThreadDoesNotDeadlock()
        {
            // Check the case where Stop or Dispose (they do the same thing) is called from
            // a FSW event callback and make sure we don't Thread.Join to deadlock

            string filePath = CreateTestFile(TestDirectory, "testfile.txt");
            AutoResetEvent are = new AutoResetEvent(false);
            FileSystemWatcher watcher = new FileSystemWatcher(Path.GetFullPath(TestDirectory), "*");
            FileSystemEventHandler callback = (sender, arg) =>
            {
                watcher.Dispose();
                are.Set();
            };
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            watcher.Changed += callback;
            watcher.EnableRaisingEvents = true;
            File.SetLastWriteTime(filePath, File.GetLastWriteTime(filePath).AddDays(1));
            Assert.True(are.WaitOne(10000));
            Assert.Throws<ObjectDisposedException>(() => watcher.EnableRaisingEvents = true);
            
        }

        [Fact]
        public void FileSystemWatcher_WatchingAliasedFolderResolvesToRealPathWhenWatching()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var fsw = new FileSystemWatcher(dir))
            {
                AutoResetEvent are = WatchCreated(fsw).EventOccured;

                fsw.Filter = "*";
                fsw.EnableRaisingEvents = true;

                using (var temp = new TempDirectory(Path.Combine(dir, "foo")))
                {
                    ExpectEvent(are, "created");
                }
            }
        }

        [Fact]
        public void DefaultFiltersValue()
        {
            var watcher = new FileSystemWatcher();
            Assert.Equal(0, watcher.Filters.Count);
            Assert.Empty(watcher.Filters);
            Assert.NotNull(watcher.Filters);
            Assert.Equal(new string[] { }, watcher.Filters);
        }

        [Fact]
        public void AddFilterToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            Assert.Equal(2, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*.dll" }, watcher.Filters);

            string[] copied = new string[2];
            watcher.Filters.CopyTo(copied, 0);
            Assert.Equal(new string[] { "*.pdb", "*.dll" }, copied);
        }

        [Fact]
        public void FiltersCaseSensitive()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("foo");
            Assert.Equal("foo", watcher.Filters[0]);
            watcher.Filters[0] = "Foo";
            Assert.Equal("Foo", watcher.Filters[0]);
        }

        [Fact]
        public void RemoveFilterFromFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Remove("*.pdb");
            Assert.DoesNotContain(watcher.Filters, t => t == "*.pdb");
            Assert.Equal(new string[] { "*.dll" }, watcher.Filters);

            // No Exception is thrown while removing an item which is not present in the list.
            watcher.Filters.Remove("*.pdb");
        }

        [Fact]
        public void AddEmptyStringToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Add(string.Empty);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*.dll", "*" }, watcher.Filters);
        }

        [Fact]
        public void AddNullToFilters()
        {
            var watcher = new FileSystemWatcher();

            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Add(null);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*.dll", "*" }, watcher.Filters);
        }

        [Fact]
        public void SetEmptyStringToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters[0] = string.Empty;
            Assert.Equal(2, watcher.Filters.Count);
            Assert.Equal("*", watcher.Filters[0]);
            Assert.Equal(new string[] { "*", "*.dll"}, watcher.Filters);
        }

        [Fact]
        public void RemoveEmptyStringToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(string.Empty);

            Assert.Equal(3, watcher.Filters.Count);
            watcher.Filters.Remove(string.Empty);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*.dll", "*" }, watcher.Filters);
        }

        [Fact]
        public void RemoveAtFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.RemoveAt(0);
            Assert.Equal(1, watcher.Filters.Count);
            Assert.Equal("*.dll", watcher.Filter);
            Assert.Equal(new string[] {"*.dll" }, watcher.Filters);
        }

        [Fact]
        public void RemoveAtEmptyFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");

            watcher.Filters.RemoveAt(0);
            Assert.Equal(0, watcher.Filters.Count);
            Assert.Equal("*", watcher.Filter);
            Assert.Equal(new string[] { }, watcher.Filters);
        }

        [Fact]
        public void SetNullToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters[0] = null;
            Assert.Equal(2, watcher.Filters.Count);
            Assert.Equal("*", watcher.Filters[0]);
            Assert.Equal(new string[] { "*", "*.dll" }, watcher.Filters);
        }

        [Fact]
        public void ContainsEmptyStringFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(string.Empty);

            Assert.DoesNotContain(string.Empty, watcher.Filters);
            Assert.Contains("*", watcher.Filters);
        }

        [Fact]
        public void ContainsNullFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(null);

            Assert.DoesNotContain(null, watcher.Filters);
            Assert.Contains("*", watcher.Filters);
        }

        [Fact]
        public void ContainsFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            Assert.Contains("*.pdb", watcher.Filters);
        }

        [Fact]
        public void InsertEmptyStringFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Insert(1, string.Empty);

            Assert.Equal("*", watcher.Filters[1]);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*", "*.dll" }, watcher.Filters);
        }

        [Fact]
        public void InsertNullFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Insert(1, null);

            Assert.Equal("*", watcher.Filters[1]);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*", "*.dll" }, watcher.Filters);
        }

        [Fact]
        public void InsertFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Insert(1, "foo");

            Assert.Equal("foo", watcher.Filters[1]);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "foo", "*.dll" }, watcher.Filters);
        }

        [Fact]
        public void InsertAtZero()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Insert(0, "foo");

            Assert.Equal("foo", watcher.Filters[0]);
            Assert.Equal("foo", watcher.Filter);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "foo", "*.pdb", "*.dll" }, watcher.Filters);
        }
        [Fact]
        public void IndexOfEmptyStringFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(string.Empty);

            Assert.Equal(-1, watcher.Filters.IndexOf(string.Empty));
        }

        [Fact]
        public void IndexOfNullFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(null);

            Assert.Equal(-1, watcher.Filters.IndexOf(null));
        }

        [Fact]
        public void IndexOfFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            Assert.Equal(-1, watcher.Filters.IndexOf("foo"));
            Assert.Equal(0, watcher.Filters.IndexOf("*.pdb"));
        }

        [Fact]
        public void GetTypeFilters()
        {
            var watcher = new FileSystemWatcher();
            Assert.IsAssignableFrom<Collection<string>>(watcher.Filters);
        }

        [Fact]
        public void ClearFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Clear();
            Assert.Equal(0, watcher.Filters.Count);
            Assert.Equal(new string[] { }, watcher.Filters) ;
        }

        [Fact]
        public void GetFilterAfterFiltersClear()
        {
            var watcher = new FileSystemWatcher(TestDirectory);
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Clear();
            Assert.Equal("*", watcher.Filter);
            Assert.Equal(new string[] { }, watcher.Filters);
            
        }

        [Fact]
        public void GetFiltersAfterFiltersClear()
        {
            var watcher = new FileSystemWatcher(TestDirectory);
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Clear();
            Assert.Throws<ArgumentOutOfRangeException>(() => watcher.Filters[0]);
            Assert.Equal(0, watcher.Filters.Count);
            Assert.Empty(watcher.Filters);
            Assert.NotNull(watcher.Filters);
            
        }

        [Fact]
        public void InvalidOperationsOnFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            Assert.Throws<ArgumentOutOfRangeException>(() => watcher.Filters.Insert(4, "*"));
            watcher.Filters.Clear();
            Assert.Throws<ArgumentOutOfRangeException>(() => watcher.Filters[0]);
        }

        [Fact]
        public void SetAndGetFilterProperty()
        {
            var watcher = new FileSystemWatcher(TestDirectory, "*.pdb");
            watcher.Filters.Add("foo");
            Assert.Equal(2, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "foo" }, watcher.Filters);

            watcher.Filter = "*.doc";
            Assert.Equal(1, watcher.Filters.Count);
            Assert.Equal("*.doc", watcher.Filter);
            Assert.Equal("*.doc", watcher.Filters[0]);
            Assert.Equal(new string[] { "*.doc" }, watcher.Filters);

            watcher.Filters.Clear();
            Assert.Equal("*", watcher.Filter);
            
        }

        [Fact]
        public void SetAndGetFiltersProperty()
        {
            var watcher = new FileSystemWatcher(TestDirectory, "*.pdb");
            watcher.Filters.Add("foo");
            Assert.Equal(new string[] { "*.pdb", "foo" }, watcher.Filters);
            
        }

        [Fact]
        public void FileSystemWatcher_File_Delete_MultipleFilters()
        {
            FileSystemWatcherTest.Execute(() =>
            {
                // Check delete events against multiple filters

                DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
                FileInfo fileOne = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
                FileInfo fileTwo = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
                FileInfo fileThree = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
                fileOne.Create().Dispose();
                fileTwo.Create().Dispose();
                fileThree.Create().Dispose();

                using (var watcher = new FileSystemWatcher(directory.FullName))
                {
                    watcher.Filters.Add(fileOne.Name);
                    watcher.Filters.Add(fileTwo.Name);

                    ExpectEvent(watcher, WatcherChangeTypes.Deleted, () => fileOne.Delete(), cleanup: null, expectedPath : fileOne.FullName);
                    ExpectEvent(watcher, WatcherChangeTypes.Deleted, () => fileTwo.Delete(), cleanup: null, expectedPath: fileTwo.FullName );
                    ExpectNoEvent(watcher, WatcherChangeTypes.Deleted, () => fileThree.Delete(), cleanup: null, expectedPath: fileThree.FullName);
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [Fact]
        public void FileSystemWatcher_Directory_Create_MultipleFilters()
        {
            FileSystemWatcherTest.Execute(() =>
            {
                // Check create events against multiple filters

                DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
                string directoryOne = Path.Combine(directory.FullName, GetTestFileName());
                string directoryTwo = Path.Combine(directory.FullName, GetTestFileName());
                string directoryThree = Path.Combine(directory.FullName, GetTestFileName());

                using (var watcher = new FileSystemWatcher(directory.FullName))
                {
                    watcher.Filters.Add(Path.GetFileName(directoryOne));
                    watcher.Filters.Add(Path.GetFileName(directoryTwo));

                    ExpectEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryOne), cleanup: null, expectedPath: directoryOne);
                    ExpectEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryTwo), cleanup: null, expectedPath: directoryTwo);
                    ExpectNoEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryThree), cleanup: null, expectedPath: directoryThree);
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [Fact]
        public void FileSystemWatcher_Directory_Create_Filter_Ctor()
        {
            FileSystemWatcherTest.Execute(() =>
            {
                // Check create events against multiple filters

                DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
                string directoryOne = Path.Combine(directory.FullName, GetTestFileName());
                string directoryTwo = Path.Combine(directory.FullName, GetTestFileName());
                string directoryThree = Path.Combine(directory.FullName, GetTestFileName());

                using (var watcher = new FileSystemWatcher(directory.FullName, Path.GetFileName(directoryOne)))
                {
                    watcher.Filters.Add(Path.GetFileName(directoryTwo));

                    ExpectEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryOne), cleanup: null, expectedPath: directoryOne);
                    ExpectEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryTwo), cleanup: null, expectedPath: directoryTwo);
                    ExpectNoEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryThree), cleanup: null, expectedPath: directoryThree);
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [Fact]
        public void FileSystemWatcher_Directory_Delete_MultipleFilters()
        {
            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo directoryOne = Directory.CreateDirectory(Path.Combine(directory.FullName, GetTestFileName()));
            DirectoryInfo directoryTwo = Directory.CreateDirectory(Path.Combine(directory.FullName, GetTestFileName()));
            DirectoryInfo directoryThree = Directory.CreateDirectory(Path.Combine(directory.FullName, GetTestFileName()));

            using (var watcher = new FileSystemWatcher(directory.FullName))
            {
                watcher.Filters.Add(Path.GetFileName(directoryOne.FullName));
                watcher.Filters.Add(Path.GetFileName(directoryTwo.FullName));

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, () => directoryOne.Delete(), cleanup: null, expectedPath: directoryOne.FullName);
                ExpectEvent(watcher, WatcherChangeTypes.Deleted, () => directoryTwo.Delete(), cleanup: null, expectedPath: directoryTwo.FullName);
                ExpectNoEvent(watcher, WatcherChangeTypes.Deleted, () => directoryThree.Delete(), cleanup: null, expectedPath: directoryThree.FullName);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Create_MultipleFilters()
        {
            FileSystemWatcherTest.Execute(() =>
            {
                DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
                FileInfo fileOne = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
                FileInfo fileTwo = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
                FileInfo fileThree = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));

                using (var watcher = new FileSystemWatcher(directory.FullName))
                {
                    watcher.Filters.Add(fileOne.Name);
                    watcher.Filters.Add(fileTwo.Name);

                    ExpectEvent(watcher, WatcherChangeTypes.Created, () => fileOne.Create().Dispose(), cleanup: null, expectedPath: fileOne.FullName);
                    ExpectEvent(watcher, WatcherChangeTypes.Created, () => fileTwo.Create().Dispose(), cleanup: null, expectedPath: fileTwo.FullName);
                    ExpectNoEvent(watcher, WatcherChangeTypes.Created, () => fileThree.Create().Dispose(), cleanup: null, expectedPath: fileThree.FullName);
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void FileSystemWatcher_ModifyFiltersConcurrentWithEvents()
        {
            FileSystemWatcherTest.Execute(() =>
            {
                DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
                FileInfo fileOne = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
                FileInfo fileTwo = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
                FileInfo fileThree = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));

                using (var watcher = new FileSystemWatcher(directory.FullName))
                {
                    watcher.Filters.Add(fileOne.Name);
                    watcher.Filters.Add(fileTwo.Name);

                    var cts = new CancellationTokenSource();
                    Thread thread = ThreadTestHelpers.CreateGuardedThread(out Action waitForThread, () =>
                    {
                        string otherFilter = Guid.NewGuid().ToString("N");
                        while (!cts.IsCancellationRequested)
                        {
                            watcher.Filters.Add(otherFilter);
                            watcher.Filters.RemoveAt(2);
                        }
                    });
                    thread.IsBackground = true;
                    thread.Start();

                    ExpectEvent(watcher, WatcherChangeTypes.Created, () => fileOne.Create().Dispose(), cleanup: null, expectedPath: fileOne.FullName);
                    ExpectEvent(watcher, WatcherChangeTypes.Created, () => fileTwo.Create().Dispose(), cleanup: null, expectedPath: fileTwo.FullName);
                    ExpectNoEvent(watcher, WatcherChangeTypes.Created, () => fileThree.Create().Dispose(), cleanup: null, expectedPath: fileThree.FullName);

                    cts.Cancel();
                    waitForThread();
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }
    }

    [Collection(nameof(DisableParallelization))]
    public partial class DangerousFileSystemWatcherTests : FileSystemWatcherTest
    {
        private readonly ITestOutputHelper _output;

        public DangerousFileSystemWatcherTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [PlatformSpecific(TestPlatforms.Linux)]  // Reads MaxUsersWatches from Linux OS files
        [OuterLoop("This test will use all available watchers and can cause failures in other concurrent tests or system processes.")]
        [Fact]
        public void FileSystemWatcher_CreateManyConcurrentWatches()
        {
            int maxUserWatches = int.Parse(File.ReadAllText("/proc/sys/fs/inotify/max_user_watches"));

            using (var watcher = new FileSystemWatcher(TestDirectory) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName })
            {
                Action action = () =>
                {
                    // Create enough directories to exceed the number of allowed watches
                    for (int i = 0; i <= maxUserWatches; i++)
                    {
                        Directory.CreateDirectory(Path.Combine(TestDirectory, i.ToString()));
                    }
                };
                Action cleanup = () =>
                {
                    for (int i = 0; i <= maxUserWatches; i++)
                    {
                        Directory.Delete(Path.Combine(TestDirectory, i.ToString()));
                    }
                };

                ExpectError(watcher, action, cleanup);

                // Make sure existing watches still work even after we've had one or more failures
                Action createAction = () => File.WriteAllText(Path.Combine(TestDirectory, Path.GetRandomFileName()), "text");
                Action createCleanup = () => File.Delete(Path.Combine(TestDirectory, Path.GetRandomFileName()));
                ExpectEvent(watcher, WatcherChangeTypes.Created, createAction, createCleanup);
            }
        }
    }
}
