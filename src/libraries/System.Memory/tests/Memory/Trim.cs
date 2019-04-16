// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Theory]
        [InlineData(new int[0], 1, new int[0])]
        [InlineData(new int[] { 1 }, 1, new int[0])]
        [InlineData(new int[] { 2 }, 1, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1 }, 1, new int[] { 2, 1 })]
        [InlineData(new int[] { 1, 1, 2, 1 }, 1, new int[] { 2, 1 })]
        [InlineData(new int[] { 1, 1, 2, 1 }, 2, new int[] { 1, 1, 2, 1 })]
        [InlineData(new int[] { 1, 1, 2, 1 }, 3, new int[] { 1, 1, 2, 1 })]
        [InlineData(new int[] { 1, 1, 1, 2 }, 1, new int[] { 2 })]
        [InlineData(new int[] { 1, 1, 1, 1 }, 1, new int[0])]
        public static void MemoryExtensions_TrimStart_Single(int[] values, int trim, int[] expected)
        {
            Memory<int> memory = new Memory<int>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<int> rom = new ReadOnlyMemory<int>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<int> span = new Span<int>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<int> ros = new ReadOnlySpan<int>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Theory]
        [InlineData(new int[0], 1, new int[0])]
        [InlineData(new int[] { 1 }, 1, new int[0])]
        [InlineData(new int[] { 2 }, 1, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1 }, 1, new int[] { 1, 2 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, 1, new int[] { 1, 2 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, 2, new int[] { 1, 2, 1, 1 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, 3, new int[] { 1, 2, 1, 1 })]
        [InlineData(new int[] { 2, 1, 1, 1 }, 1, new int[] { 2 })]
        [InlineData(new int[] { 1, 1, 1, 1 }, 1, new int[0])]
        public static void MemoryExtensions_TrimEnd_Single(int[] values, int trim, int[] expected)
        {
            Memory<int> memory = new Memory<int>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<int> rom = new ReadOnlyMemory<int>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<int> span = new Span<int>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<int> ros = new ReadOnlySpan<int>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Theory]
        [InlineData(new int[0], 1, new int[0])]
        [InlineData(new int[] { 1 }, 1, new int[0])]
        [InlineData(new int[] { 2 }, 1, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1 }, 1, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, 1, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, 2, new int[] { 1, 2, 1, 1 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, 3, new int[] { 1, 2, 1, 1 })]
        [InlineData(new int[] { 2, 1, 1, 1 }, 1, new int[] { 2 })]
        [InlineData(new int[] { 1, 1, 1, 2 }, 1, new int[] { 2 })]
        [InlineData(new int[] { 1, 1, 1, 1 }, 1, new int[0])]
        public static void MemoryExtensions_Trim_Single(int[] values, int trim, int[] expected)
        {
            Memory<int> memory = new Memory<int>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<int> rom = new ReadOnlyMemory<int>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<int> span = new Span<int>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<int> ros = new ReadOnlySpan<int>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Theory]
        [InlineData(new int[0], new int[0], new int[0])]
        [InlineData(new int[0], new int[] { 1 }, new int[0])]
        [InlineData(new int[] { 1 }, new int[0], new int[] { 1 })]
        [InlineData(new int[] { 1 }, new int[] { 1 }, new int[0])]
        [InlineData(new int[] { 2 }, new int[] { 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1 }, new int[] { 1 }, new int[] { 2, 1 })]
        [InlineData(new int[] { 1, 1, 2, 1 }, new int[] { 1 }, new int[] { 2, 1 })]
        [InlineData(new int[] { 1, 1, 2, 1 }, new int[] { 2 }, new int[] { 1, 1, 2, 1 })]
        [InlineData(new int[] { 1, 1, 2, 1 }, new int[] { 3 }, new int[] { 1, 1, 2, 1 })]
        [InlineData(new int[] { 1, 1, 2, 1 }, new int[] { 1, 2 }, new int[0])]
        [InlineData(new int[] { 1, 1, 2, 3 }, new int[] { 1, 2 }, new int[] { 3 })]
        [InlineData(new int[] { 1, 1, 2, 3 }, new int[] { 1, 2, 4 }, new int[] { 3 })]
        [InlineData(new int[] { 1, 1, 1, 2 }, new int[] { 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 1, 1, 1 }, new int[] { 1 }, new int[0])]
        public static void MemoryExtensions_TrimStart_Multi(int[] values, int[] trims, int[] expected)
        {
            Memory<int> memory = new Memory<int>(values).TrimStart(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<int> rom = new ReadOnlyMemory<int>(values).TrimStart(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<int> span = new Span<int>(values).TrimStart(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<int> ros = new ReadOnlySpan<int>(values).TrimStart(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Theory]
        [InlineData(new int[0], new int[0], new int[0])]
        [InlineData(new int[0], new int[] { 1 }, new int[0])]
        [InlineData(new int[] { 1 }, new int[0], new int[] { 1 })]
        [InlineData(new int[] { 1 }, new int[] { 1 }, new int[0])]
        [InlineData(new int[] { 2 }, new int[] { 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1 }, new int[] { 1 }, new int[] { 1, 2 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, new int[] { 1 }, new int[] { 1, 2 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, new int[] { 2 }, new int[] { 1, 2, 1, 1 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, new int[] { 3 }, new int[] { 1, 2, 1, 1 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, new int[] { 1, 2 }, new int[0])]
        [InlineData(new int[] { 3, 2, 1, 1 }, new int[] { 1, 2 }, new int[] { 3 })]
        [InlineData(new int[] { 3, 2, 1, 1 }, new int[] { 1, 2, 4 }, new int[] { 3 })]
        [InlineData(new int[] { 2, 1, 1, 1 }, new int[] { 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 1, 1, 1 }, new int[] { 1 }, new int[0])]
        public static void MemoryExtensions_TrimEnd_Multi(int[] values, int[] trims, int[] expected)
        {
            Memory<int> memory = new Memory<int>(values).TrimEnd(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<int> rom = new ReadOnlyMemory<int>(values).TrimEnd(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<int> span = new Span<int>(values).TrimEnd(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<int> ros = new ReadOnlySpan<int>(values).TrimEnd(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Theory]
        [InlineData(new int[0], new int[0], new int[0])]
        [InlineData(new int[0], new int[] { 1 }, new int[0])]
        [InlineData(new int[] { 1 }, new int[0], new int[] { 1 })]
        [InlineData(new int[] { 1 }, new int[] { 1 }, new int[0])]
        [InlineData(new int[] { 2 }, new int[] { 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1 }, new int[] { 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, new int[] { 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, new int[] { 2 }, new int[] { 1, 2, 1, 1 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, new int[] { 3 }, new int[] { 1, 2, 1, 1 })]
        [InlineData(new int[] { 1, 2, 1, 1 }, new int[] { 1, 2 }, new int[0])]
        [InlineData(new int[] { 2, 1, 3, 2, 1, 1 }, new int[] { 1, 2 }, new int[] { 3 })]
        [InlineData(new int[] { 2, 1, 3, 2, 1, 1 }, new int[] { 1, 2, 4 }, new int[] { 3 })]
        [InlineData(new int[] { 1, 2, 1, 1, 1 }, new int[] { 1 }, new int[] { 2 })]
        [InlineData(new int[] { 1, 1, 1, 1 }, new int[] { 1 }, new int[0])]
        public static void MemoryExtensions_Trim_Multi(int[] values, int[] trims, int[] expected)
        {
            Memory<int> memory = new Memory<int>(values).Trim(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<int> rom = new ReadOnlyMemory<int>(values).Trim(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<int> span = new Span<int>(values).Trim(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<int> ros = new ReadOnlySpan<int>(values).Trim(trims);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        public sealed class Foo : IEquatable<Foo>
        {
            public int Value { get; set; }

            public bool Equals(Foo other)
            {
                if (this == null && other == null)
                    return true;
                if (other == null)
                    return false;
                return Value == other.Value;
            }

            public static implicit operator Foo(int value) => new Foo { Value = value };
            public static implicit operator int? (Foo foo) => foo?.Value;
        }

        public static IEnumerable<object[]> IdempotentValues => new object[][]
        {
            new object[1] { new Foo[] { } },
            new object[1] { new Foo[] { null, 1, 2, 3, null, 2, 1, null } }
        };

        [Theory]
        [MemberData(nameof(IdempotentValues))]
        public static void MemoryExtensions_TrimStart_Idempotent(Foo[] values)
        {
            Foo[] expected = values;

            Foo[] trim = null;

            Memory<Foo> memory = new Memory<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<Foo> rom = new ReadOnlyMemory<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<Foo> span = new Span<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<Foo> ros = new ReadOnlySpan<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));

            trim = new Foo[] { };

            memory = new Memory<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            rom = new ReadOnlyMemory<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            span = new Span<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ros = new ReadOnlySpan<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Theory]
        [MemberData(nameof(IdempotentValues))]
        public static void MemoryExtensions_TrimEnd_Idempotent(Foo[] values)
        {
            Foo[] expected = values;

            Foo[] trim = null;

            Memory<Foo> memory = new Memory<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<Foo> rom = new ReadOnlyMemory<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<Foo> span = new Span<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<Foo> ros = new ReadOnlySpan<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));

            trim = new Foo[] { };

            memory = new Memory<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            rom = new ReadOnlyMemory<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            span = new Span<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ros = new ReadOnlySpan<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Theory]
        [MemberData(nameof(IdempotentValues))]
        public static void MemoryExtensions_Trim_Idempotent(Foo[] values)
        {
            Foo[] expected = values;

            Foo[] trim = null;

            Memory<Foo> memory = new Memory<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<Foo> rom = new ReadOnlyMemory<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<Foo> span = new Span<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<Foo> ros = new ReadOnlySpan<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));

            trim = new Foo[] { };

            memory = new Memory<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            rom = new ReadOnlyMemory<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            span = new Span<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ros = new ReadOnlySpan<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Fact]
        public static void MemoryExtensions_TrimStart_Single_Null()
        {
            var values = new Foo[] { null, null, 1, 2, null, null };
            Foo trim = null;
            var expected = new Foo[] { 1, 2, null, null };

            Memory<Foo> memory = new Memory<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<Foo> rom = new ReadOnlyMemory<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<Foo> span = new Span<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<Foo> ros = new ReadOnlySpan<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Fact]
        public static void MemoryExtensions_TrimStart_Multi_Null()
        {
            var values = new Foo[] { null, 1, 2, 3, null, 2, 1, null };
            var trim = new Foo[] { null, 1, 2 };
            var expected = new Foo[] { 3, null, 2, 1, null };

            Memory<Foo> memory = new Memory<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<Foo> rom = new ReadOnlyMemory<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<Foo> span = new Span<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<Foo> ros = new ReadOnlySpan<Foo>(values).TrimStart(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Fact]
        public static void MemoryExtensions_TrimEnd_Single_Null()
        {
            var values = new Foo[] { null, null, 1, 2, null, null };
            Foo trim = null;
            var expected = new Foo[] { null, null, 1, 2 };

            Memory<Foo> memory = new Memory<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<Foo> rom = new ReadOnlyMemory<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<Foo> span = new Span<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<Foo> ros = new ReadOnlySpan<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Fact]
        public static void MemoryExtensions_TrimEnd_Multi_Null()
        {
            var values = new Foo[] { null, 1, 2, 3, null, 2, 1, null };
            var trim = new Foo[] { null, 1, 2 };
            var expected = new Foo[] { null, 1, 2, 3 };

            Memory<Foo> memory = new Memory<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<Foo> rom = new ReadOnlyMemory<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<Foo> span = new Span<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<Foo> ros = new ReadOnlySpan<Foo>(values).TrimEnd(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Fact]
        public static void MemoryExtensions_Trim_Single_Null()
        {
            var values = new Foo[] { null, null, 1, 2, null, null };
            Foo trim = null;
            var expected = new Foo[] { 1, 2 };

            Memory<Foo> memory = new Memory<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<Foo> rom = new ReadOnlyMemory<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<Foo> span = new Span<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<Foo> ros = new ReadOnlySpan<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }

        [Fact]
        public static void MemoryExtensions_Trim_Multi_Null()
        {
            var values = new Foo[] { null, 1, 2, 3, null, 2, 1, null };
            var trim = new Foo[] { null, 1, 2 };
            var expected = new Foo[] { 3 };

            Memory<Foo> memory = new Memory<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, memory.ToArray()));

            ReadOnlyMemory<Foo> rom = new ReadOnlyMemory<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, rom.ToArray()));

            Span<Foo> span = new Span<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, span.ToArray()));

            ReadOnlySpan<Foo> ros = new ReadOnlySpan<Foo>(values).Trim(trim);
            Assert.True(System.Linq.Enumerable.SequenceEqual(expected, ros.ToArray()));
        }
    }
}
