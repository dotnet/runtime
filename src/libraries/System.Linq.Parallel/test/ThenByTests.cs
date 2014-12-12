// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class ThenByTests
    {
        //
        // Thenby
        //

        [Fact]
        public static void RunThenByTests()
        {
            // Simple Thenby tests.
            RunThenByTest1(1024 * 128, false);
            RunThenByTest1(1024 * 128, true);
            RunThenByTest2(1024 * 128, false);
            RunThenByTest2(1024 * 128, true);
        }

        [Fact]
        public static void RunThenByComposedWithTests()
        {
            // composition tests (WHERE, WHERE/WHERE, WHERE/SELECT).
            RunThenByComposedWithWhere1(1024 * 128, false);
            RunThenByComposedWithWhere1(1024 * 128, true);
            RunThenByComposedWithJoinJoin(32, 32, false);
            RunThenByComposedWithJoinJoin(32, 32, true);
            RunThenByComposedWithWhereWhere1(1024 * 128, false);
            RunThenByComposedWithWhereWhere1(1024 * 128, true);
            RunThenByComposedWithWhereSelect1(1024 * 128, false);
            RunThenByComposedWithWhereSelect1(1024 * 128, true);
        }

        [Fact]
        [OuterLoop]
        public static void RunThenByComposedWithJoinJoinTests_LongRunning()
        {
            RunThenByComposedWithJoinJoin(1024 * 512, 1024 * 128, false);
            RunThenByComposedWithJoinJoin(1024 * 512, 1024 * 128, true);
        }

        [Fact]
        public static void RunThenByTestRecursive()
        {
            // Multiple levels.
            RunThenByTestRecursive1(8, false);
            RunThenByTestRecursive1(1024 * 128, false);
            RunThenByTestRecursive1(1024 * 128, true);
            RunThenByTestRecursive2(1024 * 128, false);
            RunThenByTestRecursive2(1024 * 128, true);
        }

        private static void RunThenByTest1(int dataSize, bool descending)
        {
            //Random rand = new Random();

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];
            var rangeNumbers = Enumerable.Range(0, (dataSize / 250)).ToList();
            if (rangeNumbers.Count == 0)
                rangeNumbers.Add(0);

            for (int i = 0; i < dataSize; i++)
            {
                int index = i % rangeNumbers.Count;
                pairs[i].First = rangeNumbers[index];

                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;
            if (descending)
            {
                q = pairs.AsParallel().OrderByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                        .ThenByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>().OrderBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                    .ThenBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }

            // Force synchronous execution before validating results.
            List<Pair<int, int>> r = q.ToList<Pair<int, int>>();

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i - 1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i - 1].Second, r[i].Second, descending) > 0)
                {
                    string method = string.Format("RunThenByTest1(dataSize = {0}, descending = {1}) - synchronous/no pipeline", dataSize, descending);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0}.{1} came before {2}.{3} -- but isn't what we expected",
                        r[i - 1].First, r[i - 1].Second, r[i].First, r[i].Second));
                }
            }
        }

        //-----------------------------------------------------------------------------------
        // Exercises basic OrderBy behavior by sorting a fixed set of integers. This always
        // uses asynchronous channels internally, i.e. by pipelining.
        //

        private static void RunThenByTest2(int dataSize, bool descending)
        {
            //Random rand = new Random();

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];

            var rangeNumbers = Enumerable.Range(0, (dataSize / 250)).ToList();
            if (rangeNumbers.Count == 0)
                rangeNumbers.Add(0);

            for (int i = 0; i < dataSize; i++)
            {
                int index = i % rangeNumbers.Count;
                pairs[i].First = rangeNumbers[index];

                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, int>>().OrderByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; }).
                    ThenByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>().OrderBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                    .ThenBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }

            List<Pair<int, int>> r = new List<Pair<int, int>>();

            foreach (Pair<int, int> x in q)
            {
                r.Add(x);
            }

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i - 1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i - 1].Second, r[i].Second, descending) > 0)
                {
                    string method = string.Format("RunThenByTest2(dataSize = {0}, descending = {1}) - asynchronous/pipeline", dataSize, descending);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0}.{1} came before {2}.{3} -- but isn't what we expected",
                        r[i - 1].First, r[i - 1].Second, r[i].First, r[i].Second));
                }
            }
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE. This test
        // processes output sequentially (not pipelined).
        //

        private static void RunThenByComposedWithWhere1(int dataSize, bool descending)
        {
            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];
            var rangeNumbers = Enumerable.Range(0, (dataSize / 250)).ToList();
            if (rangeNumbers.Count == 0)
                rangeNumbers.Add(0);

            for (int i = 0; i < dataSize; i++)
            {
                int index = i % rangeNumbers.Count;
                pairs[i].First = rangeNumbers[index];

                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, int>>()
                    .OrderByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                    .ThenByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>()
                    .OrderBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                    .ThenBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }

            // Wrap with a WHERE:
            q = q.Where<Pair<int, int>>(delegate (Pair<int, int> x) { return (x.First % 2) == 0; });

            // Force synchronous execution before validating results.
            List<Pair<int, int>> r = q.ToList<Pair<int, int>>();

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i - 1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i - 1].Second, r[i].Second, descending) > 0)
                {
                    string method = string.Format("RunThenByComposedWithWhere1(dataSize = {0}, descending = {1}) - sequential/no pipeline",
                        dataSize, descending);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", r[i - 1], r[i]));
                }
            }
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE. This test
        // processes output asynchronously via pipelining.
        //

        private static void RunThenByComposedWithWhere2(int dataSize, bool descending)
        {
            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.
            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];

            var rangeNumbers = Enumerable.Range(0, (dataSize / 250)).ToList();
            if (rangeNumbers.Count == 0)
                rangeNumbers.Add(0);

            for (int i = 0; i < dataSize; i++)
            {
                int index = i % rangeNumbers.Count;
                pairs[i].First = rangeNumbers[index];
                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, int>>()
                    .OrderByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                    .ThenByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>()
                    .OrderBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                    .ThenBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }

            // Wrap with a WHERE:
            q = q.Where<Pair<int, int>>(delegate (Pair<int, int> x) { return (x.First % 2) == 0; });

            List<Pair<int, int>> r = new List<Pair<int, int>>();
            foreach (Pair<int, int> x in q)
                r.Add(x);

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i - 1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i - 1].Second, r[i].Second, descending) > 0)
                {
                    string method = string.Format("RunThenByComposedWithWhere2(dataSize = {0}, descending = {1}) - async/pipeline",
                                            dataSize, descending);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", r[i - 1], r[i]));
                }
            }
        }

        private static void RunThenByComposedWithJoinJoin(int outerSize, int innerSize, bool descending)
        {
            // Generate data in the reverse order in which it'll be sorted.
            DataDistributionType type = descending ? DataDistributionType.AlreadyAscending : DataDistributionType.AlreadyDescending;

            int[] leftPartOne = CreateOrderByInput(outerSize, type);
            int[] leftPartTwo = CreateOrderByInput(outerSize, DataDistributionType.Random);
            Pair<int, int>[] left = new Pair<int, int>[outerSize];
            for (int i = 0; i < outerSize; i++)
                left[i] = new Pair<int, int>(leftPartOne[i] / 1024, leftPartTwo[i]);

            int[] right = CreateOrderByInput(innerSize, type);
            int minValue = outerSize >= innerSize ? innerSize : outerSize;
            int[] middle = new int[minValue];
            if (descending)
                for (int i = middle.Length; i > 0; i--)
                    middle[i - 1] = i;
            else
                for (int i = 0; i < middle.Length; i++)
                    middle[i] = i;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };

            // Create the sort object.
            ParallelQuery<Pair<int, int>> sortedLeft;
            if (descending)
            {
                sortedLeft = left.AsParallel()
                    .OrderByDescending<Pair<int, int>, int>(delegate (Pair<int, int> p) { return p.First; })
                    .ThenByDescending<Pair<int, int>, int>(delegate (Pair<int, int> p) { return p.Second; });
            }
            else
            {
                sortedLeft = left.AsParallel()
                    .OrderBy<Pair<int, int>, int>(delegate (Pair<int, int> p) { return p.First; })
                    .ThenBy<Pair<int, int>, int>(delegate (Pair<int, int> p) { return p.Second; });
            }

            // and now the join...
            ParallelQuery<Pair<int, int>> innerJoin = sortedLeft.Join<Pair<int, int>, int, int, Pair<int, int>>(
                right.AsParallel(), delegate (Pair<int, int> p) { return p.First; }, identityKeySelector,
                delegate (Pair<int, int> x, int y) { return x; });
            ParallelQuery<Pair<int, int>> outerJoin = innerJoin.Join<Pair<int, int>, int, int, Pair<int, int>>(
                middle.AsParallel(), delegate (Pair<int, int> p) { return p.First; }, identityKeySelector,
                delegate (Pair<int, int> x, int y) { return x; });

            //Assert.True(false, string.Format("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length));

            // Ensure pairs are of equal values, and that they are in ascending or descending order.
            int cnt = 0, secondaryCnt = 0;
            Pair<int, int>? last = null;
            string methodName = string.Format("RunThenByComposedWithJoinJoin(outerSize = {0}, innerSize = {1}, descending = {2})", outerSize, innerSize, descending);

            foreach (Pair<int, int> p in outerJoin)
            {
                cnt++;
                if (!((last == null || ((last.Value.First <= p.First && !descending) || (last.Value.First >= p.First && descending)))))
                {
                    Assert.True(false, string.Format(methodName + "  > *ERROR: outer sort order not correct: last = {0}, curr = {1}", last.Value.First, p.First));
                    break;
                }
                if (last != null && last.Value.First == p.First) secondaryCnt++;
                if (!((last == null || (last.Value.First != p.First) || ((last.Value.Second <= p.Second && !descending) || (last.Value.Second >= p.Second && descending)))))
                {
                    Assert.True(false, string.Format(methodName + "  > *ERROR: inner sort order not correct: last = {0}, curr = {1}", last.Value.Second, p.Second));
                }
                last = p;
            }
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with two WHEREs. This test
        // processes output sequentially (not pipelined).
        //

        private static void RunThenByComposedWithWhereWhere1(int dataSize, bool descending)
        {
            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.
            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];

            var rangeNumbers = Enumerable.Range(0, (dataSize / 250)).ToList();
            if (rangeNumbers.Count == 0)
                rangeNumbers.Add(0);

            for (int i = 0; i < dataSize; i++)
            {

                int index = i % rangeNumbers.Count;
                pairs[i].First = rangeNumbers[index];

                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, int>>()
                        .OrderByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                        .ThenByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>()
                        .OrderBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                        .ThenBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }

            // Wrap with a WHERE:
            q = q.Where<Pair<int, int>>(delegate (Pair<int, int> x) { return (x.First % 2) == 0; });
            // Wrap with another WHERE:
            q = q.Where<Pair<int, int>>(delegate (Pair<int, int> x) { return (x.First % 4) == 0; });

            // Force synchronous execution before validating results.
            List<Pair<int, int>> r = q.ToList<Pair<int, int>>();

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i - 1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i - 1].Second, r[i].Second, descending) > 0)
                {
                    string method = string.Format("RunThenByComposedWithWhereWhere1(dataSize = {0}, descending = {1}) - sequential/no pipeline",
                        dataSize, descending);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", r[i - 1], r[i]));
                }
            }
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE and SELECT.
        // This test processes output sequentially (not pipelined).
        //
        // This is particularly interesting because the SELECT completely loses the original
        // type information in the tree, yet the merge is able to put things back in order.
        //

        private static void RunThenByComposedWithWhereSelect1(int dataSize, bool descending)
        {
            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];

            var rangeNumbers = Enumerable.Range(0, (dataSize / 250)).ToList();
            if (rangeNumbers.Count == 0)
                rangeNumbers.Add(0);

            for (int i = 0; i < dataSize; i++)
            {
                int index = i % rangeNumbers.Count;
                pairs[i].First = rangeNumbers[index];

                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q0;

            // Create the ORDERBY:
            if (descending)
            {
                q0 = pairs.AsParallel<Pair<int, int>>()
                        .OrderByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                        .ThenByDescending<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.Second; });
            }
            else
            {
                q0 = pairs.AsParallel<Pair<int, int>>()
                        .OrderBy<Pair<int, int>, int>(delegate (Pair<int, int> x) { return x.First; })
                        .ThenBy<Pair<int, int>, int>(
                    delegate (Pair<int, int> x) { return x.Second; });
            }

            // Wrap with a WHERE:
            q0 = q0.Where<Pair<int, int>>(delegate (Pair<int, int> x) { return (x.First % 2) == 0; });

            // Wrap with a SELECT:
            ParallelQuery<string> q1 = q0.Select<Pair<int, int>, string>(delegate (Pair<int, int> x) { return x.First + "." + x.Second; });

            // Force synchronous execution before validating results.
            List<string> r = q1.ToList<string>();

            for (int i = 1; i < r.Count; i++)
            {
                int i0idx = r[i - 1].IndexOf('.');
                int i1idx = r[i].IndexOf('.');
                Pair<int, int> i0 = new Pair<int, int>(
                    int.Parse(r[i - 1].Substring(0, i0idx)), int.Parse(r[i - 1].Substring(i0idx + 1)));
                Pair<int, int> i1 = new Pair<int, int>(
                    int.Parse(r[i].Substring(0, i1idx)), int.Parse(r[i].Substring(i1idx + 1))); ;

                if (CompareInts(i0.First, i1.First, descending) == 0 &&
                    CompareInts(i0.Second, i1.Second, descending) > 0)
                {
                    string method = string.Format("RunThenByComposedWithWhereSelect1(dataSize = {0}, descending = {1}) - sequential/no pipeline",
                        dataSize, descending);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", i0, i1));
                }
            }
        }

        private static void RunThenByTestRecursive1(int dataSize, bool descending)
        {
            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, Pair<int, int>>[] pairs = new Pair<int, Pair<int, int>>[dataSize];

            var rangeNumbers = Enumerable.Range(0, (dataSize / 250)).ToList();
            if (rangeNumbers.Count == 0)
                rangeNumbers.Add(0);

            for (int i = 0; i < dataSize; i++)
            {
                int index = i % rangeNumbers.Count;
                pairs[i].First = rangeNumbers[index];
                pairs[i].Second = new Pair<int, int>(10 - (i % 10), 3 - (i % 3));
            }

            ParallelQuery<Pair<int, Pair<int, int>>> q;
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, Pair<int, int>>>()
                    .OrderByDescending<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.First; })
                    .ThenByDescending<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.Second.First; })
                    .ThenByDescending<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.Second.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, Pair<int, int>>>()
                    .OrderBy<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.First; })
                    .ThenBy<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.Second.First; })
                    .ThenBy<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.Second.Second; });
            }

            // Force synchronous execution before validating results.
            List<Pair<int, Pair<int, int>>> r = q.ToList<Pair<int, Pair<int, int>>>();

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i - 1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i - 1].Second.First, r[i].Second.First, descending) == 0 &&
                    CompareInts(r[i - 1].Second.Second, r[i].Second.Second, descending) > 0)
                {
                    string method = string.Format("RunThenByTestRecursive1(dataSize = {0}, descending = {1}) - synchronous/no pipeline", dataSize, descending);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0}.{1} came before {2}.{3} -- but isn't what we expected",
                        r[i - 1].First, r[i - 1].Second, r[i].First, r[i].Second));
                }
            }
        }

        private static void RunThenByTestRecursive2(int dataSize, bool descending)
        {
            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, Pair<int, int>>[] pairs = new Pair<int, Pair<int, int>>[dataSize];
            var rangeNumbers = Enumerable.Range(0, (dataSize / 250)).ToList();
            if (rangeNumbers.Count == 0)
                rangeNumbers.Add(0);

            for (int i = 0; i < dataSize; i++)
            {
                int index = i % rangeNumbers.Count;
                pairs[i].First = rangeNumbers[index];

                pairs[i].Second = new Pair<int, int>(10 - (i % 10), 3 - (i % 3));
            }

            ParallelQuery<Pair<int, Pair<int, int>>> q;
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, Pair<int, int>>>()
                    .OrderByDescending<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.First; })
                    .ThenByDescending<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.Second.First; })
                    .ThenByDescending<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.Second.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, Pair<int, int>>>()
                    .OrderBy<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.First; })
                    .ThenBy<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.Second.First; })
                    .ThenBy<Pair<int, Pair<int, int>>, int>(delegate (Pair<int, Pair<int, int>> x) { return x.Second.Second; });
            }

            // Force synchronous execution before validating results.
            List<Pair<int, Pair<int, int>>> r = new List<Pair<int, Pair<int, int>>>();
            foreach (Pair<int, Pair<int, int>> x in q)
                r.Add(x);

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i - 1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i - 1].Second.First, r[i].Second.First, descending) == 0 &&
                    CompareInts(r[i - 1].Second.Second, r[i].Second.Second, descending) > 0)
                {
                    string method = string.Format("RunThenByTestRecursive2(dataSize = {0}, descending = {1}) - asynchronous/pipelining", dataSize, descending);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0}.{1} came before {2}.{3} -- but isn't what we expected",
                        r[i - 1].First, r[i - 1].Second, r[i].First, r[i].Second));
                }
            }
        }

        #region Helper Classes / Methods

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

        private static int CompareInts(int x, int y, bool descending)
        {
            int c = x.CompareTo(y);
            if (descending)
                return -c;
            return c;
        }

        enum DataDistributionType
        {
            AlreadyAscending,
            AlreadyDescending,
            Random
        }

        private static int[] CreateOrderByInput(int dataSize, DataDistributionType type)
        {
            int[] data = new int[dataSize];
            switch (type)
            {
                case DataDistributionType.AlreadyAscending:
                    for (int i = 0; i < data.Length; i++) data[i] = i;
                    break;
                case DataDistributionType.AlreadyDescending:
                    for (int i = 0; i < data.Length; i++) data[i] = dataSize - i;
                    break;
                case DataDistributionType.Random:
                    //Random rand = new Random();
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = i % ValueHelper.Next();
                    }
                    break;
            }
            return data;
        }

        private static class ValueHelper
        {
            private const string text =
@"Pseudo-random numbers are chosen with equal probability from a finite set of numbers. The chosen numbers are 
not completely random because a definite mathematical algorithm is used to select them, but they are sufficiently 
random for practical purposes. The current implementation of the Random class is based on a modified version of 
Donald E. Knuth's subtractive random number generator algorithm. For more information, see D. E. Knuth. 
The Art of Computer Programming, volume 2: Seminumerical Algorithms. Addison-Wesley, Reading, MA, second edition, 
1981.  The random number generation starts from a seed value. If the same seed is used repeatedly, the 
same series of numbers is generated. One way to produce different sequences is to make the seed value 
time-dependent, thereby producing a different series with each new instance of Random. By default, the 
parameterless constructor of the Random class uses the system clock to generate its seed value, while 
its parameterized constructor can take an Int32 value based on the number of ticks in the current time. 
However, because the clock has finite resolution, using the parameterless constructor to create different
Random objects in close succession creates random number generators that produce identical sequences of 
random numbers. The following example illustrates that two Random objects that are instantiated in close 
succession generate an identical series of random numbers. ";

            private static int _currentPosition = 0;

            private static StartPosition _currentStart = StartPosition.Beginning;

            private static readonly int _middlePosition = text.Length / 2;

            public static int Next()
            {
                int nextPosition;

                switch (_currentStart)
                {
                    case StartPosition.Beginning:
                    case StartPosition.Middle:
                        nextPosition = (_currentPosition + 1) % text.Length;
                        break;
                    case StartPosition.End:
                        nextPosition = (_currentPosition - 1) % text.Length;
                        break;
                    default:
                        throw new ArgumentException(string.Format("Enum does not exist {0}", _currentStart));
                }

                if ((nextPosition == 0 && _currentStart != StartPosition.Middle)
                    || (nextPosition == _middlePosition && _currentStart == StartPosition.Middle))
                {
                    _currentStart = (StartPosition)(((int)_currentStart + 1) % 3);
                    switch (_currentStart)
                    {
                        case StartPosition.Beginning:
                            nextPosition = 0;
                            break;
                        case StartPosition.Middle:
                            nextPosition = _middlePosition;
                            break;
                        case StartPosition.End:
                            nextPosition = text.Length - 1;
                            break;
                        default:
                            throw new ArgumentException(string.Format("Enum does not exist {0}", _currentStart));
                    }
                }

                int lengthOfText = text.Length;
                _currentPosition = nextPosition;
                char charValue = text[_currentPosition];
                return (int)charValue;
            }

            enum StartPosition
            {
                Beginning = 0,
                Middle = 1,
                End = 2
            }
        }
        #endregion
    }
}
