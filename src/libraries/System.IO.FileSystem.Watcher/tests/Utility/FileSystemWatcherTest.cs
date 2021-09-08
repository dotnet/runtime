// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Sdk;
using Xunit.Abstractions;

namespace System.IO.Tests
{
    [CollectionDefinition("NoParallelTests", DisableParallelization = true)]
    public partial class NoParallelTests { }

    public abstract partial class FileSystemWatcherTest : FileCleanupTestBase
    {
        // Events are reported asynchronously by the OS, so allow an amount of time for
        // them to arrive before testing an assertion.  If we expect an event to occur,
        // we can wait for it for a relatively long time, as if it doesn't arrive, we're
        // going to fail the test.  If we don't expect an event to occur, then we need
        // to keep the timeout short, as in a successful run we'll end up waiting for
        // the entire timeout specified.
        public const int WaitForExpectedEventTimeout = 500;         // ms to wait for an event to happen
        public const int LongWaitTimeout = 50000;                   // ms to wait for an event that takes a longer time than the average operation
        public const int SubsequentExpectedWait = 10;               // ms to wait for checks that occur after the first.
        public const int WaitForExpectedEventTimeout_NoRetry = 3000;// ms to wait for an event that isn't surrounded by a retry.
        public const int WaitForUnexpectedEventTimeout = 150;       // ms to wait for a non-expected event.
        public const int DefaultAttemptsForExpectedEvent = 3;       // Number of times an expected event should be retried if failing.
        public const int DefaultAttemptsForUnExpectedEvent = 2;     // Number of times an unexpected event should be retried if failing.
        public const int RetryDelayMilliseconds = 500;              // ms to wait when retrying after failure

        /// <summary>
        /// Watches the Changed WatcherChangeType and unblocks the returned AutoResetEvent when a
        /// Changed event is thrown by the watcher.
        /// </summary>
        public static (AutoResetEvent EventOccured, FileSystemEventHandler Handler) WatchChanged(FileSystemWatcher watcher, string[] expectedPaths = null)
        {
            AutoResetEvent eventOccurred = new AutoResetEvent(false);

            FileSystemEventHandler changeHandler = (o, e) =>
            {
                Assert.Equal(WatcherChangeTypes.Changed, e.ChangeType);
                if (expectedPaths != null)
                {
                    Assert.Contains(Path.GetFullPath(e.FullPath), expectedPaths);
                }
                eventOccurred.Set();
            };

            watcher.Changed += changeHandler;
            return (eventOccurred, changeHandler);
        }

        /// <summary>
        /// Watches the Created WatcherChangeType and unblocks the returned AutoResetEvent when a
        /// Created event is thrown by the watcher.
        /// </summary>
        public static (AutoResetEvent EventOccured, FileSystemEventHandler Handler) WatchCreated(FileSystemWatcher watcher, string[] expectedPaths = null, ITestOutputHelper _output = null)
        {
            AutoResetEvent eventOccurred = new AutoResetEvent(false);

            FileSystemEventHandler handler = (o, e) =>
            {
                if (e.ChangeType != WatcherChangeTypes.Created)
                {
                    _output?.WriteLine("Unexpected event {0} while waiting for {1}", e.ChangeType, WatcherChangeTypes.Created);
                    Assert.Equal(WatcherChangeTypes.Created, e.ChangeType);
                }

                Assert.Equal(WatcherChangeTypes.Created, e.ChangeType);
                if (expectedPaths != null)
                {
                    try
                    {
                        Assert.Contains(Path.GetFullPath(e.FullPath), expectedPaths);
                    }
                    catch (Exception ex)
                    {
                        _output?.WriteLine(ex.ToString());
                        throw;
                    }
                }

                eventOccurred.Set();
            };

            watcher.Created += handler;
            return (eventOccurred, handler);
        }

