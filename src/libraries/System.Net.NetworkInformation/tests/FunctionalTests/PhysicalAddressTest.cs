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
            byte[] byteAddress1 = { 0x01, 0x20, 0x03, 0x40, 0x05};
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
    }
}
