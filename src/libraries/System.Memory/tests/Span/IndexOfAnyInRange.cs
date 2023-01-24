// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.SpanTests
{
    public class IndexOfAnyInRangeTests_Byte : IndexOfAnyInRangeTests<byte> { protected override byte Create(int value) => (byte)value; }
    public class IndexOfAnyInRangeTests_SByte : IndexOfAnyInRangeTests<sbyte> { protected override sbyte Create(int value) => (sbyte)value; }
    public class IndexOfAnyInRangeTests_Char : IndexOfAnyInRangeTests<char> { protected override char Create(int value) => (char)value; }
    public class IndexOfAnyInRangeTests_Int16 : IndexOfAnyInRangeTests<short> { protected override short Create(int value) => (short)value; }
    public class IndexOfAnyInRangeTests_UInt16 : IndexOfAnyInRangeTests<ushort> { protected override ushort Create(int value) => (ushort)value; }
    public class IndexOfAnyInRangeTests_Int32 : IndexOfAnyInRangeTests<int> { protected override int Create(int value) => value; }
    public class IndexOfAnyInRangeTests_UInt32 : IndexOfAnyInRangeTests<uint> { protected override uint Create(int value) => (uint)value; }
    public class IndexOfAnyInRangeTests_Int64 : IndexOfAnyInRangeTests<long> { protected override long Create(int value) => value; }
    public class IndexOfAnyInRangeTests_UInt64 : IndexOfAnyInRangeTests<ulong> { protected override ulong Create(int value) => (ulong)value; }
    public class IndexOfAnyInRangeTests_IntPtr : IndexOfAnyInRangeTests<nint> { protected override nint Create(int value) => (nint)value; }
    public class IndexOfAnyInRangeTests_UIntPtr : IndexOfAnyInRangeTests<nuint> { protected override nuint Create(int value) => (nuint)value; }
    public class IndexOfAnyInRangeTests_TimeSpan : IndexOfAnyInRangeTests<TimeSpan> { protected override TimeSpan Create(int value) => TimeSpan.FromTicks(value); }

    public class IndexOfAnyInRangeTests_RefType : IndexOfAnyInRangeTests<RefType>
    {
        protected override RefType Create(int value) => new RefType { Value = value };

        [Fact]
        public void NullBound_Throws()
        {
            foreach ((RefType low, RefType high) in new (RefType,RefType)[] { (null, new RefType { Value = 42 }), (null, null), (new RefType { Value = 42 }, null) })
            {
                string argName = low is null ? "lowInclusive" : "highInclusive";

                AssertExtensions.Throws<ArgumentNullException>(argName, () => MemoryExtensions.IndexOfAnyInRange(Span<RefType>.Empty, low, high));
                AssertExtensions.Throws<ArgumentNullException>(argName, () => MemoryExtensions.IndexOfAnyInRange(ReadOnlySpan<RefType>.Empty, low, high));

                AssertExtensions.Throws<ArgumentNullException>(argName, () => MemoryExtensions.LastIndexOfAnyInRange(Span<RefType>.Empty, low, high));
                AssertExtensions.Throws<ArgumentNullException>(argName, () => MemoryExtensions.LastIndexOfAnyInRange(ReadOnlySpan<RefType>.Empty, low, high));

                AssertExtensions.Throws<ArgumentNullException>(argName, () => MemoryExtensions.IndexOfAnyExceptInRange(Span<RefType>.Empty, low, high));
                AssertExtensions.Throws<ArgumentNullException>(argName, () => MemoryExtensions.IndexOfAnyExceptInRange(ReadOnlySpan<RefType>.Empty, low, high));

                AssertExtensions.Throws<ArgumentNullException>(argName, () => MemoryExtensions.LastIndexOfAnyExceptInRange(Span<RefType>.Empty, low, high));
                AssertExtensions.Throws<ArgumentNullException>(argName, () => MemoryExtensions.LastIndexOfAnyExceptInRange(ReadOnlySpan<RefType>.Empty, low, high));
            }
        }

        [Fact]
        public void NullPermittedInInput()
        {
            RefType[] array = new[] { null, new RefType { Value = 1 }, null, null, new RefType { Value = 42 }, null };
            RefType low = new RefType { Value = 41 }, high = new RefType { Value = 43 };

            Assert.Equal(4, IndexOfAnyInRange(array, low, high));
            Assert.Equal(4, LastIndexOfAnyInRange(array, low, high));

            Assert.Equal(0, IndexOfAnyExceptInRange(array, low, high));
            Assert.Equal(5, LastIndexOfAnyExceptInRange(array, low, high));
        }
    }

    public class RefType : IComparable<RefType>
    {
        public int Value { get; set; }
        public int CompareTo(RefType? other) => other is null ? 1 : Value.CompareTo(other.Value);
    }

    public abstract class IndexOfAnyInRangeTests<T>
        where T : IComparable<T>
    {
        protected abstract T Create(int value);

        [Fact]
        public void EmptyInput_ReturnsMinus1()
        {
            T[] array = Array.Empty<T>();
            T low = Create(0);
            T high = Create(255);

            Assert.Equal(-1, IndexOfAnyInRange(array, low, high));
            Assert.Equal(-1, LastIndexOfAnyInRange(array, low, high));
            Assert.Equal(-1, IndexOfAnyExceptInRange(array, low, high));
            Assert.Equal(-1, LastIndexOfAnyExceptInRange(array, low, high));
        }

        [Fact]
        public void NotFound_ReturnsMinus1()
        {
            T fillValue = Create(42);
            for (int length = 0; length < 128; length++)
            {
                T[] array = Enumerable.Repeat(fillValue, length).ToArray();

                Assert.Equal(-1, IndexOfAnyInRange(array, Create(43), Create(44)));
                Assert.Equal(-1, LastIndexOfAnyInRange(array, Create(43), Create(44)));
                Assert.Equal(-1, IndexOfAnyExceptInRange(array, Create(42), Create(44)));
                Assert.Equal(-1, LastIndexOfAnyExceptInRange(array, Create(42), Create(44)));
            }
        }

        [Fact]
        public void FindValueInSequence_ReturnsFoundIndexOrMinus1()
        {
            for (int length = 1; length <= 64; length++)
            {
                T[] array = Enumerable.Range(0, length).Select(Create).ToArray();

                Assert.Equal(0, IndexOfAnyInRange(array, Create(0), Create(length - 1)));
                Assert.Equal(length - 1, LastIndexOfAnyInRange(array, Create(0), Create(length - 1)));
                Assert.Equal(-1, IndexOfAnyExceptInRange(array, Create(0), Create(length - 1)));
                Assert.Equal(-1, LastIndexOfAnyExceptInRange(array, Create(0), Create(length - 1)));

                if (length > 4)
                {
                    Assert.Equal(length / 4, IndexOfAnyInRange(array, Create(length / 4), Create(length * 3 / 4)));
                    Assert.Equal(length * 3 / 4, LastIndexOfAnyInRange(array, Create(length / 4), Create(length * 3 / 4)));
                    Assert.Equal(0, IndexOfAnyExceptInRange(array, Create(length / 4), Create(length * 3 / 4)));
                    Assert.Equal(length - 1, LastIndexOfAnyExceptInRange(array, Create(length / 4), Create(length * 3 / 4)));
                }
            }
        }

        [Fact]
        public void MatrixOfLengthsAndOffsets_Any_FindsValueAtExpectedIndex()
        {
            foreach (int length in Enumerable.Range(1, 40))
            {
                for (int offset = 0; offset < length; offset++)
                {
                    int target = -42;
                    T[] array = Enumerable.Repeat(Create(target - 2), length).ToArray();
                    array[offset] = Create(target);

                    Assert.Equal(offset, IndexOfAnyInRange(array, Create(target), Create(target)));
                    Assert.Equal(offset, IndexOfAnyInRange(array, Create(target - 1), Create(target)));
                    Assert.Equal(offset, IndexOfAnyInRange(array, Create(target), Create(target + 1)));
                    Assert.Equal(offset, IndexOfAnyInRange(array, Create(target - 1), Create(target + 1)));

                    Assert.Equal(offset, LastIndexOfAnyInRange(array, Create(target), Create(target)));
                    Assert.Equal(offset, LastIndexOfAnyInRange(array, Create(target - 1), Create(target)));
                    Assert.Equal(offset, LastIndexOfAnyInRange(array, Create(target), Create(target + 1)));
                    Assert.Equal(offset, LastIndexOfAnyInRange(array, Create(target - 1), Create(target + 1)));
                }
            }
        }

        [Fact]
        public void MatrixOfLengthsAndOffsets_AnyExcept_FindsValueAtExpectedIndex()
        {
            foreach (int length in Enumerable.Range(1, 40))
            {
                for (int offset = 0; offset < length; offset++)
                {
                    int target = -42;
                    T[] array = Enumerable.Repeat(Create(target), length).ToArray();
                    array[offset] = Create(target - 2);

                    Assert.Equal(offset, IndexOfAnyExceptInRange(array, Create(target), Create(target)));
                    Assert.Equal(offset, IndexOfAnyExceptInRange(array, Create(target - 1), Create(target)));
                    Assert.Equal(offset, IndexOfAnyExceptInRange(array, Create(target), Create(target + 1)));
                    Assert.Equal(offset, IndexOfAnyExceptInRange(array, Create(target - 1), Create(target + 1)));

                    Assert.Equal(offset, LastIndexOfAnyExceptInRange(array, Create(target), Create(target)));
                    Assert.Equal(offset, LastIndexOfAnyExceptInRange(array, Create(target - 1), Create(target)));
                    Assert.Equal(offset, LastIndexOfAnyExceptInRange(array, Create(target), Create(target + 1)));
                    Assert.Equal(offset, LastIndexOfAnyExceptInRange(array, Create(target - 1), Create(target + 1)));
                }
            }
        }

        // Wrappers for {Last}IndexOfAny{Except}InRange that invoke both the Span and ReadOnlySpan overloads,
        // ensuring they both produce the same result, and returning that result.
        // This avoids needing to code the same call sites twice in all the above tests.

        protected static int IndexOfAnyInRange(Span<T> span, T lowInclusive, T highInclusive)
        {
            int result = MemoryExtensions.IndexOfAnyInRange(span, lowInclusive, highInclusive);
            Assert.Equal(result, MemoryExtensions.IndexOfAnyInRange((ReadOnlySpan<T>)span, lowInclusive, highInclusive));
            return result;
        }

        protected static int LastIndexOfAnyInRange(Span<T> span, T lowInclusive, T highInclusive)
        {
            int result = MemoryExtensions.LastIndexOfAnyInRange(span, lowInclusive, highInclusive);
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyInRange((ReadOnlySpan<T>)span, lowInclusive, highInclusive));
            return result;
        }

        protected static int IndexOfAnyExceptInRange(Span<T> span, T lowInclusive, T highInclusive)
        {
            int result = MemoryExtensions.IndexOfAnyExceptInRange(span, lowInclusive, highInclusive);
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExceptInRange((ReadOnlySpan<T>)span, lowInclusive, highInclusive));
            return result;
        }

        protected static int LastIndexOfAnyExceptInRange(Span<T> span, T lowInclusive, T highInclusive)
        {
            int result = MemoryExtensions.LastIndexOfAnyExceptInRange(span, lowInclusive, highInclusive);
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExceptInRange((ReadOnlySpan<T>)span, lowInclusive, highInclusive));
            return result;
        }
    }
}
