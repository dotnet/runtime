// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace System.Buffers.Text.Tests
{
    internal static partial class TestData
    {
        public static IEnumerable<object[]> TypesThatCanBeParsed
        {
            get
            {
                yield return new object[] { typeof(bool) };
                yield return new object[] { typeof(sbyte) };
                yield return new object[] { typeof(byte) };
                yield return new object[] { typeof(short) };
                yield return new object[] { typeof(ushort) };
                yield return new object[] { typeof(int) };
                yield return new object[] { typeof(uint) };
                yield return new object[] { typeof(long) };
                yield return new object[] { typeof(ulong) };
                yield return new object[] { typeof(decimal) };
                yield return new object[] { typeof(float) };
                yield return new object[] { typeof(double) };
                yield return new object[] { typeof(Guid) };
                yield return new object[] { typeof(DateTime) };
                yield return new object[] { typeof(DateTimeOffset) };
                yield return new object[] { typeof(TimeSpan) };
            }
        }

        public static IEnumerable<object[]> BooleanParserTheoryData => BooleanParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> SByteParserTheoryData => SByteParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> ByteParserTheoryData => ByteParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> Int16ParserTheoryData => Int16ParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> UInt16ParserTheoryData => UInt16ParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> Int32ParserTheoryData => Int32ParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> UInt32ParserTheoryData => UInt32ParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> Int64ParserTheoryData => Int64ParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> UInt64ParserTheoryData => UInt64ParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> DecimalParserTheoryData => DecimalParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> DoubleParserTheoryData => DoubleParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> SingleParserTheoryData => SingleParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> GuidParserTheoryData => GuidParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> DateTimeParserTheoryData => DateTimeParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> DateTimeOffsetParserTheoryData => DateTimeOffsetParserTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> TimeSpanParserTheoryData => TimeSpanParserTestData.Select(td => new object[] { td });

        private static IEnumerable<ParserTestData<T>> ToParserTheoryDataCollection<T>(this IEnumerable<FormatterTestData<T>> formatterTestData)
        {
            HashSet<ReversedFormatTestDataKey> seen = new HashSet<ReversedFormatTestDataKey>();
            foreach (FormatterTestData<T> testData in formatterTestData.Where(f => f.Format.IsParsingImplemented<T>() && f.Format.ParseSynonymFor == default))
            {
                // Formatters take precisions, Parsers do not. For many individual test cases, changing the precision doesn't change the formatted text output - 
                // if that's the case, there's no reason to test the same parse case twice.
                if (seen.Add(new ReversedFormatTestDataKey(testData.FormatSymbol, testData.ExpectedOutput)))
                    yield return testData.ToParserTestData();
            }
        }

        private struct ReversedFormatTestDataKey : IEquatable<ReversedFormatTestDataKey>
        {
            public ReversedFormatTestDataKey(char formatSymbol, string text)
            {
                FormatSymbol = formatSymbol;
                Text = text;
            }

            public override bool Equals(object obj) => obj is ReversedFormatTestDataKey other && Equals(other);
            public bool Equals(ReversedFormatTestDataKey other) => FormatSymbol == other.FormatSymbol && Text == other.Text;
            public override int GetHashCode() => FormatSymbol.GetHashCode() ^ Text.GetHashCode();

            public char FormatSymbol { get; }
            public string Text { get; }
        }

        private static IEnumerable<ParserTestData<T>> GeneratedParserTestDataUsingParseExact<T>(char formatSymbol, IEnumerable<string> texts, TryParseExactDelegate<T> tryParseExact)
        {
            foreach (string text in texts)
            {
                bool expectedSuccess = tryParseExact(text, formatSymbol.ToString(), CultureInfo.InvariantCulture, out T expectedResult);
                yield return new ParserTestData<T>(text, expectedSuccess ? expectedResult : default, formatSymbol, expectedSuccess);
            }
        }

        private delegate bool TryParseExactDelegate<T>(string text, string format, IFormatProvider formatProvider, out T result);
    }
}
