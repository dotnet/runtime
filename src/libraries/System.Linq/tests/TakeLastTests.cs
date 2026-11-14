// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;
using static System.Linq.Tests.SkipTakeData;

namespace System.Linq.Tests
{
    public class TakeLastTests : EnumerableTests
    {
        [Fact]
        public void SkipLastThrowsOnNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).TakeLast(10));
        }

        [Theory]
        [MemberData(nameof(EnumerableData), MemberType = typeof(SkipTakeData))]
        public void TakeLast(IEnumerable<int> source, int count)
        {
            int[] expected = source.Reverse().Take(count).Reverse().ToArray();

            Assert.All(CreateSources(source), source =>
            {
                IEnumerable<int> actual = source.TakeLast(count);

                Assert.Equal(expected, actual);

                Assert.Equal(expected.Length, actual.Count());
                Assert.Equal(expected, actual.ToArray());
                Assert.Equal(expected, actual.ToList());
                Assert.Equal(expected.FirstOrDefault(), actual.FirstOrDefault());
                Assert.Equal(expected.LastOrDefault(), actual.LastOrDefault());

                if (expected.Length > 0)
                {
                    Assert.Equal(expected[0], actual.ElementAt(0));
                    Assert.Equal(expected[^1], actual.ElementAt(expected.Length - 1));
                    Assert.Equal(expected[expected.Length / 2], actual.ElementAt(expected.Length / 2));
                }

                Assert.Equal(0, actual.ElementAtOrDefault(-1));
                Assert.Equal(0, actual.ElementAtOrDefault(actual.Count()));
            });
        }

        [Theory]
        [MemberData(nameof(EvaluationBehaviorData), MemberType = typeof(SkipTakeData))]
        public void EvaluationBehavior(int count)
        {
            int index = 0;
            int limit = count * 2;

            var source = new DelegateIterator<int>(
                moveNext: () => index++ != limit, // Stop once we go past the limit.
                current: () => index, // Yield from 1 up to the limit, inclusive.
                dispose: () => index ^= int.MinValue);

            using IEnumerator<int> iterator = source.TakeLast(count).GetEnumerator();
            Assert.Equal(0, index); // Nothing should be done before MoveNext is called.

            for (int i = 1; i <= count; i++)
            {
                Assert.True(iterator.MoveNext());
                Assert.Equal(count + i, iterator.Current);

                // After the first MoveNext call to the enumerator, everything should be evaluated and the enumerator
                // should be disposed.
                Assert.Equal(int.MinValue, index & int.MinValue);
                Assert.Equal(limit + 1, index & int.MaxValue);
            }

            Assert.False(iterator.MoveNext());

            // Unlike SkipLast, TakeLast can tell straightaway that it can return a sequence with no elements if count <= 0.
            // The enumerable it returns is a specialized empty iterator that has no connections to the source. Hence,
            // after MoveNext returns false under those circumstances, it won't invoke Dispose on our enumerator.
            int expected = count <= 0 ? 0 : int.MinValue;
            Assert.Equal(expected, index & int.MinValue);
        }

        [Theory]
        [MemberData(nameof(EnumerableData), MemberType = typeof(SkipTakeData))]
        public void RunOnce(IEnumerable<int> source, int count)
        {
            IEnumerable<int> expected = source.TakeLast(count);
            Assert.Equal(expected, source.TakeLast(count).RunOnce());
        }

        [Fact]
        public void List_ChangesAfterTakeLast_ChangesReflectedInResults()
        {
            var list = new List<int>() { 1, 2, 3, 4, 5 };

            IEnumerable<int> e = list.TakeLast(3);

            list.RemoveAt(0);
            list.RemoveAt(0);

            Assert.Equal(new[] { 3, 4, 5 }, e.ToArray());
        }

        [Fact]
        public void List_Skip_ChangesAfterTakeLast_ChangesReflectedInResults()
        {
            var list = new List<int>() { 1, 2, 3, 4, 5 };

            IEnumerable<int> e = list.Skip(1).TakeLast(3);

            list.RemoveAt(0);

            Assert.Equal(new[] { 3, 4, 5 }, e.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleElement_UnknownCount_ResultsAndReenumeration(bool useRange)
        {
            foreach (int count in new[] { 0, 1, 3 })
            {
                Check(Enumerable.Range(0, count).ToArray());
                Check(Enumerable.Range(0, count).Select(i => i.ToString()).ToArray());
                Check(Enumerable.Range(0, count).Select(i => (i.ToString(), i)).ToArray());
            }

            void Check<T>(T[] values)
            {
                T[] expected = values.Length == 0 ? [] : [values[^1]];
                IEnumerable<T> result = SingleElement(ForceNotCollection(values), useRange);
                Assert.Equal(expected, result);
                Assert.Equal(expected, result);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void SingleElement_ChangesBeforeMoveNextAreObserved(bool useRange, bool knownCount)
        {
            var values = new List<int> { 1 };
            IEnumerable<int> result = SingleElement(knownCount ? values : ForceNotCollection(values), useRange);
            using IEnumerator<int> iterator = result.GetEnumerator();
            values.Add(2);
            Assert.True(iterator.MoveNext());
            Assert.Equal(2, iterator.Current);
            Assert.False(iterator.MoveNext());
            values.Add(3);
            Assert.Equal(new[] { 3 }, result);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 3)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        [InlineData(true, 3)]
        public void SingleElement_UnknownCount_EvaluationOrder(bool useRange, int count)
        {
            var calls = new List<string>();
            int index = -1;
            var inner = new DelegateIterator<int>(
                moveNext: () =>
                {
                    calls.Add(nameof(IEnumerator<int>.MoveNext));
                    return ++index < count;
                },
                current: () =>
                {
                    calls.Add(nameof(IEnumerator<int>.Current));
                    return index;
                },
                dispose: () => calls.Add(nameof(IDisposable.Dispose)));
            var source = new DelegateIterator<int>(getEnumerator: () =>
            {
                calls.Add(nameof(IEnumerable<int>.GetEnumerator));
                return inner;
            });
            using IEnumerator<int> iterator = SingleElement(source, useRange).GetEnumerator();
            Assert.Empty(calls);
            Assert.Equal(count != 0, iterator.MoveNext());

            var expected = new List<string> { nameof(IEnumerable<int>.GetEnumerator) };
            for (int i = 0; i < count; i++)
            {
                expected.Add(nameof(IEnumerator<int>.MoveNext));
                expected.Add(nameof(IEnumerator<int>.Current));
            }

            expected.Add(nameof(IEnumerator<int>.MoveNext));
            expected.Add(nameof(IDisposable.Dispose));
            Assert.Equal(expected, calls);
            if (count != 0)
            {
                Assert.Equal(count - 1, iterator.Current);
                Assert.False(iterator.MoveNext());
                Assert.Equal(expected, calls);
            }
        }

        public static IEnumerable<object[]> SingleElementExceptionData()
        {
            foreach (bool useRange in new[] { false, true })
            {
                yield return new object[] { useRange, nameof(IEnumerable<int>.GetEnumerator), 0 };
                foreach (string operation in new[] { nameof(IEnumerator<int>.MoveNext), nameof(IEnumerator<int>.Current), nameof(IDisposable.Dispose) })
                {
                    yield return new object[] { useRange, operation, 0 };
                    yield return new object[] { useRange, operation, 1 };
                }
            }
        }

        [Theory]
        [MemberData(nameof(SingleElementExceptionData))]
        public void SingleElement_UnknownCount_SourceExceptions(bool useRange, string operation, int throwIndex)
        {
            var error = new InvalidOperationException("source");
            int index = -1;
            int disposeCount = 0;
            var inner = new DelegateIterator<int>(
                moveNext: () =>
                {
                    ++index;
                    if (operation == nameof(IEnumerator<int>.MoveNext) && index == throwIndex)
                    {
                        throw error;
                    }
                    return index < (operation == nameof(IDisposable.Dispose) ? throwIndex : 2);
                },
                current: () => operation == nameof(IEnumerator<int>.Current) && index == throwIndex ? throw error : index,
                dispose: () =>
                {
                    ++disposeCount;
                    if (operation == nameof(IDisposable.Dispose))
                    {
                        throw error;
                    }
                });
            var source = new DelegateIterator<int>(getEnumerator: () => operation == nameof(IEnumerable<int>.GetEnumerator) ? throw error : inner);
            using IEnumerator<int> iterator = SingleElement(source, useRange).GetEnumerator();
            Assert.Same(error, Assert.Throws<InvalidOperationException>(() => iterator.MoveNext()));
            Assert.Equal(operation == nameof(IEnumerable<int>.GetEnumerator) ? 0 : 1, disposeCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleElement_UnknownCount_DisposeExceptionReplacesSourceException(bool useRange)
        {
            var disposeError = new InvalidOperationException("dispose");
            var source = new DelegateIterator<int>(
                moveNext: () => throw new ArgumentException("source"),
                dispose: () => throw disposeError);
            using IEnumerator<int> iterator = SingleElement(source, useRange).GetEnumerator();
            Assert.Same(disposeError, Assert.Throws<InvalidOperationException>(() => iterator.MoveNext()));
        }

        private static IEnumerable<T> SingleElement<T>(IEnumerable<T> source, bool useRange) =>
            useRange ? source.Take(^1..^0) : source.TakeLast(1);
    }
}
