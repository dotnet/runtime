// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Theory]
        [InlineData(new uint[] { 1u, 2u, 3u, 4u }, 0u, -1)]
        [InlineData(new uint[] { 1u, 2u, 3u, 4u }, 1u, 0)]
        [InlineData(new uint[] { 1u, 2u, 3u, 4u }, 2u, 1)]
        [InlineData(new uint[] { 1u, 2u, 3u, 4u }, 3u, 2)]
        [InlineData(new uint[] { 1u, 2u, 3u, 4u }, 4u, 3)]
        [InlineData(new uint[] { 1u, 2u, 3u, 4u }, 5u, -5)]
        public static void BinarySearch_UInt(uint[] a, uint value, int expectedIndex)
        {
            ReadOnlySpan<uint> span = new ReadOnlySpan<uint>(a);

            var index = span.BinarySearch(value);

            Assert.Equal(expectedIndex, index);
        }

        [Fact]
        public static void BinarySearch_String_B()
        {
            string[] a = { "a", "b", "c", "d" };
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);

            var index = span.BinarySearch("b");

            Assert.Equal(1, index);
        }

        //[Fact]
        //public static void AsBytesContainsReferences()
        //{
        //    ReadOnlySpan<StructWithReferences> span = new ReadOnlySpan<StructWithReferences>(Array.Empty<StructWithReferences>());
        //    TestHelpers.AssertThrows<ArgumentException, StructWithReferences>(span, (_span) => _span.AsBytes().DontBox());
        //}
    }
}
