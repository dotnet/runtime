// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public sealed class IPAddressParsing_Span : IPAddressParsing
    {
        public override IPAddress Parse(string ipString) => IPAddress.Parse(ipString.AsReadOnlySpan());
        public override bool TryParse(string ipString, out IPAddress address) => IPAddress.TryParse(ipString.AsReadOnlySpan(), out address);


        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void TryFormat_ProvidedBufferLargerThanNeeded_Success(string addressString, string expected)
        {
            IPAddress address = IPAddress.Parse(addressString);

            const int IPv4MaxLength = 15; // TryFormat currently requires at least this amount of space for IPv4 addresses
            int requiredLength = address.AddressFamily == AddressFamily.InterNetwork ?
                IPv4MaxLength :
                address.ToString().Length;

            var largerThanRequired = new char[requiredLength + 1];
            Assert.True(address.TryFormat(new Span<char>(largerThanRequired), out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(
                address.AddressFamily == AddressFamily.InterNetworkV6 ? expected.ToLowerInvariant() : expected,
                new string(largerThanRequired, 0, charsWritten));
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void TryFormat_ProvidedBufferTooSmall_Failure(string addressString, string expected)
        {
            IPAddress address = IPAddress.Parse(addressString);
            var result = new char[address.ToString().Length - 1];
            Assert.False(address.TryFormat(new Span<char>(result), out int charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.Equal<char>(new char[result.Length], result);
        }
    }
}
