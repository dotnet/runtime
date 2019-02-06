// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using Xunit;

// Implementation is not robust with respect to concurrently writing and reading log
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace System.Diagnostics.Tests
{
    internal class Helpers
    {
        public static bool NotElevatedAndSupportsEventLogs { get => !AdminHelpers.IsProcessElevated() && SupportsEventLogs; }
        public static bool IsElevatedAndSupportsEventLogs { get => AdminHelpers.IsProcessElevated() && SupportsEventLogs; }
        public static bool SupportsEventLogs { get => PlatformDetection.IsNotWindowsNanoServer && PlatformDetection.IsNotWindowsIoTCore; }

        public static void RetryOnWin7(Action func)
        {
            RetryOnWin7<object>(() => { func(); return null; });
        }

        public static T RetryOnWin7<T>(Func<T> func)
        {
            if (!PlatformDetection.IsWindows7)
            {
                return func();
            }

            return RetryOnAllPlatforms(func);
            // We are retrying on windows 7 because it throws win32exception while some operations like Writing,retrieving and Deleting log.
            // So We just try to do the operation again in case of this exception 
        }

        public static T RetryOnAllPlatforms<T>(Func<T> func)
        {
            T entry = default(T);
            int retries = 20;
            while (retries > 0)
            {
                try
                {
                    entry = func();
                    retries = -1;
                }
                catch (Win32Exception)
                {
                    Thread.Sleep(100);
                    retries--;
                }
                catch (ArgumentException)
                {
                    Thread.Sleep(100);
                    retries--;
                }
            }

            Assert.NotEqual(0, retries);
            return entry;
        }

        public static void WaitForEventLog(EventLog eventLog, int entriesExpected)
        {
            int tries = 1;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (RetryOnAllPlatforms((() => eventLog.Entries.Count)) < entriesExpected && tries <= 50)
            {
                if (tries == 50)
                {
                    Thread.Sleep(30000);
                }
                else
                {
                    Thread.Sleep(100 * (tries));
                }
                tries++;
            }

            if (stopwatch.ElapsedMilliseconds / 1000 >= 5)
                Console.WriteLine($"{stopwatch.ElapsedMilliseconds / 1000 } seconds");

            Assert.Equal(entriesExpected, RetryOnWin7((() => eventLog.Entries.Count)));
        }

        internal static EventBookmark GetBookmark(string log, PathType pathType)
        {
            var elq = new EventLogQuery(log, pathType) { ReverseDirection = true };
            var reader = new EventLogReader(elq);
            EventRecord record = reader.ReadEvent();
            return record?.Bookmark;
        }
    }
}
