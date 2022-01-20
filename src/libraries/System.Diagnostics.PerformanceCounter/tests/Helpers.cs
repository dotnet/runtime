// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Threading;
using Xunit;

// Implementation is not robust with respect to modifying counter categories
// while concurrently reading counters
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace System.Diagnostics.Tests
{
    internal class Helpers
    {
        public static bool IsElevatedAndCanWriteToPerfCounters { get => AdminHelpers.IsProcessElevated() && CanWriteToPerfCounters; }
        public static bool IsElevatedAndCanWriteAndReadNetPerfCounters { get => AdminHelpers.IsProcessElevated() && CanWriteToPerfCounters && CanReadNetPerfCounters; }
        public static bool CanWriteToPerfCounters { get => PlatformDetection.IsNotWindowsNanoServer; }
        public static bool CanReadNetPerfCounters { get => File.Exists(Environment.SystemDirectory + Path.DirectorySeparatorChar + "netfxperf.dll"); }

        public static void CreateCategory(string categoryName, PerformanceCounterCategoryType categoryType)
        {
            string counterName = categoryName.Replace("_Category", "_Counter");
            CreateCategory(categoryName, counterName, categoryType);
        }

        public static void CreateCategory(string categoryName, string counterName, PerformanceCounterCategoryType categoryType)
        {
            Assert.EndsWith("_Category", categoryName);
            Assert.EndsWith("_Counter", counterName);

            // If the category already exists, delete it, then create it.
            DeleteCategory(categoryName);
            PerformanceCounterCategory.Create(categoryName, "description", categoryType, counterName, "counter description");

            VerifyPerformanceCounterCategoryCreated(categoryName);
        }

        public static void VerifyPerformanceCounterCategoryCreated(string categoryName)
        {
            Assert.EndsWith("_Category", categoryName);
            int tries = 0;
            while (!PerformanceCounterCategory.Exists(categoryName) && tries < 10)
            {
                System.Threading.Thread.Sleep(100);
                tries++;
            }

            Assert.True(PerformanceCounterCategory.Exists(categoryName));
        }

        public static void DeleteCategory(string categoryName)
        {
            Assert.EndsWith("_Category", categoryName);
            if (PerformanceCounterCategory.Exists(categoryName))
            {
                PerformanceCounterCategory.Delete(categoryName);
            }

            int tries = 0;
            while (PerformanceCounterCategory.Exists(categoryName) && tries < 10)
            {
                System.Threading.Thread.Sleep(100);
                tries++;
            }

            Assert.True(!PerformanceCounterCategory.Exists(categoryName));
        }

        public static T RetryOnAllPlatforms<T>(Func<T> func)
        {
            // Harden the tests increasing the retry count and the timeout.
            T result = default;
            RetryHelper.Execute(() =>
            {
                result = func();
            }, maxAttempts: 10, (iteration) => iteration * 300);

            return result;
        }
    }
}
