// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class PushPopSequence : Asn1WriterTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PopNewWriter(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                // Maybe ArgumentException isn't right for this, since no argument was provided.
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.PopSequence());
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PopNewWriter_CustomTag(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true)));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PopBalancedWriter(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.PushSequence();
                writer.PopSequence();

                // Maybe ArgumentException isn't right for this, since no argument was provided.
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.PopSequence());
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PopBalancedWriter_CustomTag(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.PushSequence();
                writer.PopSequence();

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true)));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PushCustom_PopStandard(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true));

                // Maybe ArgumentException isn't right for this, since no argument was provided.
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.PopSequence());
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PushStandard_PopCustom(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.PushSequence();

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true)));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PushPrimitive_PopStandard(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.PushSequence(new Asn1Tag(UniversalTagNumber.Sequence));
                writer.PopSequence();

                if (ruleSet == PublicEncodingRules.CER)
                {
                    Verify(writer, "30800000");
                }
                else
                {
                    Verify(writer, "3000");
                }
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PushCustomPrimitive_PopConstructed(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.PushSequence(new Asn1Tag(TagClass.Private, 5));
                writer.PopSequence(new Asn1Tag(TagClass.Private, 5, true));

                if (ruleSet == PublicEncodingRules.CER)
                {
                    Verify(writer, "E5800000");
                }
                else
                {
                    Verify(writer, "E500");
                }
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PushStandard_PopPrimitive(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.PushSequence();
                writer.PopSequence(new Asn1Tag(UniversalTagNumber.Sequence, isConstructed: false));

                if (ruleSet == PublicEncodingRules.CER)
                {
                    Verify(writer, "30800000");
                }
                else
                {
                    Verify(writer, "3000");
                }
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PushCustomConstructed_PopPrimitive(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.PushSequence(new Asn1Tag(TagClass.Private, (int)ruleSet, true));
                writer.PopSequence(new Asn1Tag(TagClass.Private, (int)ruleSet));

                byte tag = (byte)((int)ruleSet | 0b1110_0000);
                string tagHex = tag.ToString("X2");
                string rest = ruleSet == PublicEncodingRules.CER ? "800000" : "00";

                Verify(writer, tagHex + rest);
            }
        }

        [Fact]
        public static void BER_WritesDefinite_Empty()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                writer.PushSequence();
                writer.PopSequence();

                Verify(writer, "3000");
            }
        }

        [Fact]
        public static void CER_WritesIndefinite_Empty()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                writer.PushSequence();
                writer.PopSequence();

                Verify(writer, "30800000");
            }
        }

        [Fact]
        public static void DER_WritesDefinite_CustomTag_Empty()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();
                writer.PopSequence();

                Verify(writer, "3000");
            }
        }

        [Fact]
        public static void BER_WritesDefinite_CustomTag__Empty()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Private, 15, true);
                writer.PushSequence(tag);
                writer.PopSequence(tag);

                Verify(writer, "EF00");
            }
        }

        [Fact]
        public static void CER_WritesIndefinite_CustomTag__Empty()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Application, 91, true);
                writer.PushSequence(tag);
                writer.PopSequence(tag);

                Verify(writer, "7F5B800000");
            }
        }

        [Fact]
        public static void DER_WritesDefinite_CustomTag__Empty()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 30, true);
                writer.PushSequence(tag);
                writer.PopSequence(tag);

                Verify(writer, "BE00");
            }
        }

        private static void TestNested(AsnWriter writer, Asn1Tag alt, string expectedHex)
        {
            writer.PushSequence();
            {
                writer.PushSequence(alt);
                writer.PopSequence(alt);

                writer.PushSequence();
                {
                    writer.PushSequence(alt);
                    {
                        writer.PushSequence();
                        writer.PopSequence();
                    }

                    writer.PopSequence(alt);
                }

                writer.PopSequence();
            }

            writer.PopSequence();

            Verify(writer, expectedHex);
        }

        [Fact]
        public static void BER_Nested()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag alt = new Asn1Tag(TagClass.Private, 127, true);

                TestNested(writer, alt, "300AFF7F003005FF7F023000");
            }
        }

        [Fact]
        public static void CER_Nested()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag alt = new Asn1Tag(TagClass.ContextSpecific, 12, true);

                TestNested(writer, alt, "3080AC8000003080AC8030800000000000000000");
            }
        }

        [Fact]
        public static void DER_Nested()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                Asn1Tag alt = new Asn1Tag(TagClass.Application, 5, true);

                TestNested(writer, alt, "30086500300465023000");
            }
        }

        private static void SimpleContentShift(AsnWriter writer, string expectedHex)
        {
            writer.PushSequence();
            
            // F00DF00D...F00DF00D
            byte[] contentBytes = new byte[126];

            for (int i = 0; i < contentBytes.Length; i += 2)
            {
                contentBytes[i] = 0xF0;
                contentBytes[i + 1] = 0x0D;
            }

            writer.WriteOctetString(contentBytes);
            writer.PopSequence();

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void SimpleContentShift(PublicEncodingRules ruleSet)
        {
            const string ExpectedHex =
                "308180" +
                    "047E" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D";

            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                SimpleContentShift(writer, ExpectedHex);
            }
        }

        [Fact]
        public static void SimpleContentShift_CER()
        {
            const string ExpectedHex =
                "3080" +
                    "047E" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                    "0000";

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                SimpleContentShift(writer, ExpectedHex);
            }
        }

        private static void WriteRSAPublicKey(AsnEncodingRules ruleSet, string expectedHex)
        {
            using (AsnWriter innerWriter = new AsnWriter(ruleSet))
            {
                byte[] paddedBigEndianN = (
                    "00" +
                    "AF81C1CBD8203F624A539ED6608175372393A2837D4890E48A19DED369731156" +
                    "20968D6BE0D3DAA38AA777BE02EE0B6B93B724E8DCC12B632B4FA80BBC925BCE" +
                    "624F4CA7CC606306B39403E28C932D24DD546FFE4EF6A37F10770B2215EA8CBB" +
                    "5BF427E8C4D89B79EB338375100C5F83E55DE9B4466DDFBEEE42539AEF33EF18" +
                    "7B7760C3B1A1B2103C2D8144564A0C1039A09C85CF6B5974EB516FC8D6623C94" +
                    "AE3A5A0BB3B4C792957D432391566CF3E2A52AFB0C142B9E0681B8972671AF2B" +
                    "82DD390A39B939CF719568687E4990A63050CA7768DCD6B378842F18FDB1F6D9" +
                    "FF096BAF7BEB98DCF930D66FCFD503F58D41BFF46212E24E3AFC45EA42BD8847").HexToByteArray();

                // Now it's padded little-endian.
                Array.Reverse(paddedBigEndianN);
                BigInteger n = new BigInteger(paddedBigEndianN);
                const long e = 8589935681;

                innerWriter.PushSequence();
                innerWriter.WriteInteger(n);
                innerWriter.WriteInteger(e);
                innerWriter.PopSequence();

                using (AsnWriter outerWriter = new AsnWriter(ruleSet))
                {
                    // RSAPublicKey
                    outerWriter.PushSequence();

                    // AlgorithmIdentifier
                    outerWriter.PushSequence();
                    outerWriter.WriteObjectIdentifier("1.2.840.113549.1.1.1");
                    outerWriter.WriteNull();
                    outerWriter.PopSequence();

                    outerWriter.WriteBitString(innerWriter.Encode());
                    outerWriter.PopSequence();

                    Verify(outerWriter, expectedHex);
                }
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void WriteRSAPublicKey(PublicEncodingRules ruleSet)
        {
            const string ExpectedHex =
                // CONSTRUCTED SEQUENCE
                "30820124" +
                    // CONSTRUCTED SEQUENCE
                    "300D" +
                        // OBJECT IDENTIFIER (1.2.840.113549.1.1.1, rsaEncryption)
                        "06092A864886F70D010101" +
                        // NULL
                        "0500" +
                    // BIT STRING
                    "03820111" +
                        // 0 unused bits
                        "00" +
                        // sneaky inspection of the payload bytes
                        // CONSTRUCTED SEQUENCE
                        "3082010C" +
                            // INTEGER (n)
                            "02820101" +
                                "00AF81C1CBD8203F624A539ED6608175372393A2837D4890E48A19DED3697311" +
                                "5620968D6BE0D3DAA38AA777BE02EE0B6B93B724E8DCC12B632B4FA80BBC925B" +
                                "CE624F4CA7CC606306B39403E28C932D24DD546FFE4EF6A37F10770B2215EA8C" +
                                "BB5BF427E8C4D89B79EB338375100C5F83E55DE9B4466DDFBEEE42539AEF33EF" +
                                "187B7760C3B1A1B2103C2D8144564A0C1039A09C85CF6B5974EB516FC8D6623C" +
                                "94AE3A5A0BB3B4C792957D432391566CF3E2A52AFB0C142B9E0681B8972671AF" +
                                "2B82DD390A39B939CF719568687E4990A63050CA7768DCD6B378842F18FDB1F6" +
                                "D9FF096BAF7BEB98DCF930D66FCFD503F58D41BFF46212E24E3AFC45EA42BD88" +
                                "47" +
                            // INTEGER (e)
                            "02050200000441";

            WriteRSAPublicKey((AsnEncodingRules)ruleSet, ExpectedHex);
        }

        [Fact]
        public static void WriteRSAPublicKey_CER()
        {
            const string ExpectedHex =
                // CONSTRUCTED SEQUENCE
                "3080" +
                    // CONSTRUCTED SEQUENCE
                    "3080" +
                        // OBJECT IDENTIFIER (1.2.840.113549.1.1.1, rsaEncryption)
                        "06092A864886F70D010101" +
                        // NULL
                        "0500" +
                        // End-of-Contents
                        "0000" +
                    // BIT STRING
                    "03820111" +
                        // 0 unused bits
                        "00" +
                        // sneaky inspection of the payload bytes
                        // CONSTRUCTED SEQUENCE
                        "3080" +
                            // INTEGER (n)
                            "02820101" +
                                "00AF81C1CBD8203F624A539ED6608175372393A2837D4890E48A19DED3697311" +
                                "5620968D6BE0D3DAA38AA777BE02EE0B6B93B724E8DCC12B632B4FA80BBC925B" +
                                "CE624F4CA7CC606306B39403E28C932D24DD546FFE4EF6A37F10770B2215EA8C" +
                                "BB5BF427E8C4D89B79EB338375100C5F83E55DE9B4466DDFBEEE42539AEF33EF" +
                                "187B7760C3B1A1B2103C2D8144564A0C1039A09C85CF6B5974EB516FC8D6623C" +
                                "94AE3A5A0BB3B4C792957D432391566CF3E2A52AFB0C142B9E0681B8972671AF" +
                                "2B82DD390A39B939CF719568687E4990A63050CA7768DCD6B378842F18FDB1F6" +
                                "D9FF096BAF7BEB98DCF930D66FCFD503F58D41BFF46212E24E3AFC45EA42BD88" +
                                "47" +
                            // INTEGER (e)
                            "02050200000441" +
                            // End-of-Contents
                            "0000" +
                        // (no EoC for the BIT STRING)
                    // End-of-Contents
                    "0000";

            WriteRSAPublicKey(AsnEncodingRules.CER, ExpectedHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, false)]
        [InlineData(PublicEncodingRules.CER, false)]
        [InlineData(PublicEncodingRules.DER, false)]
        [InlineData(PublicEncodingRules.BER, true)]
        [InlineData(PublicEncodingRules.CER, true)]
        [InlineData(PublicEncodingRules.DER, true)]
        public static void CannotEncodeWhileUnbalanced(PublicEncodingRules ruleSet, bool customTag)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (customTag)
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true));
                }
                else
                {
                    writer.PushSequence();
                }

                int written = -5;

                Assert.Throws<InvalidOperationException>(() => writer.Encode());
                Assert.Throws<InvalidOperationException>(() => writer.TryEncode(Span<byte>.Empty, out written));
                Assert.Equal(-5, written);

                byte[] buf = new byte[10];
                Assert.Throws<InvalidOperationException>(() => writer.TryEncode(buf, out written));
                Assert.Equal(-5, written);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void PushSequence_EndOfContents(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.PushSequence(Asn1Tag.EndOfContents));
            }
        }
    }
}
