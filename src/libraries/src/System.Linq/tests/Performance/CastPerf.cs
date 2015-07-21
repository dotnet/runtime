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
    public class CastPerf
    {
        private class BaseClass
        {
            public int Value;
        }
        private class ChildClass: BaseClass
        {
            public int ChildValue;
        }


        private void RunTestGroup<TSource, TDest>(string description, TSource val, Func<IEnumerable<TSource>, IEnumerable<TDest>> linqApply)
        {
            TimeSpan time = TimeSpan.Zero;

            time = LinqPerformanceCore.Measure<TSource, TDest>(1000000, 500, val, LinqPerformanceCore.WrapperType.NoWrap, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnRawArray_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.Measure<TSource, TDest>(1000000, 500, val, LinqPerformanceCore.WrapperType.IEnumerable, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnEnumerable_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.Measure<TSource, TDest>(1000000, 500, val, LinqPerformanceCore.WrapperType.IReadOnlyCollection, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnReadOnlyCollection_Performance: {0}ms", time.TotalMilliseconds);

            time = LinqPerformanceCore.Measure<TSource, TDest>(1000000, 500, val, LinqPerformanceCore.WrapperType.ICollection, linqApply);
            LinqPerformanceCore.WriteLine(description + "_OnCollection_Performance: {0}ms", time.TotalMilliseconds);
        }



        //[Fact]
        public void Cast_SameType_Performance()
        {
            RunTestGroup<int, int>("Cast_SameType", 1, col => col.Cast<int>());
        }


        //[Fact]
        public void Cast_ToBaseClass_Performance()
        {
            RunTestGroup<ChildClass, BaseClass>("Cast_ToBaseClass", new ChildClass() { Value = 1, ChildValue = 2 }, col => col.Cast<BaseClass>());
        }
    }
}
