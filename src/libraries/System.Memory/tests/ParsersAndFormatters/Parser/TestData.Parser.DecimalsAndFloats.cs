// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace System.Buffers.Text.Tests
{
    internal static partial class TestData
    {
        public static IEnumerable<ParserTestData<decimal>> DecimalParserTestData
        {
            get
            {
                foreach (FormatterTestData<decimal> ftd in DecimalFormatterTestData)
                {
                    // Not all FormatTestDatas for Decimal are actually roundtrippable.
                    if (ftd.FormatSymbol != default(char))
                        continue;

                    decimal d = ftd.Value;
                    if (d == 0 && decimal.IsNegative(d))
                        continue; // -0 is not roundtrippable

                    foreach (ParserTestData<decimal> testData in new FormatterTestData<decimal>[] { ftd }.ToParserTheoryDataCollection())
                    {
                        yield return testData;
                    }
                }

                foreach (ParserTestData<decimal> testData in GenerateNumberBasedParserTestData<decimal>(DecimalFormats, decimal.TryParse))
                {
                    yield return testData;
                }

                yield return new ParserTestData<decimal>("1e" + int.MaxValue, default, 'E', expectedSuccess: false);
                yield return new ParserTestData<decimal>("0.01e" + int.MinValue, new decimal(0, 0, 0, isNegative: false, scale: 28), 'E', expectedSuccess: true);
                yield return new ParserTestData<decimal>("-0.01e" + int.MinValue, new decimal(0, 0, 0, isNegative: true, scale: 28), 'E', expectedSuccess: true);
            }
        }

        public static IEnumerable<ParserTestData<double>> DoubleParserTestData
        {
            get
            {
                foreach (ParserTestData<double> testData in GenerateNumberBasedParserTestData<double>(FloatingPointFormats, double.TryParse))
                {
                    yield return testData;
                }

                foreach (char formatSymbol in DecimalFormats.Select(f => f.Symbol))
                {
                    yield return new ParserTestData<double>("Infinity", double.PositiveInfinity, formatSymbol, expectedSuccess: true);
                    yield return new ParserTestData<double>("-Infinity", double.NegativeInfinity, formatSymbol, expectedSuccess: true);
                    yield return new ParserTestData<double>("NaN", double.NaN, formatSymbol, expectedSuccess: true);
                }
            }
        }

        public static IEnumerable<ParserTestData<float>> SingleParserTestData
        {
            get
            {
                foreach (ParserTestData<float> testData in GenerateNumberBasedParserTestData<float>(FloatingPointFormats, float.TryParse))
                {
                    yield return testData;
                }

                foreach (char formatSymbol in DecimalFormats.Select(f => f.Symbol))
                {
                    yield return new ParserTestData<float>("Infinity", float.PositiveInfinity, formatSymbol, expectedSuccess: true);
                    yield return new ParserTestData<float>("-Infinity", float.NegativeInfinity, formatSymbol, expectedSuccess: true);
                    yield return new ParserTestData<float>("NaN", float.NaN, formatSymbol, expectedSuccess: true);
                }
            }
        }

        private static IEnumerable<ParserTestData<T>> GenerateNumberBasedParserTestData<T>(IEnumerable<SupportedFormat> formats, TryParseDelegate<T> tryParse)
        {
            foreach (char formatSymbol in formats.Select(f => f.Symbol))
            {
                foreach (ParserTestData<T> testData in GeneratedParserTestDataUsingParseExact<T>(formatSymbol, NumberTestData,
                        (string text, string format, IFormatProvider formatProvider, out T result) =>
                        {
                            NumberStyles style = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
                            if (formatSymbol != 'F' && formatSymbol != 'f')
                            {
                                style |= NumberStyles.AllowExponent;
                            }
                            if ((formatSymbol == 'E' || formatSymbol == 'e') && text.IndexOf("E", StringComparison.InvariantCultureIgnoreCase) == -1)
                            {
                                result = default;
                                return false;
                            }
                            else
                            {
                                return tryParse(text, style, formatProvider, out result);
                            }
                        }))
                {
                    yield return testData;
                }
            }
        }

        private delegate bool TryParseDelegate<T>(string text, NumberStyles style, IFormatProvider formatProvider, out T result);
    }
}
