// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Xunit;

namespace System.Diagnostics.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // In appcontainer, cannot write to perf counters
    public static class CounterSampleCalculatorTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterSampleCalculator_ElapsedTime()
        {
            var name = nameof(CounterSampleCalculator_ElapsedTime) + "_Counter";

            PerformanceCounter counterSample = CreateCounter(name, PerformanceCounterType.ElapsedTime);

            counterSample.RawValue = Stopwatch.GetTimestamp();
            DateTime Start = DateTime.Now;
            Helpers.RetryOnAllPlatforms(() => counterSample.NextValue());

            System.Threading.Thread.Sleep(500);

            var counterVal = Helpers.RetryOnAllPlatforms(() => counterSample.NextValue());
            var dateTimeVal = DateTime.Now.Subtract(Start).TotalSeconds;
            Helpers.DeleteCategory(name);
            Assert.True(Math.Abs(dateTimeVal - counterVal) < .3);
        }

        public static PerformanceCounter CreateCounter(string name, PerformanceCounterType counterType)
        {
            var category = name + "_Category";
            var instance = name + "_Instance";

            CounterCreationDataCollection ccdc = new CounterCreationDataCollection();
            CounterCreationData ccd = new CounterCreationData();
            ccd.CounterType = counterType;
            ccd.CounterName = name;
            ccdc.Add(ccd);

            Helpers.DeleteCategory(name);
            PerformanceCounterCategory.Create(category, "description", PerformanceCounterCategoryType.SingleInstance, ccdc);

            Assert.True(Helpers.PerformanceCounterCategoryCreated(category));

            return new PerformanceCounter(category, name, false);
        }
    }
}
