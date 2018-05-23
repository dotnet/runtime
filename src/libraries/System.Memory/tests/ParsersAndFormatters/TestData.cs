// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace System.Buffers.Text.Tests
{
    //
    // General purpose raw test data.
    //
    internal static partial class TestData
    {
        public static readonly IEnumerable<byte> s_precisions = new byte[] { StandardFormat.NoPrecision, 0, 1, 3, 10, StandardFormat.MaxPrecision };

        public static IEnumerable<object[]> IntegerTypesTheoryData => IntegerTypes.Select(t => new object[] { t });

        public static IEnumerable<Type> IntegerTypes
        {
            get
            {
                yield return typeof(sbyte);
                yield return typeof(byte);
                yield return typeof(short);
                yield return typeof(ushort);
                yield return typeof(int);
                yield return typeof(uint);
                yield return typeof(long);
                yield return typeof(ulong);
            }
        }

        public static IEnumerable<bool> BooleanTestData
        {
            get
            {
                yield return true;
                yield return false;
            }
        }

        public static IEnumerable<sbyte> SByteTestData
        {
            get
            {
                foreach (long l in Int64TestData)
                {
                    if (l >= sbyte.MinValue && l <= sbyte.MaxValue)
                    {
                        yield return (sbyte)l;
                    }
                }
            }
        }

        public static IEnumerable<byte> ByteTestData
        {
            get
            {
                foreach (long l in Int64TestData)
                {
                    if (l >= byte.MinValue && l <= byte.MaxValue)
                    {
                        yield return (byte)l;
                    }
                }
            }
        }

        public static IEnumerable<short> Int16TestData
        {
            get
            {
                foreach (long l in Int64TestData)
                {
                    if (l >= short.MinValue && l <= short.MaxValue)
                    {
                        yield return (short)l;
                    }
                }
            }
        }

        public static IEnumerable<ushort> UInt16TestData
        {
            get
            {
                foreach (long l in Int64TestData)
                {
                    if (l >= ushort.MinValue && l <= ushort.MaxValue)
                    {
                        yield return (ushort)l;
                    }
                }
            }
        }

        public static IEnumerable<int> Int32TestData
        {
            get
            {
                foreach (long l in Int64TestData)
                {
                    if (l >= int.MinValue && l <= int.MaxValue)
                    {
                        yield return (int)l;
                    }
                }
            }
        }

        public static IEnumerable<uint> UInt32TestData
        {
            get
            {
                foreach (long l in Int64TestData)
                {
                    if (l >= uint.MinValue && l <= uint.MaxValue)
                    {
                        yield return (uint)l;
                    }
                }
            }
        }

        public static IEnumerable<long> Int64TestData
        {
            get
            {
                yield return 0L;
                yield return 1L;
                yield return 123L;
                yield return -123L;
                yield return 1234L;
                yield return -1234L;
                yield return 12345L;
                yield return -12345L;

                yield return 4294967294999999999L; // uint.MaxValue * Billion - 1
                yield return 4294967295000000000L; // uint.MaxValue * Billion
                yield return 4294967295000000001L; // uint.MaxValue * Billion + 1
                yield return -4294967294999999999L; // -(uint.MaxValue * Billion - 1)
                yield return -4294967295000000000L; // -(uint.MaxValue * Billion)
                yield return -4294967295000000001L; // -(uint.MaxValue * Billion + 1)
                yield return 4294967296000000000L; // (uint.MaxValue + 1) * Billion
                yield return -4294967296000000000L; // -(uint.MaxValue + 1) * Billion

                long powerOf10 = 1L;
                for (int i = 0; i < 21; i++)
                {
                    powerOf10 *= 10L;
                    yield return powerOf10 - 1;
                    yield return powerOf10;
                    yield return -(powerOf10 - 1);
                    yield return -powerOf10;
                }

                yield return sbyte.MinValue;
                yield return sbyte.MinValue - 1;
                yield return sbyte.MinValue + 1;

                yield return sbyte.MaxValue;
                yield return sbyte.MaxValue - 1;
                yield return sbyte.MaxValue + 1;

                yield return short.MinValue;
                yield return short.MinValue - 1;
                yield return short.MinValue + 1;

                yield return short.MaxValue;
                yield return short.MaxValue - 1;
                yield return short.MaxValue + 1;

                yield return int.MinValue;
                yield return ((long)int.MinValue) - 1;
                yield return int.MinValue + 1;

                yield return int.MaxValue;
                yield return int.MaxValue - 1;
                yield return ((long)int.MaxValue) + 1;

                yield return long.MinValue;
                yield return long.MinValue + 1;

                yield return long.MaxValue;
                yield return long.MaxValue - 1;

                yield return byte.MaxValue;
                yield return byte.MaxValue - 1;

                yield return ushort.MaxValue;
                yield return ushort.MaxValue - 1;

                yield return uint.MaxValue;
                yield return uint.MaxValue - 1;
            }
        }

        public static IEnumerable<ulong> UInt64TestData
        {
            get
            {
                foreach (long l in Int64TestData)
                {
                    if (l >= 0)
                    {
                        yield return (ulong)l;
                    }
                }

                yield return long.MaxValue + 1LU;
                yield return ulong.MaxValue - 1LU;
                yield return ulong.MaxValue;
            }
        }

        public static IEnumerable<decimal> DecimalTestData
        {
            get
            {
                foreach (long l in Int64TestData)
                {
                    yield return l;
                }

                yield return decimal.MinValue;
                yield return decimal.MaxValue;

                // negative 0m. The formatter is expected *not* to emit a minus sign in this case.
                yield return (new MutableDecimal() { High = 0, Mid = 0, Low = 0, IsNegative = true }).ToDecimal();

                yield return 0.304m; // Round down
                yield return -0.304m;
                yield return 0.305m; // Round up
                yield return -0.305m;
                yield return 999.99m;
                yield return -999.99m;
                yield return 0.000123456m;
                yield return -0.000123456m;

                // Explicit trailing 0's (Decimal can and does preserve these by setting the Scale appropriately)
                yield return 1.00m;
                yield return 0.00m;
                yield return -1.00m;
                yield return -0.00m;
            }
        }

        public static IEnumerable<double> DoubleTestData
        {
            get
            {
                foreach (long l in Int64TestData)
                {
                    yield return l;
                }

                yield return 1.23;
            }
        }

        public static IEnumerable<float> SingleTestData
        {
            get
            {
                foreach (long d in DoubleTestData)
                {
                    float f = d;
                    if (!float.IsInfinity(f))
                        yield return f;
                }
            }
        }

        public static IEnumerable<Guid> GuidTestData
        {
            get
            {
                yield return new Guid("CB0AFB61-6F04-401A-BBEA-C0FC0B6E4E51");
                yield return new Guid("FC1911F9-9EED-4CA8-AC8B-CEEE1EBE2C72");
            }
        }

        public static IEnumerable<DateTime> DateTimeTestData
        {
            get
            {
                {
                    // Kind == Unspecified
                    TimeSpan offset = new TimeSpan(hours: 8, minutes: 0, seconds: 0);
                    DateTimeOffset dto = new DateTimeOffset(year: 2017, month: 1, day: 13, hour: 3, minute: 45, second: 32, offset: offset);
                    yield return dto.DateTime;
                }

                {
                    // Kind == Utc
                    TimeSpan offset = new TimeSpan(hours: 8, minutes: 0, seconds: 0);
                    DateTimeOffset dto = new DateTimeOffset(year: 2017, month: 1, day: 13, hour: 3, minute: 45, second: 32, offset: offset);
                    yield return dto.UtcDateTime;
                }

                {
                    // Kind == Local
                    TimeSpan offset = new TimeSpan(hours: 8, minutes: 0, seconds: 0);
                    DateTimeOffset dto = new DateTimeOffset(year: 2017, month: 1, day: 13, hour: 3, minute: 45, second: 32, offset: offset);
                    yield return dto.LocalDateTime;
                }

                {
                    // Kind == Local
                    TimeSpan offset = new TimeSpan(hours: -9, minutes: 0, seconds: 0);
                    DateTimeOffset dto = new DateTimeOffset(year: 2017, month: 1, day: 13, hour: 3, minute: 45, second: 32, offset: offset);
                    yield return dto.LocalDateTime;
                }
            }
        }

        public static IEnumerable<DateTimeOffset> DateTimeOffsetTestData
        {
            get
            {
                yield return DateTimeOffset.MinValue;
                yield return DateTimeOffset.MaxValue;
                yield return new DateTimeOffset(year: 2017, month: 1, day: 13, hour: 3, minute: 45, second: 32, new TimeSpan(hours: 0, minutes: 30, seconds: 0));
                yield return new DateTimeOffset(year: 2017, month: 1, day: 13, hour: 3, minute: 45, second: 32, new TimeSpan(hours: 0, minutes: -30, seconds: 0));
                yield return new DateTimeOffset(year: 2017, month: 1, day: 13, hour: 3, minute: 45, second: 32, new TimeSpan(hours: 8, minutes: 0, seconds: 0));
                yield return new DateTimeOffset(year: 2017, month: 1, day: 13, hour: 3, minute: 45, second: 32, new TimeSpan(hours: -8, minutes: 0, seconds: 0));
                yield return new DateTimeOffset(year: 2017, month: 12, day: 31, hour: 23, minute: 59, second: 58, new TimeSpan(hours: 14, minutes: 0, seconds: 0));
                yield return new DateTimeOffset(year: 2017, month: 12, day: 31, hour: 23, minute: 59, second: 58, new TimeSpan(hours: -14, minutes: 0, seconds: 0));

                foreach (PseudoDateTime pseudoDateTime in PseudoDateTimeTestData)
                {
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
                        yield return dto;
                    }
                }
            }
        }

        public static IEnumerable<TimeSpan> TimeSpanTestData
        {
            get
            {
                yield return TimeSpan.MinValue;
                yield return TimeSpan.MaxValue;
                yield return new TimeSpan(ticks: 0);
                yield return new TimeSpan(ticks: 1);
                yield return new TimeSpan(ticks: -1);
                yield return new TimeSpan(ticks: 12345L);
                yield return new TimeSpan(ticks: -12345L);
                yield return new TimeSpan(days: 4, hours: 9, minutes: 8, seconds: 6, milliseconds: 0);
                yield return new TimeSpan(days: -4, hours: 9, minutes: 8, seconds: 6, milliseconds: 0);
                yield return new TimeSpan(days: 4, hours: 9, minutes: 8, seconds: 6, milliseconds: 5);
                yield return new TimeSpan(days: -4, hours: 9, minutes: 8, seconds: 6, milliseconds: 5);
                yield return new TimeSpan(days: 54, hours: 10, minutes: 11, seconds: 12, milliseconds: 13);
                yield return new TimeSpan(days: -54, hours: 10, minutes: 11, seconds: 12, milliseconds: 13);
                yield return new TimeSpan(days: 54, hours: 10, minutes: 11, seconds: 12, milliseconds: 999);
            }
        }

        public static IEnumerable<string> NumberTestData
        {
            get
            {
                yield return "";
                yield return "+";
                yield return "-";
                yield return "0";
                yield return "+0";
                yield return "-0";
                yield return "0.0";
                yield return "-0.0";
                yield return "123.45";
                yield return "+123.45";
                yield return "-123.45";
                yield return "++123.45";
                yield return "--123.45";

                yield return "5.";
                yield return ".6";
                yield return "5.";
                yield return ".";

                yield return "000000123.45";
                yield return "0.000045";
                yield return "000000123.000045";

                yield return decimal.MinValue.ToString("G", CultureInfo.InvariantCulture);
                yield return decimal.MaxValue.ToString("G", CultureInfo.InvariantCulture);

                yield return float.MinValue.ToString("G9", CultureInfo.InvariantCulture);
                yield return float.MaxValue.ToString("G9", CultureInfo.InvariantCulture);
                yield return float.Epsilon.ToString("G9", CultureInfo.InvariantCulture);

                yield return double.MinValue.ToString("G17", CultureInfo.InvariantCulture);
                yield return double.MaxValue.ToString("G17", CultureInfo.InvariantCulture);
                yield return double.Epsilon.ToString("G9", CultureInfo.InvariantCulture);

                yield return "1e";
                yield return "1e+";
                yield return "1e-";

                yield return "1e10";
                yield return "1e+10";
                yield return "1e-10";

                yield return "1E10";
                yield return "1E+10";
                yield return "1E-10";

                yield return "1e+9";
                yield return "1e-9";
                yield return "1e+9";

                yield return "1e+90";
                yield return "1e-90";
                yield return "1e+90";

                yield return "1e+400";
                yield return "1e-400";
                yield return "1e+400";

                yield return "-1e+400";
                yield return "-1e-400";
                yield return "-1e+400";

                yield return "1e+/";
                yield return "1e/";
                yield return "1e-/";

                yield return "1e+:";
                yield return "1e:";
                yield return "1e-:";

                yield return "1e";
                yield return "1e/";
                yield return "1e:";
                yield return "0.5555555555555555555555555555555555555555555555555";

                yield return "0.66666666666666666666666666665";
                yield return "0.6666666666666666666666666666500000000000000000000000000000000000000000000000000000000000000";
                yield return "0.6666666666666666666666666666500000000000000000000";

                yield return "0.6666666666666666666666666666666666666666666666665";
                yield return "0.9999999999999999999999999999999999999999999999999";

                // Crazy case that's expected to yield "Decimal.MaxValue / 10" 
                // ( = 7922816251426433759354395034m (= High = 0x19999999, Mid = 0x99999999, Low = 0x9999999A))
                // and does thanks to a special overflow code path inside the Number->Decimal converter.
                yield return "0.79228162514264337593543950335" + "5" + "E28";

                // Exercise post-rounding overflow check.
                yield return "0.79228162514264337593543950335" + "5" + "E29";

                // Excercise the 20-digit lookahead inside the rounding logic inside the Number->Decimal converter.
                yield return "0.222222222222222222222222222255000000000000000000000000000000000000";

                // Code coverage for MutableDecimal.DecAdd()
                yield return "4611686018427387903.752";

                // Code coverage: "round X where {Epsilon > X >= 2.470328229206232730000000E-324} up to Epsilon"
                yield return "2.470328229206232730000000E-324";

                // Code coverage: underflow
                yield return "2.470328229206232730000000E-325";

                yield return "3.402823E+38"; //Single.MaxValue
                yield return "3.402824E+38"; //Just over Single.MaxValue
                yield return "-3.402823E+38"; //Single.MinValue
                yield return "-3.402824E+38"; //Just under Single.MinValue

                yield return "1.79769313486232E+308";   //Double.MaxValue
                yield return "1.79769313486233E+308";   //Just over Double.MaxValue
                yield return "-1.79769313486232E+308";  //Double.MinValue
                yield return "-1.79769313486233E+308";  //Just under Double.MinValue

                // Ensures that the NumberBuffer capacity is consistent with Desktop's.
                yield return ".2222222222222222222222222222500000000000000000001";
            }
        }

        public static IEnumerable<PseudoDateTime> PseudoDateTimeTestData
        {
            get
            {
                foreach (int year in new int[] { 2000, 2001, 2002, 2003, 2004, 2010, 2012, 2013, 2014, 2, 9999 })
                {
                    for (int month = 1; month <= 12; month++)
                    {
                        int daysInMonth = DateTime.DaysInMonth(year: year, month: month);
                        foreach (int day in new int[] { 1, 9, 10, daysInMonth })
                        {
                            yield return new PseudoDateTime(year: year, month: month, day: day, hour: 3, minute: 15, second: 45, expectSuccess: true);
                        }

                        yield return new PseudoDateTime(year: year, month: month, day: (daysInMonth + 1), hour: 23, minute: 15, second: 45, expectSuccess: false);
                    }
                }

                // Test data at the edge of the valid ranges.
                yield return new PseudoDateTime(year: 1, month: 1, day: 1, hour: 14, minute: 0, second: 0, expectSuccess: true);
                yield return new PseudoDateTime(year: 9999, month: 12, day: 31, hour: 9, minute: 0, second: 0, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 14, minute: 0, second: 0, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 12, day: 1, hour: 14, minute: 0, second: 0, expectSuccess: true);
                // Day range is month/year dependent. Was already covered above.
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 14, minute: 0, second: 0, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 23, minute: 0, second: 0, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 59, second: 0, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 59, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 9999999, offsetNegative: false, offsetHours: 0, offsetMinutes: 0, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: false, offsetHours: 13, offsetMinutes: 59, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: false, offsetHours: 14, offsetMinutes: 0, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: true, offsetHours: 13, offsetMinutes: 59, expectSuccess: true);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: true, offsetHours: 14, offsetMinutes: 0, expectSuccess: true);

                // Test data outside the valid ranges.
                yield return new PseudoDateTime(year: 0, month: 1, day: 1, hour: 24, minute: 0, second: 0, expectSuccess: false);
                yield return new PseudoDateTime(year: 2017, month: 0, day: 1, hour: 24, minute: 0, second: 0, expectSuccess: false);
                yield return new PseudoDateTime(year: 2017, month: 13, day: 1, hour: 24, minute: 0, second: 0, expectSuccess: false);
                // Day range is month/year dependent. Was already covered above.
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 60, minute: 0, second: 0, expectSuccess: false);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 60, second: 0, expectSuccess: false);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 60, expectSuccess: false);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: true, offsetHours: 0, offsetMinutes: 60, expectSuccess: false);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: false, offsetHours: 14, offsetMinutes: 1, expectSuccess: false);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: false, offsetHours: 15, offsetMinutes: 0, expectSuccess: false);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: true, offsetHours: 14, offsetMinutes: 1, expectSuccess: false);
                yield return new PseudoDateTime(year: 2017, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: true, offsetHours: 15, offsetMinutes: 0, expectSuccess: false);

                // Past the end of time.
                yield return new PseudoDateTime(year: 9999, month: 12, day: 31, hour: 23, minute: 59, second: 59, fraction: 9999999, offsetNegative: true, offsetHours: 0, offsetMinutes: 1, expectSuccess: false);
                yield return new PseudoDateTime(year: 1, month: 1, day: 1, hour: 0, minute: 0, second: 0, fraction: 0, offsetNegative: false, offsetHours: 0, offsetMinutes: 1, expectSuccess: false);
            }
        }
    }
}