        /// <summary>
        /// Watches the Renamed WatcherChangeType and unblocks the returned AutoResetEvent when a
        /// Renamed event is thrown by the watcher.
        /// </summary>
        public static (AutoResetEvent EventOccured, FileSystemEventHandler Handler) WatchDeleted(FileSystemWatcher watcher, string[] expectedPaths = null, ITestOutputHelper _output = null)
        {
            AutoResetEvent eventOccurred = new AutoResetEvent(false);
            FileSystemEventHandler handler = (o, e) =>
            {
                if (e.ChangeType != WatcherChangeTypes.Deleted)
                {
                    _output?.WriteLine("Unexpected event {0} while waiting for {1}", e.ChangeType, WatcherChangeTypes.Deleted);
                    Assert.Equal(WatcherChangeTypes.Deleted, e.ChangeType);
                }

                if (expectedPaths != null)
                {
                    try
                    {
                        Assert.Contains(Path.GetFullPath(e.FullPath), expectedPaths);
                    }
                    catch (Exception ex)
                    {
                        _output?.WriteLine(ex.ToString());
                        throw;
                    }
                }
                eventOccurred.Set();
            };

            watcher.Deleted += handler;
            return (eventOccurred, handler);
        }

        /// <summary>
        /// Watches the Renamed WatcherChangeType and unblocks the returned AutoResetEvent when a
        /// Renamed event is thrown by the watcher.
        /// </summary>
        public static (AutoResetEvent EventOccured, RenamedEventHandler Handler) WatchRenamed(FileSystemWatcher watcher, string[] expectedPaths = null, ITestOutputHelper _output = null)
        {
            AutoResetEvent eventOccurred = new AutoResetEvent(false);

            RenamedEventHandler handler = (o, e) =>
            {
                if (e.ChangeType != WatcherChangeTypes.Renamed)
                {
                    _output?.WriteLine("Unexpected event {0} while waiting for {1}", e.ChangeType, WatcherChangeTypes.Renamed);
                    Assert.Equal(WatcherChangeTypes.Renamed, e.ChangeType);
                }

                if (expectedPaths != null)
                {
                    try
                    {
                        Assert.Contains(Path.GetFullPath(e.FullPath), expectedPaths);
                    }
                    catch (Exception ex)
                    {
                        _output?.WriteLine(ex.ToString());
                        throw;
                    }
                }
                eventOccurred.Set();
            };

            watcher.Renamed += handler;
            return (eventOccurred, handler);
        }

        /// <summary>
        /// Asserts that the given handle will be signaled within the default timeout.
        /// </summary>
        public static void ExpectEvent(WaitHandle eventOccurred, string eventName_NoRetry)
        {
            string message = string.Format("Didn't observe a {0} event within {1}ms", eventName_NoRetry, WaitForExpectedEventTimeout_NoRetry);
            Assert.True(eventOccurred.WaitOne(WaitForExpectedEventTimeout_NoRetry), message);
        }

        /// <summary>
        /// Does verification that the given watcher will throw exactly/only the events in "expectedEvents" when
        /// "action" is executed.
        /// </summary>
        /// <param name="watcher">The FileSystemWatcher to test</param>
        /// <param name="expectedEvents">All of the events that are expected to be raised by this action</param>
        /// <param name="action">The Action that will trigger events.</param>
        /// <param name="cleanup">Optional. Undoes the action and cleans up the watcher so the test may be run again if necessary.</param>
        public static void ExpectEvent(FileSystemWatcher watcher, WatcherChangeTypes expectedEvents, Action action, Action cleanup = null)
        {
            ExpectEvent(watcher, expectedEvents, action, cleanup, (string[])null);
        }

        /// <summary>
        /// Does verification that the given watcher will throw exactly/only the events in "expectedEvents" when
        /// "action" is executed.
        /// </summary>
        /// <param name="watcher">The FileSystemWatcher to test</param>
        /// <param name="expectedEvents">All of the events that are expected to be raised by this action</param>
        /// <param name="action">The Action that will trigger events.</param>
        /// <param name="cleanup">Optional. Undoes the action and cleans up the watcher so the test may be run again if necessary.</param>
        /// <param name="expectedPath">Optional. Adds path verification to all expected events.</param>
        /// <param name="attempts">Optional. Number of times the test should be executed if it's failing.</param>
        public static void ExpectEvent(FileSystemWatcher watcher, WatcherChangeTypes expectedEvents, Action action, Action cleanup = null, string expectedPath = null, int attempts = DefaultAttemptsForExpectedEvent, int timeout = WaitForExpectedEventTimeout)
        {
            ExpectEvent(watcher, expectedEvents, action, cleanup, expectedPath == null ? null : new string[] { expectedPath }, attempts, timeout);
        }

