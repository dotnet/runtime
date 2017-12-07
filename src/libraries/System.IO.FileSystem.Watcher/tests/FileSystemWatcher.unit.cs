// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class FileSystemWatcherTests : FileSystemWatcherTest
    {
        private static void ValidateDefaults(FileSystemWatcher watcher, string path, string filter)
        {
            Assert.Equal(false, watcher.EnableRaisingEvents);
            Assert.Equal(filter, watcher.Filter);
            Assert.Equal(false, watcher.IncludeSubdirectories);
            Assert.Equal(8192, watcher.InternalBufferSize);
            Assert.Equal(NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName, watcher.NotifyFilter);
            Assert.Equal(path, watcher.Path);
        }

        [Fact]
        public void FileSystemWatcher_NewFileInfoAction_TriggersNothing()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, "file")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, Path.GetFileName(file.Path)))
            {
                Action action = () => new FileInfo(file.Path);

                ExpectEvent(watcher, 0, action, expectedPath: file.Path);
            }
        }

        [Fact]
        public void FileSystemWatcher_FileInfoGetter_TriggersNothing()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, "file")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, Path.GetFileName(file.Path)))
            {
                FileAttributes res;
                Action action = () => res = new FileInfo(file.Path).Attributes;

                ExpectEvent(watcher, 0, action, expectedPath: file.Path);
            }
        }

        [Fact]
        public void FileSystemWatcher_EmptyAction_TriggersNothing()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, "file")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, Path.GetFileName(file.Path)))
            {
                Action action = () => { };

                ExpectEvent(watcher, 0, action, expectedPath: file.Path);
            }
        }

        [Fact]
        public void FileSystemWatcher_ctor()
        {
            string path = String.Empty;
            string pattern = "*.*";
            using (FileSystemWatcher watcher = new FileSystemWatcher())
                ValidateDefaults(watcher, path, pattern);
        }

        [Fact]
        public void FileSystemWatcher_ctor_path()
        {
            string path = @".";
            string pattern = "*.*";
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
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            {
                // Null filter
                AssertExtensions.Throws<ArgumentNullException>("filter", () => new FileSystemWatcher(testDirectory.Path, null));

                // Null path
                AssertExtensions.Throws<ArgumentNullException>("path", () => new FileSystemWatcher(null));
                AssertExtensions.Throws<ArgumentNullException>("path", () => new FileSystemWatcher(null, "*"));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "On Desktop, these exceptions don't have a parameter")]
        public void FileSystemWatcher_ctor_InvalidStrings()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            {
                // Empty path
                AssertExtensions.Throws<ArgumentException>("path", () => new FileSystemWatcher(string.Empty));
                AssertExtensions.Throws<ArgumentException>("path", () => new FileSystemWatcher(string.Empty, "*"));

                // Invalid directory
                AssertExtensions.Throws<ArgumentException>("path", () => new FileSystemWatcher(GetTestFilePath()));
                AssertExtensions.Throws<ArgumentException>("path", () => new FileSystemWatcher(GetTestFilePath(), "*"));
            }
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
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            {
                FileSystemWatcher watcher = new FileSystemWatcher(testDirectory.Path);
                Assert.Equal(false, watcher.EnableRaisingEvents);

                watcher.EnableRaisingEvents = true;
                Assert.Equal(true, watcher.EnableRaisingEvents);

                watcher.EnableRaisingEvents = false;
                Assert.Equal(false, watcher.EnableRaisingEvents);
            }
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

            Assert.Equal("*.*", watcher.Filter);

            // Null and empty should be mapped to "*.*"
            watcher.Filter = null;
            Assert.Equal("*.*", watcher.Filter);

            watcher.Filter = String.Empty;
            Assert.Equal("*.*", watcher.Filter);

            watcher.Filter = " ";
            Assert.Equal(" ", watcher.Filter);

            watcher.Filter = "\0";
            Assert.Equal("\0", watcher.Filter);

            watcher.Filter = "\n";
            Assert.Equal("\n", watcher.Filter);

            watcher.Filter = "abc.dll";
            Assert.Equal("abc.dll", watcher.Filter);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || // expect no change for OrdinalIgnoreCase-equal strings
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // expect no change for OrdinalIgnoreCase-equal strings
                // it's unclear why desktop does this but preserve it for compat        
                watcher.Filter = "ABC.DLL";
                Assert.Equal("abc.dll", watcher.Filter);
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
            Assert.Equal(false, watcher.IncludeSubdirectories);

            watcher.IncludeSubdirectories = true;
            Assert.Equal(true, watcher.IncludeSubdirectories);

            watcher.IncludeSubdirectories = false;
            Assert.Equal(false, watcher.IncludeSubdirectories);
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

            // These throw InvalidEnumException on desktop, but ArgumentException on K
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
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var fsw = new FileSystemWatcher(dir.Path))
            {
                AutoResetEvent are = new AutoResetEvent(false);
                string fullPath = Path.Combine(dir.Path.ToUpper(), "Foo.txt");

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
            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(dir.Path, "file")))
            using (var fsw = new FileSystemWatcher(dir.Path))
            {
                AutoResetEvent eventOccurred = WatchRenamed(fsw);

                string newPath = Path.Combine(dir.Path, "newPath");

                fsw.Renamed += (o, e) =>
                {
                    Assert.Equal(e.OldFullPath, file.Path);
                    Assert.Equal(e.FullPath, newPath);
                };

                fsw.EnableRaisingEvents = true;
                File.Move(file.Path, newPath);
                ExpectEvent(eventOccurred, "renamed");
            }
        }

        [Fact]                        
        public void FileSystemWatcher_Path()
        {            
            FileSystemWatcher watcher = new FileSystemWatcher();
            Assert.Equal(String.Empty, watcher.Path);

            watcher.Path = null;
            Assert.Equal(String.Empty, watcher.Path);

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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || // expect no change for OrdinalIgnoreCase-equal strings
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
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
            Assert.Throws<ArgumentException>(() => watcher.Path = String.Empty);
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
        [PlatformSpecific(TestPlatforms.Linux)]  // Reads MaxUsersWatches from Linux OS files
        [OuterLoop("This test has high system resource demands and may cause failures in other concurrent tests")]
        public void FileSystemWatcher_CreateManyConcurrentWatches()
        {
            int maxUserWatches = int.Parse(File.ReadAllText("/proc/sys/fs/inotify/max_user_watches"));

            using (var dir = new TempDirectory(GetTestFilePath()))
            using (var watcher = new FileSystemWatcher(dir.Path) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName })
            {
                Action action = () =>
                {
                    // Create enough directories to exceed the number of allowed watches
                    for (int i = 0; i <= maxUserWatches; i++)
                    {
                        Directory.CreateDirectory(Path.Combine(dir.Path, i.ToString()));
                    }
                };
                Action cleanup = () =>
                {
                    for (int i = 0; i <= maxUserWatches; i++)
                    {
                        Directory.Delete(Path.Combine(dir.Path, i.ToString()));
                    }
                };

                ExpectError(watcher, action, cleanup);

                // Make sure existing watches still work even after we've had one or more failures
                Action createAction = () => File.WriteAllText(Path.Combine(dir.Path, Path.GetRandomFileName()), "text");
                Action createCleanup = () => File.Delete(Path.Combine(dir.Path, Path.GetRandomFileName()));
                ExpectEvent(watcher, WatcherChangeTypes.Created, createAction, createCleanup);
            }
        }

        [Fact]
        public void FileSystemWatcher_StopCalledOnBackgroundThreadDoesNotDeadlock()
        {
            // Check the case where Stop or Dispose (they do the same thing) is called from 
            // a FSW event callback and make sure we don't Thread.Join to deadlock
            using (var dir = new TempDirectory(GetTestFilePath()))
            {
                string filePath = Path.Combine(dir.Path, "testfile.txt");
                File.Create(filePath).Dispose();
                AutoResetEvent are = new AutoResetEvent(false);
                FileSystemWatcher watcher = new FileSystemWatcher(Path.GetFullPath(dir.Path), "*");
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
        }

        [Fact]
        public void FileSystemWatcher_WatchingAliasedFolderResolvesToRealPathWhenWatching()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, "dir")))
            using (var fsw = new FileSystemWatcher(dir.Path))
            {
                AutoResetEvent are = WatchCreated(fsw);

                fsw.Filter = "*";
                fsw.EnableRaisingEvents = true;

                using (var temp = new TempDirectory(Path.Combine(dir.Path, "foo")))
                {
                    ExpectEvent(are, "created");
                }
            }
        }
    }
}
