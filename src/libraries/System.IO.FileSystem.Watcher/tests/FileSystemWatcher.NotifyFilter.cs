// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public partial class NotifyFilterTests : FileSystemWatcherTest
    {
        [Fact]
        [ActiveIssue(2011, PlatformID.OSX)]
        public void FileSystemWatcher_NotifyFilter_Attributes()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.NotifyFilter = NotifyFilters.Attributes;
                watcher.Filter = Path.GetFileName(file.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                watcher.EnableRaisingEvents = true;

                var attributes = File.GetAttributes(file.Path);
                attributes |= FileAttributes.ReadOnly;

                File.SetAttributes(file.Path, attributes);

                ExpectEvent(eventOccurred, "changed");
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows | PlatformID.OSX)]
        [ActiveIssue(2011, PlatformID.OSX)]
        public void FileSystemWatcher_NotifyFilter_CreationTime()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.NotifyFilter = NotifyFilters.CreationTime;
                watcher.Filter = Path.GetFileName(file.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                watcher.EnableRaisingEvents = true;

                File.SetCreationTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(eventOccurred, "changed");
            }
        }

        [Fact]
        public void FileSystemWatcher_NotifyFilter_DirectoryName()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.NotifyFilter = NotifyFilters.DirectoryName;
                watcher.Filter = Path.GetFileName(dir.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Renamed);

                string newName = Path.Combine(testDirectory.Path, GetTestFileName());

                watcher.EnableRaisingEvents = true;

                Directory.Move(dir.Path, newName);

                ExpectEvent(eventOccurred, "changed");
            }
        }


        [Fact]
        [ActiveIssue(2011, PlatformID.OSX)]
        public void FileSystemWatcher_NotifyFilter_LastAccessTime()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.NotifyFilter = NotifyFilters.LastAccess;
                watcher.Filter = Path.GetFileName(file.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                watcher.EnableRaisingEvents = true;

                File.SetLastAccessTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(eventOccurred, "changed");
            }
        }

        [Fact]
        [ActiveIssue(2011, PlatformID.OSX)]
        public void FileSystemWatcher_NotifyFilter_LastWriteTime()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Filter = Path.GetFileName(file.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                watcher.EnableRaisingEvents = true;

                File.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                ExpectEvent(eventOccurred, "changed");
            }
        }

        [Fact]
        [ActiveIssue(2011, PlatformID.OSX)]
        public void FileSystemWatcher_NotifyFilter_Size()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.NotifyFilter = NotifyFilters.Size;
                watcher.Filter = Path.GetFileName(file.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                watcher.EnableRaisingEvents = true;

                byte[] buffer = new byte[16 * 1024];
                File.WriteAllBytes(file.Path, buffer);

                ExpectEvent(eventOccurred, "changed");
            }
        }

        [DllImport(
                "api-ms-win-security-provider-l1-1-0.dll",
                EntryPoint = "SetNamedSecurityInfoW",
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true,
                ExactSpelling = true,
                CharSet = CharSet.Unicode)]
        private static extern uint SetSecurityInfoByHandle(
                string name,
                uint objectType,
                uint securityInformation,
                IntPtr owner,
                IntPtr group,
                IntPtr dacl,
                IntPtr sacl);

        private const uint ERROR_SUCCESS = 0;
        private const uint DACL_SECURITY_INFORMATION = 0x00000004;
        private const uint SE_FILE_OBJECT = 0x1;

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void FileSystemWatcher_NotifyFilter_Security()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.NotifyFilter = NotifyFilters.Security;
                watcher.Filter = Path.GetFileName(file.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.Changed);

                watcher.EnableRaisingEvents = true;

                // ACL support is not yet available, so pinvoke directly.
                uint result = SetSecurityInfoByHandle(file.Path,
                    SE_FILE_OBJECT,
                    DACL_SECURITY_INFORMATION, // Only setting the DACL
                    owner: IntPtr.Zero,
                    group: IntPtr.Zero,
                    dacl: IntPtr.Zero, // full access to everyone
                    sacl: IntPtr.Zero);
                Assert.Equal(ERROR_SUCCESS, result);

                ExpectEvent(eventOccurred, "changed");
            }
        }


        [Fact]
        [ActiveIssue(2011, PlatformID.OSX)]
        public void FileSystemWatcher_NotifyFilter_Negative()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                // only detect name.
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Filter = Path.GetFileName(file.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, WatcherChangeTypes.All);

                string newName = Path.Combine(testDirectory.Path, GetTestFileName());
                watcher.EnableRaisingEvents = true;

                // Change attributes
                var attributes = File.GetAttributes(file.Path);
                File.SetAttributes(file.Path, attributes | FileAttributes.ReadOnly);
                File.SetAttributes(file.Path, attributes);

                // Change creation time
                File.SetCreationTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                // Change access time
                File.SetLastAccessTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                // Change write time
                File.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

                // Change size
                byte[] buffer = new byte[16 * 1024];
                File.WriteAllBytes(file.Path, buffer);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Change security
                    uint result = SetSecurityInfoByHandle(file.Path,
                        SE_FILE_OBJECT,
                        DACL_SECURITY_INFORMATION, // Only setting the DACL
                        owner: IntPtr.Zero,
                        group: IntPtr.Zero,
                        dacl: IntPtr.Zero, // full access to everyone
                        sacl: IntPtr.Zero);
                    Assert.Equal(ERROR_SUCCESS, result);
                }

                // None of these should trigger any events
                ExpectNoEvent(eventOccurred, "any");
            }
        }
    }
}