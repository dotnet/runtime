// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime.CompilerServices;

#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System
{
    /// <summary>
    /// Extension methods and non-generic helpers for Span, ReadOnlySpan, Memory, and ReadOnlyMemory.
    /// </summary>
    public static class Span
    {
        public static Span<byte> AsBytes<T>(Span<T> source)
            where T : struct => source.AsBytes();

        public static ReadOnlySpan<byte> AsBytes<T>(ReadOnlySpan<T> source)
            where T : struct => source.AsBytes();

        // TODO: Delete once the AsReadOnlySpan -> AsSpan rename propages through the system
        public static ReadOnlySpan<char> AsReadOnlySpan(this string text) => text.AsSpan();
        public static ReadOnlySpan<char> AsReadOnlySpan(this string text, int start) => text.AsSpan(start);
        public static ReadOnlySpan<char> AsReadOnlySpan(this string text, int start, int length) => text.AsSpan(start, length);

        public static ReadOnlyMemory<char> AsReadOnlyMemory(this string text) => text.AsMemory();
        public static ReadOnlyMemory<char> AsReadOnlyMemory(this string text, int start) => text.AsMemory(start);
        public static ReadOnlyMemory<char> AsReadOnlyMemory(this string text, int start, int length) => text.AsMemory(start, length);
    }
}
