// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class OrderByTests
    {
        #region OrderBy Tests

        //
        // OrderBy
        //

        [Fact]
        public static void RunOrderByTest1()
        {
            // Non-pipelining tests (synchronous channels).

            RunOrderByTest1Core(0, false, DataDistributionType.AlreadyAscending);
            RunOrderByTest1Core(0, true, DataDistributionType.AlreadyAscending);

            RunOrderByTest1Core(50, false, DataDistributionType.AlreadyAscending);
            RunOrderByTest1Core(50, false, DataDistributionType.AlreadyDescending);
            RunOrderByTest1Core(50, false, DataDistributionType.Random);
            RunOrderByTest1Core(50, true, DataDistributionType.AlreadyAscending);
            RunOrderByTest1Core(50, true, DataDistributionType.AlreadyDescending);
            RunOrderByTest1Core(50, true, DataDistributionType.Random);

            RunOrderByTest1Core(1024 * 128, false, DataDistributionType.AlreadyAscending);
            RunOrderByTest1Core(1024 * 128, false, DataDistributionType.AlreadyDescending);
            RunOrderByTest1Core(1024 * 128, false, DataDistributionType.Random);
            RunOrderByTest1Core(1024 * 128, true, DataDistributionType.AlreadyAscending);
            RunOrderByTest1Core(1024 * 128, true, DataDistributionType.AlreadyDescending);
            RunOrderByTest1Core(1024 * 128, true, DataDistributionType.Random);
        }

        [Fact]
        public static void RunOrderByTest2()
        {
            // Pipelining tests (asynchronous channels).

            RunOrderByTest2Core(1024 * 128, false, DataDistributionType.AlreadyAscending);
            RunOrderByTest2Core(1024 * 128, false, DataDistributionType.AlreadyDescending);
            RunOrderByTest2Core(1024 * 128, false, DataDistributionType.Random);
            RunOrderByTest2Core(1024 * 128, true, DataDistributionType.AlreadyAscending);
            RunOrderByTest2Core(1024 * 128, true, DataDistributionType.AlreadyDescending);
            RunOrderByTest2Core(1024 * 128, true, DataDistributionType.Random);
        }

        [Fact]
        public static void RunOrderByComposedWithTests()
        {
            // Try some composition tests (i.e. wrapping).

            RunOrderByComposedWithWhere1(1024 * 128, false, DataDistributionType.Random);
            RunOrderByComposedWithWhere1(1024 * 128, true, DataDistributionType.Random);
            RunOrderByComposedWithWhere2(1024 * 128, false, DataDistributionType.Random);
            RunOrderByComposedWithWhere2(1024 * 128, true, DataDistributionType.Random);
            RunOrderByComposedWithJoinJoin(32, 32, false);
            RunOrderByComposedWithJoinJoin(32, 32, true);
            RunOrderByComposedWithWhereWhere1(1024 * 128, false, DataDistributionType.Random);
            RunOrderByComposedWithWhereWhere1(1024 * 128, true, DataDistributionType.Random);
            RunOrderByComposedWithWhereSelect1(1024 * 128, false, DataDistributionType.Random);
            RunOrderByComposedWithWhereSelect1(1024 * 128, true, DataDistributionType.Random);

            RunOrderByComposedWithOrderBy(1024 * 128, false, DataDistributionType.Random);
            RunOrderByComposedWithOrderBy(1024 * 128, true, DataDistributionType.Random);
            RunOrderByComposedWithOrderBy(1024 * 128, false, DataDistributionType.Random);
            RunOrderByComposedWithOrderBy(1024 * 128, true, DataDistributionType.Random);
        }

        [Fact]
        [OuterLoop]
        public static void RunOrderByComposedWithJoinJoinTests_LongRunning()
        {
            RunOrderByComposedWithJoinJoin(1024 * 512, 1024 * 128, false);
            RunOrderByComposedWithJoinJoin(1024 * 512, 1024 * 128, true);
        }

        [Fact]
        public static void RunStableSortTest1()
        {
            // Stable sort.
            RunStableSortTest1Core(1024);
            RunStableSortTest1Core(1024 * 128);
        }

        //-----------------------------------------------------------------------------------
        // Exercises basic OrderBy behavior by sorting a fixed set of integers. This always
        // uses synchronous channels internally, i.e. by not pipelining.
        //

        private static void RunOrderByTest1Core(int dataSize, bool descending, DataDistributionType type)
        {
            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate (int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate (int x) { return x; });
            }

            // Force synchronous execution before validating results.
            List<int> r = q.ToList<int>();

            int prev = descending ? int.MaxValue : int.MinValue;
            for (int i = 0; i < r.Count; i++)
            {
                int x = r[i];

                if (descending ? x > prev : x < prev)
                {
                    string method = string.Format("RunOrderByTest1(dataSize = {0}, descending = {1}, type = {2}) - synchronous/no pipeline:",
                        dataSize, descending, type);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x));
                }

                prev = x;
            }
        }

        //-----------------------------------------------------------------------------------
        // Exercises basic OrderBy behavior by sorting a fixed set of integers. This always
        // uses asynchronous channels internally, i.e. by pipelining.
        //

        private static void RunOrderByTest2Core(int dataSize, bool descending, DataDistributionType type)
        {
            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate (int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate (int x) { return x; });
            }

            int prev = descending ? int.MaxValue : int.MinValue;
            foreach (int x in q)
            {
                if (descending ? x > prev : x < prev)
                {
                    string method = string.Format("RunOrderByTest2(dataSize = {0}, descending = {1}, type = {2}) - asynchronous/pipeline:",
                        dataSize, descending, type);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x)); ;
                }

                prev = x;
            }
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE. This test
        // processes output sequentially (not pipelined).
        //

        private static void RunOrderByComposedWithWhere1(int dataSize, bool descending, DataDistributionType type)
        {
            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate (int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate (int x) { return x; });
            }

            // Wrap with a WHERE:
            q = q.Where<int>(delegate (int x) { return (x % 2) == 0; });

            // Force synchronous execution before validating results.
            List<int> results = q.ToList<int>();

            int prev = descending ? int.MaxValue : int.MinValue;
            for (int i = 0; i < results.Count; i++)
            {
                int x = results[i];

                if (descending ? x > prev : x < prev)
                {
                    string method = string.Format("RunOrderByComposedWithWhere1(dataSize = {0}, descending = {1}, type = {2}) - sequential/no pipeline:",
                        dataSize, descending, type);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x));
                }

                prev = x;
            }
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE. This test
        // processes output asynchronously via pipelining.
        //

        private static void RunOrderByComposedWithWhere2(int dataSize, bool descending, DataDistributionType type)
        {
            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate (int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate (int x) { return x; });
            }

            // Wrap with a WHERE:
            q = q.Where<int>(delegate (int x) { return (x % 2) == 0; });

            int prev = descending ? int.MaxValue : int.MinValue;
            foreach (int x in q)
            {
                if (descending ? x > prev : x < prev)
                {
                    string method = string.Format("RunOrderByComposedWithWhere2(dataSize = {0}, descending = {1}, type = {2}) - async/pipeline",
                        dataSize, descending, type);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x));
                }

                prev = x;
            }
        }

        private static void RunOrderByComposedWithJoinJoin(int outerSize, int innerSize, bool descending)
        {
            // Generate data in the reverse order in which it'll be sorted.
            DataDistributionType type = descending ? DataDistributionType.AlreadyAscending : DataDistributionType.AlreadyDescending;

            int[] left = CreateOrderByInput(outerSize, type);
            int[] right = CreateOrderByInput(innerSize, type);
            int min = outerSize >= innerSize ? innerSize : outerSize;
            int[] middle = new int[min];
            if (descending)
                for (int i = middle.Length; i > 0; i--)
                    middle[i - 1] = i;
            else
                for (int i = 0; i < middle.Length; i++)
                    middle[i] = i;

            Func<int, int> identityKeySelector = delegate (int x) { return x; };

            // Create the sort object.
            ParallelQuery<int> sortedLeft;
            if (descending)
            {
                sortedLeft = left.AsParallel().OrderByDescending<int, int>(identityKeySelector);
            }
            else
            {
                sortedLeft = left.AsParallel().OrderBy<int, int>(identityKeySelector);
            }

            // and now the join...
            ParallelQuery<Pair<int, int>> innerJoin = sortedLeft.Join<int, int, int, Pair<int, int>>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate (int x, int y) { return new Pair<int, int>(x, y); });
            ParallelQuery<int> outerJoin = innerJoin.Join<Pair<int, int>, int, int, int>(
                middle.AsParallel(), delegate (Pair<int, int> p) { return p.First; }, identityKeySelector, delegate (Pair<int, int> x, int y) { return x.First; });

            // Ensure pairs are of equal values, and that they are in ascending or descending order.
            int cnt = 0;
            int? last = null;
            string method = string.Format("RunOrderByComposedWithJoinJoin(outerSize = {0}, innerSize = {1}, descending = {2})",
                outerSize, innerSize, descending);

            foreach (int p in outerJoin)
            {
                cnt++;
                if (!((last == null || ((last.Value <= p && !descending) || (last.Value >= p && descending)))))
                {
                    Assert.True(false, string.Format(method + "  > *ERROR: sort order not correct: last = {0}, curr = {1}", last.Value, p));
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

        private static void RunOrderByComposedWithWhereWhere1(int dataSize, bool descending, DataDistributionType type)
        {
            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate (int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate (int x) { return x; });
            }

            // Wrap with a WHERE:
            q = q.Where<int>(delegate (int x) { return (x % 2) == 0; });
            // Wrap with another WHERE:
            q = q.Where<int>(delegate (int x) { return (x % 4) == 0; });

            // Force synchronous execution before validating results.
            List<int> results = q.ToList<int>();

            int prev = descending ? int.MaxValue : int.MinValue;
            foreach (int x in results)
            {
                if (descending ? x > prev : x < prev)
                {
                    string method = string.Format("RunOrderByComposedWithWhereWhere1(dataSize = {0}, descending = {1}, type = {2}) - sequential/no pipeline",
                        dataSize, descending, type);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x));
                }

                prev = x;
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

        private static void RunOrderByComposedWithWhereSelect1(int dataSize, bool descending, DataDistributionType type)
        {
            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q0;

            // Create the ORDERBY:
            if (descending)
            {
                q0 = data.AsParallel().OrderByDescending<int, int>(
                    delegate (int x) { return x; });
            }
            else
            {
                q0 = data.AsParallel().OrderBy<int, int>(
                    delegate (int x) { return x; });
            }

            // Wrap with a WHERE:
            q0 = q0.Where<int>(delegate (int x) { return (x % 2) == 0; });

            // Wrap with a SELECT:
            ParallelQuery<string> q1 = q0.Select<int, string>(delegate (int x) { return x.ToString(); });

            // Force synchronous execution before validating results.
            List<string> results = q1.ToList<string>();

            int prev = descending ? int.MaxValue : int.MinValue;
            foreach (string xs in results)
            {
                int x = int.Parse(xs);

                if (descending ? x > prev : x < prev)
                {
                    string method = string.Format("RunOrderByComposedWithWhereSelect1(dataSize = {0}, descending = {1}, type = {2}) - sequential/no pipeline",
                        dataSize, descending, type);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x));
                }

                prev = x;
            }
        }

        //-----------------------------------------------------------------------------------
        // Nested sorts.
        //

        private static void RunOrderByComposedWithOrderBy(int dataSize, bool descending, DataDistributionType type)
        {
            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;

            // Create the ORDERBY:
            if (!descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate (int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate (int x) { return x; });
            }

            // Wrap with a WHERE:
            q = q.Where<int>(delegate (int x) { return (x % 2) == 0; });

            // And wrap with another ORDERBY:
            if (descending)
            {
                q = q.OrderByDescending<int, int>(delegate (int x) { return x; });
            }
            else
            {
                q = q.OrderBy<int, int>(delegate (int x) { return x; });
            }

            // Force synchronous execution before validating results.
            List<int> results = q.ToList<int>();

            int prev = descending ? int.MaxValue : int.MinValue;
            for (int i = 0; i < results.Count; i++)
            {
                int x = results[i];

                if (descending ? x > prev : x < prev)
                {
                    string method = string.Format("RunOrderByComposedWithOrderBy(dataSize = {0}, descending = {1}, type = {2}) - sequential/no pipeline",
                        dataSize, descending, type);
                    Assert.True(false, string.Format(method + "  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x));
                }

                prev = x;
            }
        }

        //-----------------------------------------------------------------------------------
        // Stable sort implementation: uses indices, OrderBy, ThenBy.
        //

        private static void RunStableSortTest1Core(int dataSize)
        {
            SSC[] clist = new SSC[dataSize];
            for (int i = 0; i < clist.Length; i++)
            {
                clist[i] = new SSC((dataSize - i) / (dataSize / 5), i);
            }

            IEnumerable<SSC> clistSorted = clist.AsParallel().Select<SSC, SSD>((c, i) => new SSD(c, i)).OrderBy<SSD, int>((c) => c.c.SortKey).ThenBy<SSD, int>((c) => c.idx).Select((c) => c.c);
            int lastKey = -1, lastIdx = -1;

            string method = string.Format("RunStableSortTest1(dataSize = {0}) - synchronous/no pipeline", dataSize);

            foreach (SSC c in clistSorted)
            {
                if (c.SortKey < lastKey)
                {
                    Assert.True(false, string.Format(method + "  > FAILED.  Keys not in ascending order: {0} expected to be <= {1}", lastKey, c.SortKey));
                }
                else if (c.SortKey == lastKey && c.Index < lastIdx)
                {
                    Assert.True(false, string.Format(method + "  > FAILED.  Instability on equal keys: {0} expected to be <= {1}", lastIdx, c.Index));
                }

                lastKey = c.SortKey;
                lastIdx = c.Index;
            }
        }

        #endregion

        #region Helper methods
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

        private class SSC
        {
            public int SortKey;
            public int Index;
            public SSC(int key, int idx)
            {
                SortKey = key; Index = idx;
            }
        }

        private class SSD
        {
            public SSC c;
            public int idx;
            public SSD(SSC c, int idx)
            {
                this.c = c; this.idx = idx;
            }
        }

        private static int[] CreateOrderByInput(int dataSize, DataDistributionType type)
        {
            int[] data = new int[dataSize];
            switch (type)
            {
                case DataDistributionType.AlreadyAscending:
                    for (int i = 0; i < data.Length; i++)
                        data[i] = i;
                    break;
                case DataDistributionType.AlreadyDescending:
                    for (int i = 0; i < data.Length; i++)
                        data[i] = dataSize - i;
                    break;
                case DataDistributionType.Random:
                    //Random rand = new Random();
                    for (int i = 0; i < data.Length; i++)
                        data[i] = ValueHelper.Next();
                    break;
            }
            return data;
        }

        enum DataDistributionType
        {
            AlreadyAscending,
            AlreadyDescending,
            Random
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
