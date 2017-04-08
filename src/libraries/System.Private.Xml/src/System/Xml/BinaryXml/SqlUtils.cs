// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml
{
    // This is mostly just a copy of code in SqlTypes.SqlDecimal
    internal struct BinXmlSqlDecimal
    {
        internal byte m_bLen;
        internal byte m_bPrec;
        internal byte m_bScale;
        internal byte m_bSign;
        internal uint m_data1;
        internal uint m_data2;
        internal uint m_data3;
        internal uint m_data4;

        public bool IsPositive
        {
            get
            {
                return (m_bSign == 0);
            }
        }

        private static readonly byte s_NUMERIC_MAX_PRECISION = 38;            // Maximum precision of numeric
        private static readonly byte s_maxPrecision = s_NUMERIC_MAX_PRECISION;  // max SS precision
        private static readonly byte s_maxScale = s_NUMERIC_MAX_PRECISION;      // max SS scale

        private static readonly int s_cNumeMax = 4;
        private static readonly long s_lInt32Base = ((long)1) << 32;      // 2**32
        private static readonly ulong s_ulInt32Base = ((ulong)1) << 32;     // 2**32
        private static readonly ulong s_ulInt32BaseForMod = s_ulInt32Base - 1;    // 2**32 - 1 (0xFFF...FF)
        internal static readonly ulong x_llMax = Int64.MaxValue;   // Max of Int64
        //private static readonly uint x_ulBase10 = 10;
        private static readonly double s_DUINT_BASE = (double)s_lInt32Base;     // 2**32
        private static readonly double s_DUINT_BASE2 = s_DUINT_BASE * s_DUINT_BASE;  // 2**64
        private static readonly double s_DUINT_BASE3 = s_DUINT_BASE2 * s_DUINT_BASE; // 2**96
        //private static readonly double DMAX_NUME = 1.0e+38;                  // Max value of numeric
        //private static readonly uint DBL_DIG = 17;                       // Max decimal digits of double
        //private static readonly byte x_cNumeDivScaleMin = 6;     // Minimum result scale of numeric division
        // Array of multipliers for lAdjust and Ceiling/Floor.
        private static readonly uint[] s_rgulShiftBase = new uint[9] {
            10,
            10 * 10,
            10 * 10 * 10,
            10 * 10 * 10 * 10,
            10 * 10 * 10 * 10 * 10,
            10 * 10 * 10 * 10 * 10 * 10,
            10 * 10 * 10 * 10 * 10 * 10 * 10,
            10 * 10 * 10 * 10 * 10 * 10 * 10 * 10,
            10 * 10 * 10 * 10 * 10 * 10 * 10 * 10 * 10
        };

        public BinXmlSqlDecimal(byte[] data, int offset, bool trim)
        {
            byte b = data[offset];
            switch (b)
            {
                case 7:
                    m_bLen = 1;
                    break;
                case 11:
                    m_bLen = 2;
                    break;
                case 15:
                    m_bLen = 3;
                    break;
                case 19:
                    m_bLen = 4;
                    break;
                default:
                    throw new XmlException(SR.XmlBinary_InvalidSqlDecimal, (string[])null);
            }
            m_bPrec = data[offset + 1];
            m_bScale = data[offset + 2];
            m_bSign = 0 == data[offset + 3] ? (byte)1 : (byte)0;
            m_data1 = UIntFromByteArray(data, offset + 4);
            m_data2 = (m_bLen > 1) ? UIntFromByteArray(data, offset + 8) : 0;
            m_data3 = (m_bLen > 2) ? UIntFromByteArray(data, offset + 12) : 0;
            m_data4 = (m_bLen > 3) ? UIntFromByteArray(data, offset + 16) : 0;
            if (m_bLen == 4 && m_data4 == 0)
                m_bLen = 3;
            if (m_bLen == 3 && m_data3 == 0)
                m_bLen = 2;
            if (m_bLen == 2 && m_data2 == 0)
                m_bLen = 1;
            AssertValid();
            if (trim)
            {
                TrimTrailingZeros();
                AssertValid();
            }
        }

        private static uint UIntFromByteArray(byte[] data, int offset)
        {
            int val = (data[offset]) << 0;
            val |= (data[offset + 1]) << 8;
            val |= (data[offset + 2]) << 16;
            val |= (data[offset + 3]) << 24;
            return unchecked((uint)val);
        }

        // Multi-precision one super-digit divide in place.
        // U = U / D,
        // R = U % D
        // Length of U can decrease
        private static void MpDiv1(uint[] rgulU,      // InOut| U
                                   ref int ciulU,      // InOut| # of digits in U
                                   uint iulD,       // In    | D
                                   out uint iulR        // Out    | R
                                   )
        {
            Debug.Assert(rgulU.Length == s_cNumeMax);

            uint ulCarry = 0;
            ulong dwlAccum;
            ulong ulD = (ulong)iulD;
            int idU = ciulU;

            Debug.Assert(iulD != 0, "iulD != 0", "Divided by zero!");
            Debug.Assert(iulD > 0, "iulD > 0", "Invalid data: less than zero");
            Debug.Assert(ciulU > 0, "ciulU > 0", "No data in the array");
            while (idU > 0)
            {
                idU--;
                dwlAccum = (((ulong)ulCarry) << 32) + (ulong)(rgulU[idU]);
                rgulU[idU] = (uint)(dwlAccum / ulD);
                ulCarry = (uint)(dwlAccum - (ulong)rgulU[idU] * ulD);  // (ULONG) (dwlAccum % iulD)
            }

            iulR = ulCarry;
            MpNormalize(rgulU, ref ciulU);
        }
        // Normalize multi-precision number - remove leading zeroes
        private static void MpNormalize(uint[] rgulU,      // In   | Number
                                        ref int ciulU       // InOut| # of digits
                                        )
        {
            while (ciulU > 1 && rgulU[ciulU - 1] == 0)
                ciulU--;
        }

        //Determine the number of uints needed for a numeric given a precision
        //Precision        Length
        //    0            invalid
        //    1-9            1
        //    10-19        2
        //    20-28        3
        //    29-38        4
        // The array in Shiloh. Listed here for comparison.
        //private static readonly byte[] rgCLenFromPrec = new byte[] {5,5,5,5,5,5,5,5,5,9,9,9,9,9,
        //    9,9,9,9,9,13,13,13,13,13,13,13,13,13,17,17,17,17,17,17,17,17,17,17};
        private static readonly byte[] s_rgCLenFromPrec = new byte[] {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4
        };
        private static byte CLenFromPrec(byte bPrec)
        {
            Debug.Assert(bPrec <= s_maxPrecision && bPrec > 0, "bPrec <= MaxPrecision && bPrec > 0", "Invalid numeric precision");
            return s_rgCLenFromPrec[bPrec - 1];
        }

        private static char ChFromDigit(uint uiDigit)
        {
            Debug.Assert(uiDigit < 10);
            return (char)(uiDigit + '0');
        }

        public Decimal ToDecimal()
        {
            if ((int)m_data4 != 0 || m_bScale > 28)
                throw new XmlException(SR.SqlTypes_ArithOverflow, (string)null);

            return new Decimal((int)m_data1, (int)m_data2, (int)m_data3, !IsPositive, m_bScale);
        }

        private void TrimTrailingZeros()
        {
            uint[] rgulNumeric = new uint[4] { m_data1, m_data2, m_data3, m_data4 };
            int culLen = m_bLen;
            uint ulRem; //Remainder of a division by x_ulBase10, i.e.,least significant digit

            // special-case 0
            if (culLen == 1 && rgulNumeric[0] == 0)
            {
                m_bScale = 0;
                return;
            }

            while (m_bScale > 0 && (culLen > 1 || rgulNumeric[0] != 0))
            {
                MpDiv1(rgulNumeric, ref culLen, 10, out ulRem);
                if (ulRem == 0)
                {
                    m_data1 = rgulNumeric[0];
                    m_data2 = rgulNumeric[1];
                    m_data3 = rgulNumeric[2];
                    m_data4 = rgulNumeric[3];
                    m_bScale--;
                }
                else
                {
                    break;
                }
            }
            if (m_bLen == 4 && m_data4 == 0)
                m_bLen = 3;
            if (m_bLen == 3 && m_data3 == 0)
                m_bLen = 2;
            if (m_bLen == 2 && m_data2 == 0)
                m_bLen = 1;
        }

        public override String ToString()
        {
            AssertValid();

            // Make local copy of data to avoid modifying input.
            uint[] rgulNumeric = new uint[4] { m_data1, m_data2, m_data3, m_data4 };
            int culLen = m_bLen;
            char[] pszTmp = new char[s_NUMERIC_MAX_PRECISION + 1];   //Local Character buffer to hold
                                                                     //the decimal digits, from the
                                                                     //lowest significant to highest significant

            int iDigits = 0;//Number of significant digits
            uint ulRem; //Remainder of a division by x_ulBase10, i.e.,least significant digit

            // Build the final numeric string by inserting the sign, reversing
            // the order and inserting the decimal number at the correct position

            //Retrieve each digit from the lowest significant digit
            while (culLen > 1 || rgulNumeric[0] != 0)
            {
                MpDiv1(rgulNumeric, ref culLen, 10, out ulRem);
                //modulo x_ulBase10 is the lowest significant digit
                pszTmp[iDigits++] = ChFromDigit(ulRem);
            }

            // if scale of the number has not been
            // reached pad remaining number with zeros.
            while (iDigits <= m_bScale)
            {
                pszTmp[iDigits++] = ChFromDigit(0);
            }

            bool fPositive = IsPositive;

            // Increment the result length if negative (need to add '-')
            int uiResultLen = fPositive ? iDigits : iDigits + 1;

            // Increment the result length if scale > 0 (need to add '.')
            if (m_bScale > 0)
                uiResultLen++;

            char[] szResult = new char[uiResultLen];
            int iCurChar = 0;

            if (!fPositive)
                szResult[iCurChar++] = '-';

            while (iDigits > 0)
            {
                if (iDigits-- == m_bScale)
                    szResult[iCurChar++] = '.';
                szResult[iCurChar++] = pszTmp[iDigits];
            }

            AssertValid();

            return new String(szResult);
        }


        // Is this RE numeric valid?
        [System.Diagnostics.Conditional("DEBUG")]
        private void AssertValid()
        {
            // Scale,Prec in range
            Debug.Assert(m_bScale <= s_NUMERIC_MAX_PRECISION, "m_bScale <= NUMERIC_MAX_PRECISION", "In AssertValid");
            Debug.Assert(m_bScale <= m_bPrec, "m_bScale <= m_bPrec", "In AssertValid");
            Debug.Assert(m_bScale >= 0, "m_bScale >= 0", "In AssertValid");
            Debug.Assert(m_bPrec > 0, "m_bPrec > 0", "In AssertValid");
            Debug.Assert(CLenFromPrec(m_bPrec) >= m_bLen, "CLenFromPrec(m_bPrec) >= m_bLen", "In AssertValid");
            Debug.Assert(m_bLen <= s_cNumeMax, "m_bLen <= x_cNumeMax", "In AssertValid");

            uint[] rglData = new uint[4] { m_data1, m_data2, m_data3, m_data4 };

            // highest UI4 is non-0 unless value "zero"
            if (rglData[m_bLen - 1] == 0)
            {
                Debug.Assert(m_bLen == 1, "m_bLen == 1", "In AssertValid");
            }

            // All UI4s from length to end are 0
            for (int iulData = m_bLen; iulData < s_cNumeMax; iulData++)
                Debug.Assert(rglData[iulData] == 0, "rglData[iulData] == 0", "In AssertValid");
        }
    }

    internal struct BinXmlSqlMoney
    {
        private long _data;

        public BinXmlSqlMoney(int v) { _data = v; }
        public BinXmlSqlMoney(long v) { _data = v; }

        public Decimal ToDecimal()
        {
            bool neg;
            ulong v;
            if (_data < 0)
            {
                neg = true;
                v = (ulong)unchecked(-_data);
            }
            else
            {
                neg = false;
                v = (ulong)_data;
            }
            // SQL Server stores money8 as ticks of 1/10000.
            const byte MoneyScale = 4;
            return new Decimal(unchecked((int)v), unchecked((int)(v >> 32)), 0, neg, MoneyScale);
        }

        public override String ToString()
        {
            Decimal money = ToDecimal();
            // Formatting of SqlMoney: At least two digits after decimal point
            return money.ToString("#0.00##", CultureInfo.InvariantCulture);
        }
    }

    internal abstract class BinXmlDateTime
    {
        private const int MaxFractionDigits = 7;

        internal static int[] KatmaiTimeScaleMultiplicator = new int[8] {
            10000000,
            1000000,
            100000,
            10000,
            1000,
            100,
            10,
            1,
        };

        private static void Write2Dig(StringBuilder sb, int val)
        {
            Debug.Assert(val >= 0 && val < 100);
            sb.Append((char)('0' + (val / 10)));
            sb.Append((char)('0' + (val % 10)));
        }
        private static void Write4DigNeg(StringBuilder sb, int val)
        {
            Debug.Assert(val > -10000 && val < 10000);
            if (val < 0)
            {
                val = -val;
                sb.Append('-');
            }
            Write2Dig(sb, val / 100);
            Write2Dig(sb, val % 100);
        }

        private static void Write3Dec(StringBuilder sb, int val)
        {
            Debug.Assert(val >= 0 && val < 1000);
            int c3 = val % 10;
            val /= 10;
            int c2 = val % 10;
            val /= 10;
            int c1 = val;
            sb.Append('.');
            sb.Append((char)('0' + c1));
            sb.Append((char)('0' + c2));
            sb.Append((char)('0' + c3));
        }

        private static void WriteDate(StringBuilder sb, int yr, int mnth, int day)
        {
            Write4DigNeg(sb, yr);
            sb.Append('-');
            Write2Dig(sb, mnth);
            sb.Append('-');
            Write2Dig(sb, day);
        }

        private static void WriteTime(StringBuilder sb, int hr, int min, int sec, int ms)
        {
            Write2Dig(sb, hr);
            sb.Append(':');
            Write2Dig(sb, min);
            sb.Append(':');
            Write2Dig(sb, sec);
            if (ms != 0)
            {
                Write3Dec(sb, ms);
            }
        }

        private static void WriteTimeFullPrecision(StringBuilder sb, int hr, int min, int sec, int fraction)
        {
            Write2Dig(sb, hr);
            sb.Append(':');
            Write2Dig(sb, min);
            sb.Append(':');
            Write2Dig(sb, sec);
            if (fraction != 0)
            {
                int fractionDigits = MaxFractionDigits;
                while (fraction % 10 == 0)
                {
                    fractionDigits--;
                    fraction /= 10;
                }
                char[] charArray = new char[fractionDigits];
                while (fractionDigits > 0)
                {
                    fractionDigits--;
                    charArray[fractionDigits] = (char)(fraction % 10 + '0');
                    fraction /= 10;
                }
                sb.Append('.');
                sb.Append(charArray);
            }
        }

        private static void WriteTimeZone(StringBuilder sb, TimeSpan zone)
        {
            bool negTimeZone = true;
            if (zone.Ticks < 0)
            {
                negTimeZone = false;
                zone = zone.Negate();
            }
            WriteTimeZone(sb, negTimeZone, zone.Hours, zone.Minutes);
        }

        private static void WriteTimeZone(StringBuilder sb, bool negTimeZone, int hr, int min)
        {
            if (hr == 0 && min == 0)
            {
                sb.Append('Z');
            }
            else
            {
                sb.Append(negTimeZone ? '+' : '-');
                Write2Dig(sb, hr);
                sb.Append(':');
                Write2Dig(sb, min);
            }
        }

        private static void BreakDownXsdDateTime(long val, out int yr, out int mnth, out int day, out int hr, out int min, out int sec, out int ms)
        {
            if (val < 0)
                goto Error;
            long date = val / 4; // trim indicator bits
            ms = (int)(date % 1000);
            date /= 1000;
            sec = (int)(date % 60);
            date /= 60;
            min = (int)(date % 60);
            date /= 60;
            hr = (int)(date % 24);
            date /= 24;
            day = (int)(date % 31) + 1;
            date /= 31;
            mnth = (int)(date % 12) + 1;
            date /= 12;
            yr = (int)(date - 9999);
            if (yr < -9999 || yr > 9999)
                goto Error;
            return;
Error:
            throw new XmlException(SR.SqlTypes_ArithOverflow, (string)null);
        }

        private static void BreakDownXsdDate(long val, out int yr, out int mnth, out int day, out bool negTimeZone, out int hr, out int min)
        {
            if (val < 0)
                goto Error;
            val = val / 4; // trim indicator bits
            int totalMin = (int)(val % (29 * 60)) - 60 * 14;
            long totalDays = val / (29 * 60);

            if (negTimeZone = (totalMin < 0))
                totalMin = -totalMin;

            min = totalMin % 60;
            hr = totalMin / 60;

            day = (int)(totalDays % 31) + 1;
            totalDays /= 31;
            mnth = (int)(totalDays % 12) + 1;
            yr = (int)(totalDays / 12) - 9999;
            if (yr < -9999 || yr > 9999)
                goto Error;
            return;
Error:
            throw new XmlException(SR.SqlTypes_ArithOverflow, (string)null);
        }

        private static void BreakDownXsdTime(long val, out int hr, out int min, out int sec, out int ms)
        {
            if (val < 0)
                goto Error;
            val = val / 4; // trim indicator bits
            ms = (int)(val % 1000);
            val /= 1000;
            sec = (int)(val % 60);
            val /= 60;
            min = (int)(val % 60);
            hr = (int)(val / 60);
            if (0 > hr || hr > 23)
                goto Error;
            return;
Error:
            throw new XmlException(SR.SqlTypes_ArithOverflow, (string)null);
        }

        public static string XsdDateTimeToString(long val)
        {
            int yr;
            int mnth;
            int day;
            int hr;
            int min;
            int sec;
            int ms;
            BreakDownXsdDateTime(val, out yr, out mnth, out day, out hr, out min, out sec, out ms);
            StringBuilder sb = new StringBuilder(20);
            WriteDate(sb, yr, mnth, day);
            sb.Append('T');
            WriteTime(sb, hr, min, sec, ms);
            sb.Append('Z');
            return sb.ToString();
        }
        public static DateTime XsdDateTimeToDateTime(long val)
        {
            int yr;
            int mnth;
            int day;
            int hr;
            int min;
            int sec;
            int ms;
            BreakDownXsdDateTime(val, out yr, out mnth, out day, out hr, out min, out sec, out ms);
            return new DateTime(yr, mnth, day, hr, min, sec, ms, DateTimeKind.Utc);
        }

        public static string XsdDateToString(long val)
        {
            int yr;
            int mnth;
            int day;
            int hr;
            int min;
            bool negTimeZ;
            BreakDownXsdDate(val, out yr, out mnth, out day, out negTimeZ, out hr, out min);
            StringBuilder sb = new StringBuilder(20);
            WriteDate(sb, yr, mnth, day);
            WriteTimeZone(sb, negTimeZ, hr, min);
            return sb.ToString();
        }
        public static DateTime XsdDateToDateTime(long val)
        {
            int yr;
            int mnth;
            int day;
            int hr;
            int min;
            bool negTimeZ;
            BreakDownXsdDate(val, out yr, out mnth, out day, out negTimeZ, out hr, out min);
            DateTime d = new DateTime(yr, mnth, day, 0, 0, 0, DateTimeKind.Utc);
            // adjust for timezone
            int adj = (negTimeZ ? -1 : 1) * ((hr * 60) + min);
            return TimeZoneInfo.ConvertTime(d.AddMinutes(adj), TimeZoneInfo.Local);
        }

        public static string XsdTimeToString(long val)
        {
            int hr;
            int min;
            int sec;
            int ms;
            BreakDownXsdTime(val, out hr, out min, out sec, out ms);
            StringBuilder sb = new StringBuilder(16);
            WriteTime(sb, hr, min, sec, ms);
            sb.Append('Z');
            return sb.ToString();
        }
        public static DateTime XsdTimeToDateTime(long val)
        {
            int hr;
            int min;
            int sec;
            int ms;
            BreakDownXsdTime(val, out hr, out min, out sec, out ms);
            return new DateTime(1, 1, 1, hr, min, sec, ms, DateTimeKind.Utc);
        }

        public static string SqlDateTimeToString(int dateticks, uint timeticks)
        {
            DateTime dateTime = SqlDateTimeToDateTime(dateticks, timeticks);
            string format = (dateTime.Millisecond != 0) ? "yyyy/MM/dd\\THH:mm:ss.ffff" : "yyyy/MM/dd\\THH:mm:ss";
            return dateTime.ToString(format, CultureInfo.InvariantCulture);
        }
        public static DateTime SqlDateTimeToDateTime(int dateticks, uint timeticks)
        {
            DateTime SQLBaseDate = new DateTime(1900, 1, 1);
            //long millisecond = (long)(((ulong)timeticks * 20 + (ulong)3) / (ulong)6);
            long millisecond = (long)(timeticks / s_SQLTicksPerMillisecond + 0.5);
            return SQLBaseDate.Add(new TimeSpan(dateticks * TimeSpan.TicksPerDay +
                                                  millisecond * TimeSpan.TicksPerMillisecond));
        }

        // Number of (100ns) ticks per time unit
        private static readonly double s_SQLTicksPerMillisecond = 0.3;
        public static readonly int SQLTicksPerSecond = 300;
        public static readonly int SQLTicksPerMinute = SQLTicksPerSecond * 60;
        public static readonly int SQLTicksPerHour = SQLTicksPerMinute * 60;
        private static readonly int s_SQLTicksPerDay = SQLTicksPerHour * 24;


        public static string SqlSmallDateTimeToString(short dateticks, ushort timeticks)
        {
            DateTime dateTime = SqlSmallDateTimeToDateTime(dateticks, timeticks);
            return dateTime.ToString("yyyy/MM/dd\\THH:mm:ss", CultureInfo.InvariantCulture);
        }
        public static DateTime SqlSmallDateTimeToDateTime(short dateticks, ushort timeticks)
        {
            return SqlDateTimeToDateTime((int)dateticks, (uint)(timeticks * SQLTicksPerMinute));
        }

        // Conversions of the Katmai date & time types to DateTime
        public static DateTime XsdKatmaiDateToDateTime(byte[] data, int offset)
        {
            // Katmai SQL type "DATE"
            long dateTicks = GetKatmaiDateTicks(data, ref offset);
            DateTime dt = new DateTime(dateTicks);
            return dt;
        }

        public static DateTime XsdKatmaiDateTimeToDateTime(byte[] data, int offset)
        {
            // Katmai SQL type "DATETIME2"
            long timeTicks = GetKatmaiTimeTicks(data, ref offset);
            long dateTicks = GetKatmaiDateTicks(data, ref offset);
            DateTime dt = new DateTime(dateTicks + timeTicks);
            return dt;
        }

        public static DateTime XsdKatmaiTimeToDateTime(byte[] data, int offset)
        {
            // TIME without zone is stored as DATETIME2
            return XsdKatmaiDateTimeToDateTime(data, offset);
        }

        public static DateTime XsdKatmaiDateOffsetToDateTime(byte[] data, int offset)
        {
            // read the timezoned value into DateTimeOffset and then convert to local time
            return XsdKatmaiDateOffsetToDateTimeOffset(data, offset).LocalDateTime;
        }

        public static DateTime XsdKatmaiDateTimeOffsetToDateTime(byte[] data, int offset)
        {
            // read the timezoned value into DateTimeOffset and then convert to local time
            return XsdKatmaiDateTimeOffsetToDateTimeOffset(data, offset).LocalDateTime;
        }

        public static DateTime XsdKatmaiTimeOffsetToDateTime(byte[] data, int offset)
        {
            // read the timezoned value into DateTimeOffset and then convert to local time
            return XsdKatmaiTimeOffsetToDateTimeOffset(data, offset).LocalDateTime;
        }

        public static DateTimeOffset XsdKatmaiDateOffsetToDateTimeOffset(byte[] data, int offset)
        {
            // DATE with zone is stored as DATETIMEOFFSET
            return XsdKatmaiDateTimeOffsetToDateTimeOffset(data, offset);
        }

        public static DateTimeOffset XsdKatmaiDateTimeOffsetToDateTimeOffset(byte[] data, int offset)
        {
            // Katmai SQL type "DATETIMEOFFSET"
            long timeTicks = GetKatmaiTimeTicks(data, ref offset);
            long dateTicks = GetKatmaiDateTicks(data, ref offset);
            long zoneTicks = GetKatmaiTimeZoneTicks(data, offset);
            // The DATETIMEOFFSET values are serialized in UTC, but DateTimeOffset takes adjusted time -> we need to add zoneTicks
            DateTimeOffset dto = new DateTimeOffset(dateTicks + timeTicks + zoneTicks, new TimeSpan(zoneTicks));
            return dto;
        }

        public static DateTimeOffset XsdKatmaiTimeOffsetToDateTimeOffset(byte[] data, int offset)
        {
            // TIME with zone is stored as DATETIMEOFFSET
            return XsdKatmaiDateTimeOffsetToDateTimeOffset(data, offset);
        }

        // Conversions of the Katmai date & time types to string
        public static string XsdKatmaiDateToString(byte[] data, int offset)
        {
            DateTime dt = XsdKatmaiDateToDateTime(data, offset);
            StringBuilder sb = new StringBuilder(10);
            WriteDate(sb, dt.Year, dt.Month, dt.Day);
            return sb.ToString();
        }

        public static string XsdKatmaiDateTimeToString(byte[] data, int offset)
        {
            DateTime dt = XsdKatmaiDateTimeToDateTime(data, offset);
            StringBuilder sb = new StringBuilder(33);
            WriteDate(sb, dt.Year, dt.Month, dt.Day);
            sb.Append('T');
            WriteTimeFullPrecision(sb, dt.Hour, dt.Minute, dt.Second, GetFractions(dt));
            return sb.ToString();
        }

        public static string XsdKatmaiTimeToString(byte[] data, int offset)
        {
            DateTime dt = XsdKatmaiTimeToDateTime(data, offset);
            StringBuilder sb = new StringBuilder(16);
            WriteTimeFullPrecision(sb, dt.Hour, dt.Minute, dt.Second, GetFractions(dt));
            return sb.ToString();
        }

        public static string XsdKatmaiDateOffsetToString(byte[] data, int offset)
        {
            DateTimeOffset dto = XsdKatmaiDateOffsetToDateTimeOffset(data, offset);
            StringBuilder sb = new StringBuilder(16);
            WriteDate(sb, dto.Year, dto.Month, dto.Day);
            WriteTimeZone(sb, dto.Offset);
            return sb.ToString();
        }

        public static string XsdKatmaiDateTimeOffsetToString(byte[] data, int offset)
        {
            DateTimeOffset dto = XsdKatmaiDateTimeOffsetToDateTimeOffset(data, offset);
            StringBuilder sb = new StringBuilder(39);
            WriteDate(sb, dto.Year, dto.Month, dto.Day);
            sb.Append('T');
            WriteTimeFullPrecision(sb, dto.Hour, dto.Minute, dto.Second, GetFractions(dto));
            WriteTimeZone(sb, dto.Offset);
            return sb.ToString();
        }

        public static string XsdKatmaiTimeOffsetToString(byte[] data, int offset)
        {
            DateTimeOffset dto = XsdKatmaiTimeOffsetToDateTimeOffset(data, offset);
            StringBuilder sb = new StringBuilder(22);
            WriteTimeFullPrecision(sb, dto.Hour, dto.Minute, dto.Second, GetFractions(dto));
            WriteTimeZone(sb, dto.Offset);
            return sb.ToString();
        }

        // Helper methods for the Katmai date & time types
        private static long GetKatmaiDateTicks(byte[] data, ref int pos)
        {
            int p = pos;
            pos = p + 3;
            return (data[p] | data[p + 1] << 8 | data[p + 2] << 16) * TimeSpan.TicksPerDay;
        }

        private static long GetKatmaiTimeTicks(byte[] data, ref int pos)
        {
            int p = pos;
            byte scale = data[p];
            long timeTicks;
            p++;
            if (scale <= 2)
            {
                timeTicks = data[p] | (data[p + 1] << 8) | (data[p + 2] << 16);
                pos = p + 3;
            }
            else if (scale <= 4)
            {
                timeTicks = data[p] | (data[p + 1] << 8) | (data[p + 2] << 16);
                timeTicks |= ((long)data[p + 3] << 24);
                pos = p + 4;
            }
            else if (scale <= 7)
            {
                timeTicks = data[p] | (data[p + 1] << 8) | (data[p + 2] << 16);
                timeTicks |= ((long)data[p + 3] << 24) | ((long)data[p + 4] << 32);
                pos = p + 5;
            }
            else
            {
                throw new XmlException(SR.SqlTypes_ArithOverflow, (string)null);
            }
            return timeTicks * KatmaiTimeScaleMultiplicator[scale];
        }

        private static long GetKatmaiTimeZoneTicks(byte[] data, int pos)
        {
            return (short)(data[pos] | data[pos + 1] << 8) * TimeSpan.TicksPerMinute;
        }

        private static int GetFractions(DateTime dt)
        {
            return (int)(dt.Ticks - new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).Ticks);
        }

        private static int GetFractions(DateTimeOffset dt)
        {
            return (int)(dt.Ticks - new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).Ticks);
        }
    }
}