// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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

        public static IEnumerable<object[]> RoundtripParseToString_String_Bytes()
        {
            yield return new object[] { "42", new byte[] { 0x42 } };
            yield return new object[] { "69-4F", new byte[] { 0x69, 0x4f } };
            yield return new object[] { "40-DC-27", new byte[] { 0x40, 0xdc, 0x27 } };
            yield return new object[] { "8E-35-99-87", new byte[] { 0x8e, 0x35, 0x99, 0x87 } };
            yield return new object[] { "47-FB-74-41-3B", new byte[] { 0x47, 0xfb, 0x74, 0x41, 0x3B } };
            yield return new object[] { "00-11-22-33-44-55", new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 } };
            yield return new object[] { "F0-E1-D2-C3-B4-A5", new byte[] { 0xf0, 0xe1, 0xd2, 0xc3, 0xb4, 0xa5 } };
            yield return new object[] { "54-0C-C4-7E-66-54", new byte[] { 0x54, 0x0c, 0xc4, 0x7e, 0x66, 0x54 } };
            yield return new object[] { "001122334455", new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 } };
            yield return new object[]
            {
                "00-01-02-03-04-05-06-07-08-09-0A-0B-0C-0D-0E-0F-10-11-12-13-14-15-16-17-18-19-1A-1B-1C-1D-1E-1F-" +
                "20-21-22-23-24-25-26-27-28-29-2A-2B-2C-2D-2E-2F-30-31-32-33-34-35-36-37-38-39-3A-3B-3C-3D-3E-3F-" +
                "40-41-42-43-44-45-46-47-48-49-4A-4B-4C-4D-4E-4F-50-51-52-53-54-55-56-57-58-59-5A-5B-5C-5D-5E-5F-" +
                "60-61-62-63-64-65-66-67-68-69-6A-6B-6C-6D-6E-6F-70-71-72-73-74-75-76-77-78-79-7A-7B-7C-7D-7E-7F-" +
                "80-81-82-83-84-85-86-87-88-89-8A-8B-8C-8D-8E-8F-90-91-92-93-94-95-96-97-98-99-9A-9B-9C-9D-9E-9F-" +
                "A0-A1-A2-A3-A4-A5-A6-A7-A8-A9-AA-AB-AC-AD-AE-AF-B0-B1-B2-B3-B4-B5-B6-B7-B8-B9-BA-BB-BC-BD-BE-BF-" +
                "C0-C1-C2-C3-C4-C5-C6-C7-C8-C9-CA-CB-CC-CD-CE-CF-D0-D1-D2-D3-D4-D5-D6-D7-D8-D9-DA-DB-DC-DD-DE-DF-" +
                "E0-E1-E2-E3-E4-E5-E6-E7-E8-E9-EA-EB-EC-ED-EE-EF-F0-F1-F2-F3-F4-F5-F6-F7-F8-F9-FA-FB-FC-FD-FE-FF",
                new byte[256]
                {
                    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26,
                    27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51,
                    52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76,
                    77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101,
                    102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121,
                    122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141,
                    142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161,
                    162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181,
                    182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201,
                    202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221,
                    222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241,
                    242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255
                }
            };
        }

        [Theory]
        [MemberData(nameof(RoundtripParseToString_String_Bytes))]
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
            FormatException ex = Assert.Throws<FormatException>(() => PhysicalAddress.Parse(address));
            Assert.Contains(address, ex.Message);
        }

        [Fact]
        public void ToString_NullAddress_NullReferenceException()
        {
            var pa = new PhysicalAddress(null);
            Assert.Throws<NullReferenceException>(() => pa.ToString());
        }

        [Theory]
        [InlineData("", new byte[0])]
        [MemberData(nameof(RoundtripParseToString_String_Bytes))]
        public void ToString_ExpectedResult(string expectedAddress, byte[] inputBytes)
        {
            Assert.Equal(expectedAddress.Replace("-", ""), new PhysicalAddress(inputBytes).ToString());
        }

        [Theory]
        [MemberData(nameof(RoundtripParseToString_String_Bytes))]
        public void ToStringParseGetAddressBytes_Roundtrips(string _, byte[] inputBytes)
        {
            Assert.Equal(inputBytes, PhysicalAddress.Parse(new PhysicalAddress(inputBytes).ToString()).GetAddressBytes());
        }
    }
}
