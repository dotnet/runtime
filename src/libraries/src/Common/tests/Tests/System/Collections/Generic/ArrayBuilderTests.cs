// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
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
                seed ^= 0x9e3779b9 + (seed << 6) + (seed >> 2);
                yield return generator.Generate((int)seed);
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
        public void AddAndIndexer(IEnumerable<T> seed)
        {
            // CreateBuilderFromSequence implicitly tests Add
            ArrayBuilder<T> builder = CreateBuilderFromSequence(seed);

            // Continuously shift the elements in the builder over
            // using the get/set indexers, until none are left.
            for (int left = builder.Count - 1; left >= 0; )
            {
                for (int i = 0; i < left; i++)
                {
                    builder[i] = builder[i + 1];
                }

                // Nil out the slot we're no longer using
                builder[left--] = default(T);

                int offset = (builder.Count - 1) - left; // How much we've skipped into the enumerable
                IEnumerable<T> expected = seed.Skip(offset)
                    .Concat(Enumerable.Repeat(default(T), offset)); // The count has not been changed, but slots @ the end have been nil'd out

                VerifyBuilderContents(expected, builder);
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
            
            IEnumerable<int> counts = CountData().Select(array => array[0]).Cast<int>();

            foreach (int count in counts)
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
            return NextPowerOfTwo(count);
        }

        private static int NextPowerOfTwo(int value)
        {
            // Taken from https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2

            Debug.Assert(value >= 0);

            // If the number is already a power of 2, we want to round to itself.
            value--;

            // Propogate 1-bits right: if the highest bit set is @ position n,
            // then all of the bits to the right of position n will become set.
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;

            // This yields a number of the form 2^N - 1.
            // Add 1 to get a power of 2 with the bit set @ position n + 1.
            return value + 1;
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
