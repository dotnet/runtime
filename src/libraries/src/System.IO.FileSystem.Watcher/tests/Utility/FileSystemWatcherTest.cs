// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileSystemWatcherTest : FileCleanupTestBase
    {
        // Events are reported asynchronously by the OS, so allow an amount of time for
        // them to arrive before testing an assertion.  If we expect an event to occur,
        // we can wait for it for a relatively long time, as if it doesn't arrive, we're
        // going to fail the test.  If we don't expect an event to occur, then we need
        // to keep the timeout short, as in a successful run we'll end up waiting for
        // the entire timeout specified.
        public const int WaitForExpectedEventTimeout = 30000;
        public const int WaitForUnexpectedEventTimeout = 500;

        public static AutoResetEvent WatchForEvents(FileSystemWatcher watcher, WatcherChangeTypes actions)
        {
            AutoResetEvent eventOccurred = new AutoResetEvent(false);

            if (0 != (actions & WatcherChangeTypes.Changed))
            {
                watcher.Changed += (o, e) =>
                {
                    Assert.Equal(WatcherChangeTypes.Changed, e.ChangeType);
                    eventOccurred.Set();
                };
            }

            if (0 != (actions & WatcherChangeTypes.Created))
            {
                watcher.Created += (o, e) =>
                {
                    Assert.Equal(WatcherChangeTypes.Created, e.ChangeType);
                    eventOccurred.Set();
                };
            }

            if (0 != (actions & WatcherChangeTypes.Deleted))
            {
                watcher.Deleted += (o, e) =>
                {
                    Assert.Equal(WatcherChangeTypes.Deleted, e.ChangeType);
                    eventOccurred.Set();
                };
            }

            if (0 != (actions & WatcherChangeTypes.Renamed))
            {
                watcher.Renamed += (o, e) =>
                {
                    Assert.Equal(WatcherChangeTypes.Renamed, e.ChangeType);
                    eventOccurred.Set();
                };
            }

            return eventOccurred;
        }

        public static void ExpectEvent(WaitHandle eventOccurred, string eventName, int timeout = WaitForExpectedEventTimeout)
        {
            string message = String.Format("Didn't observe a {0} event within {1}ms", eventName, timeout);
            Assert.True(eventOccurred.WaitOne(timeout), message);
        }

        public static void ExpectNoEvent(WaitHandle eventOccurred, string eventName, int timeout = WaitForUnexpectedEventTimeout)
        {
            string message = String.Format("Should not observe a {0} event within {1}ms", eventName, timeout);
            Assert.False(eventOccurred.WaitOne(timeout), message);
        }

        public static void TestNestedDirectoriesHelper(
            string testDirectory,
            WatcherChangeTypes change,
            Action<AutoResetEvent, TempDirectory> action,
            NotifyFilters changeFilers = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName)
        {
            using (var dir = new TempDirectory(testDirectory))
            using (var watcher = new FileSystemWatcher())
            {
                AutoResetEvent createdOccurred = WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
                AutoResetEvent eventOccurred = WatchForEvents(watcher, change);

                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*";
                watcher.NotifyFilter = changeFilers;
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                using (var firstDir = new TempDirectory(Path.Combine(dir.Path, "dir1")))
                {
                    ExpectEvent(createdOccurred, "dir1 created");

                    using (var nestedDir = new TempDirectory(Path.Combine(firstDir.Path, "nested")))
                    {
                        ExpectEvent(createdOccurred, "nested created");

                        action(eventOccurred, nestedDir);
                    }
                }
            }
        }

        // In some cases (such as when running without elevated privileges),
        // the symbolic link may fail to create. Only run this test if it creates
        // links successfully.
        protected static bool CanCreateSymbolicLinks
        {
            get
            {
                var path = Path.GetTempFileName();
                var linkPath = path + ".link";
                var ret = CreateSymLink(path, linkPath, isDirectory: false);
                ret = ret && File.Exists(linkPath);
                try { File.Delete(path); } catch { }
                try { File.Delete(linkPath); } catch { }
                return ret;
            }
        }

        public static bool CreateSymLink(string targetPath, string linkPath, bool isDirectory)
        {
            Process symLinkProcess = new Process();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                symLinkProcess.StartInfo.FileName = "cmd";
                symLinkProcess.StartInfo.Arguments = string.Format("/c mklink{0} \"{1}\" \"{2}\"", isDirectory ? " /D" : "", linkPath, targetPath);
            }
            else
            {
                symLinkProcess.StartInfo.FileName = "ln";
                symLinkProcess.StartInfo.Arguments = string.Format("-s \"{0}\" \"{1}\"", targetPath, linkPath);
            }
            symLinkProcess.StartInfo.RedirectStandardOutput = true;
            symLinkProcess.Start();

            if (symLinkProcess != null)
            {
                symLinkProcess.WaitForExit();
                return (0 == symLinkProcess.ExitCode);
            }
            else
            {
                return false;
            }
        }
    }
}