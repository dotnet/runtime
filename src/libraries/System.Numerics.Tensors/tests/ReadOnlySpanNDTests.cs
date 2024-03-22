// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class ReadOnlySpanNDTests
    {
        //public static IEnumerable<object[]> MemoryExtensionsToUpperLowerOverlapping()
        //{
        //    // full overlap, overlap in the middle, overlap at start, overlap at the end

        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToLower(buffer, null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToLower(buffer.Slice(1, 1), null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToLower(buffer.Slice(0, 1), null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToLower(buffer.Slice(2, 1), null) };

        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToLower(buffer, buffer, null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToLower(buffer, buffer.Slice(1, 1), null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToLower(buffer, buffer.Slice(0, 1), null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToLower(buffer, buffer.Slice(2, 1), null) };

        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToLowerInvariant(buffer) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToLowerInvariant(buffer.Slice(1, 1)) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToLowerInvariant(buffer.Slice(0, 1)) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToLowerInvariant(buffer.Slice(2, 1)) };

        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToLowerInvariant(buffer, buffer) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToLowerInvariant(buffer, buffer.Slice(1, 1)) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToLowerInvariant(buffer, buffer.Slice(0, 1)) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToLowerInvariant(buffer, buffer.Slice(2, 1)) };

        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToUpper(buffer, null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToUpper(buffer.Slice(1, 1), null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToUpper(buffer.Slice(0, 1), null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToUpper(buffer.Slice(2, 1), null) };

        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToUpper(buffer, buffer, null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToUpper(buffer, buffer.Slice(1, 1), null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToUpper(buffer, buffer.Slice(0, 1), null) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToUpper(buffer, buffer.Slice(2, 1), null) };

        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToUpperInvariant(buffer) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToUpperInvariant(buffer.Slice(1, 1)) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToUpperInvariant(buffer.Slice(0, 1)) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => ((ReadOnlySpanND<char>)buffer).ToUpperInvariant(buffer.Slice(2, 1)) };

        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToUpperInvariant(buffer, buffer) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToUpperInvariant(buffer, buffer.Slice(1, 1)) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToUpperInvariant(buffer, buffer.Slice(0, 1)) };
        //    yield return new Helpers.AssertThrowsAction<char>[] { (SpanND<char> buffer) => MemoryExtensions.ToUpperInvariant(buffer, buffer.Slice(2, 1)) };
        //}

        //[Theory]
        //[MemberData(nameof(MemoryExtensionsToUpperLowerOverlapping))]
        //public static void MemoryExtensionsToUpperLowerOverlappingThrows(Helpers.AssertThrowsAction<char> action)
        //{
        //    SpanND<char> buffer = new char[] { 'a', 'b', 'c', 'd' };
        //    Helpers.AssertThrows<InvalidOperationException, char>(buffer, action);
        //}
    }
}
