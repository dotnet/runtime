// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    internal static class IPv6AddressHelper
    {
        internal unsafe static (int longestSequenceStart, int longestSequenceLength) FindCompressionRange(
            ReadOnlySpan<ushort> numbers) => (-1, -1);
        internal unsafe static bool ShouldHaveIpv4Embedded(ushort[] numbers) => false;
        internal unsafe static bool IsValidStrict(char* name, int start, ref int end) => false;
        internal static unsafe bool Parse(ReadOnlySpan<char> ipSpan, ushort* numbers, int start, ref string scopeId) => false;
    }
}
