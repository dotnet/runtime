// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
        public static bool CanWriteToPerfCounters { get => PlatformDetection.IsNotWindowsNanoServer && PlatformDetection.IsNotArmNorArm64Process; }

        public static string CreateCategory(string name, PerformanceCounterCategoryType categoryType)
        {
            var category = name + "_Category";

            // If the categry already exists, delete it, then create it.
            DeleteCategory(name);
            PerformanceCounterCategory.Create(category, "description", categoryType, name, "counter description");

            Assert.True(PerformanceCounterCategoryCreated(category));
            return category;
        }

        public static bool PerformanceCounterCategoryCreated(string category)
        {
            int tries = 0;
            while (!PerformanceCounterCategory.Exists(category) && tries < 10)
            {
                System.Threading.Thread.Sleep(100);
                tries++;
            }

            return PerformanceCounterCategory.Exists(category);
        }

        public static void DeleteCategory(string name)
        {
            var category = name + "_Category";
            if (PerformanceCounterCategory.Exists(category))
            {
                PerformanceCounterCategory.Delete(category);
            }
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

