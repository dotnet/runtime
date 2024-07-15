// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        public record struct CustomStruct(int value) : IEquatable<CustomStruct>;
        public record class CustomClass(int value) : IEquatable<CustomClass>;

        [Fact]
        public static void DefaultSpanSplitEnumeratorBehaviour()
        {
            var charSpanEnumerator = new MemoryExtensions.SpanSplitEnumerator<char>();
            Assert.Equal(new Range(0, 0), charSpanEnumerator.Current);
            Assert.False(charSpanEnumerator.MoveNext());

            // Implicit DoesNotThrow assertion
            charSpanEnumerator.GetEnumerator();

            var stringSpanEnumerator = new MemoryExtensions.SpanSplitEnumerator<string>();
            Assert.Equal(new Range(0, 0), stringSpanEnumerator.Current);
            Assert.False(stringSpanEnumerator.MoveNext());
            stringSpanEnumerator.GetEnumerator();
        }

        public static IEnumerable<object[]> SplitSingleElementSeparatorData =>
        [
            // Split on default
            [ (char[])['a', ' ', 'b'], default(char), (Range[])[0..3] ],
            [ (int[]) [1, 2, 3], default(int),        (Range[])[0..3] ],
            [ (long[])[1, 2, 3], default(long),       (Range[])[0..3] ],
            [ (byte[])[1, 2, 3], default(byte),       (Range[])[0..3] ],
            [ (CustomStruct[])[new(1), new(2), new(3)], default(CustomStruct), (Range[])[0..3] ],
            [ (CustomClass[])[new(1), new(2), new(3)],  default(CustomClass),  (Range[])[0..3] ],

            // Split no matching element
            [ (char[])['a', ' ', 'b'], ',', (Range[])[0..3] ],
            [ (int[]) [1, 2, 3], (int)4,    (Range[])[0..3] ],
            [ (long[])[1, 2, 3], (long)4,   (Range[])[0..3] ],
            [ (byte[])[1, 2, 3], (byte)4,   (Range[])[0..3] ],
            [ (CustomStruct[])[new(1), new(2), new(3)], new CustomStruct(4), (Range[])[0..3] ],
            [ (CustomClass[])[new(1), new(2), new(3)],  new CustomClass(4),  (Range[])[0..3] ],

            // Split on sequence containing only a separator
            [ (char[])[','], ',',     (Range[])[0..0, 1..1] ],
            [ (int[]) [1], (int)1,    (Range[])[0..0, 1..1] ],
            [ (long[])[1], (long)1,   (Range[])[0..0, 1..1] ],
            [ (byte[])[1], (byte)1,   (Range[])[0..0, 1..1] ],
            [ (CustomStruct[])[new(1)], new CustomStruct(1), (Range[])[0..0, 1..1] ],
            [ (CustomClass[]) [new(1)], new CustomClass(1),  (Range[])[0..0, 1..1] ],

            // Split on empty sequence with default separator
            [ (char[])[], default(char), (Range[])[0..0] ],
            [ (int[]) [], default(int),  (Range[])[0..0] ],
            [ (long[])[], default(long), (Range[])[0..0] ],
            [ (byte[])[], default(byte), (Range[])[0..0] ],
            [ (CustomStruct[])[], default(CustomStruct), (Range[])[0..0] ],
            [ (CustomClass[]) [], default(CustomClass),  (Range[])[0..0] ],

            [ (char[])['a', ',', 'b'], ',', (Range[]) [ 0..1, 2..3 ] ],
            [ (int[]) [1, 2, 3], (int)2,    (Range[]) [ 0..1, 2..3 ] ],
            [ (long[])[1, 2, 3], (long)2,   (Range[]) [ 0..1, 2..3 ] ],
            [ (byte[])[1, 2, 3], (byte)2,   (Range[]) [ 0..1, 2..3 ] ],
            [ (CustomStruct[])[new(1), new(2), new(3)], new CustomStruct(2), (Range[]) [ 0..1, 2..3 ] ],
            [ (CustomClass[])[new(1), new(2), new(3)],  new CustomClass(2),  (Range[]) [ 0..1, 2..3 ] ],

            [ (char[])['a', 'b', ',', ','], ',', (Range[]) [ 0..2, 3..3, 4..4 ] ],
            [ (int[]) [1, 3, 2, 2], (int)2,      (Range[]) [ 0..2, 3..3, 4..4 ] ],
            [ (long[])[1, 3, 2, 2], (long)2,     (Range[]) [ 0..2, 3..3, 4..4 ] ],
            [ (byte[])[1, 3, 2, 2], (byte)2,     (Range[]) [ 0..2, 3..3, 4..4 ] ],
            [ (CustomStruct[])[new(1), new(3), new(2), new(2)], new CustomStruct(2), (Range[]) [ 0..2, 3..3, 4..4 ] ],
            [ (CustomClass[])[new(1), new(3), new(2), new(2)],  new CustomClass(2),  (Range[]) [ 0..2, 3..3, 4..4 ] ],
        ];

        [Theory]
        [MemberData(nameof(SplitSingleElementSeparatorData))]
        public static void Split_SingleElementSeparator<T>(T[] value, T separator, Range[] result) where T : IEquatable<T>
        {
            AssertEnsureCorrectEnumeration(new ReadOnlySpan<T>(value).Split(separator), result);
        }

        public static IEnumerable<object[]> SplitSequenceSeparatorData =>
        [
            // Split no separators
            [ (char[])['a', ' ', 'b'], (char[])[],      (Range[])[0..3] ],
            [ (int[]) [1, 2, 3],       (int[]) [],      (Range[])[0..3] ],
            [ (long[])[1, 2, 3],       (long[])[],      (Range[])[0..3] ],
            [ (byte[])[1, 2, 3],       (byte[])[],      (Range[])[0..3] ],
            [ (CustomStruct[])[new(1), new(2), new(3)], (CustomStruct[])[], (Range[])[0..3] ],
            [ (CustomClass[])[new(1), new(2), new(3)],  (CustomClass[])[],  (Range[])[0..3] ],

            // Split no matching elements
            [ (char[])['a', ' ', 'b'], (char[])[',', '.' ], (Range[])[0..3] ],
            [ (int[]) [1, 2, 3],       (int[]) [4, 3],      (Range[])[0..3] ],
            [ (long[])[1, 2, 3],       (long[])[4, 3],      (Range[])[0..3] ],
            [ (byte[])[1, 2, 3],       (byte[])[4, 3],      (Range[])[0..3] ],
            [ (CustomStruct[])[new(1), new(2), new(3)], (CustomStruct[])[new(4), new(3)], (Range[])[0..3] ],
            [ (CustomClass[])[new(1), new(2), new(3)],  (CustomClass[])[new(4), new(3)],  (Range[])[0..3] ],

            // Split on input span with only a single sequence separator
            [ (char[])[',', '.'], (char[])[',', '.' ], (Range[])[0..0, 2..2] ],
            [ (int[]) [4, 3],     (int[]) [4, 3],      (Range[])[0..0, 2..2] ],
            [ (long[])[4, 3],     (long[])[4, 3],      (Range[])[0..0, 2..2] ],
            [ (byte[])[4, 3],     (byte[])[4, 3],      (Range[])[0..0, 2..2] ],
            [ (CustomStruct[])[new(4), new(3)], (CustomStruct[])[new(4), new(3)], (Range[])[0..0, 2..2] ],
            [ (CustomClass[])[new(4), new(3)],  (CustomClass[])[new(4), new(3)],  (Range[])[0..0, 2..2] ],

            // Split on empty sequence with default separator
            [ (char[])[], (char[])[default(char)], (Range[])[0..0] ],
            [ (int[]) [], (int[]) [default(int)],  (Range[])[0..0] ],
            [ (long[])[], (long[])[default(long)], (Range[])[0..0] ],
            [ (byte[])[], (byte[])[default(byte)], (Range[])[0..0] ],
            [ (CustomStruct[])[], (CustomStruct[])[default], (Range[])[0..0] ],
            [ (CustomClass[]) [], (CustomClass[])[default],  (Range[])[0..0] ],

            [ (char[])['a', ',', '-', 'b'], (char[])[',', '-'], (Range[]) [ 0..1, 3..4 ] ],
            [ (int[]) [1, 2, 4, 3], (int[])[2, 4],    (Range[]) [ 0..1, 3..4 ] ],
            [ (long[])[1, 2, 4, 3], (long[])[2, 4],   (Range[]) [ 0..1, 3..4 ] ],
            [ (byte[])[1, 2, 4, 3], (byte[])[2, 4],   (Range[]) [ 0..1, 3..4 ] ],
            [ (CustomStruct[])[new(1), new(2), new(4), new(3)], (CustomStruct[]) [new(2), new(4)], (Range[]) [ 0..1, 3..4 ] ],
            [ (CustomClass[])[new(1), new(2), new(4), new(3)], (CustomClass[])[new(2), new(4)],  (Range[]) [ 0..1, 3..4 ] ],

            [ (char[])[',', '-', 'a', ',', '-', 'b'], (char[])[',', '-'], (Range[]) [ 0..0, 2..3, 5..6 ] ],
            [ (int[]) [2, 4, 3, 2, 4, 5],             (int[]) [2, 4],     (Range[]) [ 0..0, 2..3, 5..6 ] ],
            [ (long[])[2, 4, 3, 2, 4, 5],             (long[])[2, 4],     (Range[]) [ 0..0, 2..3, 5..6 ] ],
            [ (byte[])[2, 4, 3, 2, 4, 5],             (byte[])[2, 4],     (Range[]) [ 0..0, 2..3, 5..6 ] ],
            [ (CustomStruct[])[new(2), new(4), new(3), new(2), new(4), new(5)], (CustomStruct[]) [new(2), new(4)], (Range[]) [ 0..0, 2..3, 5..6 ] ],
            [ (CustomClass[])[new(2), new(4), new(3), new(2), new(4), new(5)],  (CustomClass[])[new(2), new(4)],  (Range[]) [ 0..0, 2..3, 5..6 ] ],
        ];

        [Theory]
        [MemberData(nameof(SplitSequenceSeparatorData))]
        public static void Split_SequenceSeparator<T>(T[] value, T[] separator, Range[] result) where T : IEquatable<T>
        {
            AssertEnsureCorrectEnumeration(new ReadOnlySpan<T>(value).Split(separator), result);
        }

        public static IEnumerable<object[]> SplitAnySeparatorData =>
        [
            // Split no separators
            [ (char[])['a', ' ', 'b'], (char[])[],      (Range[])[0..1, 2..3] ], // an empty span of separators for char is handled as all whitespace being separators
            [ (int[]) [1, 2, 3],       (int[]) [],      (Range[])[0..3] ],
            [ (long[])[1, 2, 3],       (long[])[],      (Range[])[0..3] ],
            [ (byte[])[1, 2, 3],       (byte[])[],      (Range[])[0..3] ],
            [ (CustomStruct[])[new(1), new(2), new(3)], (CustomStruct[])[], (Range[])[0..3] ],
            [ (CustomClass[])[new(1), new(2), new(3)],  (CustomClass[])[],  (Range[])[0..3] ],

            // Split non-matching separators
            [ (char[])['a', ' ', 'b'], (char[])[',', '.' ], (Range[])[0..3] ],
            [ (int[]) [1, 2, 3],       (int[]) [4, 5],      (Range[])[0..3] ],
            [ (long[])[1, 2, 3],       (long[])[4, 5],      (Range[])[0..3] ],
            [ (byte[])[1, 2, 3],       (byte[])[4, 5],      (Range[])[0..3] ],
            [ (CustomStruct[])[new(1), new(2), new(3)], (CustomStruct[])[new(4), new(5)], (Range[])[0..3] ],
            [ (CustomClass[])[new(1), new(2), new(3)],  (CustomClass[])[new(4), new(5)],  (Range[])[0..3] ],

            // Split on sequence containing only a separator
            [ (char[])[','], (char[])[','], (Range[])[0..0, 1..1] ],
            [ (int[]) [1],   (int[]) [1],   (Range[])[0..0, 1..1] ],
            [ (long[])[1],   (long[])[1],   (Range[])[0..0, 1..1] ],
            [ (byte[])[1],   (byte[])[1],   (Range[])[0..0, 1..1] ],
            [ (CustomStruct[])[new(1)], (CustomStruct[])[new(1)], (Range[])[0..0, 1..1] ],
            [ (CustomClass[]) [new(1)], (CustomClass[])[new(1)],  (Range[])[0..0, 1..1] ],

            // Split on empty sequence with default separator
            [ (char[])[], (char[])[default(char)], (Range[])[0..0] ],
            [ (int[]) [], (int[]) [default(int)],  (Range[])[0..0] ],
            [ (long[])[], (long[])[default(long)], (Range[])[0..0] ],
            [ (byte[])[], (byte[])[default(byte)], (Range[])[0..0] ],
            [ (CustomStruct[])[], (CustomStruct[])[new(default)], (Range[])[0..0] ],
            [ (CustomClass[]) [], (CustomClass[])[new(default)],  (Range[])[0..0] ],

            [ (char[])['a', ',', '-', 'b'], (char[])[',', '-'], (Range[]) [ 0..1, 2..2, 3..4 ] ],
            [ (int[]) [1, 2, 4, 3], (int[])[2, 4],    (Range[]) [ 0..1, 2..2, 3..4 ] ],
            [ (long[])[1, 2, 4, 3], (long[])[2, 4],   (Range[]) [ 0..1, 2..2, 3..4 ] ],
            [ (byte[])[1, 2, 4, 3], (byte[])[2, 4],   (Range[]) [ 0..1, 2..2, 3..4 ] ],
            [ (CustomStruct[])[new(1), new(2), new(4), new(3)], (CustomStruct[]) [new(2), new(4)], (Range[]) [ 0..1, 2..2, 3..4 ] ],
            [ (CustomClass[])[new(1), new(2), new(4), new(3)], (CustomClass[])[new(2), new(4)],  (Range[]) [ 0..1, 2..2, 3..4 ] ],

            [ (char[])[',', '-', 'a', ',', '-', 'b'], (char[])[',', '-'], (Range[]) [ 0..0, 1..1, 2..3, 4..4, 5..6 ] ],
            [ (int[]) [2, 4, 3, 2, 4, 5],             (int[]) [2, 4],     (Range[]) [ 0..0, 1..1, 2..3, 4..4, 5..6 ] ],
            [ (long[])[2, 4, 3, 2, 4, 5],             (long[])[2, 4],     (Range[]) [ 0..0, 1..1, 2..3, 4..4, 5..6 ] ],
            [ (byte[])[2, 4, 3, 2, 4, 5],             (byte[])[2, 4],     (Range[]) [ 0..0, 1..1, 2..3, 4..4, 5..6 ] ],
            [ (CustomStruct[])[new(2), new(4), new(3), new(2), new(4), new(5)], (CustomStruct[]) [new(2), new(4)], (Range[]) [ 0..0, 1..1, 2..3, 4..4, 5..6 ] ],
            [ (CustomClass[])[new(2), new(4), new(3), new(2), new(4), new(5)],  (CustomClass[])[new(2), new(4)],  (Range[]) [ 0..0, 1..1, 2..3, 4..4, 5..6 ] ],
        ];

        [Theory]
        [MemberData(nameof(SplitAnySeparatorData))]
        public static void Split_AnySingleElementSeparator<T>(T[] value, T[] separator, Range[] result) where T : IEquatable<T>
        {
            AssertEnsureCorrectEnumeration(new ReadOnlySpan<T>(value).SplitAny(separator), result);

            if (value is char[] source &&
                separator is char[] separators &&
                separators.Length > 0) // the SearchValues overload does not special-case empty
            {
                var charEnumerator = new ReadOnlySpan<char>(source).SplitAny(SearchValues.Create(separators));
                AssertEnsureCorrectEnumeration(charEnumerator, result);
            }
        }

        private static void AssertEnsureCorrectEnumeration<T>(MemoryExtensions.SpanSplitEnumerator<T> enumerator, Range[] result) where T : IEquatable<T>
        {
            foreach ((Range r, int index) in ((Range[])[0..0]).Concat(result).Select((e, i) => (e, i)))
            {
                Assert.Equal(r, enumerator.Current);
                if (index < result.Length)
                    Assert.True(enumerator.MoveNext());
            }
            Assert.False(enumerator.MoveNext());
        }
    }
}
