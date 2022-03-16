// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Collections.Generic.Tests
{
    public abstract class LargeArrayBuilderTests<T, TGenerator> where TGenerator : IGenerator<T>, new()
    {
        private static readonly TGenerator s_generator = new TGenerator();

        [Fact]
        public void Constructor()
        {
            var builder = new LargeArrayBuilder<T>();

            Assert.Equal(0, builder.Count);
            Assert.Same(Array.Empty<T>(), builder.ToArray());
        }

        [Theory]
        [MemberData(nameof(EnumerableData))]
        public void AddCountAndToArray(IEnumerable<T> seed)
        {
            var builder1 = new LargeArrayBuilder<T>();
            var builder2 = new LargeArrayBuilder<T>();

            int count = 0;
            foreach (T item in seed)
            {
                count++;

                builder1.Add(item);
                builder2.SlowAdd(item); // Verify SlowAdd has exactly the same effect as Add.

                Assert.Equal(count, builder1.Count);
                Assert.Equal(count, builder2.Count);

                Assert.Equal(seed.Take(count), builder1.ToArray());
                Assert.Equal(seed.Take(count), builder2.ToArray());
            }
        }

        [Theory]
        [MemberData(nameof(MaxCapacityData))]
        public void MaxCapacity(IEnumerable<T> seed, int maxCapacity)
        {
            var builder = new LargeArrayBuilder<T>(maxCapacity);

            for (int i = 0; i < maxCapacity; i++)
            {
                builder.Add(seed.ElementAt(i));

                int count = i + 1;
                Assert.Equal(count, builder.Count);
                Assert.Equal(seed.Take(count), builder.ToArray());
            }
        }

        [Theory]
        [MemberData(nameof(EnumerableData))]
        public void AddRange(IEnumerable<T> seed)
        {
            var builder = new LargeArrayBuilder<T>();

            // Call AddRange multiple times and verify contents w/ each iteration.
            for (int i = 1; i <= 10; i++)
            {
                builder.AddRange(seed);

                IEnumerable<T> expected = Enumerable.Repeat(seed, i).SelectMany(s => s);
                Assert.Equal(expected, builder.ToArray());
            }
        }

        [Theory]
        [MemberData(nameof(CopyToData))]
        public void CopyTo(IEnumerable<T> seed, int index, int count)
        {
            var array = new T[seed.Count()];

            var builder = new LargeArrayBuilder<T>();
            builder.AddRange(seed);
            builder.CopyTo(array, index, count);

            // Ensure we don't copy out of bounds by verifying contents outside the copy area, too.
            IEnumerable<T> prefix = array.Take(index);
            IEnumerable<T> suffix = array.Skip(index + count);
            IEnumerable<T> actual = array.Skip(index).Take(count);

            Assert.Equal(Enumerable.Repeat(default(T), index), prefix);
            Assert.Equal(Enumerable.Repeat(default(T), array.Length - index - count), suffix);
            Assert.Equal(seed.Take(count), actual);
        }

        public static TheoryData<IEnumerable<T>> EnumerableData()
        {
            var data = new TheoryData<IEnumerable<T>>();

            foreach (int count in Counts)
            {
                data.Add(Enumerable.Repeat(default(T), count));

                // Test perf: Capture the items into a List here so we
                // only enumerate the sequence once.
                data.Add(s_generator.GenerateEnumerable(count).ToList());
            }

            return data;
        }

        public static TheoryData<IEnumerable<T>, int> MaxCapacityData()
        {
            var data = new TheoryData<IEnumerable<T>, int>();

            IEnumerable<IEnumerable<T>> enumerables = EnumerableData().Select(array => array[0]).Cast<IEnumerable<T>>();

            foreach (IEnumerable<T> enumerable in enumerables)
            {
                int count = enumerable.Count();
                data.Add(enumerable, count);
            }

            return data;
        }

        public static TheoryData<IEnumerable<T>, int, int> CopyToData()
        {
            var data = new TheoryData<IEnumerable<T>, int, int>();

            IEnumerable<IEnumerable<T>> enumerables = EnumerableData().Select(array => array[0]).Cast<IEnumerable<T>>();

            foreach (IEnumerable<T> enumerable in enumerables)
            {
                int count = enumerable.Count();
                data.Add(enumerable, 0, count);

                // We want to make sure CopyTo works with different indices/counts too.
                data.Add(enumerable, 0, count / 2);
                data.Add(enumerable, count / 2, count / 2);
                data.Add(enumerable, count / 4, count / 2);
            }

            return data;
        }

        private static IEnumerable<int> Counts
        {
            get
            {
                for (int i = 0; i < 6; i++)
                {
                    int powerOfTwo = 1 << i;

                    // Return numbers of the form 2^N - 1, 2^N and 2^N + 1
                    // This should cover most of the interesting cases
                    yield return powerOfTwo - 1;
                    yield return powerOfTwo;
                    yield return powerOfTwo + 1;
                }
            }
        }
    }

    public class LargeArrayBuilderTestsInt32 : LargeArrayBuilderTests<int, LargeArrayBuilderTestsInt32.Generator>
    {
        public sealed class Generator : IGenerator<int>
        {
            public int Generate(int seed) => seed;
        }
    }

    public class LargeArrayBuilderTestsString : LargeArrayBuilderTests<string, LargeArrayBuilderTestsString.Generator>
    {
        public sealed class Generator : IGenerator<string>
        {
            public string Generate(int seed) => seed.ToString();
        }
    }
}
