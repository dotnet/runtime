// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace System.IO.Tests
{
    public partial class File_NotifyFilter_Tests : FileSystemWatcherTest
    {
        [LibraryImport("advapi32.dll", EntryPoint = "SetNamedSecurityInfoW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial uint SetSecurityInfoByHandle(string name, uint objectType, uint securityInformation,
            IntPtr owner, IntPtr group, IntPtr dacl, IntPtr sacl);

        private const uint ERROR_SUCCESS = 0;
        private const uint DACL_SECURITY_INFORMATION = 0x00000004;
        private const uint SE_FILE_OBJECT = 0x1;

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_File_NotifyFilter_Attributes(NotifyFilters filter)
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                watcher.NotifyFilter = filter;
                var attributes = File.GetAttributes(file);

                Action action = () => File.SetAttributes(file, attributes | FileAttributes.ReadOnly);
                Action cleanup = () => File.SetAttributes(file, attributes);

                WatcherChangeTypes expected = 0;
                if (filter == NotifyFilters.Attributes)
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForAttribute) > 0))
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsMacOS() && ((filter & NotifyFilters.Security) > 0))
                    expected |= WatcherChangeTypes.Changed; // Attribute change on OSX is a ChangeOwner operation which passes the Security NotifyFilter.

                ExpectEvent(watcher, expected, action, cleanup, file);
            }
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_File_NotifyFilter_CreationTime(NotifyFilters filter)
        {
            FileSystemWatcherTest.Execute(() =>
            {
                string file = CreateTestFile(TestDirectory, "file");
                using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
                {
                    watcher.NotifyFilter = filter;
                    Action action = () => File.SetCreationTime(file, DateTime.Now + TimeSpan.FromSeconds(10));

                    WatcherChangeTypes expected = 0;
                    if (filter == NotifyFilters.CreationTime)
                        expected |= WatcherChangeTypes.Changed;
                    else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForAttribute) > 0))
                        expected |= WatcherChangeTypes.Changed;
                    else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                        expected |= WatcherChangeTypes.Changed;

                    ExpectEvent(watcher, expected, action, expectedPath: file);
                }
            }, maxAttempts: DefaultAttemptsForExpectedEvent, backoffFunc: (iteration) => RetryDelayMilliseconds, retryWhen: e => e is XunitException);
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_File_NotifyFilter_DirectoryName(NotifyFilters filter)
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
        public void FileSystemWatcher_File_NotifyFilter_LastAccessTime(NotifyFilters filter)
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                watcher.NotifyFilter = filter;
                Action action = () => File.SetLastAccessTime(file, DateTime.Now + TimeSpan.FromSeconds(10));

                WatcherChangeTypes expected = 0;
                if (filter == NotifyFilters.LastAccess)
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForAttribute) > 0))
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                    expected |= WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, expectedPath: file);
            }
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_File_NotifyFilter_LastWriteTime(NotifyFilters filter)
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                watcher.NotifyFilter = filter;
                Action action = () => File.SetLastWriteTime(file, DateTime.Now + TimeSpan.FromSeconds(10));

                WatcherChangeTypes expected = 0;
                if (filter == NotifyFilters.LastWrite)
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForAttribute) > 0))
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                    expected |= WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, expectedPath: file);
            }
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_File_NotifyFilter_Size(NotifyFilters filter)
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                watcher.NotifyFilter = filter;
                Action action = () => File.AppendAllText(file, "longText!");
                Action cleanup = () => File.AppendAllText(file, "short");

                WatcherChangeTypes expected = 0;
                if (filter == NotifyFilters.Size || filter == NotifyFilters.LastWrite)
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForModify) > 0))
                    expected |= WatcherChangeTypes.Changed;
                else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                    expected |= WatcherChangeTypes.Changed;
                else if (PlatformDetection.IsWindows7 && filter == NotifyFilters.Attributes) // win7 FSW Size change passes the Attribute filter
                    expected |= WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, expectedPath: file);
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(FilterTypes))]
        public void FileSystemWatcher_File_NotifyFilter_Size_TwoFilters(NotifyFilters filter)
        {
            Assert.All(FilterTypes(), (filter2Arr =>
            {
                string file = CreateTestFile(TestDirectory, "file");
                using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
                {
                    filter |= (NotifyFilters)filter2Arr[0];
                    watcher.NotifyFilter = filter;
                    Action action = () => File.AppendAllText(file, "longText!");
                    Action cleanup = () => File.AppendAllText(file, "short");

                    WatcherChangeTypes expected = 0;
                    if (((filter & NotifyFilters.Size) > 0) || ((filter & NotifyFilters.LastWrite) > 0))
                        expected |= WatcherChangeTypes.Changed;
                    else if (OperatingSystem.IsLinux() && ((filter & LinuxFiltersForModify) > 0))
                        expected |= WatcherChangeTypes.Changed;
                    else if (OperatingSystem.IsMacOS() && ((filter & OSXFiltersForModify) > 0))
                        expected |= WatcherChangeTypes.Changed;
                    else if (PlatformDetection.IsWindows7 && ((filter & NotifyFilters.Attributes) > 0)) // win7 FSW Size change passes the Attribute filter
                        expected |= WatcherChangeTypes.Changed;
                    ExpectEvent(watcher, expected, action, expectedPath: file);
                }
            }));
        }

        [Theory]
        [MemberData(nameof(FilterTypes))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to set security info
        public void FileSystemWatcher_File_NotifyFilter_Security(NotifyFilters filter)
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                watcher.NotifyFilter = filter;
                Action action = () =>
                {
                    // ACL support is not yet available, so pinvoke directly.
                    uint result = SetSecurityInfoByHandle(file,
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
                    // Recreate the file.
                    File.Delete(file);
                    File.AppendAllText(file, "text");
                };

                WatcherChangeTypes expected = 0;
                if (filter == NotifyFilters.Security)
                    expected |= WatcherChangeTypes.Changed;
                else if (PlatformDetection.IsWindows7 && filter == NotifyFilters.Attributes) // win7 FSW Security change passes the Attribute filter
                    expected |= WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, expectedPath: file);
            }
        }

        /// <summary>
        /// Tests a changed event on a directory when filtering for LastWrite and FileName.
        /// </summary>
        [Fact]
        public void FileSystemWatcher_File_NotifyFilter_LastWriteAndFileName()
        {
            string dir = CreateTestDirectory(TestDirectory, "dir");
            using (var watcher = new FileSystemWatcher(TestDirectory, Path.GetFileName(dir)))
            {
                NotifyFilters filter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                watcher.NotifyFilter = filter;

                Action action = () => Directory.SetLastWriteTime(dir, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(watcher, WatcherChangeTypes.Changed, action, expectedPath: dir);
            }
        }

        /// <summary>
        /// Tests the watcher behavior when two events - a Modification and a Creation - happen closely
        /// after each other.
        /// </summary>
        [Fact]
        public void FileSystemWatcher_File_NotifyFilter_ModifyAndCreate()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                string otherFile = Path.Combine(TestDirectory, "file2");

                Action action = () =>
                {
                    File.Create(otherFile).Dispose();
                    File.SetLastWriteTime(file, DateTime.Now + TimeSpan.FromSeconds(10));
                };
                Action cleanup = () => File.Delete(otherFile);

                WatcherChangeTypes expected = 0;
                expected |= WatcherChangeTypes.Created | WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, cleanup, new string[] { otherFile, file });
            }
        }

        /// <summary>
        /// Tests the watcher behavior when two events - a Modification and a Deletion - happen closely
        /// after each other.
        /// </summary>
        [Fact]
        public void FileSystemWatcher_File_NotifyFilter_ModifyAndDelete()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                string otherFile = Path.Combine(TestDirectory, "file2");

                Action action = () =>
                {
                    File.Delete(otherFile);
                    File.SetLastWriteTime(file, DateTime.Now + TimeSpan.FromSeconds(10));
                };
                Action cleanup = () =>
                {
                    File.Create(otherFile).Dispose();
                };
                cleanup();

                WatcherChangeTypes expected = 0;
                expected |= WatcherChangeTypes.Deleted | WatcherChangeTypes.Changed;
                ExpectEvent(watcher, expected, action, cleanup, new string[] { otherFile, file });
            }
        }

        [Fact]
        public void FileSystemWatcher_File_NotifyFilter_FileNameDoesntTriggerOnDirectoryEvent()
        {
            string file = CreateTestFile(TestDirectory, "file");
            string sourcePath = CreateTestFile(TestDirectory, "sourceFile");
            using (var watcher = new FileSystemWatcher(TestDirectory, "*"))
            {
                watcher.NotifyFilter = NotifyFilters.DirectoryName;
                string otherFile = Path.Combine(TestDirectory, "file2");
                string destPath = Path.Combine(TestDirectory, "destFile");

                Action action = () =>
                {
                    File.Create(otherFile).Dispose();
                    File.SetLastWriteTime(file, DateTime.Now + TimeSpan.FromSeconds(10));
                    File.Delete(otherFile);
                    File.Move(sourcePath, destPath);
                };
                Action cleanup = () =>
                {
                    File.Move(destPath, sourcePath);
                };

                WatcherChangeTypes expected = 0;
                ExpectEvent(watcher, expected, action, cleanup, new string[] { otherFile, file });
            }
        }
    }
}
