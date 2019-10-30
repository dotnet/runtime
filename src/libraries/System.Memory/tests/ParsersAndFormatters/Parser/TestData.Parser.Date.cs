// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Buffers.Text.Tests
{
    internal static partial class TestData
    {
        public static IEnumerable<ParserTestData<DateTime>> DateTimeParserTestData
        {
            get
            {
                foreach (ParserTestData<DateTime> testData in DateTimeFormatterTestData.ToParserTheoryDataCollection())
                {
                    bool roundTrippable = testData.FormatSymbol == 'O';
                    if (roundTrippable)
                        yield return testData;
                }

                foreach (ParserTestData<DateTimeOffset> testData in DateTimeOffsetParserTestData)
                {
                    char formatSymbol = testData.FormatSymbol;
                    if (testData.ExpectedSuccess)
                    {
                        if (formatSymbol == 'R' || formatSymbol == 'l')
                        {
                            DateTime expectedDateTime = testData.ExpectedValue.UtcDateTime;
                            // TryParse returns Unspecified to match DateTime.ParseExact() though it seems Utc would be more apropos.
                            expectedDateTime = new DateTime(expectedDateTime.Ticks, DateTimeKind.Unspecified);
                            yield return new ParserTestData<DateTime>(testData.Text, expectedDateTime, testData.FormatSymbol, expectedSuccess: true) { ExpectedBytesConsumed = testData.ExpectedBytesConsumed };
                        }
                        if (formatSymbol == 'G')
                        {
                            yield return new ParserTestData<DateTime>(testData.Text, testData.ExpectedValue.DateTime, testData.FormatSymbol, expectedSuccess: true) { ExpectedBytesConsumed = testData.ExpectedBytesConsumed };
                        }
                    }
                    else
                    {
                        if (formatSymbol != default(char))
                        {
                            yield return new ParserTestData<DateTime>(testData.Text, default, testData.FormatSymbol, expectedSuccess: false);
                        }
                    }
                }
            }
        }

        public static IEnumerable<ParserTestData<DateTimeOffset>> DateTimeOffsetParserTestData
        {
            get
            {
                // Wrong day of week.
                yield return new ParserTestData<DateTimeOffset>("Thu, 13 Jan 2017 03:45:32 GMT", default, 'R', expectedSuccess: false);

                foreach (ParserTestData<DateTimeOffset> bad in GenerateCorruptedDateTimeText("05/08/2017 10:30:45 +00:00", default))
                {
                    yield return bad;
                }

                foreach (ParserTestData<DateTimeOffset> bad in GenerateCorruptedDateTimeText("05/08/2017 10:30:45", 'G'))
                {
                    yield return bad;
                }

                foreach (ParserTestData<DateTimeOffset> bad in GenerateCorruptedDateTimeText("Tue, 03 Jan 2017 08:08:05 GMT", 'R'))
                {
                    yield return bad;
                }

                foreach (ParserTestData<DateTimeOffset> bad in GenerateCorruptedDateTimeText("tue, 03 jan 2017 08:08:05 gmt", 'l'))
                {
                    yield return bad;
                }

                foreach (ParserTestData<DateTimeOffset> bad in GenerateCorruptedDateTimeText("2017-01-12T10:30:45.7680000-08:00", 'O'))
                {
                    yield return bad;
                }

                foreach (ParserTestData<DateTimeOffset> testData in DateTimeOffsetFormatterTestData.ToParserTheoryDataCollection())
                {
                    bool roundTrippable = testData.FormatSymbol == 'O';
                    if (roundTrippable)
                        yield return testData;
                }

                foreach (PseudoDateTime pseudoDateTime in PseudoDateTimeTestData)
                {
                    DateTimeOffset expectedDto;
                    if (pseudoDateTime.ExpectSuccess)
                    {
                        TimeSpan offset = new TimeSpan(hours: pseudoDateTime.OffsetHours, minutes: pseudoDateTime.OffsetMinutes, seconds: 0);
                        if (pseudoDateTime.OffsetNegative)
                        {
                            offset = -offset;
                        }
                        DateTimeOffset dto = new DateTimeOffset(year: pseudoDateTime.Year, month: pseudoDateTime.Month, day: pseudoDateTime.Day, hour: pseudoDateTime.Hour, minute: pseudoDateTime.Minute, second: pseudoDateTime.Second, offset: offset);
                        if (pseudoDateTime.Fraction != 0)
                        {
                            dto += new TimeSpan(ticks: pseudoDateTime.Fraction);
                        }
                        expectedDto = dto;
                    }
                    else
                    {
                        expectedDto = default;
                    }

                    DateTimeOffset expectedDtoConvertedToLocal;
                    if (pseudoDateTime.ExpectSuccess)
                    {
                        try
                        {
                            expectedDtoConvertedToLocal = new DateTimeOffset(expectedDto.DateTime);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            throw new Exception($"Failed on converting {expectedDto.DateTime} to local time. This is probably a piece of data that fails only in certain time zones. Time zone on this machine is {TimeZoneInfo.Local}");
                        }
                    }
                    else
                    {
                        expectedDtoConvertedToLocal = default;
                    }

                    string text;
                    if ((text = pseudoDateTime.DefaultString) != null)
                    {
                        yield return new ParserTestData<DateTimeOffset>(text, expectedDto, default, pseudoDateTime.ExpectSuccess);
                    }

                    if ((text = pseudoDateTime.GFormatString) != null)
                    {
                        yield return new ParserTestData<DateTimeOffset>(text, expectedDtoConvertedToLocal, 'G', pseudoDateTime.ExpectSuccess);
                    }

                    if ((text = pseudoDateTime.RFormatString) != null)
                    {
                        yield return new ParserTestData<DateTimeOffset>(text, expectedDto, 'R', pseudoDateTime.ExpectSuccess);
                    }

                    if ((text = pseudoDateTime.LFormatString) != null)
                    {
                        yield return new ParserTestData<DateTimeOffset>(text, expectedDto, 'l', pseudoDateTime.ExpectSuccess);
                    }

                    if ((text = pseudoDateTime.OFormatStringZ) != null)
                    {
                        yield return new ParserTestData<DateTimeOffset>(text, expectedDto, 'O', pseudoDateTime.ExpectSuccess);
                    }

                    if ((text = pseudoDateTime.OFormatStringNoOffset) != null)
                    {
                        yield return new ParserTestData<DateTimeOffset>(text, expectedDtoConvertedToLocal, 'O', pseudoDateTime.ExpectSuccess);
                    }

                    if ((text = pseudoDateTime.OFormatStringOffset) != null)
                    {
                        yield return new ParserTestData<DateTimeOffset>(text, expectedDto, 'O', pseudoDateTime.ExpectSuccess);
                    }
                }
            }
        }

        private static IEnumerable<ParserTestData<DateTimeOffset>> GenerateCorruptedDateTimeText(string goodText, char formatSymbol)
        {
            // Corrupt a single character
            {
                for (int i = 0; i < goodText.Length; i++)
                {
                    char[] bad = (char[])(goodText.ToCharArray().Clone());
                    if (char.IsDigit(goodText[i]))
                    {
                        bad[i] = (char)('0' - 1);
                        yield return new ParserTestData<DateTimeOffset>(new string(bad), default, formatSymbol, expectedSuccess: false);
                        bad[i] = (char)('9' + 1);
                        yield return new ParserTestData<DateTimeOffset>(new string(bad), default, formatSymbol, expectedSuccess: false);
                    }
                    else if (!(formatSymbol == 'O' && i == 27)) // Corrupting the "-" separating the offset doesn't yield a "bad" string - it yields a non-offset string followed by a "!"
                    {
                        bad[i] = '!';
                        yield return new ParserTestData<DateTimeOffset>(new string(bad), default, formatSymbol, expectedSuccess: false);
                    }
                }
            }

            // Too short
            {
                for (int truncatedLength = 1; truncatedLength < goodText.Length - 1; truncatedLength++)
                {
                    if (!(formatSymbol == 'O' && truncatedLength == 27)) // Chopping off the offset entirely leaves you with a good string...
                    {
                        yield return new ParserTestData<DateTimeOffset>(goodText.Substring(0, truncatedLength), default, formatSymbol, expectedSuccess: false);
                    }
                }
            }

            // 'R' and 'l' are case-sensitive. Flip the case of each letter
            if (formatSymbol == 'R' || formatSymbol == 'l')
            {
                for (int i = 0; i < goodText.Length; i++)
                {
                    char[] bad = (char[])(goodText.ToCharArray().Clone());
                    if (char.IsLetter(goodText[i]))
                    {
                        bad[i] = (char)(goodText[i] ^ 0x20u);
                        yield return new ParserTestData<DateTimeOffset>(new string(bad), default, formatSymbol, expectedSuccess: false);
                    }
                }
            }
        }
    }
}
