// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class DnsSdRecordParsingTests
    {
        [Theory]
        [InlineData("2001:db8::1", 0u, 0L)]
        [InlineData("fe80::1", 42u, 42L)]
        public void TryParseAddress_AppliesInterfaceIndexOnlyToLinkLocalIPv6(string addressString, uint interfaceIndex, long expectedScopeId)
        {
            IPAddress address = IPAddress.Parse(addressString);
            DnsSdRecord record = new DnsSdRecord(28, address.GetAddressBytes(), ttl: 60, interfaceIndex);

            Assert.True(DnsSdRecordParsing.TryParseAddress(record, out AddressRecord parsed));
            Assert.Equal(expectedScopeId, parsed.Address.ScopeId);
            Assert.Equal(AddressFamily.InterNetworkV6, parsed.Address.AddressFamily);
        }

        [Fact]
        public void TryParseAddress_IPv4_Parses()
        {
            DnsSdRecord record = new DnsSdRecord(1, new byte[] { 10, 0, 0, 7 }, ttl: 120, interfaceIndex: 0);

            Assert.True(DnsSdRecordParsing.TryParseAddress(record, out AddressRecord parsed));
            Assert.Equal("10.0.0.7", parsed.Address.ToString());
            Assert.Equal(TimeSpan.FromSeconds(120), parsed.Ttl);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(15)]
        public void TryParseAddress_InvalidLength_ReturnsFalse(int length)
        {
            DnsSdRecord record = new DnsSdRecord(1, new byte[length], ttl: 60, interfaceIndex: 0);

            Assert.False(DnsSdRecordParsing.TryParseAddress(record, out _));
        }

        [Fact]
        public void TryParseSrv_RootTarget_ReturnsDot()
        {
            // priority=0, weight=0, port=0, name=<root>
            DnsSdRecord record = new DnsSdRecord(33, new byte[] { 0, 0, 0, 0, 0, 0, 0 }, ttl: 60, interfaceIndex: 0);

            Assert.True(DnsSdRecordParsing.TryParseSrv(record, out SrvRecord parsed));
            Assert.Equal(".", parsed.Target);
        }

        [Fact]
        public void TryParseSrv_ParsesFields()
        {
            // priority=1, weight=2, port=443, name="a"
            byte[] data = { 0, 1, 0, 2, 1, 0xBB, 1, (byte)'a', 0 };
            DnsSdRecord record = new DnsSdRecord(33, data, ttl: 60, interfaceIndex: 0);

            Assert.True(DnsSdRecordParsing.TryParseSrv(record, out SrvRecord parsed));
            Assert.Equal(1, parsed.Priority);
            Assert.Equal(2, parsed.Weight);
            Assert.Equal(443, parsed.Port);
            Assert.Equal("a", parsed.Target);
        }

        [Fact]
        public void TryParseMx_RootExchange_ReturnsDot()
        {
            // preference=0, name=<root>
            DnsSdRecord record = new DnsSdRecord(15, new byte[] { 0, 0, 0 }, ttl: 60, interfaceIndex: 0);

            Assert.True(DnsSdRecordParsing.TryParseMx(record, out MxRecord parsed));
            Assert.Equal(".", parsed.Exchange);
            Assert.Equal(0, parsed.Preference);
        }

        [Fact]
        public void TryParseTxt_ParsesMultipleStrings()
        {
            byte[] data = { 3, (byte)'a', (byte)'b', (byte)'c', 2, (byte)'x', (byte)'y' };
            DnsSdRecord record = new DnsSdRecord(16, data, ttl: 60, interfaceIndex: 0);

            Assert.True(DnsSdRecordParsing.TryParseTxt(record, out TxtRecord parsed));
            Assert.Equal(new[] { "abc", "xy" }, parsed.Values);
        }

        [Fact]
        public void TryParseTxt_LengthExceedsRemaining_ReturnsFalse()
        {
            byte[] data = { 5, (byte)'a' };
            DnsSdRecord record = new DnsSdRecord(16, data, ttl: 60, interfaceIndex: 0);

            Assert.False(DnsSdRecordParsing.TryParseTxt(record, out _));
        }

        [Fact]
        public void TryParseCName_ParsesDottedName()
        {
            byte[] data = { 3, (byte)'w', (byte)'w', (byte)'w', 7, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e', 0 };
            DnsSdRecord record = new DnsSdRecord(5, data, ttl: 60, interfaceIndex: 0);

            Assert.True(DnsSdRecordParsing.TryParseCName(record, out CNameRecord parsed));
            Assert.Equal("www.example", parsed.CanonicalName);
        }

        [Fact]
        public void TryParsePtr_UnterminatedName_ReturnsFalse()
        {
            byte[] data = { 3, (byte)'w', (byte)'w', (byte)'w' };
            DnsSdRecord record = new DnsSdRecord(12, data, ttl: 60, interfaceIndex: 0);

            Assert.False(DnsSdRecordParsing.TryParsePtr(record, out _));
        }

        [Fact]
        public void TryParseNs_RejectsCompressionPointer()
        {
            byte[] data = { 0xC0, 0x00 };
            DnsSdRecord record = new DnsSdRecord(2, data, ttl: 60, interfaceIndex: 0);

            Assert.False(DnsSdRecordParsing.TryParseNs(record, out _));
        }

        [Fact]
        public void TryParseNs_RejectsOverlongLabel()
        {
            byte[] data = new byte[65];
            data[0] = 64;

            DnsSdRecord record = new DnsSdRecord(2, data, ttl: 60, interfaceIndex: 0);

            Assert.False(DnsSdRecordParsing.TryParseNs(record, out _));
        }
    }
}
