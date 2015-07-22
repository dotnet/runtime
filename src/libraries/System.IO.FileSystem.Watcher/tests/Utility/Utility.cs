// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

public static class Utility
{
    // events are reported asynchronously by the OS, so allow an amount of time for
    // them to arrive before testing an assertion.
    public const int Timeout = 500;
    public const int WaitForCreationTimeoutInMs = 1000 * 30;

    public static TemporaryTestFile CreateTestFile([CallerMemberName] string path = null)
    {
        if (String.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(path);
        }

        return new TemporaryTestFile(path);
    }

    public static TemporaryTestDirectory CreateTestDirectory([CallerMemberName] string path = null)
    {
        if (String.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(path);
        }

        return new TemporaryTestDirectory(path);
    }

    public static void EnsureDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        else if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    public static AutoResetEvent WatchForEvents(FileSystemWatcher watcher, WatcherChangeTypes actions)
    {
        AutoResetEvent eventOccured = new AutoResetEvent(false);

        if (0 != (actions & WatcherChangeTypes.Changed))
        {
            watcher.Changed += (o, e) =>
            {
                Assert.Equal(WatcherChangeTypes.Changed, e.ChangeType);
                eventOccured.Set();
            };
        }

        if (0 != (actions & WatcherChangeTypes.Created))
        {
            watcher.Created += (o, e) =>
            {
                Assert.Equal(WatcherChangeTypes.Created, e.ChangeType);
                eventOccured.Set();
            };
        }

        if (0 != (actions & WatcherChangeTypes.Deleted))
        {
            watcher.Deleted += (o, e) =>
            {
                Assert.Equal(WatcherChangeTypes.Deleted, e.ChangeType);
                eventOccured.Set();
            };
        }

        if (0 != (actions & WatcherChangeTypes.Renamed))
        {
            watcher.Renamed += (o, e) =>
            {
                Assert.Equal(WatcherChangeTypes.Renamed, e.ChangeType);
                eventOccured.Set();
            };
        }

        return eventOccured;
    }

    public static void ExpectEvent(WaitHandle eventOccured, string eventName, int timeout = Utility.Timeout)
    {
        string message = String.Format("Didn't observe a {0} event within {1}ms", eventName, timeout);
        Assert.True(eventOccured.WaitOne(timeout), message);
    }

    public static void ExpectNoEvent(WaitHandle eventOccured, string eventName, int timeout = Utility.Timeout)
    {
        string message = String.Format("Should not observe a {0} event", eventName);
        Assert.False(eventOccured.WaitOne(timeout), message);
    }

    public static void TestNestedDirectoriesHelper(
        WatcherChangeTypes change,
        Action<AutoResetEvent, TemporaryTestDirectory> action,
        NotifyFilters changeFilers = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName)
    {
        using (var dir = Utility.CreateTestDirectory())
        using (var watcher = new FileSystemWatcher())
        {
            AutoResetEvent createdOccured = Utility.WatchForEvents(watcher, WatcherChangeTypes.Created); // not "using" to avoid race conditions with FSW callbacks
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, change);

            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*";
            watcher.NotifyFilter = changeFilers;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            using (var firstDir = new TemporaryTestDirectory(Path.Combine(dir.Path, "dir1")))
            {
                Utility.ExpectEvent(createdOccured, "dir1 created", WaitForCreationTimeoutInMs);

                using (var nestedDir = new TemporaryTestDirectory(Path.Combine(firstDir.Path, "nested")))
                {
                    Utility.ExpectEvent(createdOccured, "nested created", WaitForCreationTimeoutInMs);

                    action(eventOccured, nestedDir);
                }
            }
        }
    }
}
