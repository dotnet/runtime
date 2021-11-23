// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    public static class PerformanceCounterTests
    {
        [Fact]
        public static void PerformanceCounter_CreateCounter_EmptyCounter()
        {
            using (PerformanceCounter counterSample = new PerformanceCounter())
            {
                Assert.Equal(".", counterSample.MachineName);
                Assert.Equal(string.Empty, counterSample.CategoryName);
                Assert.Equal(string.Empty, counterSample.CounterName);
                Assert.Equal(string.Empty, counterSample.InstanceName);
                Assert.True(counterSample.ReadOnly);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounter_CreateCounter_Count0()
        {
            string categoryName = nameof(PerformanceCounter_CreateCounter_Count0) + "_Category";
            using (PerformanceCounter counterSample = CreateCounterWithCategory(categoryName, readOnly:false, PerformanceCounterCategoryType.SingleInstance))
            {
                counterSample.RawValue = 0;

                Assert.Equal(0, counterSample.RawValue);
            }

            Helpers.DeleteCategory(categoryName);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60933", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.Is64BitProcess))]
        public static void PerformanceCounter_CreateCounter_ProcessorCounter()
        {
            using (PerformanceCounter counterSample = new PerformanceCounter("Processor", "Interrupts/sec", "0", "."))
            {
                Assert.Equal(0, Helpers.RetryOnAllPlatforms(() => counterSample.NextValue()));

                Assert.True(counterSample.RawValue > 0);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void PerformanceCounter_CreateCounter_MultiInstanceReadOnly()
        {
            string categoryName = nameof(PerformanceCounter_CreateCounter_MultiInstanceReadOnly) + "_Category";
            string counterName = nameof(PerformanceCounter_CreateCounter_MultiInstanceReadOnly) + "_Counter";
            string instanceName = nameof(PerformanceCounter_CreateCounter_MultiInstanceReadOnly) + "_Instance";

            Helpers.CreateCategory(categoryName, counterName, PerformanceCounterCategoryType.MultiInstance);

            using (PerformanceCounter counterSample = Helpers.RetryOnAllPlatforms(() => new PerformanceCounter(categoryName, counterName, instanceName)))
            {
                Assert.Equal(counterName, counterSample.CounterName);
                Assert.Equal(categoryName, counterSample.CategoryName);
                Assert.Equal(instanceName, counterSample.InstanceName);
                Assert.Equal("counter description",  Helpers.RetryOnAllPlatforms(() => counterSample.CounterHelp));
                Assert.True(counterSample.ReadOnly);
            }

            Helpers.DeleteCategory(categoryName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void PerformanceCounter_CreateCounter_SetReadOnly()
        {
            string categoryName = nameof(PerformanceCounter_CreateCounter_SetReadOnly) + "_Category";
            string counterName = nameof(PerformanceCounter_CreateCounter_SetReadOnly) + "_Counter";

            Helpers.CreateCategory(categoryName, PerformanceCounterCategoryType.SingleInstance);

            using (PerformanceCounter counterSample = Helpers.RetryOnAllPlatforms(() => new PerformanceCounter(categoryName, counterName)))
            {
                counterSample.ReadOnly = false;

                Assert.False(counterSample.ReadOnly);
            }

            Helpers.DeleteCategory(categoryName);
        }

        [Fact]
        public static void PerformanceCounter_SetProperties_Null()
        {
            using (PerformanceCounter counterSample = new PerformanceCounter())
            {
                Assert.Throws<ArgumentNullException>(() => counterSample.CategoryName = null);
                Assert.Throws<ArgumentNullException>(() => counterSample.CounterName = null);
                Assert.Throws<ArgumentException>(() => counterSample.MachineName = null);
            }
        }

        [Fact]
        public static void PerformanceCounter_SetRawValue_ReadOnly()
        {
            using (PerformanceCounter counterSample = new PerformanceCounter())
            {
                Assert.Throws<InvalidOperationException>(() => counterSample.RawValue = 10);
            }
        }

        [Fact]
        public static void PerformanceCounter_GetRawValue_EmptyCategoryName()
        {
            string counterName = nameof(PerformanceCounter_GetRawValue_EmptyCategoryName) + "_Counter";
            using (PerformanceCounter counterSample = new PerformanceCounter())
            {
                counterSample.ReadOnly = false;
                counterSample.CounterName = counterName;

                Assert.Throws<InvalidOperationException>(() => counterSample.RawValue);
            }
        }

        [Fact]
        public static void PerformanceCounter_GetRawValue_EmptyCounterName()
        {
            string categoryName = nameof(PerformanceCounter_GetRawValue_EmptyCounterName) + "_Category";
            using (PerformanceCounter counterSample = new PerformanceCounter())
            {
                counterSample.ReadOnly = false;
                counterSample.CategoryName = categoryName;

                Assert.Throws<InvalidOperationException>(() => counterSample.RawValue);
            }
        }

        [Fact]
        public static void PerformanceCounter_GetRawValue_CounterDoesNotExist()
        {
            string categoryName = nameof(PerformanceCounter_GetRawValue_CounterDoesNotExist) + "_Category";
            string counterName = nameof(PerformanceCounter_GetRawValue_CounterDoesNotExist) + "_Counter";

            using (PerformanceCounter counterSample = new PerformanceCounter())
            {
                counterSample.ReadOnly = false;
                counterSample.CounterName = counterName;
                counterSample.CategoryName = categoryName;

                Assert.Throws<InvalidOperationException>(() => counterSample.RawValue);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60403", typeof(PlatformDetection), nameof(PlatformDetection.IsArm64Process), nameof(PlatformDetection.IsWindows))]
        public static void PerformanceCounter_NextValue_ProcessorCounter()
        {
            using (PerformanceCounter counterSample = new PerformanceCounter("Processor", "Interrupts/sec", "_Total", "."))
            {
                float val;
                int counter = 0;
                do
                {
                    // Ensure we don't always return zero for a counter we know is not always zero
                    val = Helpers.RetryOnAllPlatforms(() => counterSample.NextValue());
                    if (val > 0f)
                    {
                        break;
                    }
                    counter++;
                    Thread.Sleep(100);
                }
                while (counter < 20);

                Assert.True(val > 0f);
            }
        }

        [Fact]
        public static void PerformanceCounter_BeginInit_ProcessorCounter()
        {
            using (PerformanceCounter counterSample = new PerformanceCounter("Processor", "Interrupts/sec", "0", "."))
            {
                counterSample.BeginInit();

                Assert.NotNull(counterSample);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60933", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.Is64BitProcess))]
        public static void PerformanceCounter_BeginInitEndInit_ProcessorCounter()
        {
            using (PerformanceCounter counterSample = new PerformanceCounter("Processor", "Interrupts/sec", "0", "."))
            {
                counterSample.BeginInit();
                counterSample.EndInit();

                Assert.NotNull(counterSample);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void PerformanceCounter_Decrement()
        {
            string categoryName = nameof(PerformanceCounter_Decrement) + "_Category";
            using (PerformanceCounter counterSample = CreateCounterWithCategory(categoryName, readOnly:false, PerformanceCounterCategoryType.SingleInstance))
            {
                counterSample.RawValue = 10;
                Helpers.RetryOnAllPlatforms(() => counterSample.Decrement());

                Assert.Equal(9, counterSample.RawValue);
            }

            Helpers.DeleteCategory(categoryName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void PerformanceCounter_Increment()
        {
            string categoryName = nameof(PerformanceCounter_Increment) + "_Category";
            using (PerformanceCounter counterSample = CreateCounterWithCategory(categoryName, readOnly:false, PerformanceCounterCategoryType.SingleInstance))
            {
                counterSample.RawValue = 10;
                Helpers.RetryOnAllPlatforms(() => counterSample.Increment());

                Assert.Equal(11, Helpers.RetryOnAllPlatforms(() => counterSample.NextSample().RawValue));
            }

            Helpers.DeleteCategory(categoryName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void PerformanceCounter_IncrementBy_IncrementBy2()
        {
            string categoryName = nameof(PerformanceCounter_IncrementBy_IncrementBy2) + "_Category";
            using (PerformanceCounter counterSample = CreateCounterWithCategory(categoryName, readOnly:false, PerformanceCounterCategoryType.SingleInstance))
            {
                counterSample.RawValue = 10;
                Helpers.RetryOnAllPlatforms(() => counterSample.IncrementBy(2));

                Assert.Equal(12, Helpers.RetryOnAllPlatforms(() => counterSample.NextSample().RawValue));
            }

            Helpers.DeleteCategory(categoryName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void PerformanceCounter_IncrementBy_IncrementByReadOnly()
        {
            string categoryName = nameof(PerformanceCounter_IncrementBy_IncrementByReadOnly) + "_Category";
            using (PerformanceCounter counterSample = CreateCounterWithCategory(categoryName, readOnly:true, PerformanceCounterCategoryType.SingleInstance))
            {
                Assert.Throws<InvalidOperationException>(() => counterSample.IncrementBy(2));
            }

            Helpers.DeleteCategory(categoryName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void PerformanceCounter_Increment_IncrementReadOnly()
        {
            string categoryName = nameof(PerformanceCounter_Increment_IncrementReadOnly) + "_Category";
            using (PerformanceCounter counterSample = CreateCounterWithCategory(categoryName, readOnly:true, PerformanceCounterCategoryType.SingleInstance))
            {
                Assert.Throws<InvalidOperationException>(() => counterSample.Increment());
            }

            Helpers.DeleteCategory(categoryName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60933", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.Is64BitProcess))]
        public static void PerformanceCounter_Decrement_DecrementReadOnly()
        {
            string categoryName = nameof(PerformanceCounter_Decrement_DecrementReadOnly) + "_Category";
            using (PerformanceCounter counterSample = CreateCounterWithCategory(categoryName, readOnly:true, PerformanceCounterCategoryType.SingleInstance))
            {
                Assert.Throws<InvalidOperationException>(() => counterSample.Decrement());
            }

            Helpers.DeleteCategory(categoryName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounter_RemoveInstance()
        {
            string categoryName = nameof(PerformanceCounter_RemoveInstance) + "_Category";
            using (PerformanceCounter counterSample = CreateCounterWithCategory(categoryName, readOnly:false, PerformanceCounterCategoryType.SingleInstance))
            {
                counterSample.RawValue = 100;
                counterSample.RemoveInstance();
                counterSample.Close();

                Assert.NotNull(counterSample);
            }

            Helpers.DeleteCategory(categoryName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void PerformanceCounter_NextSample_MultiInstance()
        {
            string categoryName = nameof(PerformanceCounter_NextSample_MultiInstance) + "_Category";
            string counterName = nameof(PerformanceCounter_NextSample_MultiInstance) + "_Counter";
            string instanceName = nameof(PerformanceCounter_NextSample_MultiInstance) + "_Instance";

            Helpers.CreateCategory(categoryName, PerformanceCounterCategoryType.MultiInstance);

            using (PerformanceCounter counterSample = new PerformanceCounter(categoryName, counterName, instanceName, readOnly:false))
            {
                counterSample.RawValue = 10;
                Helpers.RetryOnAllPlatforms(() => counterSample.Decrement());

                Assert.Equal(9, counterSample.RawValue);
            }

            Helpers.DeleteCategory(categoryName);
        }

        public static PerformanceCounter CreateCounterWithCategory(string categoryName, bool readOnly, PerformanceCounterCategoryType categoryType)
        {
            Helpers.CreateCategory(categoryName, categoryType);

            string counterName = categoryName.Replace("_Category", "_Counter");

            PerformanceCounter counterSample = Helpers.RetryOnAllPlatforms(() => new PerformanceCounter(categoryName, counterName, readOnly));

            return counterSample;
        }
    }
}
