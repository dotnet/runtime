// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Threading;
using System.ComponentModel;
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

        /// <summary>
        /// Determines if an exception is retriable for performance counter operations.
        /// Typically used for file access conflicts and transient system errors.
        /// </summary>
        internal static bool IsRetriableException(Exception ex)
        {
            // Handle Win32Exception with specific error codes for file access conflicts
            if (ex is Win32Exception win32Ex)
            {
                // ERROR_SHARING_VIOLATION (32) - The process cannot access the file because it is being used by another process
                // ERROR_ACCESS_DENIED (5) - Access is denied
                // ERROR_LOCK_VIOLATION (33) - The process cannot access the file because another process has locked a portion of the file
                return win32Ex.NativeErrorCode == 32 || win32Ex.NativeErrorCode == 5 || win32Ex.NativeErrorCode == 33;
            }

            // Handle IOException for file access issues
            if (ex is IOException)
            {
                return true;
            }

            // Handle UnauthorizedAccessException
            if (ex is UnauthorizedAccessException)
            {
                return true;
            }

            return false;
        }

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
            
            // Retry the Create operation to handle file access conflicts
            RetryHelper.Execute(() =>
            {
                PerformanceCounterCategory.Create(categoryName, "description", categoryType, counterName, "counter description");
            }, maxAttempts: 10, retryWhen: IsRetriableException);

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
                // Retry the Delete operation to handle file access conflicts
                RetryHelper.Execute(() =>
                {
                    PerformanceCounterCategory.Delete(categoryName);
                }, maxAttempts: 10, retryWhen: IsRetriableException);
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

        public static T RetryOnAllPlatformsWithClosingResources<T>(Func<T> func)
        {
            // Harden the tests increasing the retry count and the timeout.
            T result = default;
            RetryHelper.Execute(() =>
            {
                try
                {
                    result = func();
                }
                catch
                {
                    PerformanceCounter.CloseSharedResources();
                    throw;
                }
            }, maxAttempts: 10, (iteration) => iteration * 300);

            return result;
        }
    }
}
