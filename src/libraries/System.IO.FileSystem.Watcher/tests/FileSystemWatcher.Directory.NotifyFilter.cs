// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Sdk;

namespace System.IO.Tests
{
    public partial class Directory_NotifyFilter_Tests : FileSystemWatcherTest
    {
        [LibraryImport("advapi32.dll", EntryPoint = "SetNamedSecurityInfoW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial uint SetSecurityInfoByHandle( string name, uint objectType, uint securityInformation,
            IntPtr owner, IntPtr group, IntPtr dacl, IntPtr sacl);

        private const uint ERROR_SUCCESS = 0;
        private const uint DACL_SECURITY_INFORMATION = 0x00000004;
        private const uint SE_FILE_OBJECT = 0x1;

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_Directory_NotifyFilter_Attributes(NotifyFilters filter)
        {
            FileSystemWatcherTest.Execute(() =>
            {
                string dir = CreateTestDirectory(TestDirectory, "dir");
                using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(dir)))
                {
                    watcher.NotifyFilter = filter;
                    var attributes = File.GetAttributes(dir);

                    Action action = () => File.SetAttributes(dir, attributes | FileAttributes.ReadOnly);
                    Action cleanup = () => File.SetAttributes(dir, attributes);

                    WatcherChangeTypes expected = 0;
                    if (filter == NotifyFilters.Attributes)
                        expected |= WatcherChangeTypes.Changed;
                    else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForAttribute) > 0))
                        expected |= WatcherChangeTypes.Changed;
                    else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                        expected |= WatcherChangeTypes.Changed;
                    else if (OperatingSystem.IsMacOS() && ((filter & NotifyFilters.Security) > 0))
                        expected |= WatcherChangeTypes.Changed; // Attribute change on OSX is a ChangeOwner operation which passes the Security NotifyFilter.
                    ExpectEvent(watcher, expected, action, cleanup, dir);
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_Directory_NotifyFilter_CreationTime(NotifyFilters filter)
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(dir)))
            {
                watcher.NotifyFilter = filter;
                Action action = () => Directory.SetCreationTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));

                WatcherChangeTypes expected = 0;
                if (filter == NotifyFilters.CreationTime)
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForAttribute) > 0))
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                    expected |= WatcherChangeTypes.Changed;

                ExpectEvent(watcher, expected, action, expectedPath: dir);
            }
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_Directory_NotifyFilter_DirectoryName(NotifyFilters filter)
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(dir)))
            {
                string sourcePath = dir;
                string targetPath = Path.Combine(TestDirectory, "targetDir");
                watcher.NotifyFilter = filter;

                Action action = () => Directory.Move(sourcePath, targetPath);
                Action cleanup = () => Directory.Move(targetPath, sourcePath);

                WatcherChangeTypes expected = 0;
                if (filter == NotifyFilters.DirectoryName)
                    expected |= WatcherChangeTypes.Renamed;

                ExpectEvent(watcher, expected, action, cleanup, targetPath);
            }
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_Directory_NotifyFilter_LastAccessTime(NotifyFilters filter)
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(dir)))
            {
                watcher.NotifyFilter = filter;
                Action action = () => Directory.SetLastAccessTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));

                WatcherChangeTypes expected = 0;
                if (filter == NotifyFilters.LastAccess)
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForAttribute) > 0))
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                    expected |= WatcherChangeTypes.Changed;

                ExpectEvent(watcher, expected, action, expectedPath: dir);
            }
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_Directory_NotifyFilter_LastWriteTime(NotifyFilters filter)
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(dir)))
            {
                watcher.NotifyFilter = filter;
                Action action = () => Directory.SetLastWriteTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));

                WatcherChangeTypes expected = 0;
                if (filter == NotifyFilters.LastWrite)
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForAttribute) > 0))
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                    expected |= WatcherChangeTypes.Changed;

                ExpectEvent(watcher, expected, action, expectedPath: dir);
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_Directory_NotifyFilter_LastWriteTime_TwoFilters(NotifyFilters filter)
        {
            Assert.All(FilterTypes(), (filter2Arr =>
            {
                string dir = CreateTestDirectory(TestDirectory, "dir");
                using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(dir)))
                {
                    filter |= (NotifyFilters)filter2Arr[0];
                    watcher.NotifyFilter = filter;
                    Action action = () => Directory.SetLastWriteTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));

                    WatcherChangeTypes expected = 0;
                    if ((filter & NotifyFilters.LastWrite) > 0)
                        expected |= WatcherChangeTypes.Changed;
                    else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForAttribute) > 0))
                        expected |= WatcherChangeTypes.Changed;
                    else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                        expected |= WatcherChangeTypes.Changed;
                    ExpectEvent(watcher, expected, action, expectedPath: dir);
                }
            }));
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to set security info
        public void FileSystemWatcher_Directory_NotifyFilter_Security(NotifyFilters filter)
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(dir)))
            {
                watcher.NotifyFilter = filter;
                Action action = () =>
                {
                    // ACL support is not yet available, so pinvoke directly.
                    uint result = SetSecurityInfoByHandle(dir,
                        SE_FILE_OBJECT,
                        DACL_SECURITY_INFORMATION, // Only setting the DACL
                        owner: IntPtr.Zero,
                        group: IntPtr.Zero,
                        dacl: IntPtr.Zero, // full access to everyone
                        sacl: IntPtr.Zero);
                    Assert.Equal(ERROR_SUCCESS, result);
                };
                Action cleanup = () =>
                {
                    // Recreate the Directory.
                    Directory.Delete(dir);
                    Directory.CreateDirectory(dir);
                };

                WatcherChangeTypes expected = 0;

                if (filter == NotifyFilters.Security)
                    expected |= WatcherChangeTypes.Changed;

                ExpectEvent(watcher, expected, action, cleanup, dir);
            }
        }

        /// <summary>
        /// Tests a changed event on a file when filtering for LastWrite and directory name.
        /// </summary>
        [Fact]
        public void FileSystemWatcher_Directory_NotifyFilter_LastWriteAndFileName()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                NotifyFilters filter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
                watcher.NotifyFilter = filter;

                Action action = () => File.SetLastWriteTime(file, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(watcher, WatcherChangeTypes.Changed, action, expectedPath: file);
            }
        }

        /// <summary>
        /// Tests the watcher behavior when two events - a Modification and a Creation - happen closely
        /// after each other.
        /// </summary>
        [Fact]
        public void FileSystemWatcher_Directory_NotifyFilter_ModifyAndCreate()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
                string otherDir = Path.Combine(TestDirectory, "dir2");

                Action action = () =>
                {
                    Directory.CreateDirectory(otherDir);
                    Directory.SetLastWriteTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));
                };
                Action cleanup = () => Directory.Delete(otherDir);

                WatcherChangeTypes expected = 0;
                expected |= WatcherChangeTypes.Created | WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, cleanup, new string[] { otherDir, dir });
            }
        }

        /// <summary>
        /// Tests the watcher behavior when two events - a Modification and a Deletion - happen closely
        /// after each other.
        /// </summary>
        [Fact]
        public void FileSystemWatcher_Directory_NotifyFilter_ModifyAndDelete()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
                string otherDir = Path.Combine(TestDirectory, "dir2");

                Action action = () =>
                {
                    Directory.Delete(otherDir);
                    Directory.SetLastWriteTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));
                };
                Action cleanup = () =>
                {
                    Directory.CreateDirectory(otherDir);
                };
                cleanup();

                WatcherChangeTypes expected = 0;
                expected |= WatcherChangeTypes.Deleted | WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, cleanup, new string[] { otherDir, dir });
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_NotifyFilter_DirectoryNameDoesntTriggerOnFileEvent()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                string renameDirSource = Path.Combine(TestDirectory, "dir2_source");
                string renameDirDest = Path.Combine(TestDirectory, "dir2_dest");
                string otherDir = Path.Combine(TestDirectory, "dir3");
                Directory.CreateDirectory(renameDirSource);

                Action action = () =>
                {
                    Directory.CreateDirectory(otherDir);
                    Directory.Move(renameDirSource, renameDirDest);
                    Directory.SetLastWriteTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));
                    Directory.Delete(otherDir);
                };
                Action cleanup = () =>
                {
                    Directory.Move(renameDirDest, renameDirSource);
                };

                WatcherChangeTypes expected = 0;
                ExpectEvent(watcher, expected, action, cleanup, new string[] { otherDir, dir });
            }
        }
    }
}
