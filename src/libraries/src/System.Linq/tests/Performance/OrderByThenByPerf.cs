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
    public class OrderByThenByPerf
    {
        private void RunTestGroup(string description, Func<IEnumerable<int>, IEnumerable<int>> linqApply)
        {
            TimeSpan time = TimeSpan.Zero;

            time = LinqPerformanceCore.Measure<int>(1000000, 100, LinqPerformanceCore.WrapperType.NoWrap, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnRawArray_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.Measure<int>(1000000, 100, LinqPerformanceCore.WrapperType.IEnumerable, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnEnumerable_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.Measure<int>(1000000, 100, LinqPerformanceCore.WrapperType.IReadOnlyCollection, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnReadOnlyCollection_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.Measure<int>(1000000, 100, LinqPerformanceCore.WrapperType.ICollection, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnCollection_Performance: {0}ms", time.TotalMilliseconds);
        }


        //[Fact]
        public void OrderBy_Performance()
        {
            RunTestGroup("OrderBy", col => col.OrderBy(o => -o));
        }

        //[Fact]
        public void OrderByDescending_Performance()
        {
            RunTestGroup("OrderByDescending", col => col.OrderByDescending(o => o));
        }

        //[Fact]
        public void OrderByThenBy_Performance()
        {
            RunTestGroup("OrderByThenBy", col => col.OrderBy(o => -o).ThenBy(o => o));
        }
    }
}
