// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;

namespace System.SpanTests
{
    public partial class ReadOnlySpanTests
    {
        public static IEnumerable<object[]> MemoryExtensionsToUpperLowerOverlapping()
        {
            // full overlap, overlap in the middle, overlap at start, overlap at the end

            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToLower(buffer, null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToLower(buffer.Slice(1, 1), null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToLower(buffer.Slice(0, 1), null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToLower(buffer.Slice(2, 1), null) };

            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToLower(buffer, buffer, null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToLower(buffer, buffer.Slice(1, 1), null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToLower(buffer, buffer.Slice(0, 1), null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToLower(buffer, buffer.Slice(2, 1), null) };

            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToLowerInvariant(buffer) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToLowerInvariant(buffer.Slice(1, 1)) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToLowerInvariant(buffer.Slice(0, 1)) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToLowerInvariant(buffer.Slice(2, 1)) };

            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToLowerInvariant(buffer, buffer) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToLowerInvariant(buffer, buffer.Slice(1, 1)) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToLowerInvariant(buffer, buffer.Slice(0, 1)) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToLowerInvariant(buffer, buffer.Slice(2, 1)) };

            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToUpper(buffer, null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToUpper(buffer.Slice(1, 1), null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToUpper(buffer.Slice(0, 1), null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToUpper(buffer.Slice(2, 1), null) };

            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToUpper(buffer, buffer, null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToUpper(buffer, buffer.Slice(1, 1), null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToUpper(buffer, buffer.Slice(0, 1), null) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToUpper(buffer, buffer.Slice(2, 1), null) };

            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToUpperInvariant(buffer) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToUpperInvariant(buffer.Slice(1, 1)) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToUpperInvariant(buffer.Slice(0, 1)) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => ((ReadOnlySpan<char>)buffer).ToUpperInvariant(buffer.Slice(2, 1)) };

            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToUpperInvariant(buffer, buffer) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToUpperInvariant(buffer, buffer.Slice(1, 1)) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToUpperInvariant(buffer, buffer.Slice(0, 1)) };
            yield return new TestHelpers.AssertThrowsAction<char>[] { (Span<char> buffer) => MemoryExtensions.ToUpperInvariant(buffer, buffer.Slice(2, 1)) };
        }

        [Theory]
        [MemberData(nameof(MemoryExtensionsToUpperLowerOverlapping))]
        public static void MemoryExtensionsToUpperLowerOverlappingThrows(TestHelpers.AssertThrowsAction<char> action)
        {
            Span<char> buffer = new char[] { 'a', 'b', 'c', 'd' };
            TestHelpers.AssertThrows<InvalidOperationException, char>(buffer, action);
        }
    }
}
