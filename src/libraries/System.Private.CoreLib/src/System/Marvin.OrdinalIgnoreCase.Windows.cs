// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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

            // NLS doesn't have a concept of case folding. Instead, "removing case information"
            // means that data is normalized to uppercase.

            return Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CaseFoldBuffer(ReadOnlySpan<char> source, Span<char> destination)
        {
            Debug.Assert(destination.Length >= source.Length);

            // NLS doesn't have a concept of case folding. Instead, "removing case information"
            // means that data is normalized to uppercase.

            return source.ToUpperInvariant(destination);
        }
    }
}
