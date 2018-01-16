// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public abstract class WriteCharacterString : Asn1WriterTests
    {
        internal abstract void WriteString(AsnWriter writer, string s);
        internal abstract void WriteString(AsnWriter writer, Asn1Tag tag, string s);

        internal abstract void WriteSpan(AsnWriter writer, ReadOnlySpan<char> s);
        internal abstract void WriteSpan(AsnWriter writer, Asn1Tag tag, ReadOnlySpan<char> s);

        internal abstract Asn1Tag StandardTag { get; }

        protected const string GettysburgAddress =
            "Four score and seven years ago our fathers brought forth on this continent, a new nation, " +
            "conceived in Liberty, and dedicated to the proposition that all men are created equal.\r\n" +
            "\r\n" +
            "Now we are engaged in a great civil war, testing whether that nation, or any nation so " +
            "conceived and so dedicated, can long endure. We are met on a great battle-field of that " +
            "war. We have come to dedicate a portion of that field, as a final resting place for those " +
            "who here gave their lives that that nation might live. It is altogether fitting and proper " +
            "that we should do this.\r\n" +
            "\r\n" +
            "But, in a larger sense, we can not dedicate-we can not consecrate-we can not hallow-this " +
            "ground. The brave men, living and dead, who struggled here, have consecrated it, far above " +
            "our poor power to add or detract. The world will little note, nor long remember what we say " +
            "here, but it can never forget what they did here. It is for us the living, rather, to be " +
            "dedicated here to the unfinished work which they who fought here have thus far so nobly " +
            "advanced. It is rather for us to be here dedicated to the great task remaining before " +
            "us-that from these honored dead we take increased devotion to that cause for which they " +
            "gave the last full measure of devotion-that we here highly resolve that these dead shall " +
            "not have died in vain-that this nation, under God, shall have a new birth of freedom-and " +
            "that government of the people, by the people, for the people, shall not perish from the earth.";

        protected void VerifyWrite_BER_String(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                WriteString(writer, input);

                Verify(writer, Stringify(StandardTag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_BER_String_CustomTag(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 14);
                WriteString(writer, tag, input);

                Verify(writer, Stringify(tag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_CER_String(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                WriteString(writer, input);

                Verify(writer, Stringify(StandardTag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_CER_String_CustomTag(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Private, 19);
                WriteString(writer, tag, input);

                Verify(writer, Stringify(tag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_DER_String(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                WriteString(writer, input);

                Verify(writer, Stringify(StandardTag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_DER_String_CustomTag(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Application, 2);
                WriteString(writer, tag, input);

                Verify(writer, Stringify(tag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_BER_Span(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                WriteSpan(writer, input.AsReadOnlySpan());

                Verify(writer, Stringify(StandardTag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_BER_Span_CustomTag(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Private, int.MaxValue >> 1);
                WriteSpan(writer, tag, input.AsReadOnlySpan());

                Verify(writer, Stringify(tag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_CER_Span(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                WriteSpan(writer, input.AsReadOnlySpan());

                Verify(writer, Stringify(StandardTag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_CER_Span_CustomTag(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Application, 30);
                WriteSpan(writer, tag, input.AsReadOnlySpan());

                Verify(writer, Stringify(tag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_DER_Span(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                WriteSpan(writer, input.AsReadOnlySpan());

                Verify(writer, Stringify(StandardTag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_DER_Span_CustomTag(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 31);
                WriteSpan(writer, tag, input.AsReadOnlySpan());

                Verify(writer, Stringify(tag) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_BER_String_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, isConstructed: true);
                WriteString(writer, tag, input);

                Verify(writer, Stringify(standard) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_BER_String_CustomTag_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Application, 19, isConstructed: true);
                Asn1Tag expected = new Asn1Tag(tag.TagClass, tag.TagValue);
                WriteString(writer, tag, input);

                Verify(writer, Stringify(expected) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_BER_Span_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, isConstructed: true);
                WriteSpan(writer, tag, input.AsReadOnlySpan());

                Verify(writer, Stringify(standard) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_BER_Span_CustomTag_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Private, 24601, isConstructed: true);
                Asn1Tag expected = new Asn1Tag(tag.TagClass, tag.TagValue);
                WriteSpan(writer, tag, input.AsReadOnlySpan());

                Verify(writer, Stringify(expected) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_CER_String_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, isConstructed: true);
                WriteString(writer, tag, input);

                Verify(writer, Stringify(standard) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_CER_String_CustomTag_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 1701, isConstructed: true);
                Asn1Tag expected = new Asn1Tag(tag.TagClass, tag.TagValue);
                WriteString(writer, tag, input);

                Verify(writer, Stringify(expected) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_CER_Span_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, isConstructed: true);
                WriteSpan(writer, tag, input.AsReadOnlySpan());

                Verify(writer, Stringify(standard) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_CER_Span_CustomTag_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Application, 11, isConstructed: true);
                Asn1Tag expected = new Asn1Tag(tag.TagClass, tag.TagValue);
                WriteSpan(writer, tag, input.AsReadOnlySpan());

                Verify(writer, Stringify(expected) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_DER_String_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, isConstructed: true);
                WriteString(writer, tag, input);

                Verify(writer, Stringify(standard) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_DER_String_CustomTag_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Application, 19, isConstructed: true);
                Asn1Tag expected = new Asn1Tag(tag.TagClass, tag.TagValue);
                WriteString(writer, tag, input);

                Verify(writer, Stringify(expected) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_DER_Span_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, isConstructed: true);
                WriteSpan(writer, tag, input.AsReadOnlySpan());

                Verify(writer, Stringify(standard) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_DER_Span_CustomTag_ClearsConstructed(string input, string expectedPayloadHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Private, 24601, isConstructed: true);
                Asn1Tag expected = new Asn1Tag(tag.TagClass, tag.TagValue);
                WriteSpan(writer, tag, input.AsReadOnlySpan());

                Verify(writer, Stringify(expected) + expectedPayloadHex);
            }
        }

        protected void VerifyWrite_String_Null(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "str",
                    () => WriteString(writer, null));
            }
        }

        protected void VerifyWrite_String_Null_CustomTag(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "str",
                    () => WriteString(writer, new Asn1Tag(TagClass.ContextSpecific, 3), null));
            }
        }

        protected void VerifyWrite_EndOfContents_String(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => WriteString(writer, Asn1Tag.EndOfContents, "hi"));
            }
        }

        protected void VerifyWrite_EndOfContents_Span(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => WriteSpan(writer, Asn1Tag.EndOfContents, "hi".AsReadOnlySpan()));
            }
        }

        private void VerifyWrite_CERSegmented(AsnWriter writer, string tagHex, int contentByteCount)
        {
            int div = Math.DivRem(contentByteCount, 1000, out int rem);

            // tag, length(80), div full segments at 1004 bytes each, and the end of contents.
            int encodedSize = (tagHex.Length / 2) + 1 + 1004 * div + 2;

            if (rem != 0)
            {
                // tag, contents (length TBD)
                encodedSize += 1 + rem;

                if (encodedSize < 0x80)
                    encodedSize++;
                else if (encodedSize <= 0xFF)
                    encodedSize += 2;
                else
                    encodedSize += 3;
            }

            byte[] encoded = writer.Encode();

            Assert.Equal(tagHex, encoded.AsReadOnlySpan().Slice(0, tagHex.Length / 2).ByteArrayToHex());
            Assert.Equal("0000", encoded.AsReadOnlySpan().Slice(encoded.Length - 2).ByteArrayToHex());
            Assert.Equal(encodedSize, encoded.Length);
        }

        protected void VerifyWrite_CERSegmented_String(string input, int contentByteCount)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, true);
                string tagHex = Stringify(tag);

                WriteString(writer, input);
                VerifyWrite_CERSegmented(writer, tagHex, contentByteCount);
            }
        }

        protected void VerifyWrite_CERSegmented_String_CustomTag(string input, int contentByteCount)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Private, 7, true);
                string tagHex = Stringify(tag);

                WriteString(writer, tag, input);
                VerifyWrite_CERSegmented(writer, tagHex, contentByteCount);
            }
        }

        protected void VerifyWrite_CERSegmented_String_ConstructedTag(string input, int contentByteCount)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, true);
                string tagHex = Stringify(tag);

                WriteString(writer, tag, input);
                VerifyWrite_CERSegmented(writer, tagHex, contentByteCount);
            }
        }

        protected void VerifyWrite_CERSegmented_String_CustomPrimitiveTag(string input, int contentByteCount)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag prim = new Asn1Tag(TagClass.Application, 42);
                Asn1Tag constr = new Asn1Tag(prim.TagClass, prim.TagValue, true);
                string tagHex = Stringify(constr);

                WriteString(writer, prim, input);
                VerifyWrite_CERSegmented(writer, tagHex, contentByteCount);
            }
        }

        protected void VerifyWrite_CERSegmented_Span(string input, int contentByteCount)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, true);
                string tagHex = Stringify(tag);

                WriteSpan(writer, input.AsReadOnlySpan());
                VerifyWrite_CERSegmented(writer, tagHex, contentByteCount);
            }
        }

        protected void VerifyWrite_CERSegmented_Span_CustomTag(string input, int contentByteCount)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Private, 7, true);
                string tagHex = Stringify(tag);

                WriteSpan(writer, tag, input.AsReadOnlySpan());
                VerifyWrite_CERSegmented(writer, tagHex, contentByteCount);
            }
        }

        protected void VerifyWrite_CERSegmented_Span_ConstructedTag(string input, int contentByteCount)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag standard = StandardTag;
                Asn1Tag tag = new Asn1Tag(standard.TagClass, standard.TagValue, true);
                string tagHex = Stringify(tag);

                WriteSpan(writer, tag, input.AsReadOnlySpan());
                VerifyWrite_CERSegmented(writer, tagHex, contentByteCount);
            }
        }

        protected void VerifyWrite_CERSegmented_Span_CustomPrimitiveTag(string input, int contentByteCount)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag prim = new Asn1Tag(TagClass.Application, 42);
                Asn1Tag constr = new Asn1Tag(prim.TagClass, prim.TagValue, true);
                string tagHex = Stringify(constr);

                WriteSpan(writer, prim, input.AsReadOnlySpan());
                VerifyWrite_CERSegmented(writer, tagHex, contentByteCount);
            }
        }

        protected void VerifyWrite_String_NonEncodable(string input)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Assert.Throws<EncoderFallbackException>(() => WriteString(writer, input));
            }
        }

        protected void VerifyWrite_Span_NonEncodable(string input)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Assert.Throws<EncoderFallbackException>(() => WriteSpan(writer, input.AsReadOnlySpan()));
            }
        }
    }
}
