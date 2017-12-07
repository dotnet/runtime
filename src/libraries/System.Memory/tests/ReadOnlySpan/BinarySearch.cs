// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        public static TheoryData<(uint[] Array, uint Value, int ExpectedIndex)> UIntCases =>
            new TheoryData<(uint[] Array, uint Value, int ExpectedIndex)> {
                (new uint[] { }, 0u, -1),
                (new uint[] { 1u, 2u, 4u, 5u }, 0u, -1),
                (new uint[] { 1u, 2u, 4u, 5u }, 1u, 0),
                (new uint[] { 1u, 2u, 4u, 5u }, 2u, 1),
                (new uint[] { 1u, 2u, 4u, 5u }, 3u, -3),
                (new uint[] { 1u, 2u, 4u, 5u }, 4u, 2),
                (new uint[] { 1u, 2u, 4u, 5u }, 5u, 3),
                (new uint[] { 1u, 2u, 4u, 5u }, 6u, -5),
            };
        public static TheoryData<(double[] Array, double Value, int ExpectedIndex)> DoubleCases =>
            new TheoryData<(double[] Array, double Value, int ExpectedIndex)> {
                (new double[] { }, 0u, -1),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 0.0, -1),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 1.0, 0),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 2.0, 1),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 3.0, -3),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 4.0, 2),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 5.0, 3),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 6.0, -5),
            };
        public static TheoryData<(string[] Array, string Value, int ExpectedIndex)> StringCases =>
            new TheoryData<(string[] Array, string Value, int ExpectedIndex)> {
                (new string[] { "b", "c", "e", "f" }, "a", -1),
                (new string[] { "b", "c", "e", "f" }, "b", 0),
                (new string[] { "b", "c", "e", "f" }, "c", 1),
                (new string[] { "b", "c", "e", "f" }, "d", -3),
                (new string[] { "b", "c", "e", "f" }, "e", 2),
                (new string[] { "b", "c", "e", "f" }, "f", 3),
                (new string[] { "b", "c", "e", "f" }, "g", -5),
            };

        [Theory, MemberData(nameof(UIntCases))]
        public static void BinarySearch_UInt_Span(
            (uint[] Array, uint Value, int ExpectedIndex) c)
        {
            var span = new Span<uint>(c.Array);
            var index = span.BinarySearch(c.Value);
            Assert.Equal(c.ExpectedIndex, index);
        }
        [Theory, MemberData(nameof(UIntCases))]
        public static void BinarySearch_UInt_ReadOnlySpan(
            (uint[] Array, uint Value, int ExpectedIndex) c)
        {
            var span = new ReadOnlySpan<uint>(c.Array);
            var index = span.BinarySearch(c.Value);
            Assert.Equal(c.ExpectedIndex, index);
        }

        [Theory, MemberData(nameof(DoubleCases))]
        public static void BinarySearch_Double_Span(
            (double[] Array, double Value, int ExpectedIndex) c)
        {
            var span = new Span<double>(c.Array);
            var index = span.BinarySearch(c.Value);
            Assert.Equal(c.ExpectedIndex, index);
        }
        [Theory, MemberData(nameof(DoubleCases))]
        public static void BinarySearch_Double_ReadOnlySpan(
            (double[] Array, double Value, int ExpectedIndex) c)
        {
            var span = new ReadOnlySpan<double>(c.Array);
            var index = span.BinarySearch(c.Value);
            Assert.Equal(c.ExpectedIndex, index);
        }

        [Theory, MemberData(nameof(StringCases))]
        public static void BinarySearch_String_Span(
            (string[] Array, string Value, int ExpectedIndex) c)
        {
            var span = new Span<string>(c.Array);
            var index = span.BinarySearch(c.Value);
            Assert.Equal(c.ExpectedIndex, index);
        }
        [Theory, MemberData(nameof(StringCases))]
        public static void BinarySearch_String_ReadOnlySpan(
            (string[] Array, string Value, int ExpectedIndex) c)
        {
            var span = new ReadOnlySpan<string>(c.Array);
            var index = span.BinarySearch(c.Value);
            Assert.Equal(c.ExpectedIndex, index);
        }
    }
}
