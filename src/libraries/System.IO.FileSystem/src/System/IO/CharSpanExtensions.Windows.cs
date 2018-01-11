// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.IO
{
    internal static partial class CharSpanExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int CompareOrdinal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool ignoreCase = false)
        {
            int result = Interop.Kernel32.CompareStringOrdinal(
                ref MemoryMarshal.GetReference(first),
                first.Length,
                ref MemoryMarshal.GetReference(second),
                second.Length,
                ignoreCase);

            if (result == 0)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

            // CSTR_LESS_THAN            1           // string 1 less than string 2
            // CSTR_EQUAL                2           // string 1 equal to string 2
            // CSTR_GREATER_THAN         3           // string 1 greater than string 2

            return result - 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsOrdinal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool ignoreCase = false)
        {
            if (first.Length != second.Length)
                return false;

            if (!ignoreCase)
                return first.SequenceEqual(second);

            return CompareOrdinal(first, second, ignoreCase) == 0;
        }
    }
}
