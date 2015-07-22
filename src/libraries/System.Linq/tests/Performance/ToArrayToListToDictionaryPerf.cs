// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Diagnostics;

namespace System.Linq.Tests.Performance
{
    // This is not a UnitTests 
    [Trait("Perf", "true")]
    public class ToArrayToListToDictionaryPerf
    {
        //[Fact]
        public void ToArray_Performance()
        {
            int[] array = Enumerable.Range(0, 1000000).ToArray();

            TimeSpan time = TimeSpan.Zero;

            time = LinqPerformanceCore.MeasureMaterializationToArray<int>(array, 1000);
            LinqPerformanceCore.WriteLine("ToArray_OnRawArray_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.MeasureMaterializationToArray<int>(new LinqPerformanceCore.EnumerableWrapper<int>(array), 1000);
            LinqPerformanceCore.WriteLine("ToArray_OnEnumerable_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.MeasureMaterializationToArray<int>(new LinqPerformanceCore.ReadOnlyCollectionWrapper<int>(array), 1000);
            LinqPerformanceCore.WriteLine("ToArray_OnReadOnlyCollection_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.MeasureMaterializationToArray<int>(new LinqPerformanceCore.CollectionWrapper<int>(array), 1000);
            LinqPerformanceCore.WriteLine("ToArray_OnCollection_Performance: {0}ms", time.TotalMilliseconds);
        }


        // ===========

        //[Fact]
        public void ToList_Performance()
        {
            int[] array = Enumerable.Range(0, 1000000).ToArray();

            TimeSpan time = TimeSpan.Zero;

            time = LinqPerformanceCore.MeasureMaterializationToList<int>(array, 1000);
            LinqPerformanceCore.WriteLine("ToList_OnRawArray_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.MeasureMaterializationToList<int>(new LinqPerformanceCore.ReadOnlyCollectionWrapper<int>(array), 1000);
            LinqPerformanceCore.WriteLine("ToList_OnReadOnlyCollection_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.MeasureMaterializationToList<int>(new LinqPerformanceCore.ReadOnlyCollectionWrapper<int>(array), 1000);
            LinqPerformanceCore.WriteLine("ToList_OnReadOnlyCollection_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.MeasureMaterializationToList<int>(new LinqPerformanceCore.CollectionWrapper<int>(array), 1000);
            LinqPerformanceCore.WriteLine("ToList_OnCollection_Performance: {0}ms", time.TotalMilliseconds);
        }


        // ============

        //[Fact]
        public void ToDictionary_Performance()
        {
            int[] array = Enumerable.Range(0, 1000000).ToArray();

            TimeSpan time = TimeSpan.Zero;

            time = LinqPerformanceCore.MeasureMaterializationToDictionary<int>(array, 100);
            LinqPerformanceCore.WriteLine("ToDictionary_OnRawArray_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.MeasureMaterializationToDictionary<int>(new LinqPerformanceCore.EnumerableWrapper<int>(array), 100);
            LinqPerformanceCore.WriteLine("ToDictionary_OnEnumerable_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.MeasureMaterializationToDictionary<int>(new LinqPerformanceCore.ReadOnlyCollectionWrapper<int>(array), 100);
            LinqPerformanceCore.WriteLine("ToDictionary_OnReadOnlyCollection_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.MeasureMaterializationToDictionary<int>(new LinqPerformanceCore.CollectionWrapper<int>(array), 100);
            LinqPerformanceCore.WriteLine("ToDictionary_OnCollection_Performance: {0}ms", time.TotalMilliseconds);
        }
    }
}
