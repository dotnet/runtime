// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public sealed class ReadT61String : Asn1ReaderTests
    {
        public static IEnumerable<object[]> ValidEncodingData { get; } =
            new object[][]
            {
                // https://github.com/dotnet/corefx/issues/27466
                new object[]
                {
                    PublicEncodingRules.DER,
                    "140E47726170654369747920696E632E",
                    "GrapeCity inc.",
                },
                new object[]
                {
                    PublicEncodingRules.DER,
                    "1411546F6F6C7320446576656C6F706D656E74",
                    "Tools Development",
                },
                // Mono test case taken from old bug report
                new object[]
                {
                    PublicEncodingRules.DER,
                    "14244865646562792773204DF862656C68616E64656C202F2F204356523A3133343731393637",
                    "Hedeby's M\u00f8belhandel // CVR:13471967",
                },
                new object[]
                {
                    PublicEncodingRules.DER,
                    "14264865646562792773204DF862656C68616E64656C202D2053616C6773616664656C696E67656E",
                    "Hedeby's M\u00f8belhandel - Salgsafdelingen",
                },
                // Valid UTF-8 string is interpreted as UTF-8
                new object[]
                {
                    PublicEncodingRules.DER,
                    "1402C2A2",
                    "\u00a2",
                },
                // Valid UTF-8 string is interpreted as UTF-8 (multi-segment)
                new object[]
                {
                    PublicEncodingRules.BER,
                    "34800401C20401A20000",
                    "\u00a2",
                },
                // Invalid UTF-8 string with valid UTF-8 sequence is interpreted as ISO 8859-1
                new object[]
                {
                    PublicEncodingRules.DER,
                    "1403C2A2F8",
                    "\u00c2\u00a2\u00f8",
                },
            };

        [Theory]
        [MemberData(nameof(ValidEncodingData))]
        public static void GetT61String_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            string expectedValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            string value = reader.ReadCharacterString(UniversalTagNumber.T61String);

            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [MemberData(nameof(ValidEncodingData))]
        public static void TryCopyT61String(
            PublicEncodingRules ruleSet,
            string inputHex,
            string expectedValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            char[] output = new char[expectedValue.Length];

            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            bool copied;
            int charsWritten;

            if (output.Length > 0)
            {
                output[0] = 'a';

                copied = reader.TryCopyCharacterString(
                    UniversalTagNumber.T61String,
                    output.AsSpan(0, expectedValue.Length - 1),
                    out charsWritten);

                Assert.False(copied, "reader.TryCopyT61String - too short");
                Assert.Equal(0, charsWritten);
                Assert.Equal('a', output[0]);
            }

            copied = reader.TryCopyCharacterString(
                UniversalTagNumber.T61String,
                output,
                out charsWritten);

            Assert.True(copied, "reader.TryCopyT61String");

            string actualValue = new string(output, 0, charsWritten);
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
