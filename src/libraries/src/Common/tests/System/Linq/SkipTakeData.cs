// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Tests
{
    public class SkipTakeData
    {
        public static IEnumerable<object[]> EnumerableData()
        {
            IEnumerable<int> sourceCounts = new[] { 0, 1, 2, 3, 5, 8, 13, 55, 100, 250 };

            IEnumerable<int> counts = new[] { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 100, 250, 500, int.MaxValue };
            counts = counts.Concat(counts.Select(c => -c)).Append(0).Append(int.MinValue);

            return from sourceCount in sourceCounts
                   let source = Enumerable.Range(0, sourceCount)
                   from count in counts
                   select new object[] { source, count };
        }

        public static IEnumerable<object[]> EvaluationBehaviorData()
        {
            return Enumerable.Range(-1, 15).Select(count => new object[] { count });
        }

        public static IEnumerable<object[]> QueryableData()
        {
            return EnumerableData().Select(array =>
            {
                var enumerable = (IEnumerable<int>)array[0];
                return new object[] { enumerable.AsQueryable(), array[1] };
            });
        }
    }
}
