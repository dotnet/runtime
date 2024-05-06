// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.Net
{
    internal static class IPv6AddressHelper<TChar>
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        public static readonly TChar ComponentSeparator = TChar.CreateTruncating(':');

        internal static unsafe (int longestSequenceStart, int longestSequenceLength) FindCompressionRange(
            ReadOnlySpan<ushort> numbers) => (-1, -1);
        internal static unsafe bool ShouldHaveIpv4Embedded(ReadOnlySpan<ushort> numbers) => false;
        internal static unsafe bool IsValidStrict(ReadOnlySpan<TChar> name) => false;
        internal static unsafe bool Parse(ReadOnlySpan<TChar> address, Span<ushort> numbers, out ReadOnlySpan<TChar> scopeId)
        {
            scopeId = ReadOnlySpan<TChar>.Empty;
            return false;
        }
    }
}
