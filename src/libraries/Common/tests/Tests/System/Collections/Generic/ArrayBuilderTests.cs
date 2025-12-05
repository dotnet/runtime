// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Xunit;

namespace System.Collections.Generic.Tests
{
    public interface IGenerator<out T>
    {
        T Generate(int seed);
    }

    public static class GeneratorExtensions
    {
        public static IEnumerable<T> GenerateEnumerable<T>(this IGenerator<T> generator, int count)
        {
            Debug.Assert(generator != null);
            Debug.Assert(count >= 0);

            uint seed = (uint)count;
            for (int i = 0; i < count; i++)
            {
                unchecked
                {
                    seed ^= 0x9e3779b9 + (seed << 6) + (seed >> 2);
                    yield return generator.Generate((int)seed);
                }
            }
        }
    }

    public abstract class ArrayBuilderTests<T, TGenerator> where TGenerator : IGenerator<T>, new()
    {
        private static readonly TGenerator s_generator = new TGenerator();

        [Fact]
        public void ParameterlessConstructor()
        {
            var builder = new ArrayBuilder<T>();

            // Should default to count/capacity of 0
            Assert.Equal(0, builder.Count);
            Assert.Equal(0, builder.Capacity);

            // Should use a cached array for capacity of 0
            Assert.Same(Array.Empty<T>(), builder.ToArray());
        }

        [Theory]
        [MemberData(nameof(CapacityData))]
        public void CapacityConstructor(int capacity)
        {
            Debug.Assert(capacity >= 0);

            var builder = new ArrayBuilder<T>(capacity);

            Assert.Equal(0, builder.Count);
            Assert.Equal(capacity, builder.Capacity);

            // Should use a cached array for count of 0, regardless of capacity
            Assert.Same(Array.Empty<T>(), builder.ToArray());
        }

        [Theory]
        [MemberData(nameof(CountData))]
        public void Count(int count)
        {
            Debug.Assert(count >= 0);

            IEnumerable<T> sequence = Enumerable.Repeat(default(T), count);
            ArrayBuilder<T> builder = CreateBuilderFromSequence(sequence);

            Assert.Equal(count, builder.Count);
            Assert.Equal(CalculateExpectedCapacity(count), builder.Capacity);

            // Indexing everything up to Count should succeed.
            for (int i = 0; i < count; i++)
            {
                T item = builder[i];
            }
        }

        [Theory]
        [MemberData(nameof(EnumerableData))]
        public void ToArray(IEnumerable<T> seed)
        {
            ArrayBuilder<T> builder = CreateBuilderFromSequence(seed);

            int count = builder.Count; // Count needs to be called beforehand.
            T[] array = builder.ToArray(); // ToArray should only be called once.

            Assert.Equal(count, array.Length);
            Assert.Equal(seed, array);
        }

        [Theory]
        [MemberData(nameof(CapacityData))]
        public void UncheckedAdd(int capacity)
        {
            Debug.Assert(capacity >= 0);

            var builder = new ArrayBuilder<T>(capacity);

            for (int i = 0; i < capacity; i++)
            {
                builder.UncheckedAdd(default(T));
            }

            VerifyBuilderContents(Enumerable.Repeat(default(T), capacity), builder);
        }

        [Theory]
        [MemberData(nameof(EnumerableData))]
        public void AddRange(IEnumerable<T> seed)
        {
            var builder = new ArrayBuilder<T>();
            builder.AddRange(seed);

            int count = builder.Count;
            T[] array = builder.ToArray();

            Assert.Equal(count, array.Length);
            Assert.Equal(seed, array);
        }

        [Fact]
        public void AddRange_EmptyEnumerable()
        {
            var builder = new ArrayBuilder<T>();
            builder.AddRange(Enumerable.Empty<T>());

            Assert.Equal(0, builder.Count);
            Assert.Same(Array.Empty<T>(), builder.ToArray());
        }

        [Theory]
        [MemberData(nameof(EnumerableData))]
        public void AddRange_AfterAdd(IEnumerable<T> seed)
        {
            var builder = new ArrayBuilder<T>();
            builder.Add(default(T));
            builder.AddRange(seed);

            int expectedCount = 1 + seed.Count();
            Assert.Equal(expectedCount, builder.Count);

            T[] array = builder.ToArray();
            Assert.Equal(expectedCount, array.Length);
            Assert.Equal(default(T), array[0]);
            Assert.Equal(seed, array.Skip(1));
        }

        [Theory]
        [MemberData(nameof(CountData))]
        public void AddRange_ICollection_PreallocatesCapacity(int count)
        {
            // Use a List<T> which implements ICollection<T>
            List<T> collection = s_generator.GenerateEnumerable(count).ToList();

            var builder = new ArrayBuilder<T>();
            builder.AddRange(collection);

            Assert.Equal(count, builder.Count);

            // When adding an ICollection<T>, capacity should be at least
            // enough for the collection (0 if empty)
            if (count > 0)
            {
                Assert.True(builder.Capacity >= count);
            }
            else
            {
                Assert.Equal(0, builder.Capacity);
            }

            Assert.Equal(collection, builder.ToArray());
        }

