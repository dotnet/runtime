// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.Frozen.Tests
{
    /// <summary>
    /// Tests sets of integral values (and enums), which, depending on their type, their count and how densely
    /// packed they are, are backed by different specialized implementations.
    /// </summary>
    public class FrozenSet_IntegralValuesTests
    {
        public enum ByteEnum : byte { }
        public enum SByteEnum : sbyte { }
        public enum Int16Enum : short { }
        public enum UInt16Enum : ushort { }
        public enum Int32Enum : int { }
        public enum UInt32Enum : uint { }
        public enum Int64Enum : long { }
        public enum UInt64Enum : ulong { }

        public enum Shape
        {
            /// <summary>All values from 0 to count - 1.</summary>
            ContiguousFromZero,
            /// <summary>All values from -count / 2 to count / 2, i.e. wrapping around for unsigned types.</summary>
            ContiguousAroundZero,
            /// <summary>All values up to the max value of the signed type of the same size (i.e. around the middle of the range for unsigned types).</summary>
            ContiguousToMax,
            /// <summary>All values from the min value of the signed type of the same size (i.e. around the middle of the range for unsigned types).</summary>
            ContiguousFromMin,
            /// <summary>All values up to -1, i.e. up to the max value for unsigned types.</summary>
            ContiguousToMinusOne,
            /// <summary>Every third value.</summary>
            SmallGaps,
            /// <summary>Every 64th value, which is always just dense enough for the bitmap-based implementation.</summary>
            LargeGaps,
            /// <summary>Every 65th value, which is dense enough for the bitmap-based implementation only with up to 64 values.</summary>
            LargerGaps,
            /// <summary>Random values across the whole range of the type.</summary>
            Random,
            /// <summary>Values close to the min and to the max of the signed type of the same size.</summary>
            Extremes,
        }

        // The counts are around the number of values of each size that fit in a vector (e.g. 4, 8 and 16 ints for Vector128,
        // Vector256 and Vector512), and around the max number of values of the vector buckets-based implementation.
        public static IEnumerable<object[]> CountsAndShapes() =>
            from count in new[] { 1, 2, 3, 4, 5, 8, 9, 16, 17, 32, 33, 64, 65 }
            from shape in (Shape[])Enum.GetValues(typeof(Shape))
            select new object[] { count, shape };

        // Enums only differ from their underlying type in how the implementation is chosen, so they're tested with fewer
        // counts and shapes, which still lead to each implementation.
        public static IEnumerable<object[]> EnumCountsAndShapes() =>
            from count in new[] { 1, 3, 9, 17, 33, 65 }
            from shape in new[] { Shape.ContiguousAroundZero, Shape.SmallGaps, Shape.Random, Shape.Extremes }
            select new object[] { count, shape };

        [Theory, MemberData(nameof(CountsAndShapes))] public void Byte(int count, Shape shape) => Test(count, shape, sizeof(byte), l => unchecked((byte)l));
        [Theory, MemberData(nameof(CountsAndShapes))] public void SByte(int count, Shape shape) => Test(count, shape, sizeof(sbyte), l => unchecked((sbyte)l));
        [Theory, MemberData(nameof(CountsAndShapes))] public void Int16(int count, Shape shape) => Test(count, shape, sizeof(short), l => unchecked((short)l));
        [Theory, MemberData(nameof(CountsAndShapes))] public void UInt16(int count, Shape shape) => Test(count, shape, sizeof(ushort), l => unchecked((ushort)l));
        [Theory, MemberData(nameof(CountsAndShapes))] public void Char(int count, Shape shape) => Test(count, shape, sizeof(char), l => unchecked((char)l));
        [Theory, MemberData(nameof(CountsAndShapes))] public void Int32(int count, Shape shape) => Test(count, shape, sizeof(int), l => unchecked((int)l));
        [Theory, MemberData(nameof(CountsAndShapes))] public void UInt32(int count, Shape shape) => Test(count, shape, sizeof(uint), l => unchecked((uint)l));
        [Theory, MemberData(nameof(CountsAndShapes))] public void Int64(int count, Shape shape) => Test(count, shape, sizeof(long), l => l);
        [Theory, MemberData(nameof(CountsAndShapes))] public void UInt64(int count, Shape shape) => Test(count, shape, sizeof(ulong), l => unchecked((ulong)l));
        [Theory, MemberData(nameof(CountsAndShapes))] public void IntPtr(int count, Shape shape) => Test(count, shape, System.IntPtr.Size, l => unchecked((nint)l));
        [Theory, MemberData(nameof(CountsAndShapes))] public void UIntPtr(int count, Shape shape) => Test(count, shape, System.IntPtr.Size, l => unchecked((nuint)l));
        [Theory, MemberData(nameof(EnumCountsAndShapes))] public void ByteEnums(int count, Shape shape) => Test(count, shape, sizeof(byte), l => (ByteEnum)unchecked((byte)l));
        [Theory, MemberData(nameof(EnumCountsAndShapes))] public void SByteEnums(int count, Shape shape) => Test(count, shape, sizeof(sbyte), l => (SByteEnum)unchecked((sbyte)l));
        [Theory, MemberData(nameof(EnumCountsAndShapes))] public void Int16Enums(int count, Shape shape) => Test(count, shape, sizeof(short), l => (Int16Enum)unchecked((short)l));
        [Theory, MemberData(nameof(EnumCountsAndShapes))] public void UInt16Enums(int count, Shape shape) => Test(count, shape, sizeof(ushort), l => (UInt16Enum)unchecked((ushort)l));
        [Theory, MemberData(nameof(EnumCountsAndShapes))] public void Int32Enums(int count, Shape shape) => Test(count, shape, sizeof(int), l => (Int32Enum)unchecked((int)l));
        [Theory, MemberData(nameof(EnumCountsAndShapes))] public void UInt32Enums(int count, Shape shape) => Test(count, shape, sizeof(uint), l => (UInt32Enum)unchecked((uint)l));
        [Theory, MemberData(nameof(EnumCountsAndShapes))] public void Int64Enums(int count, Shape shape) => Test(count, shape, sizeof(long), l => (Int64Enum)l);
        [Theory, MemberData(nameof(EnumCountsAndShapes))] public void UInt64Enums(int count, Shape shape) => Test(count, shape, sizeof(ulong), l => (UInt64Enum)unchecked((ulong)l));

        [Theory]
        [InlineData(Shape.ContiguousAroundZero)]
        [InlineData(Shape.SmallGaps)]
        [InlineData(Shape.LargeGaps)]
        public void LargeInt32(Shape shape) => Test(5000, shape, sizeof(int), l => unchecked((int)l));

        /// <summary>Creates a set with the specified count and shape, and validates it against a <see cref="HashSet{T}"/>.</summary>
        /// <param name="size">The size in bytes of <typeparamref name="T"/>.</param>
        /// <param name="convert">Converts (with truncation) a value in the range of the signed integer type of the same size to a <typeparamref name="T"/>.</param>
        private static void Test<T>(int count, Shape shape, int size, Func<long, T> convert)
        {
            long min = size == sizeof(long) ? long.MinValue : -(1L << (size * 8 - 1));
            long max = size == sizeof(long) ? long.MaxValue : (1L << (size * 8 - 1)) - 1;

            if (!TryCreateValues(count, shape, min, max, out long[] values))
            {
                // This shape can't be created with this many values for this type.
                return;
            }

            HashSet<T> expected = new HashSet<T>(values.Select(convert));
            Assert.Equal(count, expected.Count);

            // Probe for all the values, for values just next to them, for values which only differ in their highest bits,
            // for values at the boundaries of the type, and for random values.
            var random = new Random(42);
            var probes = new List<T>(expected);
            foreach (long value in values)
            {
                probes.Add(convert(unchecked(value - 1)));
                probes.Add(convert(unchecked(value + 1)));
                probes.Add(convert(value ^ (1L << (size * 8 - 1))));
                probes.Add(convert(value ^ (1L << (size * 8 - 2))));
            }
            probes.AddRange(new[] { min, min + 1, -1, 0, 1, max - 1, max }.Select(convert));
            probes.AddRange(Enumerable.Range(0, 100).Select(_ => convert(NextInt64(random, min, max))));
            T[] distinctProbes = probes.Distinct().ToArray();
            bool[] expectedContains = Array.ConvertAll(distinctProbes, expected.Contains);

            // The different ways of creating the set end up with the same implementation, so the lookups are checked for
            // all of them, and the rest only once. The results are checked without xunit asserts, as these would make this
            // significantly more expensive.
            FrozenSet<T>[] sets = [expected.ToFrozenSet(), FrozenSet.Create(values.Select(convert).ToArray()), values.Select(convert).ToFrozenSet()];
            foreach (FrozenSet<T> set in sets)
            {
                Assert.Equal(expected.Count, set.Count);
                Assert.True(expected.SetEquals(set.Items), set.GetType().ToString());

                for (int i = 0; i < distinctProbes.Length; i++)
                {
                    if (set.Contains(distinctProbes[i]) != expectedContains[i])
                    {
                        Assert.Fail($"{set.GetType()}: Contains({distinctProbes[i]}) should be {expectedContains[i]}");
                    }
                }
            }

            FrozenSet<T> frozen = sets[0];
            Assert.Equal(expected.Count, frozen.Items.Length);
            Assert.True(expected.SetEquals(frozen), frozen.GetType().ToString());
            for (int i = 0; i < distinctProbes.Length; i++)
            {
                T probe = distinctProbes[i];
                if (frozen.TryGetValue(probe, out T actualValue) != expectedContains[i] ||
                    !EqualityComparer<T>.Default.Equals(actualValue, expectedContains[i] ? probe : default))
                {
                    Assert.Fail($"{frozen.GetType()}: TryGetValue({probe}) should be {expectedContains[i]}, got {actualValue}");
                }
            }

#if NET
            AssertImplementation(frozen, shape);
#endif

            // The set operations rely on each item being found at a distinct index.
            Assert.True(frozen.SetEquals(expected));
            Assert.True(frozen.SetEquals(values.Select(convert)));
            Assert.True(frozen.IsSubsetOf(values.Select(convert)));
            Assert.True(frozen.IsSupersetOf(values.Select(convert)));
            Assert.False(frozen.IsProperSubsetOf(values.Select(convert)));
            Assert.False(frozen.IsProperSupersetOf(values.Select(convert)));
            Assert.True(frozen.Overlaps(values.Select(convert).Take(1)));
            Assert.True(frozen.IsProperSupersetOf(values.Select(convert).Skip(1)));

            T[] missing = distinctProbes.Where(p => !expected.Contains(p)).Take(3).ToArray();
            if (missing.Length != 0)
            {
                Assert.True(frozen.IsProperSubsetOf(values.Select(convert).Concat(missing)));
                Assert.False(frozen.Overlaps(missing));
                Assert.False(frozen.IsSupersetOf(values.Select(convert).Concat(missing)));
            }

            // The rest of the set's members are checked with one shape of values per kind of implementation, as they
            // don't depend on the values beyond that.
            if (shape is Shape.ContiguousAroundZero or Shape.SmallGaps or Shape.Random)
            {
                ValidateReadOnlySetSurface(frozen, expected, missing);
            }
        }

#if NET
        /// <summary>Validates which implementation is used for the cases where it doesn't depend on hardware acceleration.</summary>
        private static void AssertImplementation<T>(FrozenSet<T> frozen, Shape shape)
        {
            string name = frozen.GetType().Name;

            // The types (and enums with the underlying types) supported by the dense implementations.
            Type type = typeof(T).IsEnum ? Enum.GetUnderlyingType(typeof(T)) : typeof(T);
            if (type != typeof(byte) && type != typeof(sbyte) && type != typeof(short) && type != typeof(ushort) && type != typeof(char) && type != typeof(int))
            {
                Assert.DoesNotContain("Dense", frozen.GetType().FullName);
                return;
            }

            long[] values = frozen.Select(v => Convert.ToInt64(v)).ToArray();
            long spread = values.Max() - values.Min();
            if (spread == values.Length - 1)
            {
                Assert.Equal("WithContiguousValues`2", name);
            }
            else if (spread >= 64L * values.Length)
            {
                Assert.DoesNotContain("Dense", frozen.GetType().FullName);
            }
            else if (values.Length * System.Runtime.CompilerServices.Unsafe.SizeOf<T>() > 64)
            {
                // Values that are dense enough, and that don't fit in any vector.
                Assert.Equal("WithBitmap`2", name);
            }
        }
#endif

        /// <summary>Validates the members of the set other than lookups, which are implemented on top of each implementation's storage.</summary>
        private static void ValidateReadOnlySetSurface<T>(FrozenSet<T> frozen, HashSet<T> expected, T[] missing)
        {
            Assert.Same(EqualityComparer<T>.Default, frozen.Comparer);
            Assert.Same(frozen, frozen.ToFrozenSet());
            Assert.Same(frozen, frozen.ToFrozenSet(EqualityComparer<T>.Default));

            // Enumeration, generic and non-generic, including after a reset.
            var enumerated = new List<T>();
            foreach (T item in frozen)
            {
                enumerated.Add(item);
            }
            Assert.Equal(expected.Count, enumerated.Count);
            Assert.True(expected.SetEquals(enumerated));
            Assert.Equal(enumerated, frozen.Items);

            using (IEnumerator<T> e = ((IEnumerable<T>)frozen).GetEnumerator())
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    int i = 0;
                    while (e.MoveNext())
                    {
                        Assert.Equal(enumerated[i++], e.Current);
                    }
                    Assert.Equal(enumerated.Count, i);
                    e.Reset();
                }
            }
            Assert.Equal(enumerated.Cast<object>(), ((IEnumerable)frozen).Cast<object>());

            // Copying.
            T[] array = new T[expected.Count + 3];
            frozen.CopyTo(array, 2);
            Assert.Equal(enumerated, array.Skip(2).Take(expected.Count));
            Assert.Equal(default, array[0]);
            Assert.Equal(default, array[1]);
            Assert.Equal(default, array[array.Length - 1]);

            Span<T> span = new T[expected.Count];
            frozen.CopyTo(span);
            Assert.Equal(enumerated, span.ToArray());

            Array nonGenericArray = new T[expected.Count + 1];
            ((ICollection)frozen).CopyTo(nonGenericArray, 1);
            Assert.Equal(enumerated, nonGenericArray.Cast<T>().Skip(1));
            Assert.Throws<ArgumentException>(() => frozen.CopyTo(new T[expected.Count - 1], 0));

            // Collection interfaces.
            Assert.Equal(expected.Count, ((ICollection<T>)frozen).Count);
            Assert.Equal(expected.Count, ((IReadOnlyCollection<T>)frozen).Count);
            Assert.Equal(expected.Count, ((ICollection)frozen).Count);
            Assert.True(((ICollection<T>)frozen).IsReadOnly);
            Assert.False(((ICollection)frozen).IsSynchronized);
            Assert.Same(frozen, ((ICollection)frozen).SyncRoot);
            T value = enumerated[0];
            Assert.Throws<NotSupportedException>(() => ((ICollection<T>)frozen).Add(value));
            Assert.Throws<NotSupportedException>(() => ((ICollection<T>)frozen).Remove(value));
            Assert.Throws<NotSupportedException>(() => ((ICollection<T>)frozen).Clear());
            Assert.Throws<NotSupportedException>(() => ((ISet<T>)frozen).Add(value));
            Assert.Throws<NotSupportedException>(() => ((ISet<T>)frozen).UnionWith(enumerated));
            Assert.Throws<NotSupportedException>(() => ((ISet<T>)frozen).IntersectWith(enumerated));
            Assert.Throws<NotSupportedException>(() => ((ISet<T>)frozen).ExceptWith(enumerated));
            Assert.Throws<NotSupportedException>(() => ((ISet<T>)frozen).SymmetricExceptWith(enumerated));
            Assert.True(((ICollection<T>)frozen).Contains(value));
            Assert.Equal(expected.Count, frozen.Count);

            // Set comparisons, with sets using the same comparer (which use the other set's lookups), with sequences
            // (which use this set's lookups, and rely on each item being found at a distinct index), with duplicates,
            // and with empty collections.
            T[] fewer = enumerated.Skip(1).ToArray();
            T[] more = enumerated.Concat(missing.Take(1)).ToArray();
            T[] withDuplicates = enumerated.Concat(enumerated).Reverse().ToArray();
            var sameSets = new List<IEnumerable<T>>
            {
                new HashSet<T>(enumerated), enumerated.ToFrozenSet(), enumerated.ToImmutableHashSet(), withDuplicates, enumerated.Select(i => i),
            };
            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)) || typeof(IComparable).IsAssignableFrom(typeof(T)))
            {
                // IntPtr and UIntPtr aren't comparable on .NET Framework.
                sameSets.Add(new SortedSet<T>(enumerated));
            }
            foreach (IEnumerable<T> same in sameSets)
            {
                Assert.True(frozen.SetEquals(same));
                Assert.True(frozen.IsSubsetOf(same));
                Assert.True(frozen.IsSupersetOf(same));
                Assert.False(frozen.IsProperSubsetOf(same));
                Assert.False(frozen.IsProperSupersetOf(same));
                Assert.True(frozen.Overlaps(same));
            }

            foreach (IEnumerable<T> subset in new IEnumerable<T>[] { new HashSet<T>(fewer), fewer.ToFrozenSet(), fewer.Concat(fewer) })
            {
                Assert.False(frozen.SetEquals(subset));
                Assert.True(frozen.IsSupersetOf(subset));
                Assert.True(frozen.IsProperSupersetOf(subset));
                Assert.False(frozen.IsSubsetOf(subset));
                Assert.False(frozen.IsProperSubsetOf(subset));
                Assert.Equal(fewer.Length != 0, frozen.Overlaps(subset));
            }

            if (missing.Length != 0)
            {
                foreach (IEnumerable<T> superset in new IEnumerable<T>[] { new HashSet<T>(more), more.ToFrozenSet(), more.Concat(more) })
                {
                    Assert.False(frozen.SetEquals(superset));
                    Assert.True(frozen.IsSubsetOf(superset));
                    Assert.True(frozen.IsProperSubsetOf(superset));
                    Assert.False(frozen.IsSupersetOf(superset));
                    Assert.False(frozen.IsProperSupersetOf(superset));
                    Assert.True(frozen.Overlaps(superset));
                }

                foreach (IEnumerable<T> disjoint in new IEnumerable<T>[] { new HashSet<T>(missing), missing.ToFrozenSet(), missing.Concat(missing) })
                {
                    Assert.False(frozen.Overlaps(disjoint));
                    Assert.False(frozen.SetEquals(disjoint));
                    Assert.False(frozen.IsSubsetOf(disjoint));
                    Assert.False(frozen.IsSupersetOf(disjoint));
                }
            }

            foreach (IEnumerable<T> empty in new IEnumerable<T>[] { Array.Empty<T>(), new HashSet<T>(), FrozenSet<T>.Empty, Enumerable.Empty<T>() })
            {
                Assert.False(frozen.SetEquals(empty));
                Assert.True(frozen.IsSupersetOf(empty));
                Assert.True(frozen.IsProperSupersetOf(empty));
                Assert.False(frozen.IsSubsetOf(empty));
                Assert.False(frozen.IsProperSubsetOf(empty));
                Assert.False(frozen.Overlaps(empty));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public void DebuggerProxy()
        {
            // A set for each implementation (some of which also depend on hardware acceleration).
            var sets = new FrozenSet<int>[]
            {
                Enumerable.Range(10, 100).ToFrozenSet(),
                Enumerable.Range(10, 100).Select(i => i * 3).ToFrozenSet(),
                new[] { 12, 34, 56_789 }.ToFrozenSet(),
                Enumerable.Range(1, 40).Select(i => i * 1_000_003).ToFrozenSet(),
            };

            foreach (FrozenSet<int> set in sets)
            {
                if (set.GetType().FullName.Contains("Dense") || set.GetType().Name.StartsWith("SmallIntegral") || set.GetType().Name.StartsWith("VectorBuckets"))
                {
                    DebuggerAttributes.ValidateDebuggerDisplayReferences(set);
                    DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(set);
                    PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
                    Assert.Equal(set.Items, (int[])itemProperty.GetValue(info.Instance));
                }
            }
        }

        private static bool TryCreateValues(int count, Shape shape, long min, long max, out long[] values)
        {
            // Number of distinct values representable by the type, capped for 64-bit types (for which it overflows to 0).
            long range = unchecked(max - min + 1);
            if (range <= 0)
            {
                range = long.MaxValue;
            }
            values = null;

            switch (shape)
            {
                case Shape.ContiguousFromZero:
                    if (count > range)
                    {
                        return false;
                    }
                    values = Enumerable.Range(0, count).Select(i => (long)i).ToArray();
                    break;

                case Shape.ContiguousAroundZero:
                    if (count > range)
                    {
                        return false;
                    }
                    values = Enumerable.Range(-count / 2, count).Select(i => (long)i).ToArray();
                    break;

                case Shape.ContiguousToMax:
                    if (count > range)
                    {
                        return false;
                    }
                    values = Enumerable.Range(0, count).Select(i => max - i).ToArray();
                    break;

                case Shape.ContiguousFromMin:
                    if (count > range)
                    {
                        return false;
                    }
                    values = Enumerable.Range(0, count).Select(i => min + i).ToArray();
                    break;

                case Shape.ContiguousToMinusOne:
                    if (count > range)
                    {
                        return false;
                    }
                    values = Enumerable.Range(1, count).Select(i => (long)-i).ToArray();
                    break;

                case Shape.SmallGaps:
                    if ((long)count * 3 > range)
                    {
                        return false;
                    }
                    values = Enumerable.Range(0, count).Select(i => (long)i * 3 - count).ToArray();
                    break;

                case Shape.LargeGaps:
                case Shape.LargerGaps:
                    int gap = shape == Shape.LargeGaps ? 64 : 65;
                    if ((long)count * gap > range)
                    {
                        return false;
                    }
                    values = Enumerable.Range(0, count).Select(i => (long)i * gap - count * 32L).ToArray();
                    break;

                case Shape.Random:
                case Shape.Extremes:
                    if (count > range / 4)
                    {
                        return false;
                    }
                    var random = new Random(count);
                    var set = new HashSet<long>();
                    while (set.Count < count)
                    {
                        set.Add(shape == Shape.Random ?
                            NextInt64(random, min, max) :
                            random.Next(2) == 0 ? min + random.Next(count) : max - random.Next(count));
                    }
                    values = set.ToArray();
                    break;
            }

            // Shuffle, as some implementations sort the values.
            values = values.OrderBy(v => unchecked(v * (long)0x9E3779B97F4A7C15)).ToArray();
            return true;
        }

        /// <summary>Gets a random value in [<paramref name="min"/>, <paramref name="max"/>].</summary>
        private static long NextInt64(Random random, long min, long max)
        {
            byte[] bytes = new byte[8];
            random.NextBytes(bytes);
            ulong bits = BitConverter.ToUInt64(bytes, 0);
            ulong range = unchecked((ulong)(max - min) + 1);
            return range == 0 ? unchecked((long)bits) : unchecked(min + (long)(bits % range));
        }
    }
}
