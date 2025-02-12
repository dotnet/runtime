// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace System.Linq.Tests
{
    public abstract class RangeIBinaryIntegerTests<T> : EnumerableTests where T : IBinaryInteger<T>
    {
#pragma warning disable xUnit1015

        //public static TheoryData<T, int> StartCountCorrectData;
        //public static TheoryData<T, int> StartCountIncorrectData;

        [Theory]
        [MemberData("StartCountCorrectData")]
        public void Range_ProduceCorrectSequence(T start, int count)
        {
            var range = Enumerable.Range(start, count);

            TestCorrectRangeResult(range, start, count);

            Assert.Equal(range, Enumerable.Range(start, count));

            // Not enumerate after the end
            using (var enumerator = range.GetEnumerator())
            {
                for (var i = 0; i < count; i++)
                    Assert.True(enumerator.MoveNext());
                for (var i = 0; i < 100; i++)
                    Assert.False(enumerator.MoveNext());
            }
        }

        [Theory]
        [MemberData("StartCountIncorrectData")]
        public void Range_IncorrectStartCount(T start, int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Enumerable.Range(start, count));
            TestCorrectRangeResult(Enumerable.Range(start, 0), start, 0);
            TestCorrectRangeResult(Enumerable.Range(start, 1), start, 1);
        }

        [Theory]
        [MemberData("StartCountCorrectData")]
        public void Range_GetEnumeratorSame(T start, int count)
        {
            var range = Enumerable.Range(start, count);
            if (count <= 0)
            {
                Assert.IsType<T[]>(range);
                return;
            }

            using (var enumerator = range.GetEnumerator())
            {
                Assert.Same(range, enumerator);
            }
        }

        [Theory]
        [MemberData("StartCountCorrectData")]
        public void Range_GetEnumeratorUnique(T start, int count)
        {
            var range = Enumerable.Range(start, count);
            if (count <= 0)
            {
                Assert.IsType<T[]>(range);
                return;
            }

            using (var enum1 = range.GetEnumerator())
            using (var enum2 = range.GetEnumerator())
            {
                Assert.NotSame(enum1, enum2);
            }
        }


        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        [MemberData("StartCountCorrectData")]
        public void Range_SpeedOpt(T start, int count)
        {
            var range = Enumerable.Range(start, count);
            TestCorrectSpeedOptRange(range, start, count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        [MemberData("StartCountCorrectData")]
        public void Range_Skip(T start, int count)
        {
            var range = Enumerable.Range(start, count).Skip(count / 2);
            start += T.CreateTruncating(count / 2);
            count -= count / 2;
            TestCorrectSpeedOptRange(range, start, count);

            if (count > 1)
            {
                TestCorrectSpeedOptRange(range.Skip(count - 1), start + T.CreateTruncating(count - 1), 1);
                TestCorrectSpeedOptRange(range.Take(1), start, 1);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        [MemberData("StartCountCorrectData")]
        public void Range_Take(T start, int count)
        {
            var range = Enumerable.Range(start, count).Take(count / 2);
            count = count / 2;
            TestCorrectSpeedOptRange(range, start, count);

            if (count > 1)
            {
                TestCorrectSpeedOptRange(range.Skip(count - 1), start + T.CreateTruncating(count - 1), 1);
                TestCorrectSpeedOptRange(range.Take(1), start, 1);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        [MemberData("StartCountCorrectData")]
        public void Range_IListImplementationIsValid(T start, int count)
        {
            var range = Enumerable.Range(start, count);
            if (count <= 0)
            {
                Assert.IsType<T[]>(range);
                return;

            }

            var expected = range.ToArray();
            IList<T> list = Assert.IsAssignableFrom<IList<T>>(range);
            IReadOnlyList<T> roList = Assert.IsAssignableFrom<IReadOnlyList<T>>(range);

            Assert.Throws<NotSupportedException>(() => list.Add(default));
            Assert.Throws<NotSupportedException>(() => list.Insert(0, default));
            Assert.Throws<NotSupportedException>(list.Clear);
            Assert.Throws<NotSupportedException>(() => list.Remove(default));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = default);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[expected.Length]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => roList[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => roList[expected.Length]);

            Assert.True(list.IsReadOnly);
            Assert.Equal(expected.Length, list.Count);
            Assert.Equal(expected.Length, roList.Count);

            if (expected.Length > 0)
            {
                if (expected[0] - T.One < expected[0])
                {
                    Assert.False(list.Contains(expected[0] - T.One));
                    Assert.False(roList.Contains(expected[0] - T.One));

                    Assert.Equal(-1, list.IndexOf(expected[0] - T.One));
                    //Assert.Equal(-1, roList.IndexOf(expected[0] - T.One));
                }

                if (expected[^1] + T.One > expected[^1])
                {
                    Assert.False(list.Contains(expected[^1] + T.One));
                    Assert.False(roList.Contains(expected[^1] + T.One));

                    Assert.Equal(-1, list.IndexOf(expected[^1] + T.One));
                    //Assert.Equal(-1, roList.IndexOf(expected[^1] + T.One));
                }
            }
            else
            {
                Assert.False(list.Contains(default));
                Assert.False(roList.Contains(default));

                Assert.Equal(-1, list.IndexOf(default));
                //Assert.Equal(-1, roList.IndexOf(default));
            }

            Assert.All(expected, item => Assert.True(list.Contains(item)));
            Assert.All(expected, item => Assert.True(roList.Contains(item)));

            Assert.All(expected, item => Assert.Equal(Array.IndexOf(expected, item), list.IndexOf(item)));
            //Assert.All(expected, item => Assert.Equal(Array.IndexOf(expected, item), roList.IndexOf(item)));

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], list[i]);
                Assert.Equal(expected[i], roList[i]);
            }

            T[] actual = new T[expected.Length + 2];
            list.CopyTo(actual, 1);
            Assert.Equal(default, actual[0]);
            Assert.Equal(default, actual[^1]);
            Assert.Equal(expected, actual.AsSpan(1, expected.Length));
        }

        private void TestCorrectRangeResult(IEnumerable<T> rangeResult, T expectedStart, int expectedCount)
        {
            T expected = expectedStart;
            int count = 0;
            foreach (var val in rangeResult)
            {
                Assert.Equal(expected, val);
                expected++;
                count++;
            }
            Assert.Equal(expectedCount, count);
            Assert.Equal(expectedCount, rangeResult.Count());
        }

        private void TestCorrectSpeedOptRange(IEnumerable<T> range, T expectedStart, int expectedCount)
        {
            TestCorrectRangeResult(range, expectedStart, expectedCount);

            {
                var select1 = range.Select(item => item - expectedStart);
                // Should be first after creating
                if (expectedCount <= 0)
                {
                    Assert.IsType<T[]>(select1);
                }
                else
                {
                    using (var enum1 = select1.GetEnumerator())
                    using (var enum2 = select1.GetEnumerator())
                    {
                        Assert.Same(select1, enum1);
                        Assert.NotSame(enum1, enum2);
                    }
                }

                TestCorrectRangeSelect(select1, T.Zero, expectedCount);

                var select2 = select1.Select(item => item - expectedStart);
                // Should be first after creating
                if (expectedCount <= 0)
                {
                    Assert.IsType<T[]>(select2);
                }
                else
                {
                    using (var enum1 = select2.GetEnumerator())
                    using (var enum2 = select2.GetEnumerator())
                    {
                        Assert.Same(select2, enum1);
                        Assert.NotSame(enum1, enum2);
                    }
                }

                TestCorrectRangeSelect(select2, T.Zero - expectedStart, expectedCount);

                TestCorrectRangeSelect(select2.Skip(expectedCount / 2), T.Zero - expectedStart + T.CreateTruncating(expectedCount / 2), expectedCount - expectedCount / 2);
                TestCorrectRangeSelect(select2.Take(expectedCount / 2), T.Zero - expectedStart, expectedCount / 2);

                if (expectedCount > 1)
                {
                    TestCorrectRangeSelect(select2.Skip(expectedCount - 1), T.Zero - expectedStart + T.CreateTruncating(expectedCount - 1), 1);
                    TestCorrectRangeSelect(select2.Take(1), T.Zero - expectedStart, 1);
                }
            }

            TestCorrectRangeResult(range.ToArray(), expectedStart, expectedCount);
            TestCorrectRangeResult(range.ToList(), expectedStart, expectedCount);

            if (expectedCount == 0)
            {
                Assert.Empty(range);

                Assert.Equal(default, range.FirstOrDefault());
                Assert.Equal(default, range.LastOrDefault());
                Assert.Equal(expectedStart - T.One, range.FirstOrDefault(expectedStart - T.One));
                Assert.Equal(expectedStart - T.One, range.LastOrDefault(expectedStart - T.One));
            }
            else
            {
                if (expectedCount == 1)
                    Assert.Single(range);

                Assert.Contains(expectedStart, range);
                Assert.Contains(expectedStart + T.CreateTruncating(expectedCount / 2), range);
                Assert.Contains(expectedStart + T.CreateTruncating(expectedCount - 1), range);

                Assert.Equal(expectedStart, range.ElementAt(0));
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount / 2), range.ElementAt(expectedCount / 2));
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), range.ElementAt(expectedCount - 1));

                Assert.Equal(expectedStart, range.ElementAtOrDefault(0));
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount / 2), range.ElementAtOrDefault(expectedCount / 2));
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), range.ElementAtOrDefault(expectedCount - 1));

                Assert.Equal(expectedStart, range.First());
                Assert.Equal(expectedStart, range.FirstOrDefault());
                Assert.Equal(expectedStart, range.FirstOrDefault(expectedStart - T.One));

                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), range.Last());
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), range.LastOrDefault());
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), range.LastOrDefault(expectedStart - T.One));
            }

            Assert.Same(range, range.Skip(0));
            Assert.Same(range, range.Skip(-1));
            Assert.Same(range, range.Skip(int.MinValue));
            Assert.Empty(range.Skip(expectedCount));
            Assert.Empty(range.Skip(expectedCount + 1));
            Assert.Empty(range.Skip(int.MaxValue));

            Assert.Same(range, range.Take(expectedCount));
            Assert.Same(range, range.Take(expectedCount + 1));
            Assert.Same(range, range.Take(int.MaxValue));
            Assert.Empty(range.Take(0));
            Assert.Empty(range.Take(-1));
            Assert.Empty(range.Take(int.MinValue));

            if (expectedStart - T.One < expectedStart)
                Assert.DoesNotContain(expectedStart - T.One, range);
            if (expectedStart + T.CreateTruncating(expectedCount) > expectedStart)
                Assert.DoesNotContain(expectedStart + T.CreateTruncating(expectedCount), range);

            Assert.Equal(default, range.ElementAtOrDefault(-1));
            Assert.Equal(default, range.ElementAtOrDefault(int.MinValue));
            Assert.Equal(default, range.ElementAtOrDefault(expectedCount));
            Assert.Equal(default, range.ElementAtOrDefault(int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => range.ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => range.ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => range.ElementAt(expectedCount));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => range.ElementAt(int.MaxValue));
        }

        private void TestCorrectRangeSelect(IEnumerable<T> select, T expectedStart, int expectedCount)
        {
            TestCorrectRangeResult(select, expectedStart, expectedCount);
            TestCorrectRangeResult(select.ToArray(), expectedStart, expectedCount);
            TestCorrectRangeResult(select.ToList(), expectedStart, expectedCount);

            if (expectedCount == 0)
            {
                Assert.Empty(select);

                Assert.Equal(default, select.FirstOrDefault());
                Assert.Equal(default, select.LastOrDefault());
                Assert.Equal(expectedStart - T.One, select.FirstOrDefault(expectedStart - T.One));
                Assert.Equal(expectedStart - T.One, select.LastOrDefault(expectedStart - T.One));
            }
            else
            {
                if (expectedCount == 1)
                    Assert.Single(select);

                Assert.Equal(expectedStart, select.ElementAt(0));
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount / 2), select.ElementAt(expectedCount / 2));
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), select.ElementAt(expectedCount - 1));

                Assert.Equal(expectedStart, select.ElementAtOrDefault(0));
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount / 2), select.ElementAtOrDefault(expectedCount / 2));
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), select.ElementAtOrDefault(expectedCount - 1));

                Assert.Equal(expectedStart, select.First());
                Assert.Equal(expectedStart, select.FirstOrDefault());
                Assert.Equal(expectedStart, select.FirstOrDefault(expectedStart - T.One));

                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), select.Last());
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), select.LastOrDefault());
                Assert.Equal(expectedStart + T.CreateTruncating(expectedCount - 1), select.LastOrDefault(expectedStart - T.One));
            }

            Assert.Same(select, select.Skip(0));
            Assert.Same(select, select.Skip(-1));
            Assert.Same(select, select.Skip(int.MinValue));
            Assert.Empty(select.Skip(expectedCount));
            Assert.Empty(select.Skip(expectedCount + 1));
            Assert.Empty(select.Skip(int.MaxValue));

            Assert.Same(select, select.Take(expectedCount));
            Assert.Same(select, select.Take(expectedCount + 1));
            Assert.Same(select, select.Take(int.MaxValue));
            Assert.Empty(select.Take(0));
            Assert.Empty(select.Take(-1));
            Assert.Empty(select.Take(int.MinValue));

            Assert.Equal(default, select.ElementAtOrDefault(-1));
            Assert.Equal(default, select.ElementAtOrDefault(int.MinValue));
            Assert.Equal(default, select.ElementAtOrDefault(expectedCount));
            Assert.Equal(default, select.ElementAtOrDefault(int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => select.ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => select.ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => select.ElementAt(expectedCount));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => select.ElementAt(int.MaxValue));

        }

