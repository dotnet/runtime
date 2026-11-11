// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers;

/// <summary>Provides downlevel polyfills for instance members on <see cref="ReadOnlySequence{T}"/>.</summary>
internal static class ReadOnlySequencePolyfills
{
    extension<T>(ReadOnlySequence<T> sequence)
    {
        public ReadOnlySpan<T> FirstSpan => sequence.First.Span;
    }
}
