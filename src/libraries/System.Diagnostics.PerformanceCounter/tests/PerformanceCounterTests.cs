// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Xunit;

namespace System.Diagnostics.Tests
{
    public static class PerformanceCounterTests
    {
        [ConditionalFact(typeof(AdminHelpers), nameof(AdminHelpers.IsProcessElevated))]
        public static void PerformanceCounterCategory_CreateCategory()
        {
            if ( !PerformanceCounterCategory.Exists("AverageCounter64SampleCategory") ) 
            {
                CounterCreationDataCollection counterDataCollection = new CounterCreationDataCollection();

                // Add the counter.
                CounterCreationData averageCount64 = new CounterCreationData();
                averageCount64.CounterType = PerformanceCounterType.AverageCount64;
                averageCount64.CounterName = "AverageCounter64Sample";
                counterDataCollection.Add(averageCount64);

                // Add the base counter.
                CounterCreationData averageCount64Base = new CounterCreationData();
                averageCount64Base.CounterType = PerformanceCounterType.AverageBase;
                averageCount64Base.CounterName = "AverageCounter64SampleBase";
                counterDataCollection.Add(averageCount64Base);

                // Create the category.
                PerformanceCounterCategory.Create("AverageCounter64SampleCategory",
                    "Demonstrates usage of the AverageCounter64 performance counter type.",
                    PerformanceCounterCategoryType.SingleInstance, counterDataCollection);
            }

            Assert.True(PerformanceCounterCategory.Exists("AverageCounter64SampleCategory"));
        }

        [ConditionalFact(typeof(AdminHelpers), nameof(AdminHelpers.IsProcessElevated))]
        public static void PerformanceCounter_CreateCounter_Count0()
        {
            PerformanceCounter averageCounter64Sample = new PerformanceCounter("AverageCounter64SampleCategory", 
                "AverageCounter64Sample", false);

            averageCounter64Sample.RawValue = 0;

            Assert.Equal(0, averageCounter64Sample.RawValue);
        }
    }
}
