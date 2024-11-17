// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Contains helpers for manipulating spans so that we can keep unsafe code out of the common path.
    /// </summary>
    internal static class SpanUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidIndex<T>(ReadOnlySpan<T> span, int index)
        {
            return ((uint)index < (uint)span.Length) ? true : false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidIndex<T>(Span<T> span, int index)
        {
            return ((uint)index < (uint)span.Length) ? true : false;
        }
    }
}
