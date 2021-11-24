// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Xunit;

namespace System.Diagnostics.Tests
{
    public static class CounterSampleCalculatorTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void CounterSampleCalculator_ElapsedTime()
        {
            string categoryName = nameof(CounterSampleCalculator_ElapsedTime) + "_Category";

            PerformanceCounter counterSample = CreateCounter(categoryName, PerformanceCounterType.ElapsedTime);

            counterSample.RawValue = Stopwatch.GetTimestamp();
            DateTime Start = DateTime.Now;
            Helpers.RetryOnAllPlatforms(() => counterSample.NextValue());

            System.Threading.Thread.Sleep(500);

            var counterVal = Helpers.RetryOnAllPlatforms(() => counterSample.NextValue());
            var dateTimeVal = DateTime.Now.Subtract(Start).TotalSeconds;
            Helpers.DeleteCategory(categoryName);
            Assert.True(Math.Abs(dateTimeVal - counterVal) < .3);
        }

        public static PerformanceCounter CreateCounter(string categoryName, PerformanceCounterType counterType)
        {
            string counterName = categoryName + "_Counter";

            CounterCreationDataCollection ccdc = new CounterCreationDataCollection();
            CounterCreationData ccd = new CounterCreationData();
            ccd.CounterType = counterType;
            ccd.CounterName = counterName;
            ccdc.Add(ccd);

            Helpers.DeleteCategory(categoryName);
            PerformanceCounterCategory.Create(categoryName, "description", PerformanceCounterCategoryType.SingleInstance, ccdc);

            Helpers.VerifyPerformanceCounterCategoryCreated(categoryName);

            return new PerformanceCounter(categoryName, counterName, readOnly:false);
        }
    }
}
