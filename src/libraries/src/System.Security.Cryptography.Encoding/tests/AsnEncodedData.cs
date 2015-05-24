// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
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
            String s = a.Format(true);
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
            String s = a.Format(true);
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
    }
}