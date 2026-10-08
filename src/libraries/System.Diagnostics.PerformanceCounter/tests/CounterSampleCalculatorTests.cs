// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using Xunit.Sdk;

namespace System.Diagnostics.Tests
{
    public static class CounterSampleCalculatorTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteAndReadNetPerfCounters))]
        public static void CounterSampleCalculator_ElapsedTime()
        {
            string categoryName = nameof(CounterSampleCalculator_ElapsedTime) + "_Category";

            PerformanceCounter counterSample = CreateCounter(categoryName, PerformanceCounterType.ElapsedTime);

            try
            {
                // Timing comparisons can be flaky under CI load, so retry.
                RetryHelper.Execute(() =>
                {
                    long startTimestamp = Stopwatch.GetTimestamp();
                    counterSample.RawValue = startTimestamp;
                    Helpers.RetryOnAllPlatforms(() => counterSample.NextValue());

                    System.Threading.Thread.Sleep(500);

                    var counterVal = Helpers.RetryOnAllPlatforms(() => counterSample.NextValue());
                    var elapsed = (double)(Stopwatch.GetTimestamp() - startTimestamp) / Stopwatch.Frequency;
                    Assert.True(Math.Abs(elapsed - counterVal) < .3, $"Expected elapsed ({elapsed:F3}s) and counterVal ({counterVal:F3}s) to be within 0.3s");
                }, maxAttempts: 3, retryWhen: e => e is XunitException);
            }
            finally
            {
                counterSample.Dispose();
                Helpers.DeleteCategory(categoryName);
            }
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
