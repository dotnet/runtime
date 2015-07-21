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
    public class RangeRepeatPerf
    {
        //[Fact]
        public void Range_Performance()
        {
            TimeSpan time = time = LinqPerformanceCore.Measure<int>(1, 1000, LinqPerformanceCore.WrapperType.NoWrap, 
                col => Enumerable.Range(0, 1000000));
            LinqPerformanceCore.WriteLine("Range_Performance: {0}ms", time.TotalMilliseconds);
        }


        //[Fact]
        public void Repeat_Performance()
        {
            TimeSpan time = time = LinqPerformanceCore.Measure<int>(1, 1000, LinqPerformanceCore.WrapperType.NoWrap, 
                col => Enumerable.Repeat(0, 1000000));
            LinqPerformanceCore.WriteLine("Repeat_Performance: {0}ms", time.TotalMilliseconds);
        }

    }
}
