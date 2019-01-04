// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.IO
{
    public static class StringExtensions
    {
        public delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);

        public static bool Contains(this string s, char value)
        {
            return s.IndexOf(value) != -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsOrdinal(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            if (span.Length != value.Length)
                return false;
            if (value.Length == 0)  // span.Length == value.Length == 0
                return true;
            return span.SequenceEqual(value);
        }

        public unsafe static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (length <= 0)
            {
                if (length == 0)
                    return string.Empty;
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            string result = new string('\0', length);
            fixed (char* r = result)
            {
                action(new Span<char>(r, length), state);
            }
            return result;
        }

        internal unsafe static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1)
        {
            var result = new string('\0', checked(str0.Length + str1.Length));
            fixed (char* resultPtr = result)
            {
                var resultSpan = new Span<char>(resultPtr, result.Length);

                str0.CopyTo(resultSpan);
                str1.CopyTo(resultSpan.Slice(str0.Length));
            }
            return result;
        }

        internal unsafe static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2)
        {
            var result = new string('\0', checked(str0.Length + str1.Length + str2.Length));
            fixed (char* resultPtr = result)
            {
                var resultSpan = new Span<char>(resultPtr, result.Length);

                str0.CopyTo(resultSpan);
                resultSpan = resultSpan.Slice(str0.Length);

                str1.CopyTo(resultSpan);
                resultSpan = resultSpan.Slice(str1.Length);

                str2.CopyTo(resultSpan);
            }
            return result;
        }
    }
}
