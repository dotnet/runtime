// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class PhysicalAddressTest
    {
        [Fact]
        public void PhysicalAddress_SameAddresses_Pass()
        {
            byte[] byteAddress1 = { 0x01, 0x20, 0x03, 0x40, 0x05, 0x60 };

            PhysicalAddress address1 = new PhysicalAddress(byteAddress1);
            PhysicalAddress address2 = new PhysicalAddress(byteAddress1);

            Assert.Equal(address1, address2);
            Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
        }

        [Fact]
        public void PhysicalAddress_EqualAddresses_Pass()
        {
            byte[] byteAddress1 = { 0x01, 0x20, 0x03, 0x40, 0x05, 0x60 };
            byte[] byteAddress2 = { 0x01, 0x20, 0x03, 0x40, 0x05, 0x60 };

            PhysicalAddress address1 = new PhysicalAddress(byteAddress1);
            PhysicalAddress address2 = new PhysicalAddress(byteAddress2);

            Assert.Equal(address1, address2);
            Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
        }

        [Fact]
        public void PhysicalAddress_DifferentAddresses_SameSize_Pass()
        {
            byte[] byteAddress1 = { 0x01, 0x20, 0x03, 0x40, 0x05, 0x60 };
            byte[] byteAddress2 = { 0x10, 0x02, 0x30, 0x04, 0x50, 0x06 };

            PhysicalAddress address1 = new PhysicalAddress(byteAddress1);
            PhysicalAddress address2 = new PhysicalAddress(byteAddress2);

            Assert.NotEqual(address1.GetHashCode(), address2.GetHashCode());
            Assert.NotEqual(address1, address2);
        }

        [Fact]
        public void PhysicalAddress_DifferentAddresses_DifferentSize_Pass()
        {
            byte[] byteAddress1 = { 0x01, 0x20, 0x03, 0x40, 0x05 };
            byte[] byteAddress2 = { 0x01, 0x20, 0x03, 0x40, 0x05, 0x60 };

            PhysicalAddress address1 = new PhysicalAddress(byteAddress1);
            PhysicalAddress address2 = new PhysicalAddress(byteAddress2);

            Assert.NotEqual(address1.GetHashCode(), address2.GetHashCode());
            Assert.NotEqual(address1, address2);
        }

        [Fact]
        public void PhysicalAddress_Clone_Pass()
        {
            byte[] byteAddress1 = { 0x01, 0x20, 0x03, 0x40, 0x05, 0x60 };
            PhysicalAddress address1 = new PhysicalAddress(byteAddress1);

            byte[] byteAddressClone = address1.GetAddressBytes();
            Assert.Equal(byteAddress1, byteAddressClone);

            byteAddressClone[0] = 0xFF;
            Assert.NotEqual(byteAddress1, byteAddressClone);
        }

        [Theory]
        [InlineData("42", new byte[] { 0x42, })]
        [InlineData("69-4F", new byte[] { 0x69, 0x4f })]
        [InlineData("40-DC-27", new byte[] { 0x40, 0xdc, 0x27 })]
        [InlineData("8E-35-99-87", new byte[] { 0x8e, 0x35, 0x99, 0x87 })]
        [InlineData("47-FB-74-41-3B", new byte[] { 0x47, 0xfb, 0x74, 0x41, 0x3B })]
        [InlineData("00-11-22-33-44-55", new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 })]
        [InlineData("F0-E1-D2-C3-B4-A5", new byte[] { 0xf0, 0xe1, 0xd2, 0xc3, 0xb4, 0xa5 })]
        [InlineData("54-0C-C4-7E-66-54", new byte[] { 0x54, 0x0c, 0xc4, 0x7e, 0x66, 0x54 })]
        [InlineData("001122334455", new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 })]
        public void Parse_Valid_Success(string address, byte[] expectedBytes)
        {
            PhysicalAddress parsedAddress = PhysicalAddress.Parse(address);
            byte[] addressBytes = parsedAddress.GetAddressBytes();
            Assert.Equal(addressBytes, expectedBytes);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "netfx doesn't have fix #32724")]
        [Theory]
        [InlineData("F")]
        [InlineData("M0")]
        [InlineData("33-3")]
        [InlineData("D2-C3-")]
        [InlineData("D2-A33")]
        [InlineData("B4-A5-F01")]
        [InlineData("f0-e1-d2-c3-b4-a5")]
        [InlineData("47:FB:74:41:66:3B")]
        [InlineData("de84.1251.1c9d")]
        [InlineData("AE88.D6EC.A720")]
        public void Parse_Invalid_ThrowsFormatException(string address)
        {
            Assert.Throws<FormatException>(() => PhysicalAddress.Parse(address));
        }
    }
}
