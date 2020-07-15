// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    internal static class IPv6AddressHelper
    {
        internal static unsafe (int longestSequenceStart, int longestSequenceLength) FindCompressionRange(
            ReadOnlySpan<ushort> numbers) => (-1, -1);
        internal static unsafe bool ShouldHaveIpv4Embedded(ReadOnlySpan<ushort> numbers) => false;
        internal static unsafe bool IsValidStrict(char* name, int start, ref int end) => false;
        internal static unsafe bool Parse(ReadOnlySpan<char> ipSpan, Span<ushort> numbers, int start, ref string scopeId) => false;
    }
}
