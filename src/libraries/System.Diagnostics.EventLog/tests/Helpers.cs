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

        // Retry that eats exceptions: for "best effort cleanup"
        public static void RetrySilently(Action func)
        {
            try
            {
                Retry(func);
            }
            catch
            {}
        }

        public static void Retry(Action func)
        {
            Retry<object>(() => { func(); return null; });
        }

        public static T Retry<T>(Func<T> func)
        {
            // Harden the tests increasing the retry count and the timeout.
            T result = default;
            RetryHelper.Execute(() =>
            {
                result = func();
            }, maxAttempts: 20, (iteration) => iteration * 300);

            return result;
        }

        public static void WaitForEventLog(EventLog eventLog, int entriesExpected)
        {
            int tries = 1;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (Retry((() => eventLog.Entries.Count)) < entriesExpected && tries <= 50)
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

            Assert.Equal(entriesExpected, Retry((() => eventLog.Entries.Count)));
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
