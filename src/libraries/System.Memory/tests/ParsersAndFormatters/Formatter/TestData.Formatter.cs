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
        public static IEnumerable<object[]> TypesThatCanBeFormatted
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

        public static IEnumerable<object[]> BooleanFormatterTheoryData => BooleanFormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> SByteFormatterTheoryData => SByteFormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> ByteFormatterTheoryData => ByteFormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> Int16FormatterTheoryData => Int16FormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> UInt16FormatterTheoryData => UInt16FormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> Int32FormatterTheoryData => Int32FormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> UInt32FormatterTheoryData => UInt32FormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> Int64FormatterTheoryData => Int64FormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> UInt64FormatterTheoryData => UInt64FormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> DecimalFormatterTheoryData => DecimalFormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> DoubleFormatterTheoryData => DoubleFormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> SingleFormatterTheoryData => SingleFormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> GuidFormatterTheoryData => GuidFormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> DateTimeFormatterTheoryData => DateTimeFormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> DateTimeOffsetFormatterTheoryData => DateTimeOffsetFormatterTestData.Select(td => new object[] { td });
        public static IEnumerable<object[]> TimeSpanFormatterTheoryData => TimeSpanFormatterTestData.Select(td => new object[] { td });

        public static IEnumerable<FormatterTestData<bool>> BooleanFormatterTestData => CreateFormatterTestData(BooleanTestData, BooleanFormats);
        public static IEnumerable<FormatterTestData<sbyte>> SByteFormatterTestData => CreateFormatterTestData(SByteTestData, IntegerFormats);
        public static IEnumerable<FormatterTestData<byte>> ByteFormatterTestData => CreateFormatterTestData(ByteTestData, IntegerFormats);
        public static IEnumerable<FormatterTestData<short>> Int16FormatterTestData => CreateFormatterTestData(Int16TestData, IntegerFormats);
        public static IEnumerable<FormatterTestData<ushort>> UInt16FormatterTestData => CreateFormatterTestData(UInt16TestData, IntegerFormats);
        public static IEnumerable<FormatterTestData<int>> Int32FormatterTestData => CreateFormatterTestData(Int32TestData, IntegerFormats);
        public static IEnumerable<FormatterTestData<uint>> UInt32FormatterTestData => CreateFormatterTestData(UInt32TestData, IntegerFormats);
        public static IEnumerable<FormatterTestData<long>> Int64FormatterTestData => CreateFormatterTestData(Int64TestData, IntegerFormats);
        public static IEnumerable<FormatterTestData<ulong>> UInt64FormatterTestData => CreateFormatterTestData(UInt64TestData, IntegerFormats);
        public static IEnumerable<FormatterTestData<decimal>> DecimalFormatterTestData => CreateFormatterTestData(DecimalTestData, DecimalFormats);
        public static IEnumerable<FormatterTestData<double>> DoubleFormatterTestData => CreateFormatterTestData(DoubleTestData, FloatingPointFormats);
        public static IEnumerable<FormatterTestData<float>> SingleFormatterTestData => CreateFormatterTestData(SingleTestData, FloatingPointFormats);
        public static IEnumerable<FormatterTestData<Guid>> GuidFormatterTestData => CreateFormatterTestData(GuidTestData, GuidFormats);
        public static IEnumerable<FormatterTestData<DateTime>> DateTimeFormatterTestData => CreateFormatterTestData(DateTimeTestData, DateTimeFormats);
        public static IEnumerable<FormatterTestData<DateTimeOffset>> DateTimeOffsetFormatterTestData => CreateFormatterTestData(DateTimeOffsetTestData, DateTimeOffsetFormats);
        public static IEnumerable<FormatterTestData<TimeSpan>> TimeSpanFormatterTestData => CreateFormatterTestData(TimeSpanTestData, TimeSpanFormats);

        private static IEnumerable<FormatterTestData<T>> CreateFormatterTestData<T>(IEnumerable<T> values, IEnumerable<SupportedFormat> formats)
        {
            foreach (T value in values)
            {
                foreach (SupportedFormat format in formats)
                {
                    if (format.FormatSynonymFor != default)
                        continue;

                    if (format.IsDefault)
                    {
                        string expectedOutput = ComputeExpectedOutput<T>(value, format.Symbol, StandardFormat.NoPrecision);
                        yield return new FormatterTestData<T>(value, new SupportedFormat(default, format.SupportsPrecision), default, expectedOutput);
                    }

                    if (!format.NoRepresentation)
                    {
                        if (!format.SupportsPrecision)
                        {
                            string expectedOutput = ComputeExpectedOutput<T>(value, format.Symbol, StandardFormat.NoPrecision);
                            yield return new FormatterTestData<T>(value, format, StandardFormat.NoPrecision, expectedOutput);
                        }
                        else
                        {
                            foreach (byte precision in TestData.s_precisions)
                            {
                                string expectedOutput = ComputeExpectedOutput<T>(value, format.Symbol, precision);
                                yield return new FormatterTestData<T>(value, format, precision, expectedOutput);
                            }
                        }
                    }
                }
            }
        }

        private static string ComputeExpectedOutput<T>(T value, char formatSymbol, int precision)
        {
            if (value is DateTime dt && formatSymbol == 'l')
            {
                return dt.ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant();
            }
            else if (value is DateTimeOffset dto && formatSymbol == 'l')
            {
                return dto.ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant();
            }
            else if (value is DateTimeOffset dto2 && formatSymbol == default(char))
            {
                return dto2.ToString(CultureInfo.InvariantCulture);
            }
            else if (value is IFormattable formattable)
            {
                string format = formatSymbol + (precision == StandardFormat.NoPrecision ? string.Empty : precision.ToString());
                return formattable.ToString(format, CultureInfo.InvariantCulture);
            }
            else if (value is bool b)
            {
                switch (formatSymbol)
                {
                    case 'G':
                        return b ? "True" : "False";
                    case 'l':
                        return b ? "true" : "false";
                    default:
                        throw new Exception("Unsupported bool format: " + formatSymbol);
                }
            }
            else
            {
                throw new Exception("Unsupported type: " + typeof(T));
            }
        }
    }
}
