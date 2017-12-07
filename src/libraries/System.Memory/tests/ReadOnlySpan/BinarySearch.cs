// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void BinarySearch_Int_2()
        {
            uint[] a = { 1, 2, 3, 4 };
            ReadOnlySpan<uint> span = new ReadOnlySpan<uint>(a);

            var index = span.BinarySearch(2u);

            Assert.Equal(1, index);
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
