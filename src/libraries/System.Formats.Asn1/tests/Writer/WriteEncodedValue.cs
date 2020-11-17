// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteEncodedValue : Asn1WriterTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyNoEmptyValues(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "value",
                () => writer.WriteEncodedValue(ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyCurrentEncodingMattersForLength(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            byte[] wideNull = { 0x05, 0x81, 0x00 };

            if (ruleSet == AsnEncodingRules.BER)
            {
                writer.WriteEncodedValue(wideNull);
                Verify(writer, "058100");
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>(
                    "value",
                    () => writer.WriteEncodedValue(wideNull));
            }
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyTrailingBytesDisallowed(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            byte[] nullAndARogueByte = { 0x05, 0x00, 0x00 };

            AssertExtensions.Throws<ArgumentException>(
                "value",
                () => writer.WriteEncodedValue(nullAndARogueByte));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.DER)]
        public void WriteComplexEncodedValue(AsnEncodingRules ruleSet)
        {
            string inputHex =
                "3082027A020100300D06092A864886F70D010101050004820264308202600201" +
                "0002818200BCACB1A5349D7B35A580AC3B3998EB15EBF900ECB329BF1F75717A" +
                "00B2199C8A18D791B592B7EC52BD5AF2DB0D3B635F0595753DFF7BA7C9872DBF" +
                "7E3226DEF44A07CA568D1017992C2B41BFE5EC3570824CF1F4B15919FED513FD" +
                "A56204AF2034A2D08FF04C2CCA49D168FA03FA2FA32FCCD3484C15F0A2E5467C" +
                "76FC760B5509020301000102818110D7A0A704F69EE247D31FECCC84324E1B69" +
                "B7B3A97DAB4639636716EB4F1E7A7463BFE9D3BE4FDE05F1B9B6A4AC7DBF247E" +
                "364051CF5DC7BF65ADCFABD5ECF6A2B627171F6798541F1BF11CAC9AA56A6B2B" +
                "C9C1082616651AB1AE6C02E10C7C8802C24A6B4D181087FD241D0753782CF4CD" +
                "0355F8FD15791B49C90022BE3CE45502410E15300A9D34BA37B6BDA831BC6727" +
                "B2F7F6D0EFB7B33A99C9AF28CFD625E245A54F251B784C4791ADA585ADB711D9" +
                "300A3D52B450CC307F55D31E1217B9FFD74502410D65C60DE8B6F54A7756FD1C" +
                "CBA76CE41EF446D024031EE9C5A40931B07336CFED35A8EE580E19DB8592CB0F" +
                "266EC69028EB9E98E3E84FF1A459A8A26860A610F502410D9DB4BE7E730D9D72" +
                "A57B2AE3738571C7C82F09A7BEB5E91D94AACC10CCBE33027B3C708BE68CC830" +
                "71BA87545B00782F5E4D49A4595886B56F9342810848725502410CF6FBDDE1E1" +
                "8B2570AF2169883A90C9809AEB1BE87D8CA0B4BDB497FD24C15A1D36DC2F29CF" +
                "1B7EAF980A20B31467DA817EE18F1A9D691F71E7C1A4C8551EDF310241010CE9" +
                "936E96FBADF87240CC419D01081BB67C981D44314E58583AC7FE9379EA0272E6" +
                "C4C7C14638E1D5ECE7840DDB15A12D7054A418F8764FA54CE134EBD2635E";

            string expectedOutput = "30820282" + "A082027E" + inputHex;

            byte[] input = inputHex.HexToByteArray();
            AsnWriter writer = new AsnWriter(ruleSet);

            using (writer.PushSequence())
            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                writer.WriteEncodedValue(input);
            }

            Verify(writer, expectedOutput);
        }
    }
}
