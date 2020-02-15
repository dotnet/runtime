// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System
{
    internal static partial class Marvin
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint CaseFoldTwoAsciiChars(uint value)
        {
            Debug.Assert(Utf16Utility.AllCharsInUInt32AreAscii(value));

            // ICU's case folding is a lowercase conversion in the ASCII range.

            return Utf16Utility.ConvertAllAsciiCharsInUInt32ToLowercase(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CaseFoldBuffer(ReadOnlySpan<char> source, Span<char> destination)
        {
            Debug.Assert(destination.Length >= source.Length);

            TextInfo.Invariant.ToCaseFold(source, destination);
            return source.Length; // case folding doesn't change the UTF-16 code unit count
        }
    }
}