        /// <summary>
        /// Does verification that the given watcher will throw exactly/only the events in "expectedEvents" when
        /// "action" is executed.
        /// </summary>
        /// <param name="watcher">The FileSystemWatcher to test</param>
        /// <param name="expectedEvents">All of the events that are expected to be raised by this action</param>
        /// <param name="action">The Action that will trigger events.</param>
        /// <param name="cleanup">Optional. Undoes the action and cleans up the watcher so the test may be run again if necessary.</param>
        /// <param name="expectedPath">Optional. Adds path verification to all expected events.</param>
        /// <param name="attempts">Optional. Number of times the test should be executed if it's failing.</param>
        public static void ExpectEvent(FileSystemWatcher watcher, WatcherChangeTypes expectedEvents, Action action, Action cleanup = null, string[] expectedPaths = null, int attempts = DefaultAttemptsForExpectedEvent, int timeout = WaitForExpectedEventTimeout)
        {
            int attemptsCompleted = 0;
            bool result = false;
            FileSystemWatcher newWatcher = watcher;
            while (!result && attemptsCompleted++ < attempts)
            {
                if (attemptsCompleted > 1)
                {
                    // Re-create the watcher to get a clean iteration.
                    newWatcher = RecreateWatcher(newWatcher);
                    // Most intermittent failures in FSW are caused by either a shortage of resources (e.g. inotify instances)
                    // or by insufficient time to execute (e.g. CI gets bogged down). Immediately re-running a failed test
                    // won't resolve the first issue, so we wait a little while hoping that things clear up for the next run.
                    Thread.Sleep(RetryDelayMilliseconds);
                }

                result = ExecuteAndVerifyEvents(newWatcher, expectedEvents, action, attemptsCompleted == attempts, expectedPaths, timeout);

                if (cleanup != null)
                    cleanup();
            }
        }

        /// <summary>Invokes the specified test action with retry on failure (other than assertion failure).</summary>
        /// <param name="action">The test action.</param>
        /// <param name="maxAttempts">The maximum number of times to attempt to run the test.</param>
        public static void ExecuteWithRetry(Action action, int maxAttempts = DefaultAttemptsForExpectedEvent)
        {
            for (int retry = 0; retry < maxAttempts; retry++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception e) when (!(e is XunitException) && retry < maxAttempts - 1)
                {
                    Thread.Sleep(RetryDelayMilliseconds);
                }
            }
        }

        /// <summary>
        /// Does verification that the given watcher will not throw exactly/only the events in "expectedEvents" when
        /// "action" is executed.
        /// </summary>
        /// <param name="watcher">The FileSystemWatcher to test</param>
        /// <param name="unExpectedEvents">All of the events that are expected to be raised by this action</param>
        /// <param name="action">The Action that will trigger events.</param>
        /// <param name="cleanup">Optional. Undoes the action and cleans up the watcher so the test may be run again if necessary.</param>
        /// <param name="expectedPath">Optional. Adds path verification to all expected events.</param>
        public static void ExpectNoEvent(FileSystemWatcher watcher, WatcherChangeTypes unExpectedEvents, Action action, Action cleanup = null, string expectedPath = null, int timeout = WaitForExpectedEventTimeout)
        {
            bool result = ExecuteAndVerifyEvents(watcher, unExpectedEvents, action, false, new string[] { expectedPath }, timeout);
            Assert.False(result, "Expected Event occured");

            if (cleanup != null)
                cleanup();
        }

