// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadUtcTime
    {
        [Theory]
        // A, B2, C2
        [InlineData(AsnEncodingRules.BER, "17113137303930383130333530332D30373030", 2017, 9, 8, 10, 35, 3, -7, 0)]
        [InlineData(AsnEncodingRules.BER, "17113137303930383130333530332D30303530", 2017, 9, 8, 10, 35, 3, 0, -50)]
        [InlineData(AsnEncodingRules.BER, "17113137303930383130333530332B30373030", 2017, 9, 8, 10, 35, 3, 7, 0)]
        [InlineData(AsnEncodingRules.BER, "17113030303130313030303030302B30303030", 2000, 1, 1, 0, 0, 0, 0, 0)]
        [InlineData(AsnEncodingRules.BER, "17113030303130313030303030302D31343030", 2000, 1, 1, 0, 0, 0, -14, 0)]
        // A, B2, C1 (only legal form for CER or DER)
        [InlineData(AsnEncodingRules.BER, "170D3132303130323233353935395A", 2012, 1, 2, 23, 59, 59, 0, 0)]
        [InlineData(AsnEncodingRules.CER, "170D3439313233313233353935395A", 2049, 12, 31, 23, 59, 59, 0, 0)]
        [InlineData(AsnEncodingRules.DER, "170D3530303130323132333435365A", 1950, 1, 2, 12, 34, 56, 0, 0)]
        // A, B1, C2
        [InlineData(AsnEncodingRules.BER, "170F313730393038313033352D30373030", 2017, 9, 8, 10, 35, 0, -7, 0)]
        [InlineData(AsnEncodingRules.BER, "170F323730393038313033352B30393132", 2027, 9, 8, 10, 35, 0, 9, 12)]
        // A, B1, C1
        [InlineData(AsnEncodingRules.BER, "170B313230313032323335395A", 2012, 1, 2, 23, 59, 0, 0, 0)]
        [InlineData(AsnEncodingRules.BER, "170B343931323331323335395A", 2049, 12, 31, 23, 59, 0, 0, 0)]
        // BER Constructed form
        [InlineData(
            AsnEncodingRules.BER,
            "3780" +
              "04023132" +
              "04023031" +
              "2480" + "040130" + "040132" + "0000" +
              "040432333539" +
              "04830000015A" +
              "0000",
            2012, 1, 2, 23, 59, 0, 0, 0)]
        public static void ParseTime_Valid(
            AsnEncodingRules ruleSet,
            string inputHex,
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            int offsetHour,
            int offsetMinute)
        {
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, ruleSet);
            DateTimeOffset value = reader.ReadUtcTime();

            Assert.Equal(year, value.Year);
            Assert.Equal(month, value.Month);
            Assert.Equal(day, value.Day);
            Assert.Equal(hour, value.Hour);
            Assert.Equal(minute, value.Minute);
            Assert.Equal(second, value.Second);
            Assert.Equal(0, value.Millisecond);
            Assert.Equal(new TimeSpan(offsetHour, offsetMinute, 0), value.Offset);
        }

        [Fact]
        public static void ParseTime_InvalidValue_LegalString()
        {
            byte[] inputData = "17113030303030303030303030302D31353030".HexToByteArray();

            var exception = Assert.Throws<AsnContentException>(
                () =>
                {
                    AsnReader reader = new AsnReader(inputData, AsnEncodingRules.BER);
                    reader.ReadUtcTime();
                });

            Assert.NotNull(exception.InnerException);
            Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
        }

        [Theory]
        [InlineData(2011, 1912)]
        [InlineData(2012, 2012)]
        [InlineData(2013, 2012)]
        [InlineData(2111, 2012)]
        [InlineData(2112, 2112)]
        [InlineData(2113, 2112)]
        [InlineData(12, 12)]
        [InlineData(99, 12)]
        [InlineData(111, 12)]
        public static void ReadUtcTime_TwoYearMaximum(int maximum, int interpretedYear)
        {
            byte[] inputData = "170D3132303130323233353935395A".HexToByteArray();

            AsnReader reader = new AsnReader(inputData, AsnEncodingRules.BER);
            DateTimeOffset value = reader.ReadUtcTime(maximum);

            Assert.Equal(interpretedYear, value.Year);
        }

        [Theory]
        [InlineData(2011, 1912)]
        [InlineData(2012, 2012)]
        [InlineData(2013, 2012)]
        [InlineData(2111, 2012)]
        [InlineData(2112, 2112)]
        [InlineData(2113, 2112)]
        [InlineData(12, 12)]
        [InlineData(99, 12)]
        [InlineData(111, 12)]
        public static void ReadUtcTime_TwoYearMaximum_FromOptions(int maximum, int interpretedYear)
        {
            byte[] inputData = "170D3132303130323233353935395A".HexToByteArray();

            AsnReaderOptions options = new AsnReaderOptions { UtcTimeTwoDigitYearMax = maximum };
            AsnReader reader = new AsnReader(inputData, AsnEncodingRules.BER, options);
            DateTimeOffset value = reader.ReadUtcTime(maximum);

            Assert.Equal(interpretedYear, value.Year);
        }

        [Theory]
        [InlineData(2011, 1912)]
        [InlineData(2012, 2012)]
        [InlineData(2013, 2012)]
        [InlineData(2111, 2012)]
        [InlineData(2112, 2112)]
        [InlineData(2113, 2112)]
        [InlineData(12, 12)]
        [InlineData(99, 12)]
        [InlineData(111, 12)]
        public static void ReadUtcTime_TwoYearMaximum_FromOptions_CustomTag(int maximum, int interpretedYear)
        {
            byte[] inputData = "820D3132303130323233353935395A".HexToByteArray();

            AsnReaderOptions options = new AsnReaderOptions { UtcTimeTwoDigitYearMax = maximum };
            AsnReader reader = new AsnReader(inputData, AsnEncodingRules.BER, options);
            DateTimeOffset value = reader.ReadUtcTime(maximum, new Asn1Tag(TagClass.ContextSpecific, 2));

            Assert.Equal(interpretedYear, value.Year);
        }

        [Theory]
        [InlineData("A,B2,C2", AsnEncodingRules.CER, "17113137303930383130333530332D30373030")]
        [InlineData("A,B2,C2", AsnEncodingRules.DER, "17113137303930383130333530332D30373030")]
        [InlineData("A,B1,C2", AsnEncodingRules.CER, "170F313730393038313033352D30373030")]
        [InlineData("A,B1,C2", AsnEncodingRules.DER, "170F313730393038313033352D30373030")]
        [InlineData("A,B1,C1", AsnEncodingRules.CER, "170B313230313032323335395A")]
        [InlineData("A,B1,C1", AsnEncodingRules.DER, "170B313230313032323335395A")]
        [InlineData("A,B1,C1-NotZ", AsnEncodingRules.BER, "170B313230313032323335392B")]
        [InlineData("A,B1,C2-NotPlusMinus", AsnEncodingRules.BER, "170F313730393038313033352C30373030")]
        [InlineData("A,B2,C2-NotPlusMinus", AsnEncodingRules.BER, "17113137303930383130333530332C30373030")]
        [InlineData("A,B2,C2-MinuteOutOfRange", AsnEncodingRules.BER, "17113030303030303030303030302D31353630")]
        [InlineData("A,B1,C2-MinuteOutOfRange", AsnEncodingRules.BER, "170F303030303030303030302D31353630")]
        [InlineData("A1,B2,C1-NotZ", AsnEncodingRules.DER, "170D3530303130323132333435365B")]
        [InlineData("A,B2,C2-MissingDigit", AsnEncodingRules.BER, "17103137303930383130333530332C303730")]
        [InlineData("A,B2,C2-TooLong", AsnEncodingRules.BER, "17123137303930383130333530332B3037303030")]
        [InlineData("WrongTag", AsnEncodingRules.BER, "1A0D3132303130323233353935395A")]
        public static void ReadUtcTime_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadUtcTime());
        }

        [Fact]
        public static void ReadUtcTime_WayTooBig_Throws()
        {
            // Need to exceed the length that the shared pool will return for 17:
            byte[] inputData = new byte[4097+4];
            inputData[0] = 0x17;
            inputData[1] = 0x82;
            inputData[2] = 0x10;
            inputData[3] = 0x01;

            AsnReader reader = new AsnReader(inputData, AsnEncodingRules.BER);

            Assert.Throws<AsnContentException>(() => reader.ReadUtcTime());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "170D3530303130323132333435365A".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadUtcTime(expectedTag: Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadUtcTime(expectedTag: new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            Assert.Equal(
                new DateTimeOffset(1950, 1, 2, 12, 34, 56, TimeSpan.Zero),
                reader.ReadUtcTime());

            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(AsnEncodingRules ruleSet)
        {
            const int TwoDigitYearMax = 2052;
            byte[] inputData = "850D3530303130323132333435365A".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadUtcTime(expectedTag: Asn1Tag.Null));
            Assert.True(reader.HasData, "HasData after bad universal tag");

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadUtcTime(TwoDigitYearMax, expectedTag: Asn1Tag.Null));
            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadUtcTime());
            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(() => reader.ReadUtcTime(TwoDigitYearMax));
            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadUtcTime(expectedTag: new Asn1Tag(TagClass.Application, 5)));
            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadUtcTime(TwoDigitYearMax, expectedTag: new Asn1Tag(TagClass.Application, 5)));
            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadUtcTime(expectedTag: new Asn1Tag(TagClass.ContextSpecific, 7)));
            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.Throws<AsnContentException>(
                () => reader.ReadUtcTime(TwoDigitYearMax, expectedTag: new Asn1Tag(TagClass.ContextSpecific, 7)));
            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.Equal(
                new DateTimeOffset(1950, 1, 2, 12, 34, 56, TimeSpan.Zero),
                reader.ReadUtcTime(expectedTag: new Asn1Tag(TagClass.ContextSpecific, 5)));
            Assert.False(reader.HasData, "HasData after reading value");

            reader = new AsnReader(inputData, ruleSet);

            Assert.Equal(
                new DateTimeOffset(2050, 1, 2, 12, 34, 56, TimeSpan.Zero),
                reader.ReadUtcTime(TwoDigitYearMax, expectedTag: new Asn1Tag(TagClass.ContextSpecific, 5)));
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "170D3530303130323132333435365A", TagClass.Universal, 23)]
        [InlineData(AsnEncodingRules.CER, "170D3530303130323132333435365A", TagClass.Universal, 23)]
        [InlineData(AsnEncodingRules.DER, "170D3530303130323132333435365A", TagClass.Universal, 23)]
        [InlineData(AsnEncodingRules.BER, "800D3530303130323132333435365A", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.CER, "4C0D3530303130323132333435365A", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "DF8A460D3530303130323132333435365A", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            DateTimeOffset val1 = reader.ReadUtcTime(expectedTag:  new Asn1Tag(tagClass, tagValue, true));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            DateTimeOffset val2 = reader.ReadUtcTime(expectedTag: new Asn1Tag(tagClass, tagValue, false));

            Assert.False(reader.HasData);

            Assert.Equal(val1, val2);
        }
    }
}