        [Fact]
        public void AddRange_ICollection_EmptyCollection()
        {
            // Use an empty List<T> which implements ICollection<T>
            List<T> emptyCollection = new List<T>();

            var builder = new ArrayBuilder<T>();
            builder.AddRange(emptyCollection);

            Assert.Equal(0, builder.Count);
            Assert.Equal(0, builder.Capacity);
            Assert.Same(Array.Empty<T>(), builder.ToArray());
        }

        [Theory]
        [MemberData(nameof(CountData))]
        public void AddRange_ICollection_AfterExistingItems(int count)
        {
            // Add some initial items
            var builder = new ArrayBuilder<T>();
            builder.Add(default(T));
            builder.Add(default(T));
            int initialCount = 2;
            int initialCapacity = builder.Capacity;

            // Use a List<T> which implements ICollection<T>
            List<T> collection = s_generator.GenerateEnumerable(count).ToList();
            builder.AddRange(collection);

            int expectedCount = initialCount + count;
            Assert.Equal(expectedCount, builder.Count);

            // When adding ICollection<T> after existing items, capacity should be
            // at least enough for all items
            Assert.True(builder.Capacity >= expectedCount);

            T[] array = builder.ToArray();
            Assert.Equal(expectedCount, array.Length);
            Assert.Equal(default(T), array[0]);
            Assert.Equal(default(T), array[1]);
            Assert.Equal(collection, array.Skip(2));
        }

        [Theory]
        [MemberData(nameof(CountData))]
        public void AddRange_NonICollection_GrowsIncrementally(int count)
        {
            // Use a generator method to create a pure IEnumerable<T> that is NOT an ICollection<T>
            IEnumerable<T> NonCollectionEnumerable()
            {
                for (int i = 0; i < count; i++)
                {
                    yield return default(T);
                }
            }

            var builder = new ArrayBuilder<T>();
            builder.AddRange(NonCollectionEnumerable());

            Assert.Equal(count, builder.Count);

            // Non-ICollection path should grow incrementally (powers of 2)
            Assert.Equal(CalculateExpectedCapacity(count), builder.Capacity);

            Assert.Equal(NonCollectionEnumerable(), builder.ToArray());
        }

        public static TheoryData<int> CapacityData()
        {
            var data = new TheoryData<int>();

            for (int i = 0; i < 6; i++)
            {
                int powerOfTwo = 1 << i;

                // Return numbers of the form 2^N - 1, 2^N and 2^N + 1
                // This should cover most of the interesting cases
                data.Add(powerOfTwo - 1);
                data.Add(powerOfTwo);
                data.Add(powerOfTwo + 1);
            }

            return data;
        }

        // At the moment, all interesting Count cases are covered by CapacityData.
        public static TheoryData<int> CountData() => CapacityData();

        public static TheoryData<IEnumerable<T>> EnumerableData()
        {
            var data = new TheoryData<IEnumerable<T>>();

            foreach (int count in CountData())
            {
                data.Add(Enumerable.Repeat(default(T), count));

                // Test perf: Capture the items into a List here so we
                // only enumerate the sequence once.
                data.Add(s_generator.GenerateEnumerable(count).ToList());
            }

            return data;
        }

        private static ArrayBuilder<T> CreateBuilderFromSequence(IEnumerable<T> sequence)
        {
            Debug.Assert(sequence != null);

            var builder = new ArrayBuilder<T>();

            int count = 0;
            foreach (T item in sequence)
            {
                count++;
                builder.Add(item);

                Assert.Equal(count, builder.Count);
                Assert.Equal(CalculateExpectedCapacity(count), builder.Capacity);
                VerifyBuilderContents(sequence.Take(count), builder);
                Assert.Equal(sequence.First(), builder.First());
                Assert.Equal(item, builder.Last());
            }

            return builder;
        }

        // Assert.Equal cannot be used directly on ArrayBuilder-- it does not implement IEnumerable<T>
        // to be as lightweight as possible. This is what you should call instead.
        private static void VerifyBuilderContents(IEnumerable<T> expected, ArrayBuilder<T> actual)
        {
            Debug.Assert(expected != null);

            using (IEnumerator<T> enumerator = expected.GetEnumerator())
            {
                int index = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(enumerator.Current, actual[index++]);
                }

                Assert.Equal(actual.Count, index);
            }
        }

        private static int CalculateExpectedCapacity(int count)
        {
            // If we create an ArrayBuilder with no initial backing store,
            // and add this many items to it, what should be it's capacity?

            Debug.Assert(count >= 0);

            // We start with no capacity for 0 items...
            if (count == 0)
            {
                return 0;
            }

            // Then allocate arrays of size 4, 8, 16, etc.
            count = Math.Max(count, 4);
            return (int)BitOperations.RoundUpToPowerOf2((uint)count);
        }
    }

    public class ArrayBuilderTestsInt32 : ArrayBuilderTests<int, ArrayBuilderTestsInt32.Generator>
    {
        public sealed class Generator : IGenerator<int>
        {
            public int Generate(int seed) => seed;
        }
    }

    public class ArrayBuilderTestsString : ArrayBuilderTests<string, ArrayBuilderTestsString.Generator>
    {
        public sealed class Generator : IGenerator<string>
        {
            public string Generate(int seed) => seed.ToString();
        }
    }
}
