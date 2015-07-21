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
    public class WhereSelectPerf
    {
        private void RunTestGroup(string description, Func<IEnumerable<int>, IEnumerable<int>> linqApply)
        {
            TimeSpan time = TimeSpan.Zero;

            time = LinqPerformanceCore.Measure<int>(1000000, 1000, LinqPerformanceCore.WrapperType.NoWrap, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnRawArray_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.Measure<int>(1000000, 1000, LinqPerformanceCore.WrapperType.IEnumerable, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnEnumerable_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.Measure<int>(1000000, 1000, LinqPerformanceCore.WrapperType.IReadOnlyCollection, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnReadOnlyCollection_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.Measure<int>(1000000, 1000, LinqPerformanceCore.WrapperType.ICollection, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnCollection_Performance: {0}ms", time.TotalMilliseconds);
        }


        //[Fact]
        public void Select_Performance()
        {
            RunTestGroup("Select", col => col.Select(o => o + 1));
        }

        //[Fact]
        public void SelectSelect_Performance()
        {
            RunTestGroup("SelectSelect", col => col.Select(o => o + 1).Select(o => o - 1));
        }

        //[Fact]
        public void Where_Performance()
        {
            RunTestGroup("Where", col => col.Where(o => o >= 0));
        }

        //[Fact]
        public void WhereWhere_Performance()
        {
            RunTestGroup("WhereWhere", col => col.Where(o => o >= 0).Where(o => o >= -1));
        }

        //[Fact]
        public void WhereSelect_Performance()
        {
            RunTestGroup("Where", col => col.Where(o => o >= 0).Select(o => o + 1));
        }
    }
}
