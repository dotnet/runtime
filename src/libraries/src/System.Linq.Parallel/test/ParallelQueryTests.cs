// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public static class ParallelQueryTests
    {
        [Fact]
        [ActiveIssue(235)]
        public static void RunTests()
        {
            LeftOperator[] leftOps = new LeftOperator[] {
                                                  new LeftOperator("MakeCast", MakeCast, true),
                                                  new LeftOperator("MakeConcat",MakeConcat, true),
                                                  new LeftOperator("MakeDefaultIfEmpty", MakeDefaultIfEmpty, true),
                                                  new LeftOperator("MakeDistinct", MakeDistinct, false),
                                                  new LeftOperator("MakeExcept", MakeExcept, false),
                                                  new LeftOperator("MakeGroupBy", MakeGroupBy, false),
                                                  new LeftOperator("MakeGroupJoin", MakeGroupJoin, false),
                                                  new LeftOperator("MakeIntersect", MakeIntersect, false),
                                                  new LeftOperator("MakeJoin", MakeJoin, false),
                                                  new LeftOperator("MakeOfType", MakeOfType, true),
                                                  new LeftOperator("MakeOrderBy", MakeOrderBy, true),
                                                  new LeftOperator("MakeOrderByThenBy", MakeOrderByThenBy, true),
                                                  new LeftOperator("MakeReverse", MakeReverse, true),
                                                  new LeftOperator("MakeSelect", MakeSelect, true),
                                                  new LeftOperator("MakeSelectMany", MakeSelectMany, false),
                                                  new LeftOperator("MakeSkip", MakeSkip, true),
                                                  new LeftOperator("MakeSkipWhile", MakeSkipWhile, true),
                                                  new LeftOperator("MakeSkipWhileIndexed", MakeSkipWhileIndexed, true),
                                                  new LeftOperator("MakeTakeWhile", MakeTakeWhile, true),
                                                  new LeftOperator("MakeTakeWhileIndexed", MakeTakeWhileIndexed, true),
                                                  new LeftOperator("MakeUnion", MakeUnion, false),
                                                  new LeftOperator("MakeWhere", MakeWhere, true),
                                                  new LeftOperator("MakeWhereIndexed", MakeWhereIndexed, true),
                                                  new LeftOperator("MakeZip", MakeZip, true)};

            TestTracker result = new TestTracker();
            List<Exception> all_ex = new List<Exception>();

            foreach (bool orderPreserved in new bool[] { true, false })
            {
                foreach (LeftOperator leftOp in leftOps)
                {
                    String operatorName = leftOp.OperatorName;
                    Console.WriteLine("Left Operator={0}, Options={1}", operatorName, orderPreserved);
                    ParallelQuery<int> q = leftOp.LeftOperatorFactory(orderPreserved);
                    try
                    {
                        RunAllTests(result, q, orderPreserved, operatorName, leftOp.OrderDefined);

                    }
                    catch (Exception ex)
                    {
                        all_ex.Add(ex);
                    }

                }
            }

            if (all_ex.Count > 0)
            {
                throw new AggregateException(all_ex);
            }
        }

        private static void RunAllTests(
            TestTracker result, ParallelQuery<int> q, bool orderPreserved,
            string leftOpName, bool leftOrderDefined)
        {
            LogTestRun(leftOpName, "All1", orderPreserved);
            result.MustEqual(
                q.All(i => i > 100),
                q.ToArray().Any(i => i > 100));

            LogTestRun(leftOpName, "All2", orderPreserved);
            result.MustEqual(
                q.All(i => i == 75),
                q.ToArray().All(i => i == 75));

            LogTestRun(leftOpName, "Any1", orderPreserved);
            result.MustEqual(
                q.Any(i => i > 100),
                q.ToArray().Any(i => i > 100));

            LogTestRun(leftOpName, "Any2", orderPreserved);
            result.MustEqual(
                q.Any(i => i == 75),
                q.ToArray().Any(i => i == 75));

            LogTestRun(leftOpName, "Concat", orderPreserved);
            result.MustSequenceEqual(
                q.Concat(q).Concat(new int[] { 1, 2, 3 }.AsParallel()),
                q.Reverse().Reverse().ToArray().Concat(q.Reverse().Reverse()).Concat(new int[] { 1, 2, 3 }),
                leftOrderDefined && orderPreserved);

            LogTestRun(leftOpName, "DefaultIfEmpty", orderPreserved);
            result.MustSequenceEqual(
                q.DefaultIfEmpty(),
                q.ToArray().DefaultIfEmpty(), orderPreserved && leftOrderDefined);

            LogTestRun(leftOpName, "ElementAt", orderPreserved);
            IEnumerable<int> q2 = q.ToArray();
            int count1 = q.Count(), count2 = q2.Count();
            List<int> list1 = new List<int>();
            List<int> list2 = new List<int>();
            for (int i = 0; i < count1; i++) list1.Add(q.ElementAt(i));
            for (int i = 0; i < count2; i++) list2.Add(q2.ElementAt(i));
            result.MustSequenceEqual(list1, list2, leftOrderDefined);

            LogTestRun(leftOpName, "Except", orderPreserved);
            result.MustSequenceEqual(
                q.Except(Enumerable.Range(90, 50).AsParallel()),
                q.ToArray().Except(Enumerable.Range(90, 50)),
                false);

            LogTestRun(leftOpName, "First", orderPreserved);
            CheckFirstOrLast(
                result,
                q.First(),
                q.ToArray().First(),
                leftOrderDefined);

            LogTestRun(leftOpName, "GroupBy", orderPreserved);
            result.MustGroupByEqual(
                q.GroupBy(i => i % 5, (i, e) => new Pair<int, IEnumerable<int>>(i, e)),
                q.ToArray().GroupBy(i => i % 5, (i, e) => new Pair<int, IEnumerable<int>>(i, e)));

            LogTestRun(leftOpName, "GroupJoin", orderPreserved);
            result.MustSequenceEqual(
                q.GroupJoin(q, i => i, i => i, (i, e) => e.FirstOrDefault()),
                q.ToArray().GroupJoin(q.ToArray(), i => i, i => i, (i, e) => e.FirstOrDefault()),
                false);

            LogTestRun(leftOpName, "Intersect", orderPreserved);
            result.MustSequenceEqual(
                q.Intersect(Enumerable.Range(90, 50).AsParallel()),
                q.ToArray().Intersect(Enumerable.Range(90, 50)),
                false);

            LogTestRun(leftOpName, "Join1", orderPreserved);
            result.MustSequenceEqual(
                q.Join((new int[] { 1, 1, 2, 3, 3 }).AsParallel(), i => i, i => i, (i, j) => i + j),
                q.ToArray().Join(new int[] { 1, 1, 2, 3, 3 }, i => i, i => i, (i, j) => i + j),
                false);

            LogTestRun(leftOpName, "Join2", orderPreserved);
            result.MustSequenceEqual(
                q.Join((new int[] { 1, 1, 100, 3, 3 }).AsParallel(), i => new String('a', i), i => new String('a', i), (i, j) => i + j),
                q.ToArray().Join(new int[] { 1, 1, 100, 3, 3 }, i => new String('a', i), i => new String('a', i), (i, j) => i + j),
                false);

            LogTestRun(leftOpName, "Last", orderPreserved);
            CheckFirstOrLast(
                result,
                q.Last(),
                q.ToArray().Last(),
                leftOrderDefined);

            LogTestRun(leftOpName, "Min", orderPreserved);
            CheckFirstOrLast(
                result,
                q.Min(),
                q.ToArray().Min(),
                leftOrderDefined);

            LogTestRun(leftOpName, "Max", orderPreserved);
            CheckFirstOrLast(
                result,
                q.Min(),
                q.ToArray().Min(),
                leftOrderDefined);

            LogTestRun(leftOpName, "OrderBy-ThenBy", orderPreserved);
            result.MustSequenceEqual(
                q.Concat(q).OrderBy(i => i % 5).ThenBy(i => -i),
                q.ToArray().Concat(q).OrderBy(i => i % 5).ThenBy(i => -i),
                true);

            LogTestRun(leftOpName, "OrderByDescending-ThenByDescending", orderPreserved);
            result.MustSequenceEqual(
                q.Concat(q).OrderByDescending(i => i % 5).ThenByDescending(i => -i),
                q.ToArray().Concat(q).OrderByDescending(i => i % 5).ThenByDescending(i => -i),
                true);

            LogTestRun(leftOpName, "Reverse", orderPreserved);
            result.MustSequenceEqual(
                q.Concat(q).Reverse(),
                q.ToArray().Concat(q).Reverse(),
                orderPreserved && leftOrderDefined);

            LogTestRun(leftOpName, "Select", orderPreserved);
            result.MustSequenceEqual(
                q.Select(i => 5 * i - 17),
                q.ToArray().Select(i => 5 * i - 17),
                orderPreserved && leftOrderDefined);

            LogTestRun(leftOpName, "SelectMany", orderPreserved);
            result.MustSequenceEqual(
                q.SelectMany(i => new int[] { 1, 2, 3 }, (i, j) => i + 100 * j),
                q.ToArray().SelectMany(i => new int[] { 1, 2, 3 }, (i, j) => i + 100 * j),
                false);

            LogTestRun(leftOpName, "SequenceEqual", orderPreserved);
            if (orderPreserved && leftOrderDefined)
            {
                result.MustEqual(q.SequenceEqual(q), true);
            }
            else
            {
                // We don't check the return value as it can be either true or false
                q.SequenceEqual(q);
            }

            LogTestRun(leftOpName, "Skip", orderPreserved);
            CheckTakeSkip(
                result,
                q.Skip(10),
                q.ToArray().Skip(10),
                leftOrderDefined && orderPreserved);

            LogTestRun(leftOpName, "SkipWhile", orderPreserved);
            CheckTakeSkip(
                result,
                q.SkipWhile(i => i < 30),
                q.ToArray().SkipWhile(i => i < 30),
                leftOrderDefined && orderPreserved);

            LogTestRun(leftOpName, "SkipWhileIndexed", orderPreserved);
            CheckTakeSkip(
                result,
                q.SkipWhile((i, j) => j < 30),
                q.ToArray().SkipWhile((i, j) => j < 30),
                leftOrderDefined && orderPreserved);

            LogTestRun(leftOpName, "Take", orderPreserved);
            CheckTakeSkip(
                result,
                q.Take(10),
                q.ToArray().Take(10),
                leftOrderDefined && orderPreserved);

            LogTestRun(leftOpName, "TakeWhile", orderPreserved);
            CheckTakeSkip(
                result,
                q.TakeWhile(i => i < 30),
                q.ToArray().TakeWhile(i => i < 30),
                leftOrderDefined && orderPreserved);

            LogTestRun(leftOpName, "TakeWhileIndexed", orderPreserved);
            CheckTakeSkip(
                result,
                q.TakeWhile((i, j) => j < 30),
                q.ToArray().TakeWhile((i, j) => j < 30),
                leftOrderDefined && orderPreserved);

            LogTestRun(leftOpName, "Union", orderPreserved);
            result.MustSequenceEqual(
                q.Union(Enumerable.Range(90, 50).AsParallel()),
                q.ToArray().Union(Enumerable.Range(90, 50)),
                false);

            LogTestRun(leftOpName, "Where", orderPreserved);
            result.MustSequenceEqual(
                q.Where(i => i < 20 || i > 80),
                q.ToArray().Where(i => i < 20 || i > 80),
                orderPreserved && leftOrderDefined);

            LogTestRun(leftOpName, "Zip", orderPreserved);
            IEnumerable<KeyValuePair<int, int>> zipQ = q.Zip(q, (i, j) => new KeyValuePair<int, int>(i, j));
            result.MustSequenceEqual(
                zipQ.Select(p => p.Key),
                q.Reverse().Reverse().ToArray(),
                orderPreserved && leftOrderDefined);
            result.MustSequenceEqual(
                zipQ.Select(p => p.Value),
                q.Reverse().Reverse().ToArray(),
                orderPreserved && leftOrderDefined);
        }

        #region Helper Classes

        private delegate ParallelQuery<int> LeftOperatorFactory(bool orderPreserved);

        private class LeftOperator
        {
            private LeftOperatorFactory _leftOperatorFactory;
            private bool _orderWellDefined;

            public LeftOperator(string name, LeftOperatorFactory leftOperatorFactory, bool orderWellDefined)
            {
                OperatorName = name;
                _leftOperatorFactory = leftOperatorFactory;
                _orderWellDefined = orderWellDefined;
                //if (leftOperatorFactory.Method.Name.Substring(0, 4) != "Make")
                //{
                //    throw new ArgumentException("LeftOperatorFactory method name must start with 'Make'");
                //}
            }

            public LeftOperatorFactory LeftOperatorFactory
            {
                get { return _leftOperatorFactory; }
            }

            public bool OrderDefined
            {
                get { return _orderWellDefined; }
            }

            public string OperatorName
            {
                get;
                set;
            }
        }

        // Helper class to track whether the test passed or failed.
        private class TestTracker
        {
            // Unfortunately, since MustEqual compares its parameters with an == operator, we can't have one
            // generic method (C# compiler does not allow == operator to be applied to a generic type parameter
            // that may be a value type for which == is not defined), but instead we need a separate MustEqual
            // method for each type of parameters.
            public void MustEqual(int a, int b)
            {
                if (a != b) AreNotEqual(a, b);
            }

            public void MustEqual(bool a, bool b)
            {
                if (a != b) AreNotEqual(a, b);
            }

            private void AreNotEqual<T>(T a, T b)
            {
                Assert.True(false, string.Format("   >> Failed. Expect: {0}  Got: {1}", a, b));
            }

            internal void MustSequenceEqual(IEnumerable<int> a, IEnumerable<int> b, bool orderPreserved)
            {
                if (!orderPreserved) { a = a.OrderBy(i => i); b = b.OrderBy(i => i); }

                int[] aa = a.ToArray(), ba = b.ToArray();

                if (!aa.SequenceEqual(ba))
                {
                    Assert.True(false, string.Format("   >> Failed. Sequence 1: {0} AND Sequence 2: {1}", EnumerableToString(aa), EnumerableToString(ba)));
                }
            }

            private string EnumerableToString<T>(IEnumerable<T> e)
            {
                return String.Join(",", e.Select(x => x.ToString()).ToArray());
            }

            // Helper method to verify the output of a GroupBy operator
            internal void MustGroupByEqual(IEnumerable<Pair<int, IEnumerable<int>>> e1, IEnumerable<Pair<int, IEnumerable<int>>> e2)
            {
                var es = new IEnumerable<Pair<int, IEnumerable<int>>>[] { e1, e2 };
                var vals = new Dictionary<int, IEnumerable<int>>[2];

                for (int i = 0; i < 2; i++)
                {
                    vals[i] = new Dictionary<int, IEnumerable<int>>();
                    foreach (var group in es[i])
                    {
                        vals[i].Add(group.First, group.Second.OrderBy(x => x));
                    }
                }

                for (int i = 0; i < 2; i++)
                {
                    foreach (var key in vals[i].Keys)
                    {
                        if (!vals[1 - i].ContainsKey(key) || !vals[1 - i][key].SequenceEqual(vals[i][key]))
                        {
                            Assert.True(false, string.Format(" >> Failed. GroupBy results differ."));
                        }
                    }
                }
            }
        }

        //-----------------------------------------------------------------------------------
        // A pair just wraps two bits of data into a single addressable unit. This is a
        // value type to ensure it remains very lightweight, since it is frequently used
        // with other primitive data types as well.
        //
        // Note: this class is another copy of the Pair<T, U> class defined in CommonDataTypes.cs.
        // For now, we have a copy of the class here, because we can't import the System.Linq.Parallel
        // namespace.
        //
        private struct Pair<T, U>
        {
            // The first and second bits of data.
            internal T m_first;
            internal U m_second;

            //-----------------------------------------------------------------------------------
            // A simple constructor that initializes the first/second fields.
            //

            public Pair(T first, U second)
            {
                m_first = first;
                m_second = second;
            }

            //-----------------------------------------------------------------------------------
            // Accessors for the left and right data.
            //

            public T First
            {
                get { return m_first; }
                set { m_first = value; }
            }

            public U Second
            {
                get { return m_second; }
                set { m_second = value; }
            }
        }

        #endregion

        #region Helper Methods

        //
        // Left operator factory methods
        //
        internal static ParallelQuery<int> MakeCast(bool orderPreserved)
        {
            object[] a = Enumerable.Range(0, 100).Cast<object>().ToArray();

            ParallelQuery<object> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();
            return ipe.Cast<int>();
        }

        internal static ParallelQuery<int> MakeConcat(bool orderPreserved)
        {
            int[] a = Enumerable.Range(1, 35).ToArray();
            int[] b = Enumerable.Range(36, 65).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Concat(b.AsParallel());
        }

        internal static ParallelQuery<int> MakeDefaultIfEmpty(bool orderPreserved)
        {
            int[] a = Enumerable.Range(1, 100).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.DefaultIfEmpty();
        }

        internal static ParallelQuery<int> MakeDistinct(bool orderPreserved)
        {
            List<int> list = new List<int>();
            for (int i = 1; i <= 100; i++) { list.Add(i); list.Add(i); list.Add(i); }
            int[] a = list.Concat(list).Concat(list).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Distinct();
        }

        internal static ParallelQuery<int> MakeExcept(bool orderPreserved)
        {
            int[] a = Enumerable.Range(-5, 110).ToArray();
            int[] b = { -100, -50, -5, -4, -3, -2, -1, 101, 102, 103, 104, 105, 106, 180 };

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Except(b.AsParallel());
        }

        internal static ParallelQuery<int> MakeGroupBy(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 1000).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.GroupBy(i => i % 100, i => i).Select(g => g.Key);
        }

        internal static ParallelQuery<int> MakeGroupJoin(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 100).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.GroupJoin(a.AsParallel(), i => i, i => i, (i, e) => e.FirstOrDefault());
        }

        internal static ParallelQuery<int> MakeIntersect(bool orderPreserved)
        {
            int[] a = Enumerable.Range(-99, 200).ToArray();
            int[] b = Enumerable.Range(0, 200).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Intersect(b.AsParallel());
        }

        internal static ParallelQuery<int> MakeJoin(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 100).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Join(a.AsParallel(), i => i, i => i, (i, j) => i);
        }

        internal static ParallelQuery<int> MakeOfType(bool orderPreserved)
        {
            object[] a = Enumerable.Range(0, 50)
                .Cast<object>()
                .Concat(Enumerable.Repeat<object>(null, 50))
                .Concat(Enumerable.Range(50, 50).Cast<object>()).ToArray();

            ParallelQuery<object> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.OfType<int>();
        }

        internal static ParallelQuery<int> MakeOrderBy(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 100).Reverse().ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.OrderBy(i => i);
        }

        internal static ParallelQuery<int> MakeOrderByThenBy(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 100).Reverse().ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.OrderBy(i => i % 5).ThenBy(i => -i);
        }

        internal static ParallelQuery<int> MakeReverse(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 100).Reverse().ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Reverse();
        }

        internal static ParallelQuery<int> MakeSelect(bool orderPreserved)
        {
            int[] a = Enumerable.Range(-99, 100).Reverse().ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Select(i => -i);
        }

        internal static ParallelQuery<int> MakeSelectMany(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 10).ToArray();
            int[] b = Enumerable.Range(0, 10).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.SelectMany(i => b, (i, j) => (10 * i + j));
        }

        internal static ParallelQuery<int> MakeSkip(bool orderPreserved)
        {
            int[] a = Enumerable.Range(-10, 110).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Skip(10);
        }

        internal static ParallelQuery<int> MakeSkipWhile(bool orderPreserved)
        {
            int[] a = Enumerable.Range(-10, 110).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.SkipWhile(i => i < 0);
        }

        internal static ParallelQuery<int> MakeSkipWhileIndexed(bool orderPreserved)
        {
            int[] a = Enumerable.Range(-10, 110).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.SkipWhile((i, j) => j < 10);
        }

        internal static ParallelQuery<int> MakeTake(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 110).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Take(100);
        }

        internal static ParallelQuery<int> MakeTakeWhile(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 110).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.TakeWhile(i => i < 100);
        }

        internal static ParallelQuery<int> MakeTakeWhileIndexed(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 110).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.TakeWhile((i, j) => j < 100);
        }

        internal static ParallelQuery<int> MakeUnion(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 80).ToArray();
            int[] b = Enumerable.Range(20, 80).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Union(b.AsParallel());
        }

        internal static ParallelQuery<int> MakeWhere(bool orderPreserved)
        {
            int[] a = Enumerable.Range(-10, 120).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Where(i => (i >= 0 && i < 100));
        }

        internal static ParallelQuery<int> MakeWhereIndexed(bool orderPreserved)
        {
            int[] a = Enumerable.Range(-10, 120).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Where((i, j) => (j >= 10 && j < 110));
        }

        internal static ParallelQuery<int> MakeZip(bool orderPreserved)
        {
            int[] a = Enumerable.Range(0, 100).ToArray();

            ParallelQuery<int> ipe = a.AsParallel();
            if (orderPreserved) ipe = ipe.AsOrdered();

            return ipe.Zip(a.AsParallel(), (i, j) => (i + j) / 2);
        }

        private static void CheckFirstOrLast(TestTracker result, int a, int b, bool leftOrderDefined)
        {
            if (leftOrderDefined) result.MustEqual(a, b);
        }

        private static void CheckTakeSkip(TestTracker result, IEnumerable<int> q1, IEnumerable<int> q2, bool leftOrderDefined)
        {
            if (leftOrderDefined)
            {
                result.MustSequenceEqual(q1, q2, true);
            }
            else
            {
                // Just run the queries, but don't compare the answer because it is not unique.
                foreach (var x in q1) { }
                foreach (var x in q2) { }
            }
        }

        private static void LogTestRun(string leftOpName, string rightOpName, bool orderPreserved)
        {
            Console.WriteLine("   Running {0}/{1}, Order Preserved={2}", leftOpName, rightOpName, orderPreserved);
        }

        #endregion
    }
}
