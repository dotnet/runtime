// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace DPStressHarness
{
    public class RecordedExceptions
    {
        // Reference wrapper around an integer which is used in order to make updating a little easier & more efficient
        public class ExceptionCount
        {
            public int Count = 0;
        }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, ExceptionCount>> _exceptions = new ConcurrentDictionary<string, ConcurrentDictionary<string, ExceptionCount>>();

        /// <summary>
        /// Records an exception and returns true if the threshold is exceeded for that exception
        /// </summary>
        public bool Record(string testName, Exception ex)
        {
            // Converting from exception to string can be expensive so only do it once and cache the string
            string exceptionString = ex.ToString();
            TraceException(testName, exceptionString);

            // Get the exceptions for the current test case
            ConcurrentDictionary<string, ExceptionCount> exceptionsForTest = _exceptions.GetOrAdd(testName, _ => new ConcurrentDictionary<string, ExceptionCount>());

            // Get the count for the current exception
            ExceptionCount exCount = exceptionsForTest.GetOrAdd(exceptionString, _ => new ExceptionCount());

            // Increment the count
            Interlocked.Increment(ref exCount.Count);

            // If the count is over the threshold, return true
            return TestMetrics.ExceptionThreshold.HasValue && (exCount.Count > TestMetrics.ExceptionThreshold);
        }

        private void TraceException(string testName, string exceptionString)
        {
            StringBuilder status = new StringBuilder();
            status.AppendLine("========================================================================");
            status.AppendLine("Exception Report");
            status.AppendLine("========================================================================");

            status.AppendLine(string.Format("Test: {0}", testName));
            status.AppendLine(exceptionString);

            status.AppendLine("========================================================================");
            status.AppendLine("End of Exception Report");
            status.AppendLine("========================================================================");
            Trace.WriteLine(status.ToString());
        }

        public void TraceAllExceptions()
        {
            StringBuilder status = new StringBuilder();
            status.AppendLine("========================================================================");
            status.AppendLine("All Exceptions Report");
            status.AppendLine("========================================================================");

            foreach (string testName in _exceptions.Keys)
            {
                ConcurrentDictionary<string, ExceptionCount> exceptionsForTest = _exceptions[testName];

                status.AppendLine(string.Format("Test: {0}", testName));
                foreach (var exceptionString in exceptionsForTest.Keys)
                {
                    status.AppendLine(string.Format("Count: {0}", exceptionsForTest[exceptionString].Count));
                    status.AppendLine(string.Format("Exception: {0}", exceptionString));
                    status.AppendLine();
                }

                status.AppendLine();
                status.AppendLine();
            }

            status.AppendLine("========================================================================");
            status.AppendLine("End of All Exceptions Report");
            status.AppendLine("========================================================================");
            Trace.WriteLine(status.ToString());
        }

        public int GetExceptionsCount()
        {
            int count = 0;

            foreach (string testName in _exceptions.Keys)
            {
                ConcurrentDictionary<string, ExceptionCount> exceptionsForTest = _exceptions[testName];

                foreach (var exceptionString in exceptionsForTest.Keys)
                {
                    count += exceptionsForTest[exceptionString].Count;
                }
            }

            return count;
        }
    }
}
