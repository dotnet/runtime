// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Utf8Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text) => text.GetSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text, int start) =>
            text.GetSpan().Slice(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text, int start, int length) =>
            text.GetSpan().Slice(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlyMemory<byte> CreateMemoryBytes(Utf8String text, int start, int length) =>
            text.CreateMemoryBytes(start, length);
    }
}
