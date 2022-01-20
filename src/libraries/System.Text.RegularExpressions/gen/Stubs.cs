// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

// This file provides helpers used to help compile some Regex source code (e.g. RegexParser) as part of the netstandard2.0 generator assembly.

namespace System.Text
{
    internal static class StringBuilderExtensions
    {
        public static unsafe StringBuilder Append(this StringBuilder stringBuilder, ReadOnlySpan<char> span)
        {
            fixed (char* ptr = &MemoryMarshal.GetReference(span))
            {
                return stringBuilder.Append(ptr, span.Length);
            }
        }

        public static ReadOnlyMemory<char>[] GetChunks(this StringBuilder stringBuilder)
        {
            var chars = new char[stringBuilder.Length];
            stringBuilder.CopyTo(0, chars, 0, chars.Length);
            return new[] { new ReadOnlyMemory<char>(chars) };
        }
    }
}

namespace System
{
    internal static class StringExtensions
    {
        public static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
        {
            Span<char> span = length <= 256 ? stackalloc char[length] : new char[length];
            action(span, state);
            return span.ToString();
        }
    }
}

namespace System.Buffers
{
    internal delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);
}

namespace System.Threading
{
    internal static class InterlockedExtensions
    {
        public static uint Or(ref uint location1, uint value)
        {
            uint current = location1;
            while (true)
            {
                uint newValue = current | value;
                uint oldValue = (uint)Interlocked.CompareExchange(ref Unsafe.As<uint, int>(ref location1), (int)newValue, (int)current);
                if (oldValue == current)
                {
                    return oldValue;
                }
                current = oldValue;
            }
        }
    }
}

namespace System.Text.RegularExpressions
{
    internal class RegexReplacement
    {
        public RegexReplacement(string rep, RegexNode concat, Hashtable caps) { }

        private const int Specials = 4;
        public const int LeftPortion = -1;
        public const int RightPortion = -2;
        public const int LastGroup = -3;
        public const int WholeString = -4;
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
