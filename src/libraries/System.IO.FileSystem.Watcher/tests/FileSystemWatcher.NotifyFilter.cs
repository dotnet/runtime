// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public partial class NotifyFilterTests
{
    [Fact]
    [ActiveIssue(2011, PlatformID.OSX)]
    public static void FileSystemWatcher_NotifyFilter_Attributes()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.NotifyFilter = NotifyFilters.Attributes;
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            watcher.EnableRaisingEvents = true;

            var attributes = File.GetAttributes(file.Path);
            attributes |= FileAttributes.ReadOnly;

            File.SetAttributes(file.Path, attributes);

            Utility.ExpectEvent(eventOccured, "changed");
        }
    }

    [Fact]
    [PlatformSpecific(PlatformID.Windows | PlatformID.OSX)]
    [ActiveIssue(2011, PlatformID.OSX)]
    public static void FileSystemWatcher_NotifyFilter_CreationTime()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.NotifyFilter = NotifyFilters.CreationTime;
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            watcher.EnableRaisingEvents = true;

            File.SetCreationTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

            Utility.ExpectEvent(eventOccured, "changed");
        }
    }

    [Fact]
    public static void FileSystemWatcher_NotifyFilter_DirectoryName()
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.NotifyFilter = NotifyFilters.DirectoryName;
            watcher.Filter = Path.GetFileName(dir.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Renamed);

            string newName = dir.Path + "_rename";
            Utility.EnsureDelete(newName);

            watcher.EnableRaisingEvents = true;

            dir.Move(newName);

            Utility.ExpectEvent(eventOccured, "changed");
        }
    }


    [Fact]
    [ActiveIssue(2011, PlatformID.OSX)]
    public static void FileSystemWatcher_NotifyFilter_LastAccessTime()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.NotifyFilter = NotifyFilters.LastAccess;
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            watcher.EnableRaisingEvents = true;

            File.SetLastAccessTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

            Utility.ExpectEvent(eventOccured, "changed");
        }
    }

    [Fact]
    [ActiveIssue(2011, PlatformID.OSX)]
    public static void FileSystemWatcher_NotifyFilter_LastWriteTime()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            watcher.EnableRaisingEvents = true;

            File.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));

            Utility.ExpectEvent(eventOccured, "changed");
        }
    }

    [Fact]
    [ActiveIssue(2011, PlatformID.OSX)]
    public static void FileSystemWatcher_NotifyFilter_Size()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.NotifyFilter = NotifyFilters.Size;
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

            watcher.EnableRaisingEvents = true;

            byte[] buffer = new byte[16 * 1024];
            file.Write(buffer, 0, buffer.Length);

            // Size changes only occur when the file is written to disk
            file.Flush(flushToDisk: true);

            Utility.ExpectEvent(eventOccured, "changed");
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
    public static void FileSystemWatcher_NotifyFilter_Security()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.NotifyFilter = NotifyFilters.Security;
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Changed);

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

            Utility.ExpectEvent(eventOccured, "changed");
        }
    }


    [Fact]
    [ActiveIssue(2011, PlatformID.OSX)]
    public static void FileSystemWatcher_NotifyFilter_Negative()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            // only detect name.
            watcher.NotifyFilter = NotifyFilters.FileName;
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.All);

            string newName = file.Path + "_rename";
            Utility.EnsureDelete(newName);

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
            file.Write(buffer, 0, buffer.Length);
            file.Flush(flushToDisk: true);

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
            Utility.ExpectNoEvent(eventOccured, "any");
        }
    }
}