        /// <summary>
        /// Helper for the ExpectEvent function.
        /// </summary>
        /// <param name="watcher">The FileSystemWatcher to test</param>
        /// <param name="expectedEvents">All of the events that are expected to be raised by this action</param>
        /// <param name="action">The Action that will trigger events.</param>
        /// <param name="assertExpected">True if results should be asserted. Used if there is no retry.</param>
        /// <param name="expectedPath"> Adds path verification to all expected events.</param>
        /// <returns>True if the events raised correctly; else, false.</returns>
        public static bool ExecuteAndVerifyEvents(FileSystemWatcher watcher, WatcherChangeTypes expectedEvents, Action action, bool assertExpected, string[] expectedPaths, int timeout)
        {
            bool result = true, verifyChanged = true, verifyCreated = true, verifyDeleted = true, verifyRenamed = true;
            (AutoResetEvent EventOccured, FileSystemEventHandler Handler) changed = default, created = default, deleted = default;
            (AutoResetEvent EventOccured, RenamedEventHandler Handler) renamed = default;

            if (verifyChanged = ((expectedEvents & WatcherChangeTypes.Changed) > 0))
                changed = WatchChanged(watcher, expectedPaths);
            if (verifyCreated = ((expectedEvents & WatcherChangeTypes.Created) > 0))
                created = WatchCreated(watcher, expectedPaths);
            if (verifyDeleted = ((expectedEvents & WatcherChangeTypes.Deleted) > 0))
                deleted = WatchDeleted(watcher, expectedPaths);
            if (verifyRenamed = ((expectedEvents & WatcherChangeTypes.Renamed) > 0))
                renamed = WatchRenamed(watcher, expectedPaths);

            watcher.EnableRaisingEvents = true;
            action();

            // Verify Changed
            if (verifyChanged)
            {
                bool Changed_expected = ((expectedEvents & WatcherChangeTypes.Changed) > 0);
                bool Changed_actual = changed.EventOccured.WaitOne(timeout);
                watcher.Changed -= changed.Handler;
                result = Changed_expected == Changed_actual;
                if (assertExpected)
                    Assert.True(Changed_expected == Changed_actual, "Changed event did not occur as expected");
            }

            // Verify Created
            if (verifyCreated)
            {
                bool Created_expected = ((expectedEvents & WatcherChangeTypes.Created) > 0);
                bool Created_actual = created.EventOccured.WaitOne(verifyChanged ? SubsequentExpectedWait : timeout);
                watcher.Created -= created.Handler;
                result = result && Created_expected == Created_actual;
                if (assertExpected)
                    Assert.True(Created_expected == Created_actual, "Created event did not occur as expected");
            }

            // Verify Deleted
            if (verifyDeleted)
            {
                bool Deleted_expected = ((expectedEvents & WatcherChangeTypes.Deleted) > 0);
                bool Deleted_actual = deleted.EventOccured.WaitOne(verifyChanged || verifyCreated ? SubsequentExpectedWait : timeout);
                watcher.Deleted -= deleted.Handler;
                result = result && Deleted_expected == Deleted_actual;
                if (assertExpected)
                    Assert.True(Deleted_expected == Deleted_actual, "Deleted event did not occur as expected");
            }

            // Verify Renamed
            if (verifyRenamed)
            {
                bool Renamed_expected = ((expectedEvents & WatcherChangeTypes.Renamed) > 0);
                bool Renamed_actual = renamed.EventOccured.WaitOne(verifyChanged || verifyCreated || verifyDeleted ? SubsequentExpectedWait : timeout);
                watcher.Renamed -= renamed.Handler;
                result = result && Renamed_expected == Renamed_actual;
                if (assertExpected)
                    Assert.True(Renamed_expected == Renamed_actual, "Renamed event did not occur as expected");
            }

            watcher.EnableRaisingEvents = false;
            return result;
        }

        /// <summary>
        /// Does verification that the given watcher will throw an Error when the given action is executed.
        /// </summary>
        /// <param name="watcher">The FileSystemWatcher to test</param>
        /// <param name="action">The Action that will trigger a failure.</param>
        /// <param name="cleanup">Undoes the action and cleans up the watcher so the test may be run again if necessary.</param>
        /// <param name="attempts">Optional. Number of times the test should be executed if it's failing.</param>
        public static void ExpectError(FileSystemWatcher watcher, Action action, Action cleanup, int attempts = DefaultAttemptsForExpectedEvent)
        {
            string message = string.Format("Did not observe an error event within {0}ms and {1} attempts.", WaitForExpectedEventTimeout, attempts);
            Assert.True(TryErrorEvent(watcher, action, cleanup, attempts, expected: true), message);
        }

