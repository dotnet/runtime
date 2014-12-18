// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class SequenceEqualTests
    {
        //
        // SequenceEqual
        //

        [Fact]
        public static void RunSequenceEqualTest()
        {
            RunSequenceEqualTestCore(1024, 1024);
            RunSequenceEqualTestCore(1024, 1024, 512);
            RunSequenceEqualTestCore(1024, 1024, 1024 + 512);
            RunSequenceEqualTestCore(1024, 512);
            RunSequenceEqualTestCore(1024, 512, 512);
            RunSequenceEqualTestCore(1024, 512, 1024 + 256);
            RunSequenceEqualTestCore(0, 0);
            RunSequenceEqualTestCore(1024 * 1027, 1024 * 1027);
            RunSequenceEqualTestCore(1024 * 1027, 1024 * 1027, 1024 * 5);
            RunSequenceEqualTestCore(1024 * 1027, 1024 * 1027, (1024 * 1027) + (1024 * 5));
        }

        [Fact]
        public static void RunSequenceEqualTest2()
        {
            RunSequenceEqualTest2Core(0, 10, 0, 10);
            RunSequenceEqualTest2Core(0, 1000, 0, 1000);
            RunSequenceEqualTest2Core(0, 1, 0, 1);
            RunSequenceEqualTest2Core(0, 0, 0, 0);

            RunSequenceEqualTest2Core(0, 10, 1, 10);
            RunSequenceEqualTest2Core(0, 10, 1, 9);
            RunSequenceEqualTest2Core(0, 10, 1, 9);
            RunSequenceEqualTest2Core(0, 0, 0, 1);
        }

        [Fact]
        public static void RunSequenceEqualTest3()
        {
            // Regression tests
            RunSequenceEqualTest3Core(ParallelExecutionMode.Default);
            RunSequenceEqualTest3Core(ParallelExecutionMode.ForceParallelism);
        }

        private static void RunSequenceEqualTestCore(int leftSize, int rightSize, params int[] notEqual)
        {
            int[] left = new int[leftSize];
            for (int i = 0; i < leftSize; i++) left[i] = i * 2;
            int[] right = new int[rightSize];
            for (int i = 0; i < rightSize; i++) right[i] = i * 2;

            if (notEqual != null)
            {
                for (int i = 0; i < notEqual.Length; i++)
                {
                    // To make the elements not equal, we just invert them.
                    int idx = notEqual[i];
                    if (idx >= leftSize)
                        right[idx - leftSize] = -right[idx - leftSize];
                    else
                        left[idx] = -left[idx];
                }
            }

            bool expect = leftSize == rightSize && (notEqual == null || notEqual.Length == 0);
            bool result = left.AsParallel().AsOrdered().SequenceEqual(right.AsParallel().AsOrdered());

            if (expect != result)
            {
                string method = string.Format("RunSequenceEqualTest(leftSize={0}, rightSize={1}, notEqual={2}):", leftSize, rightSize, notEqual);
                Assert.True(false, string.Format(method + "  > FAILED.  Expect: {0}, real: {1}", expect, result));
            }
        }

        private static void RunSequenceEqualTest2Core(int range1From, int range1Count, int range2From, int range2Count)
        {
            bool expect = (range1From == range2From) && (range1Count == range2Count);
            IEnumerable<int>[] sources = {
                                             Enumerable.Range(range1From, range1Count),
                                             Enumerable.Range(range2From, range2Count)
                                         };
            List<ParallelQuery<int>>[] enumerables = new List<ParallelQuery<int>>[2];

            for (int t = 0; t < 2; t++)
            {
                IEnumerable<int> source = sources[t];

                enumerables[t] = new List<ParallelQuery<int>>
                {
                    source.AsParallel().AsOrdered(),
                    new LinkedList<int>(source).AsParallel().AsOrdered(),
                    source.AsParallel().AsOrdered().Where(i => true),
                    source.ToArray().AsParallel().AsOrdered().Where(i => true),
                    source.AsParallel().AsOrdered().Where(i=>true).Reverse().Reverse(),
                    source.Reverse().ToArray().AsParallel().AsOrdered().Where(i=>true).Reverse(),
                    source.AsParallel().AsOrdered().OrderBy(i => i),
                    source.AsParallel().AsOrdered().Select(i => i).Reverse().Reverse().Skip(0)
                };
            }

            foreach (ParallelQuery<int> ipe1 in enumerables[0])
            {
                foreach (ParallelQuery<int> ipe2 in enumerables[1])
                {
                    bool[] results = new bool[] { ipe1.SequenceEqual(ipe2), ipe2.SequenceEqual(ipe1) };
                    foreach (bool result in results)
                    {
                        if (result != expect)
                        {
                            string method = string.Format("RunSequenceEqualTest2([{0}..{1}) vs [{2}..{3}))",
                                range1From, range1From + range1Count, range2From, range2From + range2Count);
                            Assert.True(false, string.Format(method + "  >  FAILED. Expect: {0}, real: {1}", expect, result));
                        }
                    }
                }
            }
        }

        private static void RunSequenceEqualTest3Core(ParallelExecutionMode mode)
        {
            int collectionLength = 1999;

            try
            {
                new DisposeExceptionEnumerable<int>(Enumerable.Range(0, collectionLength)).AsParallel()
                    .WithDegreeOfParallelism(2)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .SequenceEqual(new DisposeExceptionEnumerable<int>(Enumerable.Range(0, collectionLength)).AsParallel());
            }
            catch (AggregateException)
            {
                return;
            }
            catch (Exception ex)
            {
                Assert.True(false, string.Format("RunSequenceEqualTest3(mode={1}):  > Failed: Expected AggregateException, got {0}", ex.GetType(), mode));
            }

            Assert.True(false, string.Format("RunSequenceEqualTest3(mode={0}):  > Failed: Expected AggregateException, got no exception", mode));
        }

        private class DisposeExceptionEnumerable<T> : IEnumerable<T>
        {
            private IEnumerable<T> _enumerable;
            public DisposeExceptionEnumerable(IEnumerable<T> enumerable)
            {
                _enumerable = enumerable;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new DisposeExceptionEnumerator<T>(_enumerable.GetEnumerator());
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new DisposeExceptionEnumerator<T>(_enumerable.GetEnumerator());
            }
        }

        private class DisposeExceptionEnumerator<T> : IEnumerator<T>
        {
            private IEnumerator<T> _enumerator;
            public DisposeExceptionEnumerator(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }
            public T Current
            {
                get { return _enumerator.Current; }
            }
            public void Dispose()
            {
                _enumerator.Dispose();
                throw new ArgumentException();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return _enumerator.Current; }
            }
            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }
            public void Reset()
            {
                _enumerator.Reset();
            }
        }
    }
}
