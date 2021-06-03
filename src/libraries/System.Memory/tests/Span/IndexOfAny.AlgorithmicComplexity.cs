// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void IndexOfAny_LastIndexOfAny_AlgComplexity_Bytes()
            => RunIndexOfAnyLastIndexOfAnyAlgComplexityTest<byte>();

        [Fact]
        public static void IndexOfAny_LastIndexOfAny_AlgComplexity_Chars()
            => RunIndexOfAnyLastIndexOfAnyAlgComplexityTest<char>();

        [Fact]
        public static void IndexOfAny_LastIndexOfAny_AlgComplexity_Ints()
            => RunIndexOfAnyLastIndexOfAnyAlgComplexityTest<int>();

        [Fact]
        public static void IndexOfAny_LastIndexOfAny_AlgComplexity_RefType()
        {
            // Similar to RunIndexOfAnyAlgComplexityTest (see comments there), but we can't use
            // BoundedMemory because we're dealing with ref types. Instead, we'll trap the call to
            // Equals and use that to fail the test.

            Span<CustomEquatableType<int>> haystack = new CustomEquatableType<int>[8192];
            haystack[1024] = new CustomEquatableType<int>(default, isPoison: true); // fail the test if we iterate this far
            haystack[^1024] = new CustomEquatableType<int>(default, isPoison: true);

            Span<CustomEquatableType<int>> needle = Enumerable.Range(100, 20).Select(val => new CustomEquatableType<int>(val)).ToArray();
            for (int i = 0; i < needle.Length; i++)
            {
                haystack[4096] = needle[i];
                Assert.Equal(2048, MemoryExtensions.IndexOfAny(haystack[2048..], needle));
                Assert.Equal(2048, MemoryExtensions.IndexOfAny((ReadOnlySpan<CustomEquatableType<int>>)haystack[2048..], needle));
                Assert.Equal(4096, MemoryExtensions.LastIndexOfAny(haystack[..^2048], needle));
                Assert.Equal(4096, MemoryExtensions.LastIndexOfAny((ReadOnlySpan<CustomEquatableType<int>>)haystack[..^2048], needle));
            }
        }

        private static void RunIndexOfAnyLastIndexOfAnyAlgComplexityTest<T>() where T : unmanaged, IEquatable<T>
        {
            T[] needles = GetIndexOfAnyNeedlesForAlgComplexityTest<T>().ToArray();
            RunIndexOfAnyAlgComplexityTest<T>(needles);
            RunLastIndexOfAnyAlgComplexityTest<T>(needles);
        }

        private static void RunIndexOfAnyAlgComplexityTest<T>(T[] needle) where T : unmanaged, IEquatable<T>
        {
            // For the following paragraphs, let:
            //   n := length of haystack
            //   i := index of first occurrence of any needle within haystack
            //   l := length of needle array
            //
            // This test ensures that the complexity of IndexOfAny is O(i * l) rather than O(n * l),
            // or just O(n * l) if no needle is found. The reason for this is that it's common for
            // callers to invoke IndexOfAny immediately before slicing, and when this is called in
            // a loop, we want the entire loop to be bounded by O(n * l) rather than O(n^2 * l).
            //
            // We test this by utilizing the BoundedMemory infrastructure to allocate a poison page
            // after the scratch buffer, then we intentionally use MemoryMarshal to manipulate the
            // scratch buffer so that it extends into the poison page. If the runtime skips past the
            // first occurrence of the needle and attempts to read all the way to the end of the span,
            // this will manifest as an AV within this unit test.

            using BoundedMemory<T> boundedMem = BoundedMemory.Allocate<T>(4096, PoisonPagePlacement.After);
            Span<T> span = boundedMem.Span;
            span.Clear();

            span = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), span.Length + 4096);

            for (int i = 0; i < needle.Length; i++)
            {
                span[1024] = needle[i];
                Assert.Equal(1024, MemoryExtensions.IndexOfAny(span, needle));
                Assert.Equal(1024, MemoryExtensions.IndexOfAny((ReadOnlySpan<T>)span, needle));
            }
        }

        private static void RunLastIndexOfAnyAlgComplexityTest<T>(T[] needle) where T : unmanaged, IEquatable<T>
        {
            // Similar to RunIndexOfAnyAlgComplexityTest (see comments there), but we run backward
            // since we're testing LastIndexOfAny.

            using BoundedMemory<T> boundedMem = BoundedMemory.Allocate<T>(4096, PoisonPagePlacement.Before);
            Span<T> span = boundedMem.Span;
            span.Clear();

            span = MemoryMarshal.CreateSpan(ref Unsafe.Subtract(ref MemoryMarshal.GetReference(span), 4096), span.Length + 4096);

            for (int i = 0; i < needle.Length; i++)
            {
                span[^1024] = needle[i];
                Assert.Equal(span.Length - 1024, MemoryExtensions.LastIndexOfAny(span, needle));
                Assert.Equal(span.Length - 1024, MemoryExtensions.LastIndexOfAny((ReadOnlySpan<T>)span, needle));
            }
        }

        // returns [ 'a', 'b', 'c', ... ], or the equivalent in bytes, ints, etc.
        private static IEnumerable<T> GetIndexOfAnyNeedlesForAlgComplexityTest<T>() where T : unmanaged
        {
            for (int i = 0; i < 26; i++)
            {
                yield return (T)Convert.ChangeType('a' + i, typeof(T), CultureInfo.InvariantCulture);
            }
        }

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        private sealed class CustomEquatableType<T> : IEquatable<CustomEquatableType<T>> where T : IEquatable<T>
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        {
            private readonly T _value;
            private readonly bool _isPoison;

            public CustomEquatableType(T value, bool isPoison = false)
            {
                _value = value;
                _isPoison = isPoison;
            }

            public override bool Equals(object obj) => Equals(obj as CustomEquatableType<T>);

            public bool Equals(CustomEquatableType<T> other)
            {
                if (_isPoison)
                {
                    throw new InvalidOperationException("This object is poisoned and its Equals method should not be called.");
                }

                if (other is null) { return false; }
                if (other._isPoison)
                {
                    throw new InvalidOperationException("The 'other' object is poisoned and should not be passed to Equals.");
                }

                return _value.Equals(other._value);
            }
        }
    }
}
