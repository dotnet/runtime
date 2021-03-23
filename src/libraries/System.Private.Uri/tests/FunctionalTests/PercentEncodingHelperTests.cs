// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.PrivateUri.Tests
{
    public class PercentEncodingHelperTests
    {
        private const string OneByteUtf8 = "%41";           // A
        private const string TwoByteUtf8 = "%C3%BC";        // ü
        private const string ThreeByteUtf8 = "%E8%AF%B6";   // 诶
        private const string FourByteUtf8 = "%F0%9F%98%80"; // 😀

        private const string InvalidOneByteUtf8 = "%FF";
        private const string OverlongTwoByteUtf8 = "%C1%81";        // A
        private const string OverlongThreeByteUtf8 = "%E0%83%BC";   // ü
        private const string OverlongFourByteUtf8 = "%F0%88%AF%B6"; // 诶;

        public static IEnumerable<object[]> PercentEncodedAndDecodedUTF8Sequences()
        {
            static object[] Pair(string s1, string s2) => new object[] { s1, s2 };

            yield return Pair(OneByteUtf8, "A");
            yield return Pair(TwoByteUtf8, "ü");
            yield return Pair(ThreeByteUtf8, "诶");
            yield return Pair(FourByteUtf8, "😀");

            yield return Pair(OneByteUtf8 + OneByteUtf8, "AA");
            yield return Pair(TwoByteUtf8 + TwoByteUtf8, "üü");
            yield return Pair(ThreeByteUtf8 + ThreeByteUtf8, "诶诶");
            yield return Pair(FourByteUtf8 + FourByteUtf8, "😀😀");

            yield return Pair(OneByteUtf8 + TwoByteUtf8 + OneByteUtf8, "AüA");
            yield return Pair(TwoByteUtf8 + ThreeByteUtf8 + TwoByteUtf8, "ü诶ü");

            yield return Pair(InvalidOneByteUtf8 + OneByteUtf8, InvalidOneByteUtf8 + "A");
            yield return Pair(OverlongTwoByteUtf8 + TwoByteUtf8, OverlongTwoByteUtf8 + "ü");
            yield return Pair(OverlongThreeByteUtf8 + ThreeByteUtf8, OverlongThreeByteUtf8 + "诶");
            yield return Pair(OverlongFourByteUtf8 + FourByteUtf8, OverlongFourByteUtf8 + "😀");

            yield return Pair(InvalidOneByteUtf8, InvalidOneByteUtf8);
            yield return Pair(InvalidOneByteUtf8 + InvalidOneByteUtf8, InvalidOneByteUtf8 + InvalidOneByteUtf8);
            yield return Pair(InvalidOneByteUtf8 + InvalidOneByteUtf8 + InvalidOneByteUtf8, InvalidOneByteUtf8 + InvalidOneByteUtf8 + InvalidOneByteUtf8);

            // 11001010 11100100 10001000 10110010 - 2-byte marker followed by 3-byte sequence
            yield return Pair("%CA" + "%E4%88%B2", "%CA" + '䈲');

            // 4 valid UTF8 bytes followed by 5 invalid UTF8 bytes
            yield return Pair("%F4%80%80%BA" + "%FD%80%80%BA%CD", "\U0010003A" + "%FD%80%80%BA%CD");

            // BIDI char
            yield return Pair("%E2%80%8E", "\u200E");

            // Char Block: 3400..4DBF-CJK Unified Ideographs Extension A
            yield return Pair("%E4%88%B2", "\u4232");

            // BIDI char followed by a valid 3-byte UTF8 sequence (ク)
            yield return Pair("%E2%80%8E" + "%E3%82%AF", "\u200E" + "ク");

            // BIDI char followed by invalid UTF8 bytes
            yield return Pair("%E2%80%8E" + "%F0%90%90", "\u200E" + "%F0%90%90");

            // Input string:                %98%C8%D4%F3 %D4%A8 %7A %CF%DE %41 %16
            // Valid Unicode sequences:                  %D4%A8 %7A        %41 %16
            yield return Pair("%98%C8%D4%F3" + "%D4%A8" + "%7A" + "%CF%DE" + "%41" + "%16",
                "%98%C8%D4%F3" + 'Ԩ' + 'z' + "%CF%DE" + 'A' + '\x16');

            // 2-byte marker, valid 4-byte sequence, continuation byte
            yield return Pair("%C6" + "%F3%BC%A1%B8" + "%B5",
                "%C6" + "\U000FC878" + "%B5");
        }

        [Theory]
        [MemberData(nameof(PercentEncodedAndDecodedUTF8Sequences))]
        public static void UnescapeDataString_UnescapesUtf8Sequences(string stringToUnescape, string expected)
        {
            Assert.Equal(expected, Uri.UnescapeDataString(stringToUnescape));
        }
    }
}
