// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Buffers.Text.Tests
{
    public static partial class FormatterTests
    {
        [Theory]
        [MemberData(nameof(TestData.BooleanFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterBoolean(FormatterTestData<bool> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.SByteFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterSByte(FormatterTestData<sbyte> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.ByteFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterByte(FormatterTestData<byte> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.Int16FormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterInt16(FormatterTestData<short> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.UInt16FormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterUInt16(FormatterTestData<ushort> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.Int32FormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterInt32(FormatterTestData<int> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.UInt32FormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterUInt32(FormatterTestData<uint> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.Int64FormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterInt64(FormatterTestData<long> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.UInt64FormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterUInt64(FormatterTestData<ulong> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.DecimalFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterDecimal(FormatterTestData<decimal> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.DoubleFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterDouble(FormatterTestData<double> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.SingleFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterSingle(FormatterTestData<float> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.GuidFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterGuid(FormatterTestData<Guid> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.DateTimeFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterDateTime(FormatterTestData<DateTime> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.DateTimeOffsetFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterDateTimeOffset(FormatterTestData<DateTimeOffset> testData)
        {
            ValidateFormatter(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.TimeSpanFormatterTheoryData), MemberType = typeof(TestData))]
        public static void TestFormatterTimeSpan(FormatterTestData<TimeSpan> testData)
        {
            ValidateFormatter(testData);
        }
    }
}