        /// <summary>
        /// Does verification that the given watcher will <b>not</b> throw an Error when the given action is executed.
        /// </summary>
        /// <param name="watcher">The FileSystemWatcher to test</param>
        /// <param name="action">The Action that will not trigger a failure.</param>
        /// <param name="cleanup">Undoes the action and cleans up the watcher so the test may be run again if necessary.</param>
        /// <param name="attempts">Optional. Number of times the test should be executed if it's failing.</param>
        public static void ExpectNoError(FileSystemWatcher watcher, Action action, Action cleanup, int attempts = DefaultAttemptsForUnExpectedEvent)
        {
            string message = string.Format("Should not observe an error event within {0}ms. Attempted {1} times and received the event each time.", WaitForExpectedEventTimeout, attempts);
            Assert.False(TryErrorEvent(watcher, action, cleanup, attempts, expected: true), message);
        }

        /// /// <summary>
        /// Helper method for the ExpectError/ExpectNoError functions.
        /// </summary>
        /// <param name="watcher">The FileSystemWatcher to test</param>
        /// <param name="action">The Action to execute.</param>
        /// <param name="cleanup">Undoes the action and cleans up the watcher so the test may be run again if necessary.</param>
        /// <param name="attempts">Number of times the test should be executed if it's failing.</param>
        /// <param name="expected">Whether it is expected that an error event will be arisen.</param>
        /// <returns>True if an Error event was raised by the watcher when the given action was executed; else, false.</returns>
        public static bool TryErrorEvent(FileSystemWatcher watcher, Action action, Action cleanup, int attempts, bool expected)
        {
            int attemptsCompleted = 0;
            bool result = !expected;
            while (result != expected && attemptsCompleted++ < attempts)
            {
                if (attemptsCompleted > 1)
                {
                    // Re-create the watcher to get a clean iteration.
                    watcher = RecreateWatcher(watcher);
                    // Most intermittent failures in FSW are caused by either a shortage of resources (e.g. inotify instances)
                    // or by insufficient time to execute (e.g. CI gets bogged down). Immediately re-running a failed test
                    // won't resolve the first issue, so we wait a little while hoping that things clear up for the next run.
                    Thread.Sleep(500);
                }

                AutoResetEvent errorOccurred = new AutoResetEvent(false);
                watcher.Error += (o, e) =>
                {
                    errorOccurred.Set();
                };

                // Enable raising events but be careful with the possibility of the max user inotify instances being reached already.
                if (attemptsCompleted <= attempts)
                {
                    try
                    {
                        watcher.EnableRaisingEvents = true;
                    }
                    catch (IOException) // Max User INotify instances. Isn't the type of error we're checking for.
                    {
                        continue;
                    }
                }
                else
                {
                    watcher.EnableRaisingEvents = true;
                }

                action();
                result = errorOccurred.WaitOne(WaitForExpectedEventTimeout);
                watcher.EnableRaisingEvents = false;
                cleanup();
            }
            return result;
        }

        public static IEnumerable<object[]> FilterTypes()
        {
            foreach (NotifyFilters filter in Enum.GetValues(typeof(NotifyFilters)))
                yield return new object[] { filter };
        }

        // Linux and OSX systems have less precise filtering systems than Windows, so most
        // metadata filters are effectively equivalent to each other on those systems. For example
        // there isn't a way to filter only LastWrite events on either system; setting
        // Filters to LastWrite will allow events from attribute change, creation time
        // change, size change, etc.
        public const NotifyFilters LinuxFiltersForAttribute = NotifyFilters.Attributes |
                                                                NotifyFilters.CreationTime |
                                                                NotifyFilters.LastAccess |
                                                                NotifyFilters.LastWrite |
                                                                NotifyFilters.Security |
                                                                NotifyFilters.Size;
        public const NotifyFilters LinuxFiltersForModify = NotifyFilters.LastAccess |
                                                            NotifyFilters.LastWrite |
                                                            NotifyFilters.Security |
                                                            NotifyFilters.Size;
        public const NotifyFilters OSXFiltersForModify = NotifyFilters.Attributes |
                                                        NotifyFilters.CreationTime |
                                                        NotifyFilters.LastAccess |
                                                        NotifyFilters.LastWrite |
                                                        NotifyFilters.Size;

