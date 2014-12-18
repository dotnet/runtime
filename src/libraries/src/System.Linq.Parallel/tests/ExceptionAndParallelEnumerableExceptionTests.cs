// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class ExceptionAndParallelEnumerableExceptionTests
    {
        //
        // exceptions...
        //

        [Fact]
        public static void RunExceptionTestSync1()
        {
            int[] xx = new int[1024];
            for (int i = 0; i < xx.Length; i++) xx[i] = i;
            ParallelQuery<int> q = xx.AsParallel().Select<int, int>(
                delegate (int x) { if ((x % 250) == 249) { throw new Exception("Fail!"); } return x; });

            Assert.Throws<AggregateException>(() =>
            {
                List<int> aa = q.ToList<int>();
            });
        }

        [Fact]
        public static void RunExceptionTestAsync1()
        {
            int[] xx = new int[1024];
            for (int i = 0; i < xx.Length; i++) xx[i] = i;
            ParallelQuery<int> q = xx.AsParallel().Select<int, int>(
                delegate (int x) { if ((x % 250) == 249) { throw new Exception("Fail!"); } return x; });

            Assert.Throws<AggregateException>(() =>
            {
                foreach (int y in q) { }
            });
        }

        [Fact]
        public static void RunExceptionTestForAll1()
        {
            int[] xx = new int[1024];
            for (int i = 0; i < xx.Length; i++) xx[i] = i;
            ParallelQuery<int> q = xx.AsParallel().Select<int, int>(
                delegate (int x) { if ((x % 250) == 249) { throw new Exception("Fail!"); } return x; });

            Assert.Throws<AggregateException>(() =>
            {
                q.ForAll<int>(delegate (int x) { });
            });
        }

        //
        // ParallelEnumerable Exceptions
        //
        [Fact]
        public static void RunParallelEnumerableExceptionsTests()
        {
            int[] ints = new int[0];
            ParallelQuery<int> pquery = ints.AsParallel();

            //AsParallel
            Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null).AsParallel());
            Assert.Throws<ArgumentNullException>(() => ((IEnumerable)null).AsParallel());
            Assert.Throws<ArgumentNullException>(() => ((Partitioner<int>)null).AsParallel());

            //AsOrdered
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.AsOrdered((ParallelQuery<int>)null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.AsOrdered((ParallelQuery)null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.AsUnordered<int>((ParallelQuery<int>)null));

            //With*
            Assert.Throws<ArgumentException>(() => ParallelEnumerable.WithExecutionMode(pquery, (ParallelExecutionMode)100));
            Assert.Throws<ArgumentException>(() => ParallelEnumerable.WithMergeOptions(pquery, (ParallelMergeOptions)100));

            //Obsoleted operators 
#pragma warning disable 618 //disable build warnning for Obsoleted methods
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Zip<int, int, int>(null, (IEnumerable<int>)null, null));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Join<int, int, int, int>(null, (IEnumerable<int>)null, null, null, null));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Join<int, int, int, int>(null, (IEnumerable<int>)null, null, null, null, null));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.GroupJoin<int, int, int, int>(null, (IEnumerable<int>)null, null, null, null));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.GroupJoin<int, int, int, int>(null, (IEnumerable<int>)null, null, null, null, null));
#pragma warning restore 618

            //SelectMany
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.SelectMany<int, int, int>(null, (Func<int, int, IEnumerable<int>>)null, null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.SelectMany<int, int, int>(pquery, (Func<int, int, IEnumerable<int>>)null, null));

            //OrderBy
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.OrderBy<int, int>(null, null, null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.OrderBy<int, int>(pquery, null, null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.OrderByDescending<int, int>(null, null, null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.OrderByDescending<int, int>(pquery, null, null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.ThenBy<int, int>(null, null, null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.ThenBy<int, int>(pquery.OrderBy(x => x), null, null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.ThenByDescending<int, int>(null, null, null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.ThenByDescending<int, int>(pquery.OrderBy(x => x), null, null));

            //GroupBy
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.GroupBy<int, int, int>((ParallelQuery<int>)null, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.GroupBy<int, int, int>(pquery, null, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.GroupBy<int, int, int>(pquery, i => i, (Func<int, IEnumerable<int>, int>)null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.GroupBy<int, int, int, int>((ParallelQuery<int>)null, i => i, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.GroupBy<int, int, int, int>(pquery, null, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.GroupBy<int, int, int, int>(pquery, i => i, null, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.GroupBy<int, int, int, int>(pquery, i => i, i => i, (Func<int, IEnumerable<int>, int>)null));

            //Aggregate
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Aggregate<int, int, int>((ParallelQuery<int>)null, 0, (i, j) => i, (i, j) => i, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Aggregate<int, int, int>(pquery, 0, null, (i, j) => i, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Aggregate<int, int, int>(pquery, 0, (i, j) => i, null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Aggregate<int, int, int>(pquery, 0, (i, j) => i, (i, j) => i, null));

            //Count
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Count((ParallelQuery<int>)null, i => true));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Count(pquery, null));

            //Sum
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<int>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<int?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<long>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<long?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<float>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<float?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<double>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<double?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<double>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Sum((ParallelQuery<double?>)null, i => i));

            //Min
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<int>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<int?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<long>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<long?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<float>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<float?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<double>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<double?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<double>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Min((ParallelQuery<double?>)null, i => i));

            //Max
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<int>)null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<int>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<int?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<long>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<long?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<float>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<float?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<double>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<double?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<double>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Max((ParallelQuery<double?>)null, i => i));

            //Average
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<int>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<int?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<long>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<long?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<float>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<float?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<double>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<double?>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<double>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Average((ParallelQuery<double?>)null, i => i));
        }
    }
}
