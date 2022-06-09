// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadGeneralizedTime
    {
        [Theory]
        // yyyyMMddHH (2017090821)
        [InlineData(AsnEncodingRules.BER, "180A32303137303930383231", 2017, 9, 8, 21, 0, 0, 0, null, 0)]
        // yyyyMMddHHZ (2017090821Z)
        [InlineData(AsnEncodingRules.BER, "180B323031373039303832315A", 2017, 9, 8, 21, 0, 0, 0, 0, 0)]
        // yyyyMMddHH-HH (2017090821-01)
        [InlineData(AsnEncodingRules.BER, "180D323031373039303832312D3031", 2017, 9, 8, 21, 0, 0, 0, -1, 0)]
        // yyyyMMddHH+HHmm (2017090821+0118)
        [InlineData(AsnEncodingRules.BER, "180F323031373039303832312B30313138", 2017, 9, 8, 21, 0, 0, 0, 1, 18)]
        // yyyyMMddHH,hourFrac (2017090821.1)
        [InlineData(AsnEncodingRules.BER, "180C323031373039303832312C31", 2017, 9, 8, 21, 6, 0, 0, null, 0)]
        // yyyyMMddHH.hourFracZ (2017090821.2010Z)
        [InlineData(AsnEncodingRules.BER, "1810323031373039303832312E323031305A", 2017, 9, 8, 21, 12, 3, 600, 0, 0)]
        // yyyyMMddHH,hourFrac-HH (2017090821,3099-01)
        [InlineData(AsnEncodingRules.BER, "1812323031373039303832312C333039392D3031", 2017, 9, 8, 21, 18, 35, 640, -1, 0)]
        // yyyyMMddHH.hourFrac+HHmm (2017090821.201+0118)
        [InlineData(AsnEncodingRules.BER, "1813323031373039303832312E3230312B30313138", 2017, 9, 8, 21, 12, 3, 600, 1, 18)]
        // yyyyMMddHHmm (201709082358)
        [InlineData(AsnEncodingRules.BER, "180C323031373039303832333538", 2017, 9, 8, 23, 58, 0, 0, null, 0)]
        // yyyyMMddHHmmZ (201709082358Z)
        [InlineData(AsnEncodingRules.BER, "180D3230313730393038323335385A", 2017, 9, 8, 23, 58, 0, 0, 0, 0)]
        // yyyyMMddHHmm-HH (201709082358-01)
        [InlineData(AsnEncodingRules.BER, "180F3230313730393038323335382D3031", 2017, 9, 8, 23, 58, 0, 0, -1, 0)]
        // yyyyMMddHHmm+HHmm (201709082358+0118)
        [InlineData(AsnEncodingRules.BER, "18113230313730393038323335382B30313138", 2017, 9, 8, 23, 58, 0, 0, 1, 18)]
        // yyyyMMddHHmm.minuteFrac (201709082358.01)
        [InlineData(AsnEncodingRules.BER, "180F3230313730393038323335382E3031", 2017, 9, 8, 23, 58, 0, 600, null, 0)]
        // yyyyMMddHHmm,minuteFracZ (201709082358,11Z)
        [InlineData(AsnEncodingRules.BER, "18103230313730393038323335382C31315A", 2017, 9, 8, 23, 58, 6, 600, 0, 0)]
        // yyyyMMddHHmm.minuteFrac-HH (201709082358.05-01)
        [InlineData(AsnEncodingRules.BER, "18123230313730393038323335382E30352D3031", 2017, 9, 8, 23, 58, 3, 0, -1, 0)]
        // yyyyMMddHHmm,minuteFrac+HHmm (201709082358,007+0118)
        [InlineData(AsnEncodingRules.BER, "18153230313730393038323335382C3030372B30313138", 2017, 9, 8, 23, 58, 0, 420, 1, 18)]
        // yyyyMMddHHmmss (20161106012345) - Ambiguous time due to DST "fall back" in US & Canada
        [InlineData(AsnEncodingRules.BER, "180E3230313631313036303132333435", 2016, 11, 6, 1, 23, 45, 0, null, 0)]
        // yyyyMMddHHmmssZ (20161106012345Z)
        [InlineData(AsnEncodingRules.BER, "180F32303136313130363031323334355A", 2016, 11, 6, 1, 23, 45, 0, 0, 0)]
        // yyyyMMddHHmmss-HH (20161106012345-01)
        [InlineData(AsnEncodingRules.BER, "181132303136313130363031323334352D3031", 2016, 11, 6, 1, 23, 45, 0, -1, 0)]
        // yyyyMMddHHmmss+HHmm (20161106012345+0118)
        [InlineData(AsnEncodingRules.BER, "181332303136313130363031323334352B30313138", 2016, 11, 6, 1, 23, 45, 0, 1, 18)]
        // yyyyMMddHHmmss.secondFrac (20161106012345.6789) - Ambiguous time due to DST "fall back" in US & Canada
        [InlineData(AsnEncodingRules.BER, "181332303136313130363031323334352E36373839", 2016, 11, 6, 1, 23, 45, 678, null, 0)]
        // yyyyMMddHHmmss,secondFracZ (20161106012345,7654Z)
        [InlineData(AsnEncodingRules.BER, "181432303136313130363031323334352C373635345A", 2016, 11, 6, 1, 23, 45, 765, 0, 0)]
        // yyyyMMddHHmmss.secondFrac-HH (20161106012345.001-01)
        [InlineData(AsnEncodingRules.BER, "181532303136313130363031323334352E3030312D3031", 2016, 11, 6, 1, 23, 45, 1, -1, 0)]
        // yyyyMMddHHmmss,secondFrac+HHmm (20161106012345,0009+0118)
        [InlineData(AsnEncodingRules.BER, "181832303136313130363031323334352C303030392B30313138", 2016, 11, 6, 1, 23, 45, 0, 1, 18)]

        // yyyyMMddHHmmssZ (20161106012345Z)
        [InlineData(AsnEncodingRules.CER, "180F32303136313130363031323334355A", 2016, 11, 6, 1, 23, 45, 0, 0, 0)]
        [InlineData(AsnEncodingRules.DER, "180F32303136313130363031323334355A", 2016, 11, 6, 1, 23, 45, 0, 0, 0)]

        // yyyyMMddHHmmss.secondFracZ (20161106012345,7654Z)
        [InlineData(AsnEncodingRules.CER, "181432303136313130363031323334352E373635345A", 2016, 11, 6, 1, 23, 45, 765, 0, 0)]
        [InlineData(AsnEncodingRules.DER, "181432303136313130363031323334352E373635345A", 2016, 11, 6, 1, 23, 45, 765, 0, 0)]
        public static void ParseTime_Valid(
            AsnEncodingRules ruleSet,
            string inputHex,
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            int millisecond,
            int? offsetHour,
            int offsetMinute)
        {
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, ruleSet);
            DateTimeOffset value = reader.ReadGeneralizedTime();
            Assert.False(reader.HasData, "reader.HasData");

            Assert.Equal(year, value.Year);
            Assert.Equal(month, value.Month);
            Assert.Equal(day, value.Day);
            Assert.Equal(hour, value.Hour);
            Assert.Equal(minute, value.Minute);
            Assert.Equal(second, value.Second);
            Assert.Equal(millisecond, value.Millisecond);

            TimeSpan timeOffset;

            if (offsetHour == null)
            {
                // Ask the system what offset it thinks was relevant for that time.
                // Includes DST ambiguity.
                timeOffset = new DateTimeOffset(value.LocalDateTime).Offset;
            }
            else
            {
                timeOffset = new TimeSpan(offsetHour.Value, offsetMinute, 0);
            }

            Assert.Equal(timeOffset, value.Offset);
        }

        [Theory]
        // yyyyMMddHH (2017090821)
        [InlineData("180A32303137303930383231")]
        // yyyyMMddHHZ (2017090821Z)
        [InlineData("180B323031373039303832315A")]
        // yyyyMMddHH-HH (2017090821-01)
        [InlineData("180D323031373039303832312D3031")]
        // yyyyMMddHH+HHmm (2017090821+0118)
        [InlineData("180F323031373039303832312B30313138")]
        // yyyyMMddHH,hourFrac (2017090821,1)
        [InlineData("180C323031373039303832312C31")]
        // yyyyMMddHH.hourFrac (2017090821.1)
        [InlineData("180C323031373039303832312E31")]
        // yyyyMMddHH,hourFracZ (2017090821,2010Z)
        [InlineData("1810323031373039303832312C323031305A")]
        // yyyyMMddHH.hourFracZ (2017090821.2010Z)
        [InlineData("1810323031373039303832312E323031305A")]
        // yyyyMMddHH,hourFrac-HH (2017090821,3099-01)
        [InlineData("1812323031373039303832312C333039392D3031")]
        // yyyyMMddHH.hourFrac-HH (2017090821.3099-01)
        [InlineData("1812323031373039303832312E333039392D3031")]
        // yyyyMMddHH,hourFrac+HHmm (2017090821,201+0118)
        [InlineData("1813323031373039303832312C3230312B30313138")]
        // yyyyMMddHH.hourFrac+HHmm (2017090821.201+0118)
        [InlineData("1813323031373039303832312E3230312B30313138")]
        // yyyyMMddHHmm (201709082358)
        [InlineData("180C323031373039303832333538")]
        // yyyyMMddHHmmZ (201709082358Z)
        [InlineData("180D3230313730393038323335385A")]
        // yyyyMMddHHmm-HH (201709082358-01)
        [InlineData("180F3230313730393038323335382D3031")]
        // yyyyMMddHHmm+HHmm (201709082358+0118)
        [InlineData("18113230313730393038323335382B30313138")]
        // yyyyMMddHHmm,minuteFrac (201709082358,01)
        [InlineData("180F3230313730393038323335382C3031")]
        // yyyyMMddHHmm.minuteFrac (201709082358.01)
        [InlineData("180F3230313730393038323335382E3031")]
        // yyyyMMddHHmm,minuteFracZ (201709082358,11Z)
        [InlineData("18103230313730393038323335382C31315A")]
        // yyyyMMddHHmm.minuteFracZ (201709082358.11Z)
        [InlineData("18103230313730393038323335382E31315A")]
        // yyyyMMddHHmm,minuteFrac-HH (201709082358m05-01)
        [InlineData("18123230313730393038323335382C30352D3031")]
        // yyyyMMddHHmm.minuteFrac-HH (201709082358.05-01)
        [InlineData("18123230313730393038323335382E30352D3031")]
        // yyyyMMddHHmm,minuteFrac+HHmm (201709082358,007+0118)
        [InlineData("18153230313730393038323335382C3030372B30313138")]
        // yyyyMMddHHmm.minuteFrac+HHmm (201709082358.007+0118)
        [InlineData("18153230313730393038323335382E3030372B30313138")]
        // yyyyMMddHHmmss (20161106012345)
        [InlineData("180E3230313631313036303132333435")]
        // yyyyMMddHHmmss-HH (20161106012345-01)
        [InlineData("181132303136313130363031323334352D3031")]
        // yyyyMMddHHmmss+HHmm (20161106012345+0118)
        [InlineData("181332303136313130363031323334352B30313138")]
        // yyyyMMddHHmmss,secondFrac (20161106012345,6789)
        [InlineData("181332303136313130363031323334352C36373839")]
        // yyyyMMddHHmmss.secondFrac (20161106012345.6789)
        [InlineData("181332303136313130363031323334352E36373839")]
        // yyyyMMddHHmmss,secondFracZ (20161106012345,7654Z)
        [InlineData("181432303136313130363031323334352C373635345A")]
        // yyyyMMddHHmmss,secondFrac-HH (20161106012345,001-01)
        [InlineData("181532303136313130363031323334352C3030312D3031")]
        // yyyyMMddHHmmss.secondFrac-HH (20161106012345.001-01)
        [InlineData("181532303136313130363031323334352E3030312D3031")]
        // yyyyMMddHHmmss,secondFrac+HHmm (20161106012345,0009+0118)
        [InlineData("181832303136313130363031323334352C303030392B30313138")]
        // yyyyMMddHHmmss.secondFrac+HHmm (20161106012345.0009+0118)
        [InlineData("181832303136313130363031323334352E303030392B30313138")]
        // yyyyMMddHHmmss.secondFrac0Z (20161106012345.76540Z)
        [InlineData("181532303136313130363031323334352E37363534305A")]
        // Constructed encoding of yyyyMMddHHmmssZ
        [InlineData(
            "3880" +
              "040432303136" +
              "04023131" +
              "0403303630" +
              "040131" +
              "0405323334355A" +
              "0000")]
        public static void ParseTime_BerOnly(string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader cerReader = new AsnReader(inputData, AsnEncodingRules.CER);
            AsnReader derReader = new AsnReader(inputData, AsnEncodingRules.DER);

            Assert.Throws<AsnContentException>(() => cerReader.ReadGeneralizedTime());
            Assert.Throws<AsnContentException>(() => derReader.ReadGeneralizedTime());

            // Prove it was not just corrupt input
            AsnReader berReader = new AsnReader(inputData, AsnEncodingRules.BER);
            berReader.ReadGeneralizedTime();
            Assert.False(berReader.HasData, "berReader.HasData");
            Assert.True(cerReader.HasData, "cerReader.HasData");
            Assert.True(derReader.HasData, "derReader.HasData");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "2017121900.06861111087Z")]
        [InlineData(AsnEncodingRules.BER, "201712190004.11666665167Z")]
        [InlineData(AsnEncodingRules.BER, "20171219000406.9999991Z")]
        [InlineData(AsnEncodingRules.CER, "20171219000406.9999991Z")]
        [InlineData(AsnEncodingRules.DER, "20171219000406.9999991Z")]
        public static void MaximumEffectivePrecision(AsnEncodingRules ruleSet, string dateAscii)
        {
            DateTimeOffset expectedTime = new DateTimeOffset(2017, 12, 19, 0, 4, 6, TimeSpan.Zero);
            expectedTime += new TimeSpan(TimeSpan.TicksPerSecond - 9);

            byte[] inputData = new byte[dateAscii.Length + 2];
            inputData[0] = 0x18;
            inputData[1] = (byte)dateAscii.Length;
            Text.Encoding.ASCII.GetBytes(dateAscii, 0, dateAscii.Length, inputData, 2);

            AsnReader reader = new AsnReader(inputData, ruleSet);
            Assert.Equal(expectedTime, reader.ReadGeneralizedTime());
        }

        [Fact]
        public static void ExcessivelyPreciseFraction()
        {
            byte[] inputData = "\u0018\u002A2017092118.012345678901234567890123456789Z"u8.ToArray();

            AsnReader berReader = new AsnReader(inputData, AsnEncodingRules.BER);
            DateTimeOffset value = berReader.ReadGeneralizedTime();
            Assert.False(berReader.HasData, "berReader.HasData");

            DateTimeOffset expected = new DateTimeOffset(2017, 9, 21, 18, 0, 44, 444, TimeSpan.Zero);
            expected += new TimeSpan(4440);

            Assert.Equal(expected, value);
        }

        [Fact]
        public static void ExcessivelyPreciseFraction_OneTenthPlusEpsilon()
        {
            byte[] inputData = "\u0018\u002A20170921180044.10000000000000000000000001Z"u8.ToArray();

            AsnReader derReader = new AsnReader(inputData, AsnEncodingRules.DER);
            DateTimeOffset value = derReader.ReadGeneralizedTime();
            Assert.False(derReader.HasData, "derReader.HasData");

            DateTimeOffset expected = new DateTimeOffset(2017, 9, 21, 18, 0, 44, 100, TimeSpan.Zero);

            Assert.Equal(expected, value);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        public static void MultiSegmentExcessivelyPreciseFraction(AsnEncodingRules ruleSet)
        {
            // This builds "20171207173522.0000...0001Z" where the Z required a second CER segment.
            // This is a bit of nonsense, really, because it is encoding 1e-985 seconds, which is
            // oodles of orders of magnitude smaller than Planck time (~5e-44).
            // But, the spec says "any number of decimal places", and 985 is a number.

            // A0 80 (context specifc 0, constructed, indefinite length)
            //    04 82 03 E8 (octet string, primitive, 1000 bytes)
            //       ASCII("20171207173522." + new string('0', 984) + '1')
            //    04 01 (octet string, primitive, 1 byte)
            //       ASCII("Z")
            //    00 00 (end of contents)
            //
            // 1001 content bytes + 10 bytes of structure.
            byte[] header = "A080048203E8".HexToByteArray();
            byte[] contents0 = Text.Encoding.ASCII.GetBytes("20171207173522." + new string('0', 984) + "1");
            byte[] cdr = { 0x04, 0x01, (byte)'Z', 0x00, 0x00 };
            byte[] inputData = header.Concat(contents0).Concat(cdr).ToArray();

            AsnReader reader = new AsnReader(inputData, ruleSet);
            DateTimeOffset value = reader.ReadGeneralizedTime(new Asn1Tag(TagClass.ContextSpecific, 0));
            DateTimeOffset expected = new DateTimeOffset(2017, 12, 7, 17, 35, 22, TimeSpan.Zero);
            Assert.Equal(expected, value);
        }

        [Fact]
        public static void ExcessivelyPreciseFraction_OneTenthPlusEpsilonAndZero()
        {
            byte[] inputData = "\u0018\u002A20170921180044.10000000000000000000000010Z"u8.ToArray();

            AsnReader berReader = new AsnReader(inputData, AsnEncodingRules.BER);
            DateTimeOffset value = berReader.ReadGeneralizedTime();
            Assert.False(berReader.HasData, "berReader.HasData");

            DateTimeOffset expected = new DateTimeOffset(2017, 9, 21, 18, 0, 44, 100, TimeSpan.Zero);
            Assert.Equal(expected, value);

            AsnReader cerReader = new AsnReader(inputData, AsnEncodingRules.CER);
            AsnReader derReader = new AsnReader(inputData, AsnEncodingRules.DER);
            Assert.Throws<AsnContentException>(() => cerReader.ReadGeneralizedTime());
            Assert.Throws<AsnContentException>(() => derReader.ReadGeneralizedTime());
        }

        [Fact]
        public static void ExcessivelyPreciseNonFraction()
        {
            byte[] inputData = "\u0018\u002A2017092118.012345678901234567890123Q56789Z"u8.ToArray();
            AsnReader berReader = new AsnReader(inputData, AsnEncodingRules.BER);

            Assert.Throws<AsnContentException>(() => berReader.ReadGeneralizedTime());
        }

        [Theory]
        [InlineData("Wrong Tag", "170F32303136313130363031323334355A")]
        [InlineData("Incomplete Tag", "1F")]
        [InlineData("No Length", "18")]
        [InlineData("No payload", "180F")]
        [InlineData("Length exceeds content", "181032303136313130363031323334355A")]
        [InlineData("yyyyMMdd", "18083230313631313036")]
        [InlineData("yyyyMMddZ", "180932303136313130365A")]
        [InlineData("yyyyMMdd+HH", "180B32303136313130362D3030")]
        [InlineData("yyyyMMdd-HHmm", "180D32303136313130362B30303030")]
        [InlineData("yyyyMMddH", "1809323031363131303630")]
        [InlineData("yyyyMMddHZ", "180A3230313631313036305A")]
        [InlineData("yQyyMMddHH", "180A32513136313130363031")]
        [InlineData("yyyyMQddHH", "180A32303136315130363031")]
        [InlineData("yyyyMMQdHH", "180A32303136313151363031")]
        [InlineData("yyyyMMddHQ", "180A32303136313130363051")]
        [InlineData("yyyyMMddHH,", "180B323031363131303630312C")]
        [InlineData("yyyyMMddHH.", "180B323031363131303630312E")]
        [InlineData("yyyyMMddHHQ", "180B3230313631313036303151")]
        [InlineData("yyyyMMddHH,Q", "180C323031363131303630312C51")]
        [InlineData("yyyyMMddHH.Q", "180C323031363131303630312E51")]
        [InlineData("yyyyMMddHH..", "180C323031363131303630312E2E")]
        [InlineData("yyyyMMddHH-", "180B323031363131303630312B")]
        [InlineData("yyyyMMddHH+", "180B323031363131303630312D")]
        [InlineData("yyyyMMddHHmQ", "180C323031363131303630313251")]
        [InlineData("yyyyMMddHHm+", "180C32303136313130363031322D")]
        [InlineData("yyyyMMddHHmmQ", "180D32303136313130363031323351")]
        [InlineData("yyyyMMddHHmm-", "180D3230313631313036303132332B")]
        [InlineData("yyyyMMddHHmm,", "180D3230313631313036303132332C")]
        [InlineData("yyyyMMddHHmm+", "180D3230313631313036303132332D")]
        [InlineData("yyyyMMddHHmm.", "180D3230313631313036303132332E")]
        [InlineData("yyyyMMddHHmm+Q", "180E3230313631313036303132332D51")]
        [InlineData("yyyyMMddHHmm.Q", "180E3230313631313036303132332E51")]
        [InlineData("yyyyMMddHHmm..", "180E3230313631313036303132332E2E")]
        [InlineData("yyyyMMddHHmm.ss,", "18103230313631313036303132332E31312E")]
        [InlineData("yyyyMMddHHmms", "180D32303136313130363031323334")]
        [InlineData("yyyyMMddHHmmsQ", "180E3230313631313036303132333451")]
        [InlineData("yyyyMMddHHmmss-", "180F32303136313130363031323334352B")]
        [InlineData("yyyyMMddHHmmss,", "180F32303136313130363031323334352C")]
        [InlineData("yyyyMMddHHmmss+", "180F32303136313130363031323334352D")]
        [InlineData("yyyyMMddHHmmss.", "180F32303136313130363031323334352E")]
        [InlineData("yyyyMMddHHmmssQ", "180F323031363131303630313233343551")]
        [InlineData("yyyyMMddHHmmss.Q", "181032303136313130363031323334352E51")]
        [InlineData("yyyyMMddHHmmss.Q", "181032303136313130363031323334352E2E")]
        [InlineData("yyyyMMddHHZmm", "180D323031363131303630315A3233")]
        [InlineData("yyyyMMddHHmmZss", "180F3230313631313036303132335A3435")]
        [InlineData("yyyyMMddHHmmssZ.s", "181232303136313130363031323334355A2E36")]
        [InlineData("yyyyMMddHH+H", "180C323031363131303630312D30")]
        [InlineData("yyyyMMddHH+HQ", "180D323031363131303630312D3051")]
        [InlineData("yyyyMMddHH+HHQ", "180E323031363131303630312D303051")]
        [InlineData("yyyyMMddHH+HHmQ", "180F323031363131303630312D30303051")]
        [InlineData("yyyyMMddHH+HHmmQ", "1810323031363131303630312D3030303151")]
        [InlineData("yyyyMMdd+H", "180A32303137313230382D30")]
        [InlineData("yyyyMMddH+", "180A3230313731323038302D")]
        [InlineData("yyyyMMddHH+0060", "180F323031373132303830392D30303630")]
        [InlineData("yyyyMMddHH-2400", "180F323031373132303830392D32343030")]
        [InlineData("yyyyMMddHH-HH:mm", "1810323031373039303832312D30313A3138")]
        [InlineData("yyyyMMddHHmm-HH:mm", "18123230313730393038323131302D30313A3138")]
        [InlineData("yyyyMMddHHmmss-HH:mm", "181432303137303930383231313032302D30313A3138")]
        [InlineData("yyyyMMddHH,hourFrac-HH:mm", "1812323031373039303832312C312D30313A3138")]
        [InlineData("yyyyMMddHH.hourFrac-HH:mm", "1812323031373039303832312E312D30313A3138")]
        [InlineData("yyyyMMddHHmm,minuteFrac-HH:mm", "18183230313730393038323335382C30303030352D30313A3138")]
        [InlineData("yyyyMMddHHmm.minuteFrac-HH:mm", "18183230313730393038323335382E30303030352D30313A3138")]
        [InlineData("yyyyMMddHHmmss,secondFrac-HH:mm", "181932303136313130363031323334352C393939392D30313A3138")]
        [InlineData("yyyyMMddHHmmss.secondFrac-HH:mm", "181932303136313130363031323334352E393939392D30313A3138")]
        public static void GetGeneralizedTime_Throws(string description, string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, AsnEncodingRules.BER);

            Assert.Throws<AsnContentException>(() => reader.ReadGeneralizedTime());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "180F32303136313130363031323334355A".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadGeneralizedTime(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadGeneralizedTime(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            Assert.Equal(
                new DateTimeOffset(2016, 11, 6, 1, 23, 45, TimeSpan.Zero),
                reader.ReadGeneralizedTime());

            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "850F32303136313130363031323334355A".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadGeneralizedTime(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadUtcTime());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadGeneralizedTime(new Asn1Tag(TagClass.Application, 5)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadGeneralizedTime(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.Equal(
                new DateTimeOffset(2016, 11, 6, 1, 23, 45, TimeSpan.Zero),
                reader.ReadGeneralizedTime(new Asn1Tag(TagClass.ContextSpecific, 5)));

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "180F32303136313130363031323334355A", TagClass.Universal, 24)]
        [InlineData(AsnEncodingRules.CER, "180F32303136313130363031323334355A", TagClass.Universal, 24)]
        [InlineData(AsnEncodingRules.DER, "180F32303136313130363031323334355A", TagClass.Universal, 24)]
        [InlineData(AsnEncodingRules.BER, "800F31393530303130323132333435365A", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.CER, "4C0F31393530303130323132333435365A", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "DF8A460F31393530303130323132333435365A", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            DateTimeOffset val1 = reader.ReadGeneralizedTime(new Asn1Tag(tagClass, tagValue, true));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            DateTimeOffset val2 = reader.ReadGeneralizedTime(new Asn1Tag(tagClass, tagValue, false));

            Assert.False(reader.HasData);

            Assert.Equal(val1, val2);
        }
    }
}
