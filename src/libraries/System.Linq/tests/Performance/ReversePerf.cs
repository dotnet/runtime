using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Diagnostics;

namespace System.Linq.Tests.Performance
{
    // This is not a UnitTests 
    public class ReversePerf
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
        public void Reverse_Performance()
        {
            RunTestGroup("Reverse", col => col.Reverse());
        }
    }
}
