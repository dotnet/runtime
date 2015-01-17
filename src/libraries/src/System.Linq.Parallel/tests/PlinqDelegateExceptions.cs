// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    /// <summary>
    /// public so that the tests can also be called by other exe/wrappers eg VSTS test harness.
    /// </summary>
    public static class PlinqDelegateExceptions
    {
        /// <summary>
        /// A basic test for a query that throws user delegate exceptions
        /// </summary>
        /// <returns></returns>
        [Fact]
        public static void DistintOrderBySelect()
        {
            Exception caughtAggregateException = null;
            try
            {
                var query2 = new int[] { 1, 2, 3 }.AsParallel()
                    .Distinct()
                    .OrderBy(i => i)
                    .Select(i => UserDelegateException.Throw<int, int>(i));
                foreach (var x in query2)
                {
                }
            }
            catch (AggregateException e)
            {
                caughtAggregateException = e;
            }
            Assert.NotNull(caughtAggregateException);
        }

        /// <summary>
        /// Another basic test for a query that throws user delegate exceptions
        /// </summary>
        /// <returns></returns>
        [Fact]
        public static void SelectJoin()
        {
            Exception caughtAggregateException = null;

            try
            {
                var query = new int[] { 1, 2, 3 }.AsParallel()
                    .Select(i => UserDelegateException.Throw<int, int>(i))
                    .Join(new int[] { 1 }.AsParallel(), i => i, j => j, (i, j) => 0);
                foreach (var x in query)
                {
                }
            }
            catch (AggregateException e)
            {
                caughtAggregateException = e;
            }

            Assert.NotNull(caughtAggregateException);
        }

        [Fact]
        [OuterLoop]
        public static void OrderBy()
        {
            // the orderby was a particular problem for the June 2008 CTP.
            OrderByCore(10);
            OrderByCore(100);
            OrderByCore(1000);
        }

        /// <summary>
        /// Heavily exercises OrderBy in the face of user-delegate exceptions. 
        /// On CTP-M1, this would deadlock for DOP=7,9,11,... on 4-core, but works for DOP=1..6 and 8,10,12, ...
        /// 
        /// In this test, every call to the key-selector delegate throws.
        /// </summary>
        private static void OrderByCore(int range)
        {
            for (int dop = 1; dop <= 30; dop++)
            {
                AggregateException caughtAggregateException = null;

                try
                {
                    var query = Enumerable.Range(0, range)
                        .AsParallel().WithDegreeOfParallelism(dop)
                        .OrderBy(i => UserDelegateException.Throw<int, int>(i));
                    foreach (int i in query)
                    {
                    }
                }
                catch (AggregateException e)
                {
                    caughtAggregateException = e;
                }

                Assert.NotNull(caughtAggregateException);
            }
        }

        [Fact]
        [OuterLoop]
        public static void OrderBy_OnlyOneException()
        {
            // and try situations where only one user delegate exception occurs
            OrderBy_OnlyOneExceptionCore(10);
            OrderBy_OnlyOneExceptionCore(100);
            OrderBy_OnlyOneExceptionCore(1000);
        }

        /// <summary>
        /// Heavily exercises OrderBy, but only throws one user delegate exception to simulate an occasional failure.
        /// </summary>
        private static void OrderBy_OnlyOneExceptionCore(int range)
        {
            for (int dop = 1; dop <= 30; dop++)
            {
                int indexForException = range / (2 * dop); // eg 1000 items on 4-core, throws on item 125.

                AggregateException caughtAggregateException = null;

                try
                {
                    var query = Enumerable.Range(0, range)
                        .AsParallel().WithDegreeOfParallelism(dop)
                        .OrderBy(i =>
                        {
                            UserDelegateException.ThrowIf(i == indexForException);
                            return i;
                        }
                         );
                    foreach (int i in query)
                    {
                    }
                }
                catch (AggregateException e)
                {
                    caughtAggregateException = e;
                }


                Assert.NotNull(caughtAggregateException);
                Assert.True(caughtAggregateException.InnerExceptions.Count == 1, string.Format("PLinqDelegateExceptions.OrderBy_OnlyOneException Range: {0}:  AggregateException.InnerExceptions != 1.", range));
            }
        }

        [Fact]
        [OuterLoop]
        public static void ZipAndOrdering()
        {
            // zip and ordering was also broken in June 2008 CTP, but this was due to the ordering component.
            ZipAndOrderingCore(10);
            ZipAndOrderingCore(100);
            ZipAndOrderingCore(1000);
        }

        /// <summary>
        /// Zip with ordering on showed issues, but it was due to the ordering component.
        /// This is included as a regression test for that particular repro.
        /// </summary>
        private static void ZipAndOrderingCore(int range)
        {
            //Console.Write("       DOP: ");
            for (int dop = 1; dop <= 30; dop++)
            {
                AggregateException ex = null;
                try
                {
                    var enum1 = Enumerable.Range(1, range);
                    var enum2 = Enumerable.Repeat(1, range * 2);

                    var query1 = enum1
                        .AsParallel()
                        .AsOrdered().WithDegreeOfParallelism(dop)
                        .Zip(enum2.AsParallel().AsOrdered(),
                             (a, b) => UserDelegateException.Throw<int, int, int>(a, b));

                    var output = query1.ToArray();
                }
                catch (AggregateException ae)
                {
                    ex = ae;
                }


                Assert.NotNull(ex != null);
                Assert.False(AggregateExceptionContains(ex, typeof(OperationCanceledException)), string.Format("PLinqDelegateExceptions.ZipAndOrdering Range: {0}: the wrong exception was inside the aggregate exception.", range));
                Assert.True(AggregateExceptionContains(ex, typeof(UserDelegateException)), string.Format("PLinqDelegateExceptions.ZipAndOrdering Range: {0}: the wrong exception was inside the aggregate exception.", range));
            }
        }

        /// <summary>
        /// If the user delegate throws an OperationCanceledException, it should get aggregated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public static void OperationCanceledExceptionsGetAggregated()
        {
            AggregateException caughtAggregateException = null;
            try
            {
                var enum1 = Enumerable.Range(1, 13);

                var query1 =
                    enum1
                        .AsParallel()
                        .Select<int, int>(i => { throw new OperationCanceledException(); });
                var output = query1.ToArray();
            }
            catch (AggregateException ae)
            {
                caughtAggregateException = ae;
            }

            Assert.NotNull(caughtAggregateException);

            Assert.True(AggregateExceptionContains(caughtAggregateException, typeof(OperationCanceledException)),
                "the wrong exception was inside the aggregate exception.");
        }

        /// <summary>
        /// The plinq chunk partitioner calls takes an IEnumerator over the source, and disposes the enumerator when it is
        /// finished.
        /// If an exception occurs, the calling enumerator disposes the enumerator... but then other callers generate ODEs.
        /// These ODEs either should not occur (prefered), or should not get into the aggregate exception.
        /// 
        /// Also applies to the standard stand-alone chunk partitioner.
        /// Does not apply to other partitioners unless an exception in one consumer would cause flow-on exception in others.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public static void PlinqChunkPartitioner_DontEnumerateAfterException()
        {
            try
            {
                Enumerable.Range(1, 10)
                    .AsParallel()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(x => { if (x == 4) throw new ArgumentException("manual exception"); return x; })
                    .Zip(Enumerable.Range(1, 10).AsParallel(), (a, b) => a + b)
                 .AsParallel()
                 .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                 .ToArray();
            }
            catch (AggregateException e)
            {
                if (!e.Flatten().InnerExceptions.All(ex => ex.GetType() == typeof(ArgumentException)))
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var exception in e.Flatten().InnerExceptions)
                    {
                        builder.Append("     exception = " + exception);
                    }
                    Assert.True(false, string.Format("PlinqChunkPartitioner_DontEnumerateAfterException:  FAIL. only a single ArgumentException should appear in the aggregate: {0}", builder.ToString()));
                }
            }
        }

        /// <summary>
        /// The stand-alone chunk partitioner calls takes an IEnumerator over the source, and disposes the enumerator when it is
        /// finished.
        /// If an exception occurs, the calling enumerator disposes the enumerator... but then other callers generate ODEs.
        /// These ODEs either should not occur (prefered), or should not get into the aggregate exception.
        /// 
        /// Also applies to the plinq stand-alone chunk partitioner.
        /// Does not apply to other partitioners unless an exception in one consumer would cause flow-on exception in others.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public static void ManualChunkPartitioner_DontEnumerateAfterException()
        {
            try
            {
                Partitioner.Create(
                    Enumerable.Range(1, 10)
                        .AsParallel()
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .Select(x => { if (x == 4) throw new ArgumentException("manual exception"); return x; })
                        .Zip(Enumerable.Range(1, 10).AsParallel(), (a, b) => a + b)
                    )
                    .AsParallel()
                    .ToArray();
            }
            catch (AggregateException e)
            {
                if (!e.Flatten().InnerExceptions.All(ex => ex.GetType() == typeof(ArgumentException)))
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var exception in e.Flatten().InnerExceptions)
                    {
                        builder.Append("     exception = " + exception);
                    }
                    Assert.True(false, string.Format("ManualChunkPartitioner_DontEnumerateAfterException  FAIL. only a single ArgumentException should appear in the aggregate: {0}", builder.ToString()));
                }
            }
        }
        [Fact]
        public static void MoveNextAfterQueryOpeningFailsIsIllegal()
        {
            var query = Enumerable.Range(0, 10)
                .AsParallel()
                .Select<int, int>(x => { throw new ArgumentException(); })
                .OrderBy(x => x);

            IEnumerator<int> enumerator = query.GetEnumerator();

            //moveNext will cause queryOpening to fail (no element generated)
            Assert.Throws<AggregateException>(() => enumerator.MoveNext());

            //moveNext after queryOpening failed
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        public static bool AggregateExceptionContains(AggregateException aggregateException, Type exceptionType)
        {
            foreach (Exception innerException in aggregateException.InnerExceptions)
            {
                if (innerException.GetType() == exceptionType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Exception for simulating exceptions from user delegates.
        /// </summary>
        public class UserDelegateException : Exception
        {
            public static TOut Throw<TIn, TOut>(TIn input)
            {
                throw new UserDelegateException();
            }

            public static TOut Throw<TIn1, TIn2, TOut>(TIn1 input1, TIn2 input2)
            {
                throw new UserDelegateException();
            }

            public static void ThrowIf(bool predicate)
            {
                if (predicate)
                    throw new UserDelegateException();
            }
        }
    }
}
