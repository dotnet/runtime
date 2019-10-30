// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public class AsnEncodedDataTests
    {
        [Fact]
        public static void FormatUnknownData()
        {
            byte[] rawData = { 0x41, 0x42, 0x43 };
            AsnEncodedData a = new AsnEncodedData(rawData);
            a.Oid = null;
            string s = a.Format(true);
            Assert.Equal("41 42 43", s);
            return;
        }

        [Fact]
        public static void FormatInvalidTypedData()
        {
            // This passes in data in an illegal format. AsnEncodedData.Format() swallows the error and falls back to a simple hex-encoding scheme.
            byte[] rawData = { 0x41, 0x42, 0x43 };
            AsnEncodedData a = new AsnEncodedData(rawData);
            a.Oid = new Oid("1.3.6.1.4.1.311.2.1.27");  //SPC_FINANCIAL_CRITERIA_OBJID
            string s = a.Format(true);
            Assert.Equal("414243", s);
            return;
        }

        [Fact]
        public static void TestSubjectAlternativeName()
        {
            byte[] sanExtension =
            {
                0x30, 0x31, 0x82, 0x0B, 0x65, 0x78, 0x61, 0x6D,
                0x70, 0x6C, 0x65, 0x2E, 0x6F, 0x72, 0x67, 0x82,
                0x0F, 0x73, 0x75, 0x62, 0x2E, 0x65, 0x78, 0x61,
                0x6D, 0x70, 0x6C, 0x65, 0x2E, 0x6F, 0x72, 0x67,
                0x82, 0x11, 0x2A, 0x2E, 0x73, 0x75, 0x62, 0x2E,
                0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C, 0x65, 0x2E,
                0x6F, 0x72, 0x67,
            };

            AsnEncodedData asnData = new AsnEncodedData(
                new Oid("2.5.29.17"),
                sanExtension);

            string s = asnData.Format(false);
            // Windows says: "DNS Name=example.org, DNS Name=sub.example.org, DNS Name=*.sub.example.org"
            // X-Plat (OpenSSL) says: "DNS:example.org, DNS:sub.example.org, DNS:*.sub.example.org".
            // This keeps the parsing generalized until we can get them to converge
            string[] parts = s.Split(new[] { ':', '=', ',' }, StringSplitOptions.RemoveEmptyEntries);
            // Parts is now { header, data, header, data, header, data }.
            string[] output = new string[parts.Length / 2];

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = parts[2 * i + 1];
            }

            Assert.Equal(new[] { "example.org", "sub.example.org", "*.sub.example.org" }, output);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void TestSubjectAlternativeName_Unix()
        {
            byte[] sanExtension = (
                "3081D1A027060A2B0601040182371402" +
                "03A0190C177375626A65637475706E31" +
                "406578616D706C652E6F726781157361" +
                "6E656D61696C31406578616D706C652E" +
                "6F72678218646E73312E7375626A6563" +
                "742E6578616D706C652E6F7267A30630" +
                "0441027573863168747470733A2F2F77" +
                "77772E6578616D706C652E6F72672F70" +
                "6174682F746F2F612F7265736F757263" +
                "6523616E63686F7287047F0000018710" +
                "20010DB8AC10FE010000000000000000" +
                "870F20010DB8AC10FE01000000000000" +
                "008704FFFFFFFF8704020F6364880529" +
                "01020203").HexToByteArray();

            AsnEncodedData asnData = new AsnEncodedData(
                new Oid("2.5.29.17"),
                sanExtension);

            string s = asnData.Format(false);

            string expected = string.Join(
                ", ",
                // Choice[0]: OtherName
                "othername:<unsupported>",
                // Choice[1]: Rfc822Name (EmailAddress)
                "email:sanemail1@example.org",
                // Choice[2]: DnsName
                "DNS:dns1.subject.example.org",
                // Choice[3]: X400Name
                "X400Name:<unsupported>",
                // Skip Choice[4]: DirName
                //   (Supported by OpenSSL, but not by our Apple version)
                // Skip Choice[5]: EdiName
                //   (Buggy parsing in OpenSSL)
                // Choice[6]: URI
                "URI:https://www.example.org/path/to/a/resource#anchor",
                // Choice[7]: IPAddress (IPv4)
                "IP Address:127.0.0.1",
                // Choice[7]: IPAddress (IPv6)
                "IP Address:2001:DB8:AC10:FE01:0:0:0:0",
                // Choice[7]: IPAddress (unknown type)
                "IP Address:<invalid>",
                // Choice[7]: IPAddress (IPv4, longer string)
                "IP Address:255.255.255.255",
                // Choice[7]: IPAddress (IPv4, medium string)
                // Note that between this, 127.0.0.1, and 255.255.255.255 all fields
                // had both length-1 and length-3 (and some had length-2)
                "IP Address:2.15.99.100",
                // Choice[8]: RegisteredID
                "Registered ID:1.1.1.2.2.3");

            Assert.Equal(expected, s);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public static void TestSubjectAlternativeName_Mac()
        {
            byte[] sanExtension = "300EA50CA10A13086564695061727479".HexToByteArray();

            AsnEncodedData asnData = new AsnEncodedData(
                new Oid("2.5.29.17"),
                sanExtension);

            Assert.Equal("EdiPartyName:<unsupported>", asnData.Format(false));
        }
    }
}
