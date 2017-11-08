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
    public static class CounterCreationDataCollectionTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationDataCollection_CreateCounterCreationDataCollection_Empty()
        {
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection();
            Assert.Equal(0, ccdc.Count);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationDataCollection_CreateCounterCreationDataCollection_CCDC()
        {
            CounterCreationData[] ccds = { new CounterCreationData("Simple1", "Simple Help", PerformanceCounterType.RawBase), new CounterCreationData("Simple2", "Simple Help", PerformanceCounterType.RawBase) };
            CounterCreationDataCollection ccdc1 = new CounterCreationDataCollection(ccds);
            CounterCreationDataCollection ccdc2 = new CounterCreationDataCollection(ccdc1);

            Assert.Equal(2, ccdc2.Count);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationDataCollection_CreateCounterCreationDataCollection_Array()
        {
            CounterCreationData[] ccds = { new CounterCreationData("Simple1", "Simple Help", PerformanceCounterType.RawBase), new CounterCreationData("Simple2", "Simple Help", PerformanceCounterType.RawBase) };
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection(ccds);
            Assert.Equal(2, ccdc.Count);
            Assert.True(ccdc.Contains(ccds[0]));
            Assert.Equal(0, ccdc.IndexOf(ccds[0]));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationDataCollection_CreateCounterCreationDataCollection_Invalid()
        {
            CounterCreationData[] ccds = null;
            CounterCreationDataCollection ccdc = null;
            Assert.Throws<ArgumentNullException>(() => new CounterCreationDataCollection(ccds));
            Assert.Throws<ArgumentNullException>(() => new CounterCreationDataCollection(ccdc));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationDataCollection_SetIndex2()
        {
            CounterCreationData[] ccds = { new CounterCreationData("Simple1", "Simple Help", PerformanceCounterType.RawBase), new CounterCreationData("Simple2", "Simple Help", PerformanceCounterType.RawBase) };
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection(ccds);

            CounterCreationData ccd = new CounterCreationData("Simple3", "Simple Help", PerformanceCounterType.RawBase);

            ccdc[1] = ccd;

            Assert.Equal(ccd, ccdc[1]);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationDataCollection_Remove()
        {
            CounterCreationData[] ccds = { new CounterCreationData("Simple1", "Simple Help", PerformanceCounterType.RawBase), new CounterCreationData("Simple2", "Simple Help", PerformanceCounterType.RawBase) };
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection(ccds);

            ccdc.Remove(ccds[0]);
            Assert.False(ccdc.Contains(ccds[0]));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationDataCollection_Insert()
        {
            CounterCreationData[] ccds = { new CounterCreationData("Simple1", "Simple Help", PerformanceCounterType.RawBase), new CounterCreationData("Simple2", "Simple Help", PerformanceCounterType.RawBase) };
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection(ccds);

            CounterCreationData ccd = new CounterCreationData("Simple3", "Simple Help", PerformanceCounterType.RawBase);
            ccdc.Insert(1, ccd);

            Assert.True(ccdc.Contains(ccd));
            Assert.Equal(1, ccdc.IndexOf(ccd));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationDataCollection_CopyTo()
        {
            CounterCreationData[] ccds = { new CounterCreationData("Simple1", "Simple Help", PerformanceCounterType.RawBase), new CounterCreationData("Simple2", "Simple Help", PerformanceCounterType.RawBase) };
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection(ccds);

            CounterCreationData[] ccds2 = new CounterCreationData[2];

            ccdc.CopyTo(ccds2, 0);

            Assert.Equal(ccdc[0], ccds2[0]);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_CreateCategory()
        {
            if (!PerformanceCounterCategory.Exists("AverageCounter64SampleCategory"))
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
            PerformanceCounterCategory.Delete("AverageCounter64SampleCategory");
        }
    }
}