#pragma warning restore xUnit1015
    }

    public class RangeByteTests : RangeIBinaryIntegerTests<byte>
    {
        public static TheoryData<byte, int> StartCountCorrectData { get; } = new TheoryData<byte, int>()
        {
            { 0, 256 },
            { 0, 15 },
            { 0, 1 },
            { 0, 0 },

            { 63, 256 - 63 },
            { 63, 15 },
            { 63, 1 },
            { 63, 0 },

            { 128, 128 },
            { 128, 15 },
            { 128, 1 },
            { 128, 0 },

            { 255 - 14, 15 },
            { 255, 1 },
            { 255, 0 },
        };

        public static TheoryData<byte, int> StartCountIncorrectData { get; } = new TheoryData<byte, int>()
        {
            { 0, byte.MaxValue + 2 },
            { 0, byte.MaxValue + 64 },
            { 0, byte.MaxValue + 128 },
            { 0, byte.MaxValue + 172 },
            { 0, byte.MaxValue + byte.MaxValue },
            { 0, int.MaxValue },
            { 0, -1 },
            { 0, int.MinValue },

            { 1, byte.MaxValue + 1 },
            { 1, byte.MaxValue + 64 },
            { 1, byte.MaxValue + 128 },
            { 1, byte.MaxValue + 172 },
            { 1, byte.MaxValue + byte.MaxValue },
            { 1, int.MaxValue },
            { 1, -1 },
            { 1, int.MinValue },

            { 127, 130 },
            { 127, byte.MaxValue + 64 },
            { 127, byte.MaxValue + 128 },
            { 127, byte.MaxValue + 172 },
            { 127, byte.MaxValue + byte.MaxValue },
            { 127, int.MaxValue },
            { 127, -1 },
            { 127, int.MinValue },

            { 128, 129 },
            { 128, byte.MaxValue + 64 },
            { 128, byte.MaxValue + 128 },
            { 128, byte.MaxValue + 172 },
            { 128, byte.MaxValue + byte.MaxValue },
            { 128, int.MaxValue },
            { 128, -1 },
            { 128, int.MinValue },

            { 129, 128 },
            { 129, byte.MaxValue + 64 },
            { 129, byte.MaxValue + 128 },
            { 129, byte.MaxValue + 172 },
            { 129, byte.MaxValue + byte.MaxValue },
            { 129, int.MaxValue },
            { 129, -1 },
            { 129, int.MinValue },

            { 254, 3 },
            { 254, byte.MaxValue + 64 },
            { 254, byte.MaxValue + 128 },
            { 254, byte.MaxValue + 172 },
            { 254, byte.MaxValue + byte.MaxValue },
            { 254, int.MaxValue },
            { 254, -1 },
            { 254, int.MinValue },

            { 255, 2 },
            { 255, byte.MaxValue + 64 },
            { 255, byte.MaxValue + 128 },
            { 255, byte.MaxValue + 172 },
            { 255, byte.MaxValue + byte.MaxValue },
            { 255, int.MaxValue },
            { 255, -1 },
            { 255, int.MinValue },
        };
    }

    public class RangeSByteTests : RangeIBinaryIntegerTests<sbyte>
    {
        public static TheoryData<sbyte, int> StartCountCorrectData { get; } = new TheoryData<sbyte, int>()
        {
            { sbyte.MinValue, 256 },
            { sbyte.MinValue, 15 },
            { sbyte.MinValue, 1 },
            { sbyte.MinValue, 0 },
        
            { sbyte.MinValue / 2, 128 - sbyte.MinValue / 2 },
            { sbyte.MinValue / 2, 15 },
            { sbyte.MinValue / 2, 1 },
            { sbyte.MinValue / 2, 0 },

            { -1, 129 },
            { -1, 15 },
            { -1, 1 },
            { -1, 0 },

            { sbyte.MaxValue - 14, 15 },
            { sbyte.MaxValue, 1 },
            { sbyte.MaxValue, 0 },
        };

        public static TheoryData<sbyte, int> StartCountIncorrectData { get; } = new TheoryData<sbyte, int>()
        {
            { -128, byte.MaxValue + 2 },
            { -128, byte.MaxValue + 64 },
            { -128, byte.MaxValue + 128 },
            { -128, byte.MaxValue + 172 },
            { -128, byte.MaxValue + byte.MaxValue },
            { -128, int.MaxValue },
            { -128, -1 },
            { -128, int.MinValue },

            { -127, byte.MaxValue + 1},
            { -127, byte.MaxValue + 64 },
            { -127, byte.MaxValue + 128 },
            { -127, byte.MaxValue + 172 },
            { -127, byte.MaxValue + byte.MaxValue },
            { -127, int.MaxValue },
            { -127, -1 },
            { -127, int.MinValue },

            { -1, byte.MaxValue + 3 },
            { -1, byte.MaxValue + 64 },
            { -1, byte.MaxValue + 128 },
            { -1, byte.MaxValue + 172 },
            { -1, byte.MaxValue + byte.MaxValue },
            { -1, int.MaxValue },
            { -1, -1 },
            { -1, int.MinValue },

            { 0, byte.MaxValue + 2 },
            { 0, byte.MaxValue + 64 },
            { 0, byte.MaxValue + 128 },
            { 0, byte.MaxValue + 172 },
            { 0, byte.MaxValue + byte.MaxValue },
            { 0, int.MaxValue },
            { 0, -1 },
            { 0, int.MinValue },

            { 1, byte.MaxValue + 1 },
            { 1, byte.MaxValue + 64 },
            { 1, byte.MaxValue + 128 },
            { 1, byte.MaxValue + 172 },
            { 1, byte.MaxValue + byte.MaxValue },
            { 1, int.MaxValue },
            { 1, -1 },
            { 1, int.MinValue },

            { 126, 3 },
            { 126, byte.MaxValue + 64 },
            { 126, byte.MaxValue + 128 },
            { 126, byte.MaxValue + 172 },
            { 126, byte.MaxValue + byte.MaxValue },
            { 126, int.MaxValue },
            { 126, -1 },
            { 126, int.MinValue },

            { 127, 2 },
            { 127, byte.MaxValue + 64 },
            { 127, byte.MaxValue + 128 },
            { 127, byte.MaxValue + 172 },
            { 127, byte.MaxValue + byte.MaxValue },
            { 127, int.MaxValue },
            { 127, -1 },
            { 127, int.MinValue },
        };
    }

    public class RangeUshortTests : RangeIBinaryIntegerTests<ushort>
    {
        public static TheoryData<ushort, int> StartCountCorrectData { get; } = new TheoryData<ushort, int>()
        {
            { 0, 15 },
            { 0, 1 },
            { 0, 0 },

            { ushort.MaxValue / 2, 15 },
            { ushort.MaxValue / 2, 1 },
            { ushort.MaxValue / 2, 0 },

            { ushort.MaxValue - 14, 15 },
            { ushort.MaxValue, 1 },
            { ushort.MaxValue, 0 },
        };

        public static TheoryData<ushort, int> StartCountIncorrectData { get; } = new TheoryData<ushort, int>()
        {
            { 0, ushort.MaxValue + 2 },
            { 0, ushort.MaxValue + ushort.MaxValue / 4 },
            { 0, ushort.MaxValue + ushort.MaxValue / 4 },
            { 0, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { 0, ushort.MaxValue + ushort.MaxValue },
            { 0, int.MaxValue },
            { 0, -1 },
            { 0, int.MinValue },

            { 1, ushort.MaxValue + 1},
            { 1, ushort.MaxValue + ushort.MaxValue / 4 },
            { 1, ushort.MaxValue + ushort.MaxValue / 4 },
            { 1, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { 1, ushort.MaxValue + ushort.MaxValue },
            { 1, int.MaxValue },
            { 1, -1 },
            { 1, int.MinValue },

            { ushort.MaxValue / 2, ushort.MaxValue - ushort.MaxValue / 2 + 2 },
            { ushort.MaxValue / 2, ushort.MaxValue + ushort.MaxValue / 4 },
            { ushort.MaxValue / 2, ushort.MaxValue + ushort.MaxValue / 2 },
            { ushort.MaxValue / 2, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { ushort.MaxValue / 2, ushort.MaxValue + ushort.MaxValue },
            { ushort.MaxValue / 2, int.MaxValue },
            { ushort.MaxValue / 2, -1 },
            { ushort.MaxValue / 2, int.MinValue },

            { ushort.MaxValue / 2 + 1, ushort.MaxValue - ushort.MaxValue / 2 + 1 },
            { ushort.MaxValue / 2 + 1, ushort.MaxValue + ushort.MaxValue / 4 },
            { ushort.MaxValue / 2 + 1, ushort.MaxValue + ushort.MaxValue / 2 },
            { ushort.MaxValue / 2 + 1, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { ushort.MaxValue / 2 + 1, ushort.MaxValue + ushort.MaxValue },
            { ushort.MaxValue / 2 + 1, int.MaxValue },
            { ushort.MaxValue / 2 + 1, -1 },
            { ushort.MaxValue / 2 + 1, int.MinValue },

            { ushort.MaxValue / 2 + 2, ushort.MaxValue - ushort.MaxValue / 2 },
            { ushort.MaxValue / 2 + 2, ushort.MaxValue + ushort.MaxValue / 4 },
            { ushort.MaxValue / 2 + 2, ushort.MaxValue + ushort.MaxValue / 2 },
            { ushort.MaxValue / 2 + 2, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { ushort.MaxValue / 2 + 2, ushort.MaxValue + ushort.MaxValue },
            { ushort.MaxValue / 2 + 2, int.MaxValue },
            { ushort.MaxValue / 2 + 2, -1 },
            { ushort.MaxValue / 2 + 2, int.MinValue },

            { ushort.MaxValue - 1, 3 },
            { ushort.MaxValue - 1, ushort.MaxValue + ushort.MaxValue / 4 },
            { ushort.MaxValue - 1, ushort.MaxValue + ushort.MaxValue / 2 },
            { ushort.MaxValue - 1, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { ushort.MaxValue - 1, ushort.MaxValue + ushort.MaxValue },
            { ushort.MaxValue - 1, int.MaxValue },
            { ushort.MaxValue - 1, -1 },
            { ushort.MaxValue - 1, int.MinValue },

            { ushort.MaxValue, 2 },
            { ushort.MaxValue, ushort.MaxValue + ushort.MaxValue / 4 },
            { ushort.MaxValue, ushort.MaxValue + ushort.MaxValue / 2 },
            { ushort.MaxValue, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { ushort.MaxValue, ushort.MaxValue + ushort.MaxValue },
            { ushort.MaxValue, int.MaxValue },
            { ushort.MaxValue, -1 },
            { ushort.MaxValue, int.MinValue },
        };
    }

    public class RangeShortTests : RangeIBinaryIntegerTests<short>
    {
        public static TheoryData<short, int> StartCountCorrectData { get; } = new TheoryData<short, int>()
        {
            { short.MinValue, 15 },
            { short.MinValue, 1 },
            { short.MinValue, 0 },


            { -1, 15 },
            { -1, 1 },
            { -1, 0 },

            { short.MaxValue - 14, 15 },
            { short.MaxValue, 1 },
            { short.MaxValue, 0 },
        };

        public static TheoryData<short, int> StartCountIncorrectData { get; } = new TheoryData<short, int>()
        {
            { short.MinValue, ushort.MaxValue + 2 },
            { short.MinValue, ushort.MaxValue + 64 },
            { short.MinValue, ushort.MaxValue + 128 },
            { short.MinValue, ushort.MaxValue + 172 },
            { short.MinValue, ushort.MaxValue + short.MaxValue },
            { short.MinValue, int.MaxValue },
            { short.MinValue, -1 },
            { short.MinValue, int.MinValue },

            { short.MinValue + 1, ushort.MaxValue + 1},
            { short.MinValue + 1, ushort.MaxValue + 64 },
            { short.MinValue + 1, ushort.MaxValue + 128 },
            { short.MinValue + 1, ushort.MaxValue + 172 },
            { short.MinValue + 1, ushort.MaxValue + ushort.MaxValue },
            { short.MinValue + 1, int.MaxValue },
            { short.MinValue + 1, -1 },
            { short.MinValue + 1, int.MinValue },

            { -1, short.MaxValue + 3 },
            { -1, ushort.MaxValue + 64 },
            { -1, ushort.MaxValue + 128 },
            { -1, ushort.MaxValue + 172 },
            { -1, ushort.MaxValue + ushort.MaxValue },
            { -1, int.MaxValue },
            { -1, -1 },
            { -1, int.MinValue },

            { 0, short.MaxValue + 2 },
            { 0, ushort.MaxValue + 64 },
            { 0, ushort.MaxValue + 128 },
            { 0, ushort.MaxValue + 172 },
            { 0, ushort.MaxValue + ushort.MaxValue },
            { 0, int.MaxValue },
            { 0, -1 },
            { 0, int.MinValue },

            { 1, short.MaxValue + 1 },
            { 1, ushort.MaxValue + 64 },
            { 1, ushort.MaxValue + 128 },
            { 1, ushort.MaxValue + 172 },
            { 1, ushort.MaxValue + ushort.MaxValue },
            { 1, int.MaxValue },
            { 1, -1 },
            { 1, int.MinValue },

            { short.MaxValue - 1, 3 },
            { short.MaxValue - 1, ushort.MaxValue + 64 },
            { short.MaxValue - 1, ushort.MaxValue + 128 },
            { short.MaxValue - 1, ushort.MaxValue + 172 },
            { short.MaxValue - 1, ushort.MaxValue + ushort.MaxValue },
            { short.MaxValue - 1, int.MaxValue },
            { short.MaxValue - 1, -1 },
            { short.MaxValue - 1, int.MinValue },

            { short.MaxValue, 2 },
            { short.MaxValue, ushort.MaxValue + 64 },
            { short.MaxValue, ushort.MaxValue + 128 },
            { short.MaxValue, ushort.MaxValue + 172 },
            { short.MaxValue, ushort.MaxValue + ushort.MaxValue },
            { short.MaxValue, int.MaxValue },
            { short.MaxValue, -1 },
            { short.MaxValue, int.MinValue },
        };
    }

    public class RangeCharTests : RangeIBinaryIntegerTests<char>
    {
        public static TheoryData<char, int> StartCountCorrectData { get; } = new TheoryData<char, int>()
        {
            { (char)0, 15 },
            { (char)0, 1 },
            { (char)0, 0 },

            { (char)(ushort.MaxValue / 2), 15 },
            { (char)(ushort.MaxValue / 2), 1 },
            { (char)(ushort.MaxValue / 2), 0 },

            { (char)(ushort.MaxValue - 14), 15 },
            { (char)ushort.MaxValue, 1 },
            { (char)ushort.MaxValue, 0 },
        };

        public static TheoryData<char, int> StartCountIncorrectData { get; } = new TheoryData<char, int>()
        {
            { (char)0, ushort.MaxValue + 2 },
            { (char)0, ushort.MaxValue + ushort.MaxValue / 4 },
            { (char)0, ushort.MaxValue + ushort.MaxValue / 4 },
            { (char)0, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { (char)0, ushort.MaxValue + ushort.MaxValue },
            { (char)0, int.MaxValue },
            { (char)0, -1 },
            { (char)0, int.MinValue },

            { (char)1, ushort.MaxValue + 1},
            { (char)1, ushort.MaxValue + ushort.MaxValue / 4 },
            { (char)1, ushort.MaxValue + ushort.MaxValue / 4 },
            { (char)1, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { (char)1, ushort.MaxValue + ushort.MaxValue },
            { (char)1, int.MaxValue },
            { (char)1, -1 },
            { (char)1, int.MinValue },

            { (char)(ushort.MaxValue / 2), ushort.MaxValue - ushort.MaxValue / 2 + 2 },
            { (char)(ushort.MaxValue / 2), ushort.MaxValue + ushort.MaxValue / 4 },
            { (char)(ushort.MaxValue / 2), ushort.MaxValue + ushort.MaxValue / 2 },
            { (char)(ushort.MaxValue / 2), ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { (char)(ushort.MaxValue / 2), ushort.MaxValue + ushort.MaxValue },
            { (char)(ushort.MaxValue / 2), int.MaxValue },
            { (char)(ushort.MaxValue / 2), -1 },
            { (char)(ushort.MaxValue / 2), int.MinValue },

            { (char)(ushort.MaxValue / 2 + 1), ushort.MaxValue - ushort.MaxValue / 2 + 1 },
            { (char)(ushort.MaxValue / 2 + 1), ushort.MaxValue + ushort.MaxValue / 4 },
            { (char)(ushort.MaxValue / 2 + 1), ushort.MaxValue + ushort.MaxValue / 2 },
            { (char)(ushort.MaxValue / 2 + 1), ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { (char)(ushort.MaxValue / 2 + 1), ushort.MaxValue + ushort.MaxValue },
            { (char)(ushort.MaxValue / 2 + 1), int.MaxValue },
            { (char)(ushort.MaxValue / 2 + 1), -1 },
            { (char)(ushort.MaxValue / 2 + 1), int.MinValue },

            { (char)(ushort.MaxValue / 2 + 2), ushort.MaxValue - ushort.MaxValue / 2 },
            { (char)(ushort.MaxValue / 2 + 2), ushort.MaxValue + ushort.MaxValue / 4 },
            { (char)(ushort.MaxValue / 2 + 2), ushort.MaxValue + ushort.MaxValue / 2 },
            { (char)(ushort.MaxValue / 2 + 2), ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { (char)(ushort.MaxValue / 2 + 2), ushort.MaxValue + ushort.MaxValue },
            { (char)(ushort.MaxValue / 2 + 2), int.MaxValue },
            { (char)(ushort.MaxValue / 2 + 2), -1 },
            { (char)(ushort.MaxValue / 2 + 2), int.MinValue },

            { (char)(ushort.MaxValue - 1), 3 },
            { (char)(ushort.MaxValue - 1), ushort.MaxValue + ushort.MaxValue / 4 },
            { (char)(ushort.MaxValue - 1), ushort.MaxValue + ushort.MaxValue / 2 },
            { (char)(ushort.MaxValue - 1), ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { (char)(ushort.MaxValue - 1), ushort.MaxValue + ushort.MaxValue },
            { (char)(ushort.MaxValue - 1), int.MaxValue },
            { (char)(ushort.MaxValue - 1), -1 },
            { (char)(ushort.MaxValue - 1), int.MinValue },

            { (char)ushort.MaxValue, 2 },
            { (char)ushort.MaxValue, ushort.MaxValue + ushort.MaxValue / 4 },
            { (char)ushort.MaxValue, ushort.MaxValue + ushort.MaxValue / 2 },
            { (char)ushort.MaxValue, ushort.MaxValue + ushort.MaxValue * 3 / 4 },
            { (char)ushort.MaxValue, ushort.MaxValue + ushort.MaxValue },
            { (char)ushort.MaxValue, int.MaxValue },
            { (char)ushort.MaxValue, -1 },
            { (char)ushort.MaxValue, int.MinValue },
        };
    }

    public class RangeUintTests : RangeIBinaryIntegerTests<uint>
    {
        public static TheoryData<uint, int> StartCountCorrectData { get; } = new TheoryData<uint, int>()
        {
            { 0, 15 },
            { 0, 1 },
            { 0, 0 },

            { uint.MaxValue / 2, 15 },
            { uint.MaxValue / 2, 1 },
            { uint.MaxValue / 2, 0 },

            { uint.MaxValue - 14, 15 },
            { uint.MaxValue, 1 },
            { uint.MaxValue, 0 },
        };

        public static TheoryData<uint, int> StartCountIncorrectData { get; } = new TheoryData<uint, int>()
        {
            { 0, -1 },
            { 0, int.MinValue },

            { 1, -1 },
            { 1, int.MinValue },

            { uint.MaxValue / 2, -1 },
            { uint.MaxValue / 2, int.MinValue },

            { uint.MaxValue - int.MaxValue + 2, int.MaxValue },
            { uint.MaxValue - int.MaxValue + 2, -1 },
            { uint.MaxValue - int.MaxValue + 2, int.MinValue },

            { uint.MaxValue - int.MaxValue / 2, int.MaxValue / 2 + 2 },
            { uint.MaxValue - int.MaxValue / 2, int.MaxValue },
            { uint.MaxValue - int.MaxValue / 2, -1 },
            { uint.MaxValue - int.MaxValue / 2, int.MinValue },

            { uint.MaxValue - 1, 3 },
            { uint.MaxValue - 1, int.MaxValue / 4 },
            { uint.MaxValue - 1, int.MaxValue / 2 },
            { uint.MaxValue - 1, int.MaxValue },
            { uint.MaxValue - 1, -1 },
            { uint.MaxValue - 1, int.MinValue },

            { uint.MaxValue, 2 },
            { uint.MaxValue, int.MaxValue / 4 },
            { uint.MaxValue, int.MaxValue / 2 },
            { uint.MaxValue, int.MaxValue },
            { uint.MaxValue, -1 },
            { uint.MaxValue, int.MinValue },
        };
    }

    public class RangeIntTests : RangeIBinaryIntegerTests<int>
    {
        public static TheoryData<int, int> StartCountCorrectData { get; } = new TheoryData<int, int>()
        {
            { int.MinValue, 15 },
            { int.MinValue, 1 },
            { int.MinValue, 0 },


            { -1, 15 },
            { -1, 1 },
            { -1, 0 },

            { int.MaxValue - 14, 15 },
            { int.MaxValue, 1 },
            { int.MaxValue, 0 },
        };

        public static TheoryData<int, int> StartCountIncorrectData { get; } = new TheoryData<int, int>()
        {
            { int.MinValue, -1 },
            { int.MinValue, int.MinValue },

            { int.MinValue + 1, -1 },
            { int.MinValue + 1, int.MinValue },

            { int.MinValue / 2, -1 },
            { int.MinValue / 2, int.MinValue },

            { 0, -1 },
            { 0, int.MinValue },

            { 2, int.MaxValue },
            { 2, -1 },
            { 2, int.MinValue },

            { int.MaxValue / 2, int.MaxValue - int.MaxValue / 2 + 2},
            { int.MaxValue / 2, int.MaxValue},
            { int.MaxValue / 2, -1 },
            { int.MaxValue / 2, int.MinValue },

            { int.MaxValue - 1, 3 },
            { int.MaxValue - 1, int.MaxValue },
            { int.MaxValue - 1, -1 },
            { int.MaxValue - 1, int.MinValue },

            { int.MaxValue, 2 },
            { int.MaxValue, int.MaxValue },
            { int.MaxValue, -1 },
            { int.MaxValue, int.MinValue },
        };
    }

    public class RangeUlongTests : RangeIBinaryIntegerTests<ulong>
    {
        public static TheoryData<ulong, int> StartCountCorrectData { get; } = new TheoryData<ulong, int>()
        {
            { 0, 15 },
            { 0, 1 },
            { 0, 0 },

            { ulong.MaxValue / 2, 15 },
            { ulong.MaxValue / 2, 1 },
            { ulong.MaxValue / 2, 0 },

            { ulong.MaxValue - 14, 15 },
            { ulong.MaxValue, 1 },
            { ulong.MaxValue, 0 },
        };

        public static TheoryData<ulong, int> StartCountIncorrectData { get; } = new TheoryData<ulong, int>()
        {
            { 0, -1 },
            { 0, int.MinValue },

            { 1, -1 },
            { 1, int.MinValue },

            { ulong.MaxValue / 2, -1 },
            { ulong.MaxValue / 2, int.MinValue },

            { ulong.MaxValue - int.MaxValue + 2, int.MaxValue },
            { ulong.MaxValue - int.MaxValue + 2, -1 },
            { ulong.MaxValue - int.MaxValue + 2, int.MinValue },

            { ulong.MaxValue - int.MaxValue / 2, int.MaxValue / 2 + 2 },
            { ulong.MaxValue - int.MaxValue / 2, int.MaxValue },
            { ulong.MaxValue - int.MaxValue / 2, -1 },
            { ulong.MaxValue - int.MaxValue / 2, int.MinValue },

            { ulong.MaxValue - 1, 3 },
            { ulong.MaxValue - 1, int.MaxValue / 4 },
            { ulong.MaxValue - 1, int.MaxValue / 2 },
            { ulong.MaxValue - 1, int.MaxValue },
            { ulong.MaxValue - 1, -1 },
            { ulong.MaxValue - 1, int.MinValue },

            { ulong.MaxValue, 2 },
            { ulong.MaxValue, int.MaxValue / 4 },
            { ulong.MaxValue, int.MaxValue / 2 },
            { ulong.MaxValue, int.MaxValue },
            { ulong.MaxValue, -1 },
            { ulong.MaxValue, int.MinValue },
        };
    }

    public class RangeLongTests : RangeIBinaryIntegerTests<long>
    {
        public static TheoryData<long, int> StartCountCorrectData { get; } = new TheoryData<long, int>()
        {
            { long.MinValue, 15 },
            { long.MinValue, 1 },
            { long.MinValue, 0 },


            { -1, 15 },
            { -1, 1 },
            { -1, 0 },

            { long.MaxValue - 14, 15 },
            { long.MaxValue, 1 },
            { long.MaxValue, 0 },
        };

        public static TheoryData<long, int> StartCountIncorrectData { get; } = new TheoryData<long, int>()
        {
            { long.MinValue, -1 },
            { long.MinValue, int.MinValue },

            { long.MinValue + 1, -1 },
            { long.MinValue + 1, int.MinValue },

            { long.MinValue / 2, -1 },
            { long.MinValue / 2, int.MinValue },

            { 0, -1 },
            { 0, int.MinValue },

            { 2, -1 },
            { 2, int.MinValue },

            { long.MaxValue / 2, -1 },
            { long.MaxValue / 2, int.MinValue },

            { long.MaxValue - int.MaxValue + 2, int.MaxValue },
            { long.MaxValue - int.MaxValue + 2, -1 },
            { long.MaxValue - int.MaxValue + 2, int.MinValue },

            { long.MaxValue - int.MaxValue / 2, int.MaxValue / 2 + 2 },
            { long.MaxValue - int.MaxValue / 2, int.MaxValue },
            { long.MaxValue - int.MaxValue / 2, -1 },
            { long.MaxValue - int.MaxValue / 2, int.MinValue },

            { long.MaxValue - 1, 3 },
            { long.MaxValue - 1, int.MaxValue },
            { long.MaxValue - 1, -1 },
            { long.MaxValue - 1, int.MinValue },

            { long.MaxValue, 2 },
            { long.MaxValue, int.MaxValue },
            { long.MaxValue, -1 },
            { long.MaxValue, int.MinValue },
        };
    }

    public class RangeBigIntegerTests : RangeIBinaryIntegerTests<BigInteger>
    {
        public static TheoryData<BigInteger, int> StartCountCorrectData { get; } = new TheoryData<BigInteger, int>()
        {
            { -new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, 15 },
            { -new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, 1 },
            { -new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, 0 },


            { -1, 15 },
            { -1, 1 },
            { -1, 0 },

            { new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, 15 },
            { new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, 1 },
            { new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, 0 },
        };

        public static TheoryData<BigInteger, int> StartCountIncorrectData { get; } = new TheoryData<BigInteger, int>()
        {
            { -new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, -1 },
            { -new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, int.MinValue },

            { -1, -1 },
            { -1, int.MinValue },

            { 0, -1 },
            { 0, int.MinValue },

            { 1, -1 },
            { 1, int.MinValue },

            { new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, -1 },
            { new BigInteger(long.MaxValue) * long.MaxValue * long.MaxValue * long.MaxValue, int.MinValue },
        };
    }

}
