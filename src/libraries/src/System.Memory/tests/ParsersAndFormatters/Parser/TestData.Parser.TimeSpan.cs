// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace System.Buffers.Text.Tests
{
    internal static partial class TestData
    {
        public static IEnumerable<ParserTestData<TimeSpan>> TimeSpanParserTestData
        {
            get
            {
                foreach (ParserTestData<TimeSpan> testData in GeneratedParserTestDataUsingParseExact<TimeSpan>('G', TimeSpanParserBigGTestData.Concat(TimeSpanCombinatorialData), TimeSpan.TryParseExact))
                {
                    yield return testData;
                }

                foreach (ParserTestData<TimeSpan> testData in GeneratedParserTestDataUsingParseExact<TimeSpan>('g', TimeSpanParserLittleGTestData.Concat(TimeSpanCombinatorialData), TimeSpan.TryParseExact))
                {
                    yield return testData;
                }

                foreach (ParserTestData<TimeSpan> testData in GeneratedParserTestDataUsingParseExact<TimeSpan>('c', TimeSpanParserCTestData.Concat(TimeSpanCombinatorialData), TimeSpan.TryParseExact))
                {
                    yield return testData;
                }
            }
        }

        //
        // Generate sequences of numbers separated by various combinations of periods and colons. 
        //
        private static IEnumerable<string> TimeSpanCombinatorialData
        {
            get
            {
                for (int numComponents = 1; numComponents <= 6; numComponents++)
                {
                    for (int separatorMask = 0; separatorMask < (1 << (numComponents - 1)); separatorMask++)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < numComponents; i++)
                        {
                            sb.Append((20 + i).ToString());
                            if (i != numComponents - 1)
                            {
                                char separator = ((separatorMask & (1 << i)) != 0) ? '.' : ':';
                                sb.Append(separator);
                            }
                        }

                        yield return sb.ToString();
                    }
                }
            }
        }

        private static IEnumerable<string> TimeSpanParserBigGTestData
        {
            get
            {
                yield return "1:02:03:04.5678912";

                // Leading space (allowed)
                yield return " 1:02:03:04.5678912";
                yield return "  1:02:03:04.5678912";
                yield return " \t 1:02:03:04.5678912";

                // Sign (only '-' allowed at start)
                yield return "-1:02:03:04.5678912";
                yield return "+1:02:03:04.5678912";
                yield return "1:-02:03:04.5678912";
                yield return "1:+02:03:04.5678912";
                yield return "1:02:-03:04.5678912";
                yield return "1:02:+03:04.5678912";
                yield return "1:02:03:-04.5678912";
                yield return "1:02:03:+04.5678912";
                yield return "1:02:03:04.-5678912";
                yield return "1:02:03:04.+5678912";

                // Random bad stuff
                yield return "";
                yield return "-";

                yield return "10675199:02:48:05.4775807"; // TimeSpan.MaxValue
                yield return "10675199:02:48:05.4775808"; // TimeSpan.MaxValue + 1
                yield return "10675199:02:48:06.0000000"; // (next non-fractional overflow)

                yield return "-10675199:02:48:05.4775808"; // TimeSpan.MinValue
                yield return "-10675199:02:48:05.4775809"; // TimeSpan.MinValue - 1
                yield return "-10675199:02:48:06.0000000"; // (next non-fractional underflow)

                // Individual ranges
                yield return "0:0:0:0.0000000";
                yield return "0:23:0:0.0000000";
                yield return "0:24:0:0.0000000";
                yield return "0:0:59:0.0000000";
                yield return "0:0:60:0.0000000";
                yield return "0:0:0:59.0000000";
                yield return "0:0:0:60.0000000";
                yield return "0:0:0:0.9999999";
                yield return "0:0:0:0.10000000";

                yield return "-0:0:0:0.0000000";
                yield return "-0:23:0:0.0000000";
                yield return "-0:24:0:0.0000000";
                yield return "-0:0:59:0.0000000";
                yield return "-0:0:60:0.0000000";
                yield return "-0:0:0:59.0000000";
                yield return "-0:0:0:60.0000000";
                yield return "-0:0:0:0.9999999";
                yield return "-0:0:0:0.10000000";

                // Padding
                yield return "000001:0000002:000003:0000004.0000000";
                yield return "0:0:0:0."; // Not allowed
                yield return "0:0:0:0.1";
                yield return "0:0:0:0.12";
                yield return "0:0:0:0.123";
                yield return "0:0:0:0.1234";
                yield return "0:0:0:0.12345";
                yield return "0:0:0:0.123456";
                yield return "0:0:0:0.1234567";

                // Not allowed (picky, picky)
                yield return "0:0:0:0.12345670";

                // Missing components (none allowed: 'G' is strict)
                yield return "1";
                yield return "1:";
                yield return "1:2";
                yield return "1:2:";
                yield return "1:2:3";
                yield return "1:2:3:";
                yield return "1:2:3:4";
                yield return "1:2:3:4.";
            }
        }

        private static IEnumerable<string> TimeSpanParserLittleGTestData
        {
            get
            {
                // All BigG strings are valid LittleG Strings so reuse that data.
                foreach (string bigG in TimeSpanParserBigGTestData)
                {
                    yield return bigG;
                }

                yield return "1"; // 1-component -> day
                yield return "1.9999999"; // illegal
                yield return "4294967295"; // 1-component overflow
                yield return "1:2"; // 2-component -> hour:minutes
                yield return "1:2.9999999"; //illegal
                yield return "1:4294967295"; // 2-component overflow
                yield return "1:2:3"; // 3-component -> hour:minutes:seconds[.fraction]
                yield return "1:2:3.9999999"; // 3-component -> hour:minutes:seconds[.fraction]
                yield return "1:2:3.$$$$$$$"; //illegal
                yield return "1:2:4294967295"; // 3-component overflow
                yield return "1:2:4294967295.9999999"; // 4-component overflow
                yield return "1:2:3:4"; // 4-component -> day:hour:minutes:seconds[.fraction]
                yield return "1:2:3:4.9999999"; // 4-component -> day:hour:minutes:seconds[.fraction]
                yield return "1:2:3:4.$$$$$$$"; //illegal
                yield return "1:2:3:4294967295"; // 4-component overflow
                yield return "1:2:3:4294967295.9999999"; // 4-component overflow
                yield return "1:2:3:4:5"; // illegal - too many components
                yield return "1:2:3:4:5.9999999"; // illegal - too many components
                yield return "1:2:3:4.9999999:"; // intentionally flagged as error
                yield return "1:2:3:4.9999999."; // intentionally flagged as error 
            }
        }

        private static IEnumerable<string> TimeSpanParserCTestData
        {
            get
            {
                yield return "1";
                yield return "1.9999999";
                yield return "4294967295";
                yield return "1:2";
                yield return "1:2.9999999";
                yield return "1:4294967295";
                yield return "1:2:3";
                yield return "1.2:3";
                yield return "1.2:3:4";
                yield return "1:2:3.9999999";
                yield return "1:2:3.$$$$$$$";
                yield return "1:2:4294967295";
                yield return "1:2:4294967295.9999999";
                yield return "1:2:3:4";
                yield return "1:2:3:4.9999999";
                yield return "1:2:3:4.$$$$$$$";
                yield return "1:2:3:4294967295";
                yield return "1:2:3:4294967295.9999999";
                yield return "1.2:3:4.9999999";
                yield return "1.2:3:4.$$$$$$$";
                yield return "1.2:3:4294967295";
                yield return "1.2:3:4294967295.9999999";
                yield return "1.2:3:4:5";
                yield return "1.2:3:4:5.9999999";
                yield return "1.2:3:4.9999999:";
                yield return "1.2:3:4.9999999.";

                yield return "   \t  1"; // Leading whitespace is allowed

                yield return "-"; // Illegal
                yield return "+"; // Illegal

                yield return "-1"; // Legal
                yield return "+1"; // Illegal
            }
        }
    }
}