        private static FileSystemWatcher RecreateWatcher(FileSystemWatcher watcher)
        {
            FileSystemWatcher newWatcher = new FileSystemWatcher()
            {
                IncludeSubdirectories = watcher.IncludeSubdirectories,
                NotifyFilter = watcher.NotifyFilter,
                Path = watcher.Path,
                InternalBufferSize = watcher.InternalBufferSize,
                SynchronizingObject = watcher.SynchronizingObject,
            };

            foreach (string filter in watcher.Filters)
            {
                newWatcher.Filters.Add(filter);
            }

            return newWatcher;
        }

        internal readonly struct FiredEvent
        {
            public FiredEvent(WatcherChangeTypes eventType, string dir1, string dir2 = "") => (EventType, Dir1, Dir2) = (eventType, dir1, dir2);

            public readonly WatcherChangeTypes EventType;
            public readonly string Dir1;
            public readonly string Dir2;

            public override bool Equals(object obj) => obj is FiredEvent evt && Equals(evt);

            public bool Equals(FiredEvent other) => EventType == other.EventType &&
                Dir1 == other.Dir1 &&
                Dir2 == other.Dir2;


            public override int GetHashCode() => EventType.GetHashCode() ^ Dir1.GetHashCode() ^ Dir2.GetHashCode();

            public override string ToString() => $"{EventType} {Dir1} {Dir2}";

        }

        // Observe until an expected count of events is triggered, otherwise fail. Return all collected events.
        internal static List<FiredEvent> ExpectEvents(FileSystemWatcher watcher, int expectedEvents, Action action)
        {
            using var eventsOccured = new AutoResetEvent(false);
            var eventsOrrures = 0;

            var events = new List<FiredEvent>();

            ErrorEventArgs error = null;

            FileSystemEventHandler fileWatcherEvent = (_, e) => AddEvent(e.ChangeType, e.FullPath);
            RenamedEventHandler renameWatcherEvent = (_, e) => AddEvent(e.ChangeType, e.FullPath, e.OldFullPath);
            ErrorEventHandler errorHandler = (_, e) => error ??= e ?? new ErrorEventArgs(null);

            watcher.Changed += fileWatcherEvent;
            watcher.Created += fileWatcherEvent;
            watcher.Deleted += fileWatcherEvent;
            watcher.Renamed += renameWatcherEvent;
            watcher.Error += errorHandler;

            bool raisingEvent = watcher.EnableRaisingEvents;
            watcher.EnableRaisingEvents = true;

            try
            {
                action();
                eventsOccured.WaitOne(new TimeSpan(0, 0, 5));
            }
            finally
            {
                watcher.Changed -= fileWatcherEvent;
                watcher.Created -= fileWatcherEvent;
                watcher.Deleted -= fileWatcherEvent;
                watcher.Renamed -= renameWatcherEvent;
                watcher.Error -= errorHandler;
                watcher.EnableRaisingEvents = raisingEvent;
            }

            if (error != null)
            {
                Assert.False(true, $"Filewatcher error event triggered: { error.GetException()?.Message ?? "Unknow error" }");
            }

            return events;

            void AddEvent(WatcherChangeTypes eventType, string dir1, string dir2 = "")
            {
                events.Add(new FiredEvent(eventType, dir1, dir2));
                if (Interlocked.Increment(ref eventsOrrures) == expectedEvents)
                {
                    eventsOccured.Set();
                }
            }
        }

        internal class TestISynchronizeInvoke : ISynchronizeInvoke
        {
            public bool BeginInvoke_Called;
            public Delegate ExpectedDelegate;

            public IAsyncResult BeginInvoke(Delegate method, object[] args)
            {
                if (ExpectedDelegate != null)
                    Assert.Equal(ExpectedDelegate, method);

                BeginInvoke_Called = true;
                method.DynamicInvoke(args[0], args[1]);
                return null;
            }

            public bool InvokeRequired => true;
            public object EndInvoke(IAsyncResult result) => null;
            public object Invoke(Delegate method, object[] args) => null;
        }
    }
}
