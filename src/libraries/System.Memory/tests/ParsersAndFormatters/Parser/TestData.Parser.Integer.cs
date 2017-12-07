// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace System.Buffers.Text.Tests
{
    internal static partial class TestData
    {
        public static IEnumerable<ParserTestData<sbyte>> SByteParserTestData
        {
            get
            {
                foreach (ParserTestData<sbyte> testData in SByteFormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                foreach (ParserTestData<sbyte> testData in GeneralIntegerParserTestData<sbyte>())
                {
                    yield return testData;
                }

                // Code coverage
                yield return new ParserTestData<sbyte>("5$", 5, 'D', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<sbyte>("5$", 5, 'x', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<sbyte>("5ff", 0, 'x', expectedSuccess: false);
            }
        }

        public static IEnumerable<ParserTestData<byte>> ByteParserTestData
        {
            get
            {
                foreach (ParserTestData<byte> testData in ByteFormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                foreach (ParserTestData<byte> testData in GeneralIntegerParserTestData<byte>())
                {
                    yield return testData;
                }

                // Code coverage
                yield return new ParserTestData<byte>("5$", 5, 'D', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<byte>("5$", 5, 'x', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<byte>("5ff", 0, 'x', expectedSuccess: false);
            }
        }

        public static IEnumerable<ParserTestData<short>> Int16ParserTestData
        {
            get
            {
                foreach (ParserTestData<short> testData in Int16FormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                foreach (ParserTestData<short> testData in GeneralIntegerParserTestData<short>())
                {
                    yield return testData;
                }

                // Code coverage
                yield return new ParserTestData<short>("5$", 5, 'D', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<short>("5$", 5, 'x', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<short>("5faaf", 0, 'x', expectedSuccess: false);
            }
        }

        public static IEnumerable<ParserTestData<ushort>> UInt16ParserTestData
        {
            get
            {
                foreach (ParserTestData<ushort> testData in UInt16FormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                foreach (ParserTestData<ushort> testData in GeneralIntegerParserTestData<ushort>())
                {
                    yield return testData;
                }

                // Code coverage
                yield return new ParserTestData<ushort>("5$", 5, 'D', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<ushort>("5$", 5, 'x', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<ushort>("5faaf", 0, 'x', expectedSuccess: false);
            }
        }

        public static IEnumerable<ParserTestData<int>> Int32ParserTestData
        {
            get
            {
                foreach (ParserTestData<int> testData in Int32FormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                foreach (ParserTestData<int> testData in GeneralIntegerParserTestData<int>())
                {
                    yield return testData;
                }

                // Code coverage
                yield return new ParserTestData<int>("5$", 5, 'x', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<int>("5faaccbbf", 0, 'x', expectedSuccess: false);

                // This value will overflow a UInt32 accumulator upon assimilating the 0 in a way such that the wrapped-around value still looks like
                // it's in the range of an Int32. Unless, of course, the implementation had the foresight to check before assimilating. 
                yield return new ParserTestData<int>("9999999990", 0, 'D', expectedSuccess: false);
            }
        }

        public static IEnumerable<ParserTestData<uint>> UInt32ParserTestData
        {
            get
            {
                foreach (ParserTestData<uint> testData in UInt32FormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                foreach (ParserTestData<uint> testData in GeneralIntegerParserTestData<uint>())
                {
                    yield return testData;
                }

                // Code coverage
                yield return new ParserTestData<uint>("5$", 5, 'D', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<uint>("5$", 5, 'x', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<uint>("5faaccbbf", 0, 'x', expectedSuccess: false);
            }
        }

        public static IEnumerable<ParserTestData<long>> Int64ParserTestData
        {
            get
            {
                foreach (ParserTestData<long> testData in Int64FormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                foreach (ParserTestData<long> testData in GeneralIntegerParserTestData<long>())
                {
                    yield return testData;
                }

                // Code coverage
                yield return new ParserTestData<long>("5$", 5, 'D', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<long>("5$", 5, 'x', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<long>("5faaccbb11223344f", 0, 'x', expectedSuccess: false);
            }
        }

        public static IEnumerable<ParserTestData<ulong>> UInt64ParserTestData
        {
            get
            {
                foreach (ParserTestData<ulong> testData in UInt64FormatterTestData.ToParserTheoryDataCollection())
                {
                    yield return testData;
                }

                foreach (ParserTestData<ulong> testData in GeneralIntegerParserTestData<ulong>())
                {
                    yield return testData;
                }

                // Code coverage
                yield return new ParserTestData<ulong>("5$", 5, 'D', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<ulong>("5$", 5, 'x', expectedSuccess: true) { ExpectedBytesConsumed = 1 };
                yield return new ParserTestData<ulong>("5faaccbb11223344f", 0, 'x', expectedSuccess: false);
            }
        }

        private static IEnumerable<ParserTestData<T>> GeneralIntegerParserTestData<T>()
        {
            string[] GeneralIntegerNegativeInputs =
            {
                string.Empty,
                "-",
                "+",
                "--5",
                "++5",
            };

            BigInteger minValue = TestUtils.GetMinValue<T>();
            BigInteger maxValue = TestUtils.GetMaxValue<T>();
            bool isSigned = !minValue.IsZero;

            foreach (SupportedFormat format in IntegerFormats.Where(f => f.IsParsingImplemented<T>() && f.ParseSynonymFor == default))
            {
                foreach (string integerNegativeInput in GeneralIntegerNegativeInputs)
                {
                    yield return new ParserTestData<T>(integerNegativeInput, default, format.Symbol, expectedSuccess: false);
                }

                // The hex format always parses as an unsigned number. That violates the assumptions made by this next set of test data.
                // Since the parser uses the same code for hex-parsing signed and unsigned, just generate the data for the unsigned types only with
                // no loss in code coverage.
                if (!((format.Symbol == 'X' || format.Symbol == 'x') && isSigned))
                {
                    // Make sure that MaxValue and values just below it parse successfully.
                    for (int offset = -20; offset <= 0; offset++)
                    {
                        BigInteger bigValue = maxValue + offset;
                        string textD = bigValue.ToString("D");
                        string text = bigValue.ToString(format.Symbol.ToString());
                        T expectedValue = (T)(Convert.ChangeType(textD, typeof(T)));
                        yield return new ParserTestData<T>(text, expectedValue, format.Symbol, expectedSuccess: true);
                    }

                    // Make sure that values just above MaxValue don't parse successfully.
                    for (int offset = 1; offset <= 20; offset++)
                    {
                        BigInteger bigValue = maxValue + offset;
                        string text = bigValue.ToString(format.Symbol.ToString());
                        yield return new ParserTestData<T>(text, default, format.Symbol, expectedSuccess: false);
                    }

                    {
                        BigInteger bigValue = maxValue * 10;
                        string text = bigValue.ToString(format.Symbol.ToString());
                        yield return new ParserTestData<T>(text, default, format.Symbol, expectedSuccess: false);
                    }

                    if (isSigned) // No such thing as an underflow for unsigned integer parsing...
                    {
                        // Make sure that MinValue and values just above it parse successfully.
                        for (int offset = 0; offset <= 20; offset++)
                        {
                            BigInteger bigValue = minValue + offset;
                            string textD = bigValue.ToString("D");
                            string text = bigValue.ToString(format.Symbol.ToString());
                            T expectedValue = (T)(Convert.ChangeType(textD, typeof(T)));
                            yield return new ParserTestData<T>(text, expectedValue, format.Symbol, expectedSuccess: true);
                        }

                        // Make sure that values just below MinValue don't parse successfully.
                        for (int offset = -20; offset <= -1; offset++)
                        {
                            BigInteger bigValue = minValue + offset;
                            string text = bigValue.ToString(format.Symbol.ToString());
                            yield return new ParserTestData<T>(text, default, format.Symbol, expectedSuccess: false);
                        }

                        {
                            BigInteger bigValue = minValue * 10;
                            string text = bigValue.ToString(format.Symbol.ToString());
                            yield return new ParserTestData<T>(text, default, format.Symbol, expectedSuccess: false);
                        }
                    }
                }
            }
        }
    }
}
