// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.Net
{
    internal static class IPv6AddressHelper
    {
        internal static unsafe (int longestSequenceStart, int longestSequenceLength) FindCompressionRange(
            ReadOnlySpan<ushort> numbers) => (-1, -1);
        internal static unsafe bool ShouldHaveIpv4Embedded(ReadOnlySpan<ushort> numbers) => false;
        internal static unsafe bool IsValidStrict<TChar>(TChar* name, int start, ref int end)
            where TChar : unmanaged, IBinaryInteger<TChar> => false;
        internal static unsafe bool Parse<TChar>(ReadOnlySpan<TChar> address, Span<ushort> numbers, out ReadOnlySpan<TChar> scopeId)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            scopeId = ReadOnlySpan<TChar>.Empty;
            return false;
        }
    }
}
