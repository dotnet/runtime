// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.SqlTypes
{
    /// <summary>
    /// Represents a fixed precision and scale numeric value between -10<superscript term='38'/>
    /// -1 and 10<superscript term='38'/> -1 to be stored in or retrieved from a database.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    public struct SqlDecimal : INullable, IComparable, IXmlSerializable, IEquatable<SqlDecimal>
    {
        // data in CSsNumeric in SQL Server
        // BYTE    m_cbLen;                // # of DWORDs + 1 (1 is for sign)
        // BYTE    m_bPrec;                // precision
        // BYTE    m_bScale;                // scale
        // BYTE    m_bSign;                // NUM_POSITIVE or NUM_NEGATIVE
        // ULONG    m_rgulData [x_culNumeMax];

        // NOTE: If any instance fields change, update SqlTypeWorkarounds type in System.Data.SqlClient.
        internal byte _bStatus;      // bit 0: fNotNull, bit 1: fNegative
        internal byte _bLen;         // number of uints used, = (CSsNumeric.m_cbLen - 1) / 4.
        internal byte _bPrec;
        internal byte _bScale;
        internal uint _data1;
        internal uint _data2;
        internal uint _data3;
        internal uint _data4;

        private const byte s_NUMERIC_MAX_PRECISION = 38;            // Maximum precision of numeric
        public static readonly byte MaxPrecision = s_NUMERIC_MAX_PRECISION;  // max SS precision
        public static readonly byte MaxScale = s_NUMERIC_MAX_PRECISION;      // max SS scale

        private const byte s_bNullMask = 1;    // bit mask for null bit in m_bStatus
        private const byte s_bIsNull = 0;    // is null
        private const byte s_bNotNull = 1;    // is not null
        private const byte s_bReverseNullMask = unchecked((byte)~s_bNullMask);

        private const byte s_bSignMask = 2;    // bit mask for sign bit in m_bStatus
        private const byte s_bPositive = 0;    // is positive
        private const byte s_bNegative = 2;    // is negative
        private const byte s_bReverseSignMask = unchecked((byte)~s_bSignMask);

        private const uint s_uiZero = 0;

        private const int s_cNumeMax = 4;
        private const long s_lInt32Base = ((long)1) << 32;      // 2**32
        private const ulong s_ulInt32Base = ((ulong)1) << 32;     // 2**32
        private const ulong s_ulInt32BaseForMod = s_ulInt32Base - 1;    // 2**32 - 1 (0xFFF...FF)

        internal const ulong s_llMax = long.MaxValue;   // Max of Int64

        private const uint s_ulBase10 = 10;

        private const double s_DUINT_BASE = s_lInt32Base;     // 2**32
        private const double s_DUINT_BASE2 = s_DUINT_BASE * s_DUINT_BASE;  // 2**64
        private const double s_DUINT_BASE3 = s_DUINT_BASE2 * s_DUINT_BASE; // 2**96
        private const double s_DMAX_NUME = 1.0e+38;                  // Max value of numeric
        private const uint s_DBL_DIG = 17;                       // Max decimal digits of double

        private const byte s_cNumeDivScaleMin = 6;     // Minimum result scale of numeric division

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

        #region DecimalHelperTableGenerator
        /*
                // the code below will generate the DecimalHelpers tables
                private static string[] HelperNames = {
                    "DecimalHelpersLo", "DecimalHelpersMid", "DecimalHelpersHi", "DecimalHelpersHiHi",
                };


                private static void DumpDecimalHelperParts(int index)
                {
                    SqlDecimal sqlDecimalValue = 10;    // start precision=2

                    Console.WriteLine("private static readonly UInt32[] {0} = {{", HelperNames[index]);
                    for (int precision = 2; precision <= SqlDecimal.MaxPrecision; precision++){
                        Console.WriteLine("    0x{0,8:x8}, // precision:{1}, value:{2}", sqlDecimalValue.Data[index], precision, sqlDecimalValue.ToString());
                        if (precision < SqlDecimal.MaxPrecision){
                            sqlDecimalValue *= 10;
                        }
                    }
                    sqlDecimalValue = SqlDecimal.MaxValue;
                    int[] data = sqlDecimalValue.Data;
                    UInt32[] udata = { (UInt32)data[0], (UInt32)data[1], (UInt32)data[2], (UInt32)data[3]};
                    bool carry = true;
                    for (int i = 0; i < 4; i++){
                        if (carry){
                            carry = (++udata[i] == 0);
                        }
                    }
                    Console.WriteLine("    0x{0,8:x8}, // precision:{1}+1, value:{2}+1", udata[index], SqlDecimal.MaxPrecision, SqlDecimal.MaxValue.ToString());

                    Console.WriteLine("};");
                    Console.WriteLine();
                }


                public static void CreateDecimalHelperTable()
                {
                    for (int i = 0; i < 4; i++)
                    {
                        DumpDecimalHelperParts(i);
                    }
                }

        */
        #endregion

        #region DecimalHelperTable
        private static readonly uint[] s_decimalHelpersLo = {
            0x0000000a, // precision:2, value:10
            0x00000064, // precision:3, value:100
            0x000003e8, // precision:4, value:1000
            0x00002710, // precision:5, value:10000
            0x000186a0, // precision:6, value:100000
            0x000f4240, // precision:7, value:1000000
            0x00989680, // precision:8, value:10000000
            0x05f5e100, // precision:9, value:100000000
            0x3b9aca00, // precision:10, value:1000000000
            0x540be400, // precision:11, value:10000000000
            0x4876e800, // precision:12, value:100000000000
            0xd4a51000, // precision:13, value:1000000000000
            0x4e72a000, // precision:14, value:10000000000000
            0x107a4000, // precision:15, value:100000000000000
            0xa4c68000, // precision:16, value:1000000000000000
            0x6fc10000, // precision:17, value:10000000000000000
            0x5d8a0000, // precision:18, value:100000000000000000
            0xa7640000, // precision:19, value:1000000000000000000
            0x89e80000, // precision:20, value:10000000000000000000
            0x63100000, // precision:21, value:100000000000000000000
            0xdea00000, // precision:22, value:1000000000000000000000
            0xb2400000, // precision:23, value:10000000000000000000000
            0xf6800000, // precision:24, value:100000000000000000000000
            0xa1000000, // precision:25, value:1000000000000000000000000
            0x4a000000, // precision:26, value:10000000000000000000000000
            0xe4000000, // precision:27, value:100000000000000000000000000
            0xe8000000, // precision:28, value:1000000000000000000000000000
            0x10000000, // precision:29, value:10000000000000000000000000000
            0xa0000000, // precision:30, value:100000000000000000000000000000
            0x40000000, // precision:31, value:1000000000000000000000000000000
            0x80000000, // precision:32, value:10000000000000000000000000000000
            0x00000000, // precision:33, value:100000000000000000000000000000000
            0x00000000, // precision:34, value:1000000000000000000000000000000000
            0x00000000, // precision:35, value:10000000000000000000000000000000000
            0x00000000, // precision:36, value:100000000000000000000000000000000000
            0x00000000, // precision:37, value:1000000000000000000000000000000000000
            0x00000000, // precision:38, value:10000000000000000000000000000000000000
            0x00000000, // precision:38+1, value:99999999999999999999999999999999999999+1
        };

        private static readonly uint[] s_decimalHelpersMid = {
            0x00000000, // precision:2, value:10
            0x00000000, // precision:3, value:100
            0x00000000, // precision:4, value:1000
            0x00000000, // precision:5, value:10000
            0x00000000, // precision:6, value:100000
            0x00000000, // precision:7, value:1000000
            0x00000000, // precision:8, value:10000000
            0x00000000, // precision:9, value:100000000
            0x00000000, // precision:10, value:1000000000
            0x00000002, // precision:11, value:10000000000
            0x00000017, // precision:12, value:100000000000
            0x000000e8, // precision:13, value:1000000000000
            0x00000918, // precision:14, value:10000000000000
            0x00005af3, // precision:15, value:100000000000000
            0x00038d7e, // precision:16, value:1000000000000000
            0x002386f2, // precision:17, value:10000000000000000
            0x01634578, // precision:18, value:100000000000000000
            0x0de0b6b3, // precision:19, value:1000000000000000000
            0x8ac72304, // precision:20, value:10000000000000000000
            0x6bc75e2d, // precision:21, value:100000000000000000000
            0x35c9adc5, // precision:22, value:1000000000000000000000
            0x19e0c9ba, // precision:23, value:10000000000000000000000
            0x02c7e14a, // precision:24, value:100000000000000000000000
            0x1bcecced, // precision:25, value:1000000000000000000000000
            0x16140148, // precision:26, value:10000000000000000000000000
            0xdcc80cd2, // precision:27, value:100000000000000000000000000
            0x9fd0803c, // precision:28, value:1000000000000000000000000000
            0x3e250261, // precision:29, value:10000000000000000000000000000
            0x6d7217ca, // precision:30, value:100000000000000000000000000000
            0x4674edea, // precision:31, value:1000000000000000000000000000000
            0xc0914b26, // precision:32, value:10000000000000000000000000000000
            0x85acef81, // precision:33, value:100000000000000000000000000000000
            0x38c15b0a, // precision:34, value:1000000000000000000000000000000000
            0x378d8e64, // precision:35, value:10000000000000000000000000000000000
            0x2b878fe8, // precision:36, value:100000000000000000000000000000000000
            0xb34b9f10, // precision:37, value:1000000000000000000000000000000000000
            0x00f436a0, // precision:38, value:10000000000000000000000000000000000000
            0x098a2240, // precision:38+1, value:99999999999999999999999999999999999999+1
        };

        private static readonly uint[] s_decimalHelpersHi = {
            0x00000000, // precision:2, value:10
            0x00000000, // precision:3, value:100
            0x00000000, // precision:4, value:1000
            0x00000000, // precision:5, value:10000
            0x00000000, // precision:6, value:100000
            0x00000000, // precision:7, value:1000000
            0x00000000, // precision:8, value:10000000
            0x00000000, // precision:9, value:100000000
            0x00000000, // precision:10, value:1000000000
            0x00000000, // precision:11, value:10000000000
            0x00000000, // precision:12, value:100000000000
            0x00000000, // precision:13, value:1000000000000
            0x00000000, // precision:14, value:10000000000000
            0x00000000, // precision:15, value:100000000000000
            0x00000000, // precision:16, value:1000000000000000
            0x00000000, // precision:17, value:10000000000000000
            0x00000000, // precision:18, value:100000000000000000
            0x00000000, // precision:19, value:1000000000000000000
            0x00000000, // precision:20, value:10000000000000000000
            0x00000005, // precision:21, value:100000000000000000000
            0x00000036, // precision:22, value:1000000000000000000000
            0x0000021e, // precision:23, value:10000000000000000000000
            0x0000152d, // precision:24, value:100000000000000000000000
            0x0000d3c2, // precision:25, value:1000000000000000000000000
            0x00084595, // precision:26, value:10000000000000000000000000
            0x0052b7d2, // precision:27, value:100000000000000000000000000
            0x033b2e3c, // precision:28, value:1000000000000000000000000000
            0x204fce5e, // precision:29, value:10000000000000000000000000000
            0x431e0fae, // precision:30, value:100000000000000000000000000000
            0x9f2c9cd0, // precision:31, value:1000000000000000000000000000000
            0x37be2022, // precision:32, value:10000000000000000000000000000000
            0x2d6d415b, // precision:33, value:100000000000000000000000000000000
            0xc6448d93, // precision:34, value:1000000000000000000000000000000000
            0xbead87c0, // precision:35, value:10000000000000000000000000000000000
            0x72c74d82, // precision:36, value:100000000000000000000000000000000000
            0x7bc90715, // precision:37, value:1000000000000000000000000000000000000
            0xd5da46d9, // precision:38, value:10000000000000000000000000000000000000
            0x5a86c47a, // precision:38+1, value:99999999999999999999999999999999999999+1
        };

        private static readonly uint[] s_decimalHelpersHiHi = {
            0x00000000, // precision:2, value:10
            0x00000000, // precision:3, value:100
            0x00000000, // precision:4, value:1000
            0x00000000, // precision:5, value:10000
            0x00000000, // precision:6, value:100000
            0x00000000, // precision:7, value:1000000
            0x00000000, // precision:8, value:10000000
            0x00000000, // precision:9, value:100000000
            0x00000000, // precision:10, value:1000000000
            0x00000000, // precision:11, value:10000000000
            0x00000000, // precision:12, value:100000000000
            0x00000000, // precision:13, value:1000000000000
            0x00000000, // precision:14, value:10000000000000
            0x00000000, // precision:15, value:100000000000000
            0x00000000, // precision:16, value:1000000000000000
            0x00000000, // precision:17, value:10000000000000000
            0x00000000, // precision:18, value:100000000000000000
            0x00000000, // precision:19, value:1000000000000000000
            0x00000000, // precision:20, value:10000000000000000000
            0x00000000, // precision:21, value:100000000000000000000
            0x00000000, // precision:22, value:1000000000000000000000
            0x00000000, // precision:23, value:10000000000000000000000
            0x00000000, // precision:24, value:100000000000000000000000
            0x00000000, // precision:25, value:1000000000000000000000000
            0x00000000, // precision:26, value:10000000000000000000000000
            0x00000000, // precision:27, value:100000000000000000000000000
            0x00000000, // precision:28, value:1000000000000000000000000000
            0x00000000, // precision:29, value:10000000000000000000000000000
            0x00000001, // precision:30, value:100000000000000000000000000000
            0x0000000c, // precision:31, value:1000000000000000000000000000000
            0x0000007e, // precision:32, value:10000000000000000000000000000000
            0x000004ee, // precision:33, value:100000000000000000000000000000000
            0x0000314d, // precision:34, value:1000000000000000000000000000000000
            0x0001ed09, // precision:35, value:10000000000000000000000000000000000
            0x00134261, // precision:36, value:100000000000000000000000000000000000
            0x00c097ce, // precision:37, value:1000000000000000000000000000000000000
            0x0785ee10, // precision:38, value:10000000000000000000000000000000000000
            0x4b3b4ca8, // precision:38+1, value:99999999999999999999999999999999999999+1
        };
        #endregion

        // note that the algorithm covers a range from -5 to +4 from the initial index
        // at the end of the algorithm the tableindex will point to the greatest value that is
        // less than the current value
        // except (!) if the current value is less than 10 (precision=1). There is no value ins
        // the table that is less than 10. In this case the algorithm terminates earlier.
        //
        // The startindex values have been chosen so that the highest possible index (startindex+5)
        // does not point to a value that has bits in a higher word set
        //
        private const int HelperTableStartIndexLo = 5;
        private const int HelperTableStartIndexMid = 15;
        private const int HelperTableStartIndexHi = 24;
        private const int HelperTableStartIndexHiHi = 33;

        private byte CalculatePrecision()
        {
            int tableIndex;
            byte precision;
            uint[] decimalHelpers;
            uint decimalPart;

            if (_data4 != 0)
            {
                tableIndex = HelperTableStartIndexHiHi;
                decimalHelpers = s_decimalHelpersHiHi;
                decimalPart = _data4;
            }
            else if (_data3 != 0)
            {
                tableIndex = HelperTableStartIndexHi;
                decimalHelpers = s_decimalHelpersHi;
                decimalPart = _data3;
            }
            else if (_data2 != 0)
            {
                tableIndex = HelperTableStartIndexMid;
                decimalHelpers = s_decimalHelpersMid;
                decimalPart = _data2;
            }
            else
            {
                tableIndex = HelperTableStartIndexLo;
                decimalHelpers = s_decimalHelpersLo;
                decimalPart = _data1;
            }


            // this code will move the index no more than -2 -2 -1 (-5) or +2 +1 +1 (+4)
            // from the initial position
            //
            if (decimalPart < decimalHelpers[tableIndex])
            {
                tableIndex -= 2;
                if (decimalPart < decimalHelpers[tableIndex])
                {
                    tableIndex -= 2;
                    if (decimalPart < decimalHelpers[tableIndex])
                    {
                        tableIndex -= 1;
                    }
                    else
                    {
                        tableIndex += 1;
                    }
                }
                else
                {
                    tableIndex += 1;
                }
            }
            else
            {
                tableIndex += 2;
                if (decimalPart < decimalHelpers[tableIndex])
                {
                    tableIndex -= 1;
                }
                else
                {
                    tableIndex += 1;
                }
            }
            if (decimalPart >= decimalHelpers[tableIndex])
            {
                tableIndex += 1;
                if (tableIndex == 37 && decimalPart >= decimalHelpers[tableIndex])
                {
                    // This can happen only if the actual value is greater than 1E+38,
                    // in which case, tableIndex starts at 33 and ends at 37.
                    // Note that in this case, the actual value will be out of SqlDeicmal's range,
                    // and tableIndex is out of the array boudary. We'll throw later in ctors.
                    tableIndex += 1;
                }
            }

            precision = (byte)(tableIndex + 1);
            if (precision > 1)
            {
                // not done yet
                // tableIndex may still be off by one since we didn't look at the lower words
                //
                if (VerifyPrecision((byte)(precision - 1)))
                {
                    precision -= 1;
                }
            }
            Debug.Assert((precision == MaxPrecision + 1) || VerifyPrecision(precision), "Calcualted precision too low?");
            Debug.Assert((precision == 1) || !VerifyPrecision((byte)(precision - 1)), "Calculated precision too high?");

            // adjust the precision
            // This might not be correct but is to our best knowledge
            precision = Math.Max(precision, _bScale);
#if DEBUG
            byte bPrec = _bPrec;   // store current value
            _bPrec = MaxPrecision; // BActualPrec does not work if m_bPrec uninitialized!
            byte bActualPrecision = BActualPrec();
            _bPrec = bPrec;  // restore current value

            Debug.Assert(precision == bActualPrecision, $"CalculatePrecision={precision}, BActualPrec={bActualPrecision}. Results must be equal!");
#endif
            return precision;
        }

        // VerifyPrecision
        //
        // returns true if the current value is less or equal than the max value of the
        // supplied precision.
        //
        private bool VerifyPrecision(byte precision)
        {
            Debug.Assert(precision > 0, "Precision cannot be less than 1");
            Debug.Assert(precision <= MaxPrecision, "Precision > MaxPrecision");

            int tableIndex = checked((precision - 1));
            if (_data4 < s_decimalHelpersHiHi[tableIndex])
            {
                return true;
            }
            else if (_data4 == s_decimalHelpersHiHi[tableIndex])
            {
                if (_data3 < s_decimalHelpersHi[tableIndex])
                {
                    return true;
                }
                else if (_data3 == s_decimalHelpersHi[tableIndex])
                {
                    if (_data2 < s_decimalHelpersMid[tableIndex])
                    {
                        return true;
                    }
                    else if (_data2 == s_decimalHelpersMid[tableIndex])
                    {
                        if (_data1 < s_decimalHelpersLo[tableIndex])
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }



        // constructor
        // construct a Null
        private SqlDecimal(bool fNull)
        {
            _bLen =
            _bPrec =
            _bScale = 0;
            _bStatus = 0;
            _data1 =
            _data2 =
            _data3 =
            _data4 = s_uiZero;
        }

        public SqlDecimal(decimal value)
        {
            // set the null bit
            _bStatus = s_bNotNull;

            // note: using usafe code we could get the data directly out of the structure like that:
            // UInt32 * pInt = (UInt32*) &value;
            // UInt32 sgnscl = *pInt++;

            // m_data3 = *pInt++; // high part
            // m_data1 = *pInt++; // lo part
            // m_data2 = *pInt++; // mid part

            Span<int> bits = stackalloc int[4];
            decimal.GetBits(value, bits);
            uint sgnscl;

            unchecked
            {
                sgnscl = (uint)bits[3];
                _data1 = (uint)bits[0];
                _data2 = (uint)bits[1];
                _data3 = (uint)bits[2];
                _data4 = s_uiZero;
            }

            // set the sign bit
            _bStatus |= ((sgnscl & 0x80000000) == 0x80000000) ? s_bNegative : (byte)0;

            if (_data3 != 0)
                _bLen = 3;
            else if (_data2 != 0)
                _bLen = 2;
            else
                _bLen = 1;

            // Get the scale info from Decimal
            _bScale = (byte)((int)(sgnscl & 0xff0000) >> 16);

            // initialize precision
            _bPrec = 0; // The this object cannot be used before all of its fields are assigned to


            _bPrec = CalculatePrecision();
            // CalculatePrecision adjusts!
            //            if (m_bPrec < m_bScale)
            //                m_bPrec = m_bScale;
        }

        public SqlDecimal(int value)
        {
            // set the null bit
            _bStatus = s_bNotNull;

            uint uiValue = unchecked((uint)value);

            // set the sign bit
            if (value < 0)
            {
                _bStatus |= s_bNegative;
                // The negative of -2147483648 doesn't fit into int32, directly cast to int should work.
                if (value != int.MinValue)
                    uiValue = (uint)(-value);
            }

            // set the data
            _data1 = uiValue;
            _data2 = _data3 = _data4 = s_uiZero;

            _bLen = 1;
            _bPrec = BGetPrecUI4(_data1);
            _bScale = 0;
        }

        public SqlDecimal(long value)
        {
            // set the null bit
            _bStatus = s_bNotNull;

            ulong dwl = unchecked((ulong)value);

            // set the sign bit
            if (value < 0)
            {
                _bStatus |= s_bNegative;
                // The negative of Int64.MinValue doesn't fit into int64, directly cast to ulong should work.
                if (value != long.MinValue)
                    dwl = (ulong)(-value);
            }

            // Copy DWL into bottom 2 UI4s of numeric
            _data1 = (uint)dwl;
            _data2 = (uint)(dwl >> 32);
            _data3 = _data4 = 0;

            // Trim any leading zero from the length
            _bLen = (byte)((_data2 == 0) ? 1 : 2);

            _bPrec = BGetPrecUI8(dwl);
            _bScale = 0;

            AssertValid();
        }

        public SqlDecimal(byte bPrecision, byte bScale, bool fPositive, int[] bits)
        {
            CheckValidPrecScale(bPrecision, bScale);
            if (bits == null)
                throw new ArgumentNullException(nameof(bits));
            else if (bits.Length != 4)
                throw new ArgumentException(SQLResource.InvalidArraySizeMessage, nameof(bits));

            _bPrec = bPrecision;
            _bScale = bScale;
            _data1 = (uint)bits[0];
            _data2 = (uint)bits[1];
            _data3 = (uint)bits[2];
            _data4 = (uint)bits[3];
            _bLen = 1;
            for (int i = 3; i >= 0; i--)
            {
                if (bits[i] != 0)
                {
                    _bLen = (byte)(i + 1);
                    break;
                }
            }

            // set the null bit
            _bStatus = s_bNotNull;

            // set the sign bit
            if (!fPositive)
            {
                _bStatus |= s_bNegative;
            }

            // If result is -0, adjust sign to positive.
            if (FZero())
                SetPositive();

            if (bPrecision < CalculatePrecision())
                throw new OverflowException(SQLResource.ArithOverflowMessage);
        }

        public SqlDecimal(byte bPrecision, byte bScale, bool fPositive, int data1, int data2, int data3, int data4)
        {
            CheckValidPrecScale(bPrecision, bScale);
            _bPrec = bPrecision;
            _bScale = bScale;

            _data1 = (uint)data1;
            _data2 = (uint)data2;
            _data3 = (uint)data3;
            _data4 = (uint)data4;

            _bLen = 1;
            if (data4 == 0)
                if (data3 == 0)
                    if (data2 == 0)
                        _bLen = 1;
                    else
                        _bLen = 2;
                else
                    _bLen = 3;
            else
                _bLen = 4;

            // set the null bit
            _bStatus = s_bNotNull;

            // set the sign bit
            if (!fPositive)
            {
                _bStatus |= s_bNegative;
            }

            // If result is -0, adjust sign to positive.
            if (FZero())
                SetPositive();

            if (bPrecision < CalculatePrecision())
                throw new OverflowException(SQLResource.ArithOverflowMessage);
        }

        public SqlDecimal(double dVal) : this(false)
        {
            // set the null bit
            _bStatus = s_bNotNull;

            // Split double to sign, integer, and fractional parts
            if (dVal < 0)
            {
                dVal = -dVal;
                _bStatus |= s_bNegative;
            }

            // If it will not fit into numeric(NUMERIC_MAX_PRECISION,0), overflow.
            if (dVal >= s_DMAX_NUME)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            double dInt = Math.Floor(dVal);
            double dFrac = dVal - dInt;

            _bPrec = s_NUMERIC_MAX_PRECISION;
            _bLen = 1;
            if (dInt > 0.0)
            {
                dVal = Math.Floor(dInt / s_DUINT_BASE);
                _data1 = (uint)(dInt - dVal * s_DUINT_BASE);
                dInt = dVal;

                if (dInt > 0.0)
                {
                    dVal = Math.Floor(dInt / s_DUINT_BASE);
                    _data2 = (uint)(dInt - dVal * s_DUINT_BASE);
                    dInt = dVal;
                    _bLen++;

                    if (dInt > 0.0)
                    {
                        dVal = Math.Floor(dInt / s_DUINT_BASE);
                        _data3 = (uint)(dInt - dVal * s_DUINT_BASE);
                        dInt = dVal;
                        _bLen++;

                        if (dInt > 0.0)
                        {
                            dVal = Math.Floor(dInt / s_DUINT_BASE);
                            _data4 = (uint)(dInt - dVal * s_DUINT_BASE);
                            _bLen++;
                        }
                    }
                }
            }

            uint ulLen, ulLenDelta;
            uint ulTemp;

            // Get size of the integer part
            ulLen = FZero() ? 0 : (uint)CalculatePrecision();
            Debug.Assert(ulLen <= s_NUMERIC_MAX_PRECISION, "ulLen <= NUMERIC_MAX_PRECISION", "");

            // If we got more than 17 decimal digits, zero lower ones.
            if (ulLen > s_DBL_DIG)
            {
                // Divide number by 10 while there are more then 17 digits
                uint ulWrk = ulLen - s_DBL_DIG;
                do
                {
                    ulTemp = DivByULong(10);
                    ulWrk--;
                }
                while (ulWrk > 0);
                ulWrk = ulLen - s_DBL_DIG;

                // Round, if necessary. # of digits can change. Cannot be overflow.
                if (ulTemp >= 5)
                {
                    AddULong(1);
                    ulLen = CalculatePrecision() + ulWrk;
                }

                // Multiply back
                do
                {
                    MultByULong(10);
                    ulWrk--;
                }
                while (ulWrk > 0);
            }

            _bScale = (byte)(ulLen < s_DBL_DIG ? s_DBL_DIG - ulLen : 0);
            _bPrec = (byte)(ulLen + _bScale);

            // Add meaningful fractional part - max 9 digits per iteration
            if (_bScale > 0)
            {
                ulLen = _bScale;
                do
                {
                    ulLenDelta = (ulLen >= 9) ? 9 : ulLen;

                    dFrac *= s_rgulShiftBase[(int)ulLenDelta - 1];
                    ulLen -= ulLenDelta;
                    MultByULong(s_rgulShiftBase[(int)ulLenDelta - 1]);
                    AddULong((uint)dFrac);
                    dFrac -= Math.Floor(dFrac);
                }
                while (ulLen > 0);
            }

            // Round, if necessary
            if (dFrac >= 0.5)
            {
                AddULong(1);
            }

            if (FZero())
                SetPositive();

            AssertValid();
        }

        private SqlDecimal(ReadOnlySpan<uint> rglData, byte bLen, byte bPrec, byte bScale, bool fPositive)
        {
            CheckValidPrecScale(bPrec, bScale);
            Debug.Assert(rglData.Length >= 4);

            _bLen = bLen;
            _bPrec = bPrec;
            _bScale = bScale;
            _data1 = rglData[0];
            _data2 = rglData[1];
            _data3 = rglData[2];
            _data4 = rglData[3];

            // set the null bit
            _bStatus = s_bNotNull;

            // set the sign bit
            if (!fPositive)
            {
                _bStatus |= s_bNegative;
            }

            // If result is -0, adjust sign to positive.
            if (FZero())
                SetPositive();
        }


        // INullable
        public bool IsNull
        {
            get { return (_bStatus & s_bNullMask) == s_bIsNull; }
        }

        public decimal Value
        {
            get { return ToDecimal(); }
        }

        public bool IsPositive
        {
            get
            {
                if (IsNull)
                    throw new SqlNullValueException();
                return (_bStatus & s_bSignMask) == s_bPositive;
            }
        }

        private void SetPositive()
        {
            Debug.Assert(!IsNull);
            _bStatus = (byte)(_bStatus & s_bReverseSignMask);
        }

        private void SetSignBit(bool fPositive)
        {
            Debug.Assert(!IsNull);
            _bStatus = (byte)(fPositive ? (_bStatus & s_bReverseSignMask) : (_bStatus | s_bNegative));
        }

        public byte Precision
        {
            get
            {
                if (IsNull)
                    throw new SqlNullValueException();
                return _bPrec;
            }
        }

        public byte Scale
        {
            get
            {
                if (IsNull)
                    throw new SqlNullValueException();
                return _bScale;
            }
        }

        public int[] Data
        {
            get
            {
                if (IsNull)
                    throw new SqlNullValueException();

                unchecked
                {
                    return new int[4] { (int)_data1, (int)_data2, (int)_data3, (int)_data4 };
                }
            }
        }

        public byte[] BinData
        {
            get
            {
                if (IsNull)
                    throw new SqlNullValueException();

                int data1 = (int)_data1;
                int data2 = (int)_data2;
                int data3 = (int)_data3;
                int data4 = (int)_data4;
                byte[] rgBinData = new byte[16];
                rgBinData[0] = (byte)(data1 & 0xff);
                data1 >>= 8;
                rgBinData[1] = (byte)(data1 & 0xff);
                data1 >>= 8;
                rgBinData[2] = (byte)(data1 & 0xff);
                data1 >>= 8;
                rgBinData[3] = (byte)(data1 & 0xff);
                rgBinData[4] = (byte)(data2 & 0xff);
                data2 >>= 8;
                rgBinData[5] = (byte)(data2 & 0xff);
                data2 >>= 8;
                rgBinData[6] = (byte)(data2 & 0xff);
                data2 >>= 8;
                rgBinData[7] = (byte)(data2 & 0xff);
                rgBinData[8] = (byte)(data3 & 0xff);
                data3 >>= 8;
                rgBinData[9] = (byte)(data3 & 0xff);
                data3 >>= 8;
                rgBinData[10] = (byte)(data3 & 0xff);
                data3 >>= 8;
                rgBinData[11] = (byte)(data3 & 0xff);
                rgBinData[12] = (byte)(data4 & 0xff);
                data4 >>= 8;
                rgBinData[13] = (byte)(data4 & 0xff);
                data4 >>= 8;
                rgBinData[14] = (byte)(data4 & 0xff);
                data4 >>= 8;
                rgBinData[15] = (byte)(data4 & 0xff);

                return rgBinData;
            }
        }

        public override string ToString()
        {
            if (IsNull)
                return SQLResource.NullString;
            AssertValid();

            // Make local copy of data to avoid modifying input.
            Span<uint> rgulNumeric = stackalloc uint[4] { _data1, _data2, _data3, _data4 };
            int culLen = _bLen;
            Span<char> pszTmp = stackalloc char[s_NUMERIC_MAX_PRECISION + 1];   //Local Character buffer to hold
            pszTmp.Clear();                                                     //the decimal digits, from the
                                                                                //lowest significant to highest significant

            int iDigits = 0; //Number of significant digits
            uint ulRem; //Remainder of a division by x_ulBase10, i.e.,least significant digit

            // Build the final numeric string by inserting the sign, reversing
            // the order and inserting the decimal number at the correct position

            //Retrieve each digit from the lowest significant digit
            while (culLen > 1 || rgulNumeric[0] != 0)
            {
                MpDiv1(rgulNumeric, ref culLen, s_ulBase10, out ulRem);
                //modulo x_ulBase10 is the lowest significant digit
                pszTmp[iDigits++] = ChFromDigit(ulRem);
            }

            // if scale of the number has not been
            // reached pad remaining number with zeros.
            while (iDigits <= _bScale)
            {
                pszTmp[iDigits++] = ChFromDigit(0);
            }


            int uiResultLen = 0;
            int iCurChar = 0;
            char[] szResult;

            // Increment the result length if scale > 0 (need to add '.')
            if (_bScale > 0)
            {
                uiResultLen = 1;
            }

            if (IsPositive)
            {
                szResult = new char[uiResultLen + iDigits];
            }
            else
            {
                // Increment the result length if negative (need to add '-')
                szResult = new char[uiResultLen + iDigits + 1];
                szResult[iCurChar++] = '-';
            }

            while (iDigits > 0)
            {
                if (iDigits-- == _bScale)
                    szResult[iCurChar++] = '.';

                szResult[iCurChar++] = pszTmp[iDigits];
            }

            AssertValid();

            return new string(szResult);
        }

        public static SqlDecimal Parse(string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            if (s == SQLResource.NullString)
                return SqlDecimal.Null;

            SqlDecimal snResult = SqlDecimal.Null;

            char[] rgwchStr = s.ToCharArray();
            int cwchStr = rgwchStr.Length;

            int iData;            //index to string
            char usChar;            //current value in string
            int lDecPnt = -1;    //position of decimal point in string
            int iCurChar = 0;

            //Must initialize precision and scale to valid values
            snResult._bPrec = 1;
            snResult._bScale = 0;

            //Must initialize *this to zero
            snResult.SetToZero();

            // Trim trailing blanks.
            while (cwchStr != 0 && rgwchStr[cwchStr - 1] == ' ')
                cwchStr--;

            // If string contains only spaces, stop
            if (cwchStr == 0)
                throw new FormatException(SQLResource.FormatMessage);

            // Trim leading blanks.
            while (rgwchStr[iCurChar] == ' ')
            {
                iCurChar++;
                cwchStr--;
            }

            // Get sign for numeric value.
            if (rgwchStr[iCurChar] == '-')
            {
                snResult.SetSignBit(false);
                iCurChar++;
                cwchStr--;
            }
            else
            {
                snResult.SetSignBit(true);
                if (rgwchStr[iCurChar] == '+')
                {
                    iCurChar++;
                    cwchStr--;
                }
            }

            // Hack: Check for "0.". If so, replace by ".0".
            while ((cwchStr > 2) && (rgwchStr[iCurChar] == '0'))
            {
                iCurChar++;
                cwchStr--;
            }
            if (2 == cwchStr && '0' == rgwchStr[iCurChar] && '.' == rgwchStr[iCurChar + 1])
            {
                rgwchStr[iCurChar] = '.';
                rgwchStr[iCurChar + 1] = '0';
            }

            // Invalid string?
            if (cwchStr == 0 || cwchStr > s_NUMERIC_MAX_PRECISION + 1)
                throw new FormatException(SQLResource.FormatMessage);

            // Trim leading zeros.  (There shouldn't be any except for floats
            // less than 1.  e.g.  0.01)
            while ((cwchStr > 1) && (rgwchStr[iCurChar] == '0'))
            {
                iCurChar++;
                cwchStr--;
            }

            // Convert string to numeric value by looping through input string.
            for (iData = 0; iData < cwchStr; iData++)
            {
                usChar = rgwchStr[iCurChar];
                iCurChar++;

                if (char.IsAsciiDigit(usChar))
                {
                    usChar -= '0';
                }
                else if (usChar == '.' && lDecPnt < 0)
                {
                    lDecPnt = iData;
                    continue;
                }
                else
                {
                    throw new FormatException(SQLResource.FormatMessage);
                }

                snResult.MultByULong(s_ulBase10);
                snResult.AddULong(usChar);
            }

            // Save precision and scale.
            if (lDecPnt < 0)
            {
                snResult._bPrec = (byte)iData;
                snResult._bScale = 0;
            }
            else
            {
                snResult._bPrec = (byte)(iData - 1);
                snResult._bScale = (byte)(snResult._bPrec - lDecPnt);
            }

            //Check for overflow condition
            if (snResult._bPrec > s_NUMERIC_MAX_PRECISION)
                throw new FormatException(SQLResource.FormatMessage);

            // Check for invalid precision for numeric value.
            // e.g., when string is ".", precision will be 0
            if (snResult._bPrec == 0)
                throw new FormatException(SQLResource.FormatMessage);

            // If result is -0, adjust sign to positive.
            if (snResult.FZero())
                snResult.SetPositive();

            snResult.AssertValid();

            return snResult;
        }

        public double ToDouble()
        {
            if (IsNull)
                throw new SqlNullValueException();

            double dRet;

            dRet = _data4;
            dRet = dRet * s_lInt32Base + _data3;
            dRet = dRet * s_lInt32Base + _data2;
            dRet = dRet * s_lInt32Base + _data1;

            dRet /= System.Math.Pow(10.0, _bScale);

            return IsPositive ? dRet : -dRet;
        }

        private decimal ToDecimal()
        {
            if (IsNull)
                throw new SqlNullValueException();

            if ((int)_data4 != 0 || _bScale > 28)
                throw new OverflowException(SQLResource.ConversionOverflowMessage);

            unchecked
            {
                return new decimal((int)_data1, (int)_data2, (int)_data3, !IsPositive, _bScale);
            }
        }

        [CLSCompliant(false)]
        public int WriteTdsValue(Span<uint> destination)
        {
            if (IsNull)
            {
                throw new SqlNullValueException();
            }

            if (destination.Length < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            destination[0] = _data1;
            destination[1] = _data2;
            destination[2] = _data3;
            destination[3] = _data4;
            return 4;
        }

        // Implicit conversion from Decimal to SqlDecimal
        public static implicit operator SqlDecimal(decimal x)
        {
            return new SqlDecimal(x);
        }

        // Explicit conversion from Double to SqlDecimal
        public static explicit operator SqlDecimal(double x)
        {
            return new SqlDecimal(x);
        }

        // Implicit conversion from long to SqlDecimal
        public static implicit operator SqlDecimal(long x)
        {
            return new SqlDecimal(new decimal(x));
        }

        // Explicit conversion from SqlDecimal to Decimal. Throw exception if x is Null.
        public static explicit operator decimal(SqlDecimal x)
        {
            return x.Value;
        }


        // Unary operators
        public static SqlDecimal operator -(SqlDecimal x)
        {
            if (x.IsNull)
                return Null;
            else
            {
                SqlDecimal s = x;
                if (s.FZero())
                    s.SetPositive();
                else
                    s.SetSignBit(!s.IsPositive);
                return s;
            }
        }


        // Binary operators

        // Arithmetic operators
        public static SqlDecimal operator +(SqlDecimal x, SqlDecimal y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            ulong dwlAccum;           //accumulated sum
            bool fMySignPos;         //sign of x was positive at start
            bool fOpSignPos;         // sign of y positive at start
            bool fResSignPos = true; //sign of result should be positive
            int MyScale;    //scale of x
            int OpScale;    //scale of y
            int ResScale;   //scale of result
            int ResPrec;    //precision of result
            int ResInteger; //number of digits for the integer part of result
            int culOp1;     //# of UI4s in x
            int culOp2;     //# of UI4s in y
            int iulData;    //which UI4 we are operating on in x, y
            byte bLen;       // length for the result


            x.AssertValid();
            y.AssertValid();

            fMySignPos = x.IsPositive;
            fOpSignPos = y.IsPositive;

            //result scale = max(s1,s2)
            //result precision = max(s1,s2) + max(p1-s1,p2-s2)
            MyScale = x._bScale;
            OpScale = y._bScale;

            // Calculate the integer part of the result.
            ResInteger = Math.Max(x._bPrec - MyScale, y._bPrec - OpScale);
            Debug.Assert(ResInteger <= MaxPrecision);

            // Calculate the scale of the result.
            ResScale = Math.Max(MyScale, OpScale);
            Debug.Assert(ResScale <= MaxScale);

            // Calculate the precision of the result.
            // Add 1 for final carry.
            ResPrec = ResInteger + ResScale + 1;
            ResPrec = Math.Min(MaxPrecision, ResPrec);

            // If precision adjusted, scale is reduced to keep the integer part untruncated.
            // But discard the extra carry, only keep the integer part as ResInteger, not ResInteger + 1.
            Debug.Assert(ResPrec - ResInteger >= 0);
            if (ResPrec - ResInteger < ResScale)
                ResScale = ResPrec - ResInteger;

            // Adjust both operands to be the same scale as ResScale.
            if (MyScale != ResScale)
                x.AdjustScale(ResScale - MyScale, true);

            if (OpScale != ResScale)
                y.AdjustScale(ResScale - OpScale, true);

            // When sign of first operand is negative
            // negate all operands including result.
            if (!fMySignPos)
            {
                fMySignPos = !fMySignPos;
                fOpSignPos = !fOpSignPos;
                fResSignPos = !fResSignPos;
            }

            // Initialize operand lengths and pointer.
            culOp1 = x._bLen;
            culOp2 = y._bLen;

            Span<uint> rglData1 = stackalloc uint[4] { x._data1, x._data2, x._data3, x._data4 };
            Span<uint> rglData2 = stackalloc uint[4] { y._data1, y._data2, y._data3, y._data4 };

            if (fOpSignPos)
            {
                dwlAccum = 0;

                // CONSIDER: Call AddUlong when possible

                // Loop through UI4s adding operands and putting result in *this
                // of the operands and put result in *this
                for (iulData = 0; iulData < culOp1 || iulData < culOp2; iulData++)
                {
                    // None of these DWORDLONG additions can overflow, as dwlAccum comes in < x_lInt32Base
                    if (iulData < culOp1)
                        dwlAccum += rglData1[iulData];
                    if (iulData < culOp2)
                        dwlAccum += rglData2[iulData];

                    rglData1[iulData] = unchecked((uint)dwlAccum); // equiv to mod x_lInt32Base
                    dwlAccum >>= 32; // equiv to div x_lInt32Base
                }

                //If carry
                if (dwlAccum != 0)
                {
                    Debug.Assert(dwlAccum < s_ulInt32Base);

                    //Either overflowed
                    if (iulData == s_cNumeMax)
                        throw new OverflowException(SQLResource.ArithOverflowMessage);

                    // Or extended length
                    rglData1[iulData] = (uint)dwlAccum;
                    iulData++;
                }

                // Set result length
                bLen = (byte)iulData;
            }
            else
            {
                int iulLastNonZero = 0; // The last nonzero UI

                // When second operand is negative, switch operands
                // if operand2 is greater than operand1
                if (x.LAbsCmp(y) < 0)
                {
                    fResSignPos = !fResSignPos;
                    Span<uint> rguiTemp = rglData2;
                    rglData2 = rglData1;
                    rglData1 = rguiTemp;
                    culOp1 = culOp2;
                    culOp2 = x._bLen;
                }

                dwlAccum = s_ulInt32Base;
                for (iulData = 0; iulData < culOp1 || iulData < culOp2; iulData++)
                {
                    if (iulData < culOp1)
                        dwlAccum += rglData1[iulData];
                    if (iulData < culOp2)
                        dwlAccum -= rglData2[iulData];

                    rglData1[iulData] = unchecked((uint)dwlAccum); // equiv to mod BaseUI4
                    if (rglData1[iulData] != 0)
                        iulLastNonZero = iulData;
                    dwlAccum >>= 32; // equiv to /= BaseUI4
                    dwlAccum += s_ulInt32BaseForMod; // equiv to BaseUI4 - 1
                }
                // Set length based on highest non-zero ULONG
                bLen = (byte)(iulLastNonZero + 1);
            }

            SqlDecimal ret = new SqlDecimal(rglData1, bLen, (byte)ResPrec, (byte)ResScale, fResSignPos);

            if (ret.FGt10_38() || ret.CalculatePrecision() > s_NUMERIC_MAX_PRECISION)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            if (ret.FZero())
                ret.SetPositive();

            ret.AssertValid();

            return ret;
        }

        public static SqlDecimal operator -(SqlDecimal x, SqlDecimal y)
        {
            return x + (-y);
        }

        //    MultNm()
        //
        //    Multiply two numerics.
        //
        //  Parameters:
        //        x    - IN Multiplier
        //        y    - IN Multiplicand
        //
        //    Result scale and precision(same as in SQL Server Manual and Hydra):
        //        scale = s1 + s2
        //        precision = s1 + s2 + (p1 - s1) + (p2 - s2) + 1
        //
        //    Overflow Rules:
        //        If scale is greater than NUMERIC_MAX_PRECISION it is set to
        //    NUMERIC_MAX_PRECISION.  If precision is greater than NUMERIC_MAX_PRECISION
        //    it is set to NUMERIC_MAX_PRECISION, then scale is reduced to keep the
        //    integer part untruncated but keeping a minimum value of x_cNumeDivScaleMin.
        //    For example, if using the above formula, the resulting precision is 46 and
        //    scale is 10, the precision will be reduced to 38. To keep the integral part
        //    untruncated the scale needs be reduced to 2, but since x_cNumeDivScaleMin
        //    is set to 6 currently, resulting scale will be 6.
        //        O_OVERFLOW is returned only if the actual precision is greater than
        //     NUMERIC_MAX_PRECISION or the actual length is greater than x_cbNumeBuf.
        //
        //    Algorithm:
        //        Starting from the lowest significant UI4, for each UI4 of the multiplier
        //    iterate through the UI4s of the multiplicand starting from
        //    the least significant UI4s, multiply the multiplier UI4 with
        //    multiplicand UI4, update the result buffer with the product modulo
        //    x_dwlBaseUI4 at the same index as the multiplicand, and carry the quotient to
        //    add to the next multiplicand UI4.  Until the end of the multiplier data
        //    array is reached.
        //
        public static SqlDecimal operator *(SqlDecimal x, SqlDecimal y)
        {
            x.AssertValid();
            y.AssertValid();

            if (x.IsNull || y.IsNull)
                return Null;

            //Implementation:
            //        I) Figure result scale,prec
            //        II) Perform mult.
            //        III) Adjust product to result scale,prec

            // Local variables for actual multiplication
            int iulPlier;           //index of UI4 in the Multiplier
            uint ulPlier;            //current mutiplier UI4
            ulong dwlAccum;           //accumulated sum
            ulong dwlNextAccum;       //overflow of accumulated sum
            int culCand = y._bLen; //length of multiplicand in UI4s

            //Local variables to track scale,precision
            int ActualScale;                    // Scale after mult done
            int ResScale;                       // Final scale we will force result to
            int ResPrec;                        // Final precision we will force result to
            int ResInteger;                     // # of digits in integer part of result (prec-scale)
            int lScaleAdjust;   //How much result scale will be adjusted
            bool fResPositive;  // Result sign

            SqlDecimal ret;


            //I) Figure result prec,scale
            ActualScale = x._bScale + y._bScale;
            ResScale = ActualScale;
            ResInteger = (x._bPrec - x._bScale) + (y._bPrec - y._bScale) + 1;

            //result precision = s1 + s2 + (p1 - s1) + (p2 - s2) + 1
            ResPrec = ResScale + ResInteger;

            // Downward adjust res prec,scale if either larger than NUMERIC_MAX_PRECISION
            if (ResPrec > s_NUMERIC_MAX_PRECISION)
                ResPrec = s_NUMERIC_MAX_PRECISION;
            if (ResScale > s_NUMERIC_MAX_PRECISION)
                ResScale = s_NUMERIC_MAX_PRECISION;

            //
            // It is possible when two large numbers are being multiplied the scale
            // can be reduced to 0 to keep data untruncated; the fix here is to
            // preserve a minimum scale of 6.
            //
            // If overflow, reduce the scale to avoid truncation of data
            ResScale = Math.Min((ResPrec - ResInteger), ResScale);
            // But keep a minimum scale of NUMERIC_MIN_DVSCALE
            ResScale = Math.Max(ResScale, Math.Min(ActualScale, s_cNumeDivScaleMin));

            lScaleAdjust = ResScale - ActualScale;

            fResPositive = (x.IsPositive == y.IsPositive); //positive if both signs same.

            // II) Perform multiplication

            ReadOnlySpan<uint> rglData1 = stackalloc uint[4] { x._data1, x._data2, x._data3, x._data4 };
            ReadOnlySpan<uint> rglData2 = stackalloc uint[4] { y._data1, y._data2, y._data3, y._data4 };

            //Local buffer to hold the result of multiplication.
            //Longer than CReNumeBuf because full precision of multiplication is carried out
            const int x_culNumeMultRes = 9;       // Maximum # UI4s in result buffer in multiplication
            Span<uint> rgulRes = stackalloc uint[x_culNumeMultRes]; //new [] are already initialized to zero
            rgulRes.Clear(); // but spans in core libraries are not for performance reasons so clear it
            int culRes;             // # of UI4s in result
            int idRes = 0;

            //Iterate over the bytes of multiplier
            for (iulPlier = 0; iulPlier < x._bLen; iulPlier++)
            {
                ulPlier = rglData1[iulPlier];
                dwlAccum = 0;

                //Multiply each UI4 of multiCand by ulPliear and accumulate into result buffer

                // Start on correct place in result
                idRes = iulPlier;

                for (int iulCand = 0; iulCand < culCand; iulCand++)
                {
                    // dwlAccum = dwlAccum + rgulRes[idRes] + ulPlier*rglData2[iulCand]
                    //        use dwlNextAccum to detect overflow of DWORDLONG
                    dwlNextAccum = dwlAccum + rgulRes[idRes];
                    ulong ulTemp = rglData2[iulCand];
                    dwlAccum = ulPlier * ulTemp;
                    dwlAccum += dwlNextAccum;
                    if (dwlAccum < dwlNextAccum) // indicates dwl addition overflowed
                        dwlNextAccum = s_ulInt32Base; // = maxUI64/x_dwlBaseUI4
                    else
                        dwlNextAccum = 0;

                    // Update result and accum
                    rgulRes[idRes++] = unchecked((uint)dwlAccum); // & x_ulInt32BaseForMod); // equiv to mod x_lInt32Base
                    dwlAccum = (dwlAccum >> 32) + dwlNextAccum; // equiv to div BaseUI4 + dwlNAccum

                    // dwlNextAccum can't overflow next iteration
                    Debug.Assert(dwlAccum < s_ulInt32Base * 2);
                }

                Debug.Assert(dwlAccum < s_ulInt32Base); // can never final accum > 1 more UI4
                if (dwlAccum != 0)
                    rgulRes[idRes++] = (uint)dwlAccum;
            }
            // Skip leading 0s (may exist if we are multiplying by 0)
            for (; (rgulRes[idRes] == 0) && (idRes > 0); idRes--)
                ;
            // Calculate actual result length
            culRes = idRes + 1;

            // III) Adjust precision,scale to result prec,scale
            if (lScaleAdjust != 0)
            {
                // If need to decrease scale
                if (lScaleAdjust < 0)
                {
                    Debug.Assert(s_NUMERIC_MAX_PRECISION == ResPrec);

                    // have to adjust - might yet end up fitting.
                    // Cannot call AdjustScale - number cannot fit in a numeric, so
                    // have to duplicate code here

                    uint ulRem;          //Remainder when downshifting
                    uint ulShiftBase;    //What to multiply by to effect scale adjust

                    do
                    {
                        if (lScaleAdjust <= -9)
                        {
                            ulShiftBase = s_rgulShiftBase[8];
                            lScaleAdjust += 9;
                        }
                        else
                        {
                            ulShiftBase = s_rgulShiftBase[-lScaleAdjust - 1];
                            lScaleAdjust = 0;
                        }
                        MpDiv1(rgulRes, ref culRes, ulShiftBase, out ulRem);
                    }
                    while (lScaleAdjust != 0);

                    // Still do not fit?
                    if (culRes > s_cNumeMax)
                        throw new OverflowException(SQLResource.ArithOverflowMessage);

                    for (idRes = culRes; idRes < s_cNumeMax; idRes++)
                        rgulRes[idRes] = 0;
                    ret = new SqlDecimal(rgulRes, (byte)culRes, (byte)ResPrec, (byte)ResScale, fResPositive);

                    // Is it greater than 10**38?
                    if (ret.FGt10_38())
                        throw new OverflowException(SQLResource.ArithOverflowMessage);

                    ret.AssertValid();

                    // If remainder is 5 or above, increment/decrement by 1.
                    if (ulRem >= ulShiftBase / 2)
                        ret.AddULong(1);
                    // After adjusting, if the result is 0 and remainder is less than 5,
                    // set the sign to be positive
                    if (ret.FZero())
                        ret.SetPositive();

                    return ret;
                }

                // Otherwise call AdjustScale
                if (culRes > s_cNumeMax)    // Do not fit now, so will not fit after asjustement
                    throw new OverflowException(SQLResource.ArithOverflowMessage);
                // NOTE: Have not check for value in the range (10**38..2**128),
                // as we'll call AdjustScale with positive argument, and it'll
                // return "normal" overflow

                for (idRes = culRes; idRes < s_cNumeMax; idRes++)
                    rgulRes[idRes] = 0;
                ret = new SqlDecimal(rgulRes, (byte)culRes, (byte)ResPrec, (byte)ActualScale, fResPositive);

                if (ret.FZero())
                    ret.SetPositive();

                ret.AssertValid();

                ret.AdjustScale(lScaleAdjust, true);

                return ret;
            }
            else
            {
                if (culRes > s_cNumeMax)
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                for (idRes = culRes; idRes < s_cNumeMax; idRes++)
                    rgulRes[idRes] = 0;
                ret = new SqlDecimal(rgulRes, (byte)culRes, (byte)ResPrec, (byte)ResScale, fResPositive);

                // Is it greater than 10**38?
                if (ret.FGt10_38())
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                if (ret.FZero())
                    ret.SetPositive();

                ret.AssertValid();

                return ret;
            }
        }

        //-----------------------------------------------------------
        //DivNm():
        //  Divide numeric by numeric.
        //    The Quotient will be returned in *this
        //
        //Result scale&precision:
        //    NOTE: same as in Hydra but different from SQL Server Manual,
        //            where scale = max(s1+p2-s2+1,x_cNumeDivScaleMin)):
        //        scale = max(s1 + p2 + 1, x_cNumeDivScaleMin);
        //        precision = max(s1 + p2 + 1, x_cNumeDivScaleMin) + p1 + p2 + 1;
        //
        //Overflow Rules:
        //        If scale is greater than NUMERIC_MAX_PRECISION it is set to
        //    NUMERIC_MAX_PRECISION.  If precision is greater than NUMERIC_MAX_PRECISION
        //    it's set to NUMERIC_MAX_PRECISION, then scale is reduced to keep the
        //    integer part untruncated but keeping a minimum value of x_cNumeDivScaleMin.
        //    For example, if using the above formula, the resulting precision is 46 and
        //    scale is 10, the precision will be reduced to 38, to keep the integral part
        //    untruncated the scale needs be recuded to 2, but since x_cNumeDivScaleMin
        //    is set to 6 currently, resulting scale will be 6.
        //        OverflowException is thrown only if the actual precision is greater than
        //    NUMERIC_MAX_PRECISION or actual length is greater than x_cbNumeBuf
        //
        //Algorithm
        //  Call general purpose arbitrary precision division routine with scale = 0.
        //    Scale,prec adjusted later.
        //
        public static SqlDecimal operator /(SqlDecimal x, SqlDecimal y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            x.AssertValid();
            y.AssertValid();

            // Variables for figuring prec,scale
            int bScaleD;            // Input Scale of dividend (output scale of remainder)
            int bPrecD;             // Input Prec of dividend (output prec of remainder)
            int ResScale;           // Final scale we will force quotient to
            int ResPrec;            // Final precision we will force quotient to
            int ResInteger;         // # of digits in integer part of result (prec-scale)
            int MinScale;           // Temp to help compute ResScale
            int lScaleAdjust;       // How much result scale will be adjusted
            bool fResSignPos;       // sign of result

            // Steps:
            //    1) Figure result prec,scale; adjust scale of dividend
            //    2) Compute result remainder/quotient in 0 scale numbers
            //    3) Set result prec,scale and adjust as necessary

            // 0) Check for Div by 0
            if (y.FZero())
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);

            // 1) Figure out result prec,scale,sign..
            fResSignPos = (x.IsPositive == y.IsPositive); //sign of result

            //scale = max(s1 + p2 + 1, x_cNumeDivScaleMin);
            //precision = max(s1 + p2 + 1, x_cNumeDivScaleMin) + p1 + p2 + 1;
            //For backward compatibility, use exactly the same scheme as in Hydra
            bScaleD = x._bScale;
            bPrecD = x._bPrec;
            ResScale = Math.Max(x._bScale + y._bPrec + 1, s_cNumeDivScaleMin);
            ResInteger = x._bPrec - x._bScale + y._bScale;
            ResPrec = ResScale + x._bPrec + y._bPrec + 1;
            MinScale = Math.Min(ResScale, s_cNumeDivScaleMin);

            ResInteger = Math.Min(ResInteger, s_NUMERIC_MAX_PRECISION);
            ResPrec = ResInteger + ResScale;

            if (ResPrec > s_NUMERIC_MAX_PRECISION)
                ResPrec = s_NUMERIC_MAX_PRECISION;

            // If overflow, reduce the scale to avoid truncation of data
            ResScale = Math.Min((ResPrec - ResInteger), ResScale);
            ResScale = Math.Max(ResScale, MinScale);

            //Adjust the scale of the dividend
            lScaleAdjust = ResScale - x._bScale + y._bScale;
            x.AdjustScale(lScaleAdjust, true);

            // Step2: Actual Computation

            Span<uint> rgulData1 = stackalloc uint[4] { x._data1, x._data2, x._data3, x._data4 };
            Span<uint> rgulData2 = stackalloc uint[4] { y._data1, y._data2, y._data3, y._data4 };

            // Buffers for arbitrary precision divide
            Span<uint> rgulR = stackalloc uint[s_cNumeMax + 1];
            Span<uint> rgulQ = stackalloc uint[s_cNumeMax];
            rgulR.Clear();
            rgulQ.Clear();
            // # of ULONGs in result
            int culQ, culR;

            // Divide mantissas. V is not zero - already checked.
            // Cannot overflow, as Q <= U, R <= V. (and both are positive)
            MpDiv(rgulData1, x._bLen, rgulData2, y._bLen, rgulQ, out culQ, rgulR, out culR);

            // Construct the result from Q
            ZeroToMaxLen(rgulQ, culQ);
            SqlDecimal ret = new SqlDecimal(rgulQ, (byte)culQ, (byte)ResPrec, (byte)ResScale, fResSignPos);

            if (ret.FZero())
                ret.SetPositive();

            ret.AssertValid();

            return ret;
        }



        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlDecimal
        public static explicit operator SqlDecimal(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlDecimal(x.ByteValue);
        }

        // Implicit conversion from SqlByte to SqlDecimal
        public static implicit operator SqlDecimal(SqlByte x)
        {
            return x.IsNull ? Null : new SqlDecimal(x.Value);
        }

        // Implicit conversion from SqlInt16 to SqlDecimal
        public static implicit operator SqlDecimal(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlDecimal(x.Value);
        }

        // Implicit conversion from SqlInt32 to SqlDecimal
        public static implicit operator SqlDecimal(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlDecimal(x.Value);
        }

        // Implicit conversion from SqlInt64 to SqlDecimal
        public static implicit operator SqlDecimal(SqlInt64 x)
        {
            return x.IsNull ? Null : new SqlDecimal(x.Value);
        }

        // Implicit conversion from SqlMoney to SqlDecimal
        public static implicit operator SqlDecimal(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlDecimal(x.ToDecimal());
        }


        // Explicit conversions

        // Explicit conversion from SqlSingle to SqlDecimal
        public static explicit operator SqlDecimal(SqlSingle x)
        {
            return x.IsNull ? SqlDecimal.Null : new SqlDecimal(x.Value);
        }

        // Explicit conversion from SqlDouble to SqlDecimal
        public static explicit operator SqlDecimal(SqlDouble x)
        {
            return x.IsNull ? SqlDecimal.Null : new SqlDecimal(x.Value);
        }

        // Explicit conversion from SqlString to SqlDecimal
        // Throws FormatException or OverflowException if necessary.
        public static explicit operator SqlDecimal(SqlString x)
        {
            return x.IsNull ? Null : SqlDecimal.Parse(x.Value);
        }

        // private methods

        //----------------------------------------------------------------------
        // Is this RE numeric valid?
        [Conditional("DEBUG")]
        private void AssertValid()
        {
            if (IsNull)
                return;

            // Scale,Prec in range
            Debug.Assert(_bScale <= s_NUMERIC_MAX_PRECISION, "m_bScale <= NUMERIC_MAX_PRECISION", "In AssertValid");
            Debug.Assert(_bScale <= _bPrec, "m_bScale <= m_bPrec", "In AssertValid");
            Debug.Assert(_bScale >= 0, "m_bScale >= 0", "In AssertValid");
            Debug.Assert(_bPrec > 0, "m_bPrec > 0", "In AssertValid");

            Debug.Assert(CLenFromPrec(_bPrec) >= _bLen, "CLenFromPrec(m_bPrec) >= m_bLen", "In AssertValid");
            Debug.Assert(_bLen <= s_cNumeMax, "m_bLen <= x_cNumeMax", "In AssertValid");

            ReadOnlySpan<uint> rglData = stackalloc uint[4] { _data1, _data2, _data3, _data4 };

            // highest UI4 is non-0 unless value "zero"
            if (rglData[_bLen - 1] == 0)
            {
                Debug.Assert(_bLen == 1, "m_bLen == 1", "In AssertValid");
            }

            // All UI4s from length to end are 0
            for (int iulData = _bLen; iulData < s_cNumeMax; iulData++)
                Debug.Assert(rglData[iulData] == 0, "rglData[iulData] == 0", "In AssertValid");
        }
        /*
                // print the data members
                [System.Diagnostics.Conditional("DEBUG")]
                private void Print() {
                    if (IsNull) {
                        Debug.WriteLine("Numeric: Null");
                        return;
                    }
                    Debug.WriteLine("Numeric: data - " + m_data4.ToString() + ", " + m_data3.ToString() + ", " +
                                      m_data2.ToString() + ", " + m_data1.ToString());
                    Debug.WriteLine("\tlen = " + m_bLen.ToString() + ", Prec = " + m_bPrec.ToString() +
                                      ", Scale = " + m_bScale.ToString() + ", Sign = " + IsPositive.ToString());
                }

                [System.Diagnostics.Conditional("DEBUG")]
                private void Print(String s) {
                    Debug.WriteLine("*** " + s + " ***");
                    if (IsNull) {
                        Debug.WriteLine("Numeric: Null");
                        return;
                    }
                    Debug.WriteLine("Numeric: data - " + m_data4.ToString() + ", " + m_data3.ToString() + ", " +
                                      m_data2.ToString() + ", " + m_data1.ToString());
                    Debug.WriteLine("\tlen = " + m_bLen.ToString() + ", Prec = " + m_bPrec.ToString() +
                                      ", Scale = " + m_bScale.ToString() + ", Sign = " + IsPositive.ToString());
                }
        */
        // Set all extra uints to zero
        private static void ZeroToMaxLen(Span<uint> rgulData, int cUI4sCur)
        {
            Debug.Assert(rgulData.Length == s_cNumeMax, "rgulData.Length == x_cNumeMax", "Invalid array length");

            switch (cUI4sCur)
            {
                case 1:
                    rgulData[1] = rgulData[2] = rgulData[3] = 0;
                    break;

                case 2:
                    rgulData[2] = rgulData[3] = 0;
                    break;

                case 3:
                    rgulData[3] = 0;
                    break;
            }
        }

        // Set all extra uints to zero
        /*
        private void ZeroToMaxLen(int cUI4sCur) {
            switch (cUI4sCur) {
                case 1:
                    m_data2 = m_data3 = m_data4 = x_uiZero;
                    break;

                case 2:
                    m_data3 = m_data4 = x_uiZero;
                    break;

                case 3:
                    m_data4 = x_uiZero;
                    break;
            }
        }
        */

        //Determine the number of uints needed for a numeric given a precision
        //Precision        Length
        //    0            invalid
        //    1-9            1
        //    10-19          2
        //    20-28          3
        //    29-38          4
        private static ReadOnlySpan<byte> RgCLenFromPrec => new byte[] // rely on C# compiler optimization to eliminate allocation
        {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4
        };

        private static byte CLenFromPrec(byte bPrec)
        {
            Debug.Assert(bPrec <= MaxPrecision && bPrec > 0, "bPrec <= MaxPrecision && bPrec > 0",
                           "Invalid numeric precision");
            return RgCLenFromPrec[bPrec - 1];
        }

        // check whether is zero
        private bool FZero()
        {
            return (_data1 == 0) && (_bLen <= 1);
        }

        // Find the case where we overflowed 10**38, but not 2**128
        private bool FGt10_38()
        {
            return _data4 >= 0x4b3b4ca8L && _bLen == 4 &&
            ((_data4 > 0x4b3b4ca8L) || (_data3 > 0x5a86c47aL) ||
             (_data3 == 0x5a86c47aL) && (_data2 >= 0x098a2240L));
        }

        private static bool FGt10_38(Span<uint> rglData)
        {
            Debug.Assert(rglData.Length == 4, "rglData.Length == 4", $"Wrong array length: {rglData.Length}");

            return rglData[3] >= 0x4b3b4ca8L &&
            ((rglData[3] > 0x4b3b4ca8L) || (rglData[2] > 0x5a86c47aL) ||
             (rglData[2] == 0x5a86c47aL) && (rglData[1] >= 0x098a2240L));
        }


        // Powers of ten (used by BGetPrecI4,BGetPrecI8)
        private const uint s_ulT1 = 10;
        private const uint s_ulT2 = 100;
        private const uint s_ulT3 = 1000;
        private const uint s_ulT4 = 10000;
        private const uint s_ulT5 = 100000;
        private const uint s_ulT6 = 1000000;
        private const uint s_ulT7 = 10000000;
        private const uint s_ulT8 = 100000000;
        private const uint s_ulT9 = 1000000000;
        private const ulong s_dwlT10 = 10000000000;
        private const ulong s_dwlT11 = 100000000000;
        private const ulong s_dwlT12 = 1000000000000;
        private const ulong s_dwlT13 = 10000000000000;
        private const ulong s_dwlT14 = 100000000000000;
        private const ulong s_dwlT15 = 1000000000000000;
        private const ulong s_dwlT16 = 10000000000000000;
        private const ulong s_dwlT17 = 100000000000000000;
        private const ulong s_dwlT18 = 1000000000000000000;
        private const ulong s_dwlT19 = 10000000000000000000;

        //------------------------------------------------------------------
        //BGetPrecI4
        //    Return the precision(number of significant digits) of a integer
        private static byte BGetPrecUI4(uint value)
        {
            int ret;

            // Now do the (almost) binary search
            if (value < s_ulT4)
            {
                if (value < s_ulT2)
                    ret = value >= s_ulT1 ? 2 : 1;
                else
                    ret = value >= s_ulT3 ? 4 : 3;
            }
            else if (value < s_ulT8)
            {
                if (value < s_ulT6)
                    ret = value >= s_ulT5 ? 6 : 5;
                else
                    ret = value >= s_ulT7 ? 8 : 7;
            }
            else
                ret = value >= s_ulT9 ? 10 : 9;

            return (byte)ret;
        }

        //------------------------------------------------------------------
        //BGetPrecI8
        //    Return the precision (number of significant digits) of an Int8
        private static byte BGetPrecUI8(uint ulU0, uint ulU1)
        {
            ulong dwlVal = ulU0 + (((ulong)ulU1) << 32);
            return BGetPrecUI8(dwlVal);
        }

        private static byte BGetPrecUI8(ulong dwlVal)
        {
            int ret;

            // Now do the (almost) binary search
            if (dwlVal < s_ulT8)
            {
                uint ulVal = (uint)dwlVal;

                if (ulVal < s_ulT4)
                {
                    if (ulVal < s_ulT2)
                        ret = (ulVal >= s_ulT1) ? 2 : 1;
                    else
                        ret = (ulVal >= s_ulT3) ? 4 : 3;
                }
                else
                {
                    if (ulVal < s_ulT6)
                        ret = (ulVal >= s_ulT5) ? 6 : 5;
                    else
                        ret = (ulVal >= s_ulT7) ? 8 : 7;
                }
            }
            else if (dwlVal < s_dwlT16)
            {
                if (dwlVal < s_dwlT12)
                {
                    if (dwlVal < s_dwlT10)
                        ret = (dwlVal >= s_ulT9) ? 10 : 9;
                    else
                        ret = (dwlVal >= s_dwlT11) ? 12 : 11;
                }
                else
                {
                    if (dwlVal < s_dwlT14)
                        ret = (dwlVal >= s_dwlT13) ? 14 : 13;
                    else
                        ret = (dwlVal >= s_dwlT15) ? 16 : 15;
                }
            }
            else
            {
                if (dwlVal < s_dwlT18)
                    ret = (dwlVal >= s_dwlT17) ? 18 : 17;
                else
                    ret = (dwlVal >= s_dwlT19) ? 20 : 19;
            }

            return (byte)ret;
        }

#if DEBUG
        // This is the old precision routine. It is only used in debug code to verify that both code paths
        // return the same value

        //
        //    BActualPrec()
        //
        //    Determine the actual number of significant digits (precision) of a numeric
        //
        //    Parameters:
        //
        //    Complexity:
        //        For small numerics, use simpler routines = O(n)
        //        Else, max 3 divisions of mp by ULONG, then again simpler routine.
        //
        //    Returns:
        //        a byte containing the actual precision
        //
        private byte BActualPrec()
        {
            if (_bPrec == 0 || _bLen < 1)
                return 0;

            int ciulU = _bLen;
            int Prec;
            uint ulRem;

            if (ciulU == 1)
            {
                Prec = BGetPrecUI4(_data1);
            }
            else if (ciulU == 2)
            {
                Prec = BGetPrecUI8(_data1, _data2);
            }
            else
            {
                Span<uint> rgulU = stackalloc uint[4] { _data1, _data2, _data3, _data4 };
                Prec = 0;
                do
                {
                    MpDiv1(rgulU, ref ciulU, 1000000000, out ulRem);
                    Prec += 9;
                }
                while (ciulU > 2);
                Debug.Assert(Prec == 9 || Prec == 18 || Prec == 27);
                Prec += BGetPrecUI8(rgulU[0], rgulU[1]);
            }

            // If number of significant digits less than scale, return scale
            return (Prec < _bScale ? _bScale : (byte)Prec);
        }
#endif

        //    AddULong()
        //
        //    Add ulAdd to this numeric.  The result will be returned in *this.
        //
        //    Parameters:
        //        this    - IN Operand1 & OUT Result
        //        ulAdd    - IN operand2.
        //
        private void AddULong(uint ulAdd)
        {
            ulong dwlAccum = ulAdd;
            int iData;                  // which UI4 in this we are on
            int iDataMax = _bLen; // # of UI4s in this

            Span<uint> rguiData = stackalloc uint[4] { _data1, _data2, _data3, _data4 };

            // Add, starting at the LS UI4 until out of UI4s or no carry
            iData = 0;
            do
            {
                dwlAccum += rguiData[iData];
                rguiData[iData] = (uint)dwlAccum;       // equivalent to mod x_dwlBaseUI4
                dwlAccum >>= 32;                        // equivalent to dwlAccum /= x_dwlBaseUI4;
                if (0 == dwlAccum)
                {
                    StoreFromWorkingArray(rguiData);
                    return;
                }
                iData++;
            }
            while (iData < iDataMax);

            // There is carry at the end

            Debug.Assert(dwlAccum < s_ulInt32Base, "dwlAccum < x_lInt32Base", "");

            // Either overflowed
            if (iData == s_cNumeMax)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            // Or need to extend length by 1 UI4
            rguiData[iData] = (uint)dwlAccum;
            _bLen++;

            if (FGt10_38(rguiData))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            StoreFromWorkingArray(rguiData);
        }

        // multiply by a long integer
        private void MultByULong(uint uiMultiplier)
        {
            int iDataMax = _bLen; // How many UI4s currently in *this

            ulong dwlAccum = 0;       // accumulated sum
            ulong dwlNextAccum = 0;   // accumulation past dwlAccum
            int iData;              // which UI4 in *This we are on.

            Span<uint> rguiData = stackalloc uint[4] { _data1, _data2, _data3, _data4 };

            for (iData = 0; iData < iDataMax; iData++)
            {
                Debug.Assert(dwlAccum < s_ulInt32Base);

                ulong ulTemp = rguiData[iData];
                dwlNextAccum = ulTemp * uiMultiplier;
                dwlAccum += dwlNextAccum;
                if (dwlAccum < dwlNextAccum)        // Overflow of int64 add
                    dwlNextAccum = s_ulInt32Base;   // how much to add to dwlAccum after div x_dwlBaseUI4
                else
                    dwlNextAccum = 0;
                rguiData[iData] = unchecked((uint)dwlAccum); // equivalent to mod x_dwlBaseUI4
                dwlAccum = (dwlAccum >> 32) + dwlNextAccum;  // equivalent to div x_dwlBaseUI4
            }

            // If any carry,
            if (dwlAccum != 0)
            {
                // Either overflowed
                Debug.Assert(dwlAccum < s_ulInt32Base, "dwlAccum < x_dwlBaseUI4", "Integer overflow");
                if (iDataMax == s_cNumeMax)
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                // Or extend length by one uint
                rguiData[iDataMax] = (uint)dwlAccum;
                _bLen++;
            }

            if (FGt10_38(rguiData))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            StoreFromWorkingArray(rguiData);
        }


        //    DivByULong()
        //
        //    Divide numeric value by a ULONG.  The result will be returned
        //    in the dividend *this.
        //
        //    Parameters:
        //        this        - IN Dividend & OUT Result
        //        ulDivisor    - IN Divisor
        //    Returns:        - OUT Remainder
        //
        private uint DivByULong(uint iDivisor)
        {
            ulong dwlDivisor = iDivisor;
            ulong dwlAccum = 0;           //Accumulated sum
            uint ulQuotientCur = 0;      // Value of the current UI4 of the quotient
            bool fAllZero = true;    // All of the quotient (so far) has been 0
            int iData;              //Which UI4 currently on

            // Check for zero divisor.
            if (dwlDivisor == 0)
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);

            // Copy into array, so that we can iterate through the data
            Span<uint> rguiData = stackalloc uint[4] { _data1, _data2, _data3, _data4 };

            // Start from the MS UI4 of quotient, divide by divisor, placing result
            //        in quotient and carrying the remainder.
            // DWORDLONG sufficient accumulator since:
            //        Accum < Divisor <= 2^32 - 1    at start each loop
            //                                    initially,and mod end previous loop
            //        Accum*2^32 < 2^64 - 2^32
            //                                    multiply both side by 2^32 (x_dwlBaseUI4)
            //        Accum*2^32 + m_rgulData < 2^64
            //                                    rglData < 2^32
            for (iData = _bLen; iData > 0; iData--)
            {
                Debug.Assert(dwlAccum < dwlDivisor);
                dwlAccum = (dwlAccum << 32) + rguiData[iData - 1]; // dwlA*x_dwlBaseUI4 + rglData
                Debug.Assert((dwlAccum / dwlDivisor) < s_ulInt32Base);
                //Update dividend to the quotient.
                ulQuotientCur = (uint)(dwlAccum / dwlDivisor);
                rguiData[iData - 1] = ulQuotientCur;
                //Remainder to be carried to the next lower significant byte.
                dwlAccum %= dwlDivisor;

                // While current part of quotient still 0, reduce length
                if (fAllZero && (ulQuotientCur == 0))
                {
                    _bLen--;
                }
                else
                {
                    fAllZero = false;
                }
            }

            StoreFromWorkingArray(rguiData);

            // If result is 0, preserve sign but set length to 5
            if (fAllZero)
                _bLen = 1;

            AssertValid();

            // return the remainder
            Debug.Assert(dwlAccum < s_ulInt32Base);
            return (uint)dwlAccum;
        }

        //    AdjustScale()
        //
        //    Adjust number of digits to the right of the decimal point.
        //    A positive adjustment increases the scale of the numeric value
        //    while a negative adjustment decreases the scale.  When decreasing
        //    the scale for the numeric value, the remainder is checked and
        //    rounded accordingly.
        //
        internal void AdjustScale(int digits, bool fRound)
        {
            Debug.Assert(!IsNull, "!IsNull", "In AdjustScale");

            uint ulRem;                  //Remainder when downshifting
            uint ulShiftBase;            //What to multiply by to effect scale adjust
            bool fNeedRound = false;     //Do we really need to round?
            byte bNewScale, bNewPrec;
            int lAdjust = digits;

            //If downshifting causes truncation of data
            if (lAdjust + _bScale < 0)
                throw new SqlTruncateException();

            //If uphifting causes scale overflow
            if (lAdjust + _bScale > s_NUMERIC_MAX_PRECISION)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            bNewScale = (byte)(lAdjust + _bScale);
            bNewPrec = (byte)(Math.Min(s_NUMERIC_MAX_PRECISION, Math.Max(1, lAdjust + _bPrec)));

            if (lAdjust > 0)
            {
                _bScale = bNewScale;
                _bPrec = bNewPrec;

                while (lAdjust > 0)
                {
                    //if lAdjust>=9, downshift by 10^9 each time, otherwise by the full amount
                    if (lAdjust >= 9)
                    {
                        ulShiftBase = s_rgulShiftBase[8];
                        lAdjust -= 9;
                    }
                    else
                    {
                        ulShiftBase = s_rgulShiftBase[lAdjust - 1];
                        lAdjust = 0;
                    }
                    MultByULong(ulShiftBase);
                }
            }
            else if (lAdjust < 0)
            {
                do
                {
                    if (lAdjust <= -9)
                    {
                        ulShiftBase = s_rgulShiftBase[8];
                        lAdjust += 9;
                    }
                    else
                    {
                        ulShiftBase = s_rgulShiftBase[-lAdjust - 1];
                        lAdjust = 0;
                    }
                    ulRem = DivByULong(ulShiftBase);
                }
                while (lAdjust < 0);

                // Do we really need to round?
                fNeedRound = (ulRem >= ulShiftBase / 2);

                _bScale = bNewScale;
                _bPrec = bNewPrec;
            }

            AssertValid();

            // After adjusting, if the result is 0 and remainder is less than 5,
            // set the sign to be positive and return.
            if (fNeedRound && fRound)
            {
                // If remainder is 5 or above, increment/decrement by 1.
                AddULong(1);
            }
            else if (FZero())
                SetPositive();
        }

        // Adjust scale of a SqlDecimal
        public static SqlDecimal AdjustScale(SqlDecimal n, int digits, bool fRound)
        {
            if (n.IsNull)
                return SqlDecimal.Null;

            SqlDecimal ret = n;
            ret.AdjustScale(digits, fRound);
            return ret;
        }

        // Convert to a specific precision and scale
        public static SqlDecimal ConvertToPrecScale(SqlDecimal n, int precision, int scale)
        {
            CheckValidPrecScale(precision, scale);
            n.AssertValid();

            if (n.IsNull)
                return SqlDecimal.Null;

            SqlDecimal ret = n;

            int lScaleAdjust = scale - ret._bScale; //Adjustment to scale

            //Adjust scale
            ret.AdjustScale(lScaleAdjust, true);

            //The number of bytes storage required by the new precision
            byte cbWithNewPrec = CLenFromPrec((byte)precision);

            if (cbWithNewPrec < ret._bLen)
            {
                //if current actual length greater than length corresponding to bNewPrec
                //there must be truncating
                throw new SqlTruncateException();
            }
            else if (cbWithNewPrec == ret._bLen)
            {
                //if the two lengths equal, need to check the actual precision
                if (precision < ret.CalculatePrecision())
                    throw new SqlTruncateException();
            }

            //Adjust precision
            ret._bPrec = (byte)precision;

            ret.AssertValid();

            return ret;
        }

        //    LAbsCmp()
        //
        //    Compare the absolute value of two numerics without checking scale
        //
        //  Parameters:
        //        this    - IN Operand1
        //        snumOp    - IN Operand2
        //
        //    Returns:
        //        positive    - |this| > |snumOp|
        //        0            - |this| = |snumOp|
        //        negative    - |this| < |snumOp|
        //
        private int LAbsCmp(SqlDecimal snumOp)
        {
            int iData;  //which UI4 we are operating on
            int culOp;  //#of UI4s on operand
            int culThis; //# of UI4s in this

            // If one longer, it is larger
            culOp = snumOp._bLen;
            culThis = _bLen;
            if (culOp != culThis)
                return (culThis > culOp) ? 1 : -1;

            ReadOnlySpan<uint> rglData1 = stackalloc uint[4] { _data1, _data2, _data3, _data4 };
            ReadOnlySpan<uint> rglData2 = stackalloc uint[4] { snumOp._data1, snumOp._data2, snumOp._data3, snumOp._data4 };

            // Loop through numeric value checking each byte for differences.
            iData = culOp - 1;
            do
            {
                // CONSIDER PERF: Could replace second comparison with
                //        cast to LONGLONG and subtract.  Probably not worth it.
                if (rglData1[iData] != rglData2[iData])
                    return ((rglData1[iData] > rglData2[iData]) ? 1 : -1);
                iData--;
            }
            while (iData >= 0);

            // All UI4s the same, return 0.
            return 0;
        }

        // Move multi-precision number
        private static void MpMove
        (
        ReadOnlySpan<uint> rgulS,      // In    | Source number
        int ciulS,      // In    | # of digits in S
        Span<uint> rgulD,      // Out    | Destination number
        out int ciulD       // Out    | # of digits in D
        )
        {
            ciulD = ciulS;

            Debug.Assert(rgulS.Length >= ciulS, "rgulS.Length >= ciulS", "Invalid array length");
            Debug.Assert(rgulD.Length >= ciulS, "rgulD.Length >= ciulS", "Invalid array length");

            for (int i = 0; i < ciulS; i++)
                rgulD[i] = rgulS[i];
        }

        // Set multi-precision number to one super-digit
        private static void MpSet
        (
        Span<uint> rgulD,      // Out    | Number
        out int ciulD,      // Out    | # of digits in D
        uint iulN        // In    | ULONG to set
        )
        {
            ciulD = 1;
            rgulD[0] = iulN;
        }

        // Normalize multi-precision number - remove leading zeroes
        private static void MpNormalize
        (
        ReadOnlySpan<uint> rgulU,      // In   | Number
        ref int ciulU       // InOut| # of digits
        )
        {
            while (ciulU > 1 && rgulU[ciulU - 1] == 0)
                ciulU--;
        }

        // Multi-precision one super-digit multiply in place.
        // D = D * X
        // Length can increase
        private static void MpMul1
        (
        Span<uint> piulD,      // InOut| D
        ref int ciulD,      // InOut| # of digits in D
        uint iulX        // In    | X
        )
        {
            Debug.Assert(iulX > s_uiZero);
            uint ulCarry = 0;
            int iData;
            ulong dwlAccum;

            for (iData = 0; iData < ciulD; iData++)
            {
                ulong ulTemp = piulD[iData];
                dwlAccum = ulCarry + ulTemp * iulX;
                ulCarry = HI(dwlAccum);
                piulD[iData] = LO(dwlAccum);
            }

            // If overflow occurs, increase precision
            if (ulCarry != 0)
            {
                piulD[iData] = ulCarry;
                ciulD++;
            }
        }

        // Multi-precision one super-digit divide in place.
        // U = U / D,
        // R = U % D
        // Length of U can decrease
        private static void MpDiv1
        (
        Span<uint> rgulU,      // InOut| U
        ref int ciulU,      // InOut| # of digits in U
        uint iulD,       // In    | D
        out uint iulR        // Out    | R
        )
        {
            Debug.Assert(rgulU.Length == s_cNumeMax);

            uint ulCarry = 0;
            ulong dwlAccum;
            ulong ulD = iulD;
            int idU = ciulU;

            Debug.Assert(iulD != 0, "iulD != 0", "Divided by zero!");
            Debug.Assert(iulD > 0, "iulD > 0", "Invalid data: less than zero");
            Debug.Assert(ciulU > 0, "ciulU > 0", "No data in the array");

            while (idU > 0)
            {
                idU--;
                dwlAccum = (((ulong)ulCarry) << 32) + rgulU[idU];
                rgulU[idU] = (uint)(dwlAccum / ulD);
                ulCarry = (uint)(dwlAccum - rgulU[idU] * ulD);  // (ULONG) (dwlAccum % iulD)
            }
            iulR = ulCarry;
            MpNormalize(rgulU, ref ciulU);
        }

        internal static ulong DWL(uint lo, uint hi)
        {
            return lo + (((ulong)hi) << 32);
        }

        private static uint HI(ulong x)
        {
            return (uint)(x >> 32);
        }

        private static uint LO(ulong x)
        {
            return (uint)x;
        }

        // Multi-precision divide.
        // Q = U / D,
        // R = U % D,
        // U and D not changed.
        // It is Ok for U and R to have the same location in memory,
        // but then U will be changed.
        // Assumes that there is enough room in Q and R for results.

        // Drawbacks of this implementation:
        //    1)    Need one extra super-digit (ULONG) in R
        //    2)    As it modifies D during work, then it do (probably) unnecessary
        //        work to restore it
        //    3)    Always get Q and R - if R is unnecessary, can be slightly faster
        // Most of this can be fixed if it'll be possible to have a working buffer. But
        // then we'll use alloca() or there will be limit on the upper size of numbers
        // (maybe not a problem in SQL Server).
        //
        private static void MpDiv
        (
        ReadOnlySpan<uint> rgulU,      // In    | U
        int ciulU,      // In    | # of digits in U
        Span<uint> rgulD,      // InOut    | D
        int ciulD,      // In    | # of digits in D
        Span<uint> rgulQ,      // Out    | Q
        out int ciulQ,      // Out    | # of digits in Q
        Span<uint> rgulR,      // Out    | R
        out int ciulR       // Out    | # of digits in R
        )
        {
            Debug.Assert(ciulU > 0, "ciulU > 0", "In method MpDiv");
            Debug.Assert(ciulD > 0, "ciulD > 0", "In method MpDiv");
            Debug.Assert(rgulU.Length == s_cNumeMax);
            Debug.Assert(rgulD.Length == s_cNumeMax);

            // Division by zero?
            if (ciulD == 1 && rgulD[0] == 0)
            {
                ciulQ = ciulR = 0;
            }

            // Check for simplest case, so it'll be fast
            else if (ciulU == 1 && ciulD == 1)
            {
                MpSet(rgulQ, out ciulQ, rgulU[0] / rgulD[0]);
                MpSet(rgulR, out ciulR, rgulU[0] % rgulD[0]);
            }

            // If D > U then do not divide at all
            else if (ciulD > ciulU)
            {
                MpMove(rgulU, ciulU, rgulR, out ciulR);        // R = U
                MpSet(rgulQ, out ciulQ, 0);                    // Q = 0
            }

            // Try to divide faster - check for remaining good sizes (8 / 4, 8 / 8)
            else if (ciulU <= 2)
            {
                ulong dwlU, dwlD, dwlT;

                dwlU = DWL(rgulU[0], rgulU[1]);
                dwlD = rgulD[0];
                if (ciulD > 1)
                    dwlD += (((ulong)rgulD[1]) << 32);
                dwlT = dwlU / dwlD;
                rgulQ[0] = LO(dwlT);
                rgulQ[1] = HI(dwlT);
                ciulQ = (HI(dwlT) != 0) ? 2 : 1;
                dwlT = dwlU % dwlD;
                rgulR[0] = LO(dwlT);
                rgulR[1] = HI(dwlT);
                ciulR = (HI(dwlT) != 0) ? 2 : 1;
            }

            // If we are dividing by one digit - use simpler routine
            else if (ciulD == 1)
            {
                MpMove(rgulU, ciulU, rgulQ, out ciulQ);        // Q = U
                uint remainder;
                MpDiv1(rgulQ, ref ciulQ, rgulD[0], out remainder);     // Q = Q / D, R = Q % D
                rgulR[0] = remainder;
                ciulR = 1;
            }

            // Worst case. Knuth, "The Art of Computer Programming", 3rd edition, vol.II, Alg.D, pg 272
            else
            {
                ciulQ = ciulR = 0;

                uint D1, ulDHigh, ulDSecond;
                int iulRindex;

                if (rgulU != rgulR)
                    MpMove(rgulU, ciulU, rgulR, out ciulR);        // R = U

                ciulQ = ciulU - ciulD + 1;
                ulDHigh = rgulD[ciulD - 1];

                // D1.    Normalize so high digit of D >= BASE/2 - that guarantee
                //        that QH will not be too far from the correct digit later in D3
                rgulR[ciulU] = 0;
                iulRindex = ciulU;
                D1 = (uint)(s_ulInt32Base / ((ulong)ulDHigh + 1));
                if (D1 > 1)
                {
                    MpMul1(rgulD, ref ciulD, D1);
                    ulDHigh = rgulD[ciulD - 1];
                    MpMul1(rgulR, ref ciulR, D1);
                }
                ulDSecond = rgulD[ciulD - 2];
                // D2 already done - iulRindex initialized before normalization of R.
                // D3-D7. Loop on iulRindex - obtaining digits one-by-one, as "in paper"
                do
                {
                    uint QH, RH;
                    int iulDindex, iulRwork;
                    ulong dwlAccum, dwlMulAccum;

                    // D3. Calculate Q hat - estimation of the next digit
                    dwlAccum = DWL(rgulR[iulRindex - 1], rgulR[iulRindex]);
                    if (ulDHigh == rgulR[iulRindex])
                        QH = (uint)(s_ulInt32Base - 1);
                    else
                        QH = (uint)(dwlAccum / ulDHigh);
                    ulong ulTemp = QH;
                    RH = (uint)(dwlAccum - ulTemp * ulDHigh);

                    while (ulDSecond * ulTemp > DWL(rgulR[iulRindex - 2], RH))
                    {
                        QH--;
                        if (RH >= (uint)-((int)ulDHigh))
                            break;
                        RH += ulDHigh;
                        ulTemp = QH;
                    }

                    // D4. Multiply and subtract: (some digits of) R -= D * QH
                    for (dwlAccum = s_ulInt32Base, dwlMulAccum = 0, iulDindex = 0, iulRwork = iulRindex - ciulD;
                        iulDindex < ciulD; iulDindex++, iulRwork++)
                    {
                        ulong ulTemp2 = rgulD[iulDindex];
                        dwlMulAccum += QH * ulTemp2;
                        dwlAccum += (ulong)rgulR[iulRwork] - LO(dwlMulAccum);
                        dwlMulAccum = HI(dwlMulAccum);
                        rgulR[iulRwork] = LO(dwlAccum);
                        dwlAccum = HI(dwlAccum) + s_ulInt32Base - 1;
                    }
                    dwlAccum += rgulR[iulRwork] - dwlMulAccum;
                    rgulR[iulRwork] = LO(dwlAccum);
                    rgulQ[iulRindex - ciulD] = QH;

                    // D5. Test remainder. Carry indicates result<0, therefore QH 1 too large
                    if (HI(dwlAccum) == 0)
                    {
                        // D6. Add back - probability is 2**(-31). R += D. Q[digit] -= 1
                        uint ulCarry;

                        rgulQ[iulRindex - ciulD] = QH - 1;
                        for (ulCarry = 0, iulDindex = 0, iulRwork = iulRindex - ciulD;
                            iulDindex < ciulD; iulDindex++, iulRwork++)
                        {
                            dwlAccum = rgulD[iulDindex] + (ulong)rgulR[iulRwork] + ulCarry;
                            ulCarry = HI(dwlAccum);
                            rgulR[iulRwork] = LO(dwlAccum);
                        }
                        rgulR[iulRwork] += ulCarry;
                    }
                    // D7. Loop on iulRindex
                    iulRindex--;
                }
                while (iulRindex >= ciulD);
                // Normalize results
                MpNormalize(rgulQ, ref ciulQ);
                ciulR = ciulD;
                MpNormalize(rgulR, ref ciulR);
                // D8. Unnormalize: Divide D and R to get result
                if (D1 > 1)
                {
                    MpDiv1(rgulD, ref ciulD, D1, out _);
                    MpDiv1(rgulR, ref ciulR, D1, out _);
                }
            }
        }

        //    CmpCompareNm()
        //
        //    Compare the value of two numerics
        //
        //    Complexity: O(pn) p: precision  n: length
        //
        //  Parameters:
        //        this    - IN Operand1
        //        snumOp    - IN operand2
        //
        //    Returns:
        //        EComparison.LT    - this < snumOp
        //        EComparison.EQ        - this = snumOp
        //        EComparison.GT    - this > snumOp
        //
        private EComparison CompareNm
        (
        SqlDecimal snumOp
        )
        {
            AssertValid();
            snumOp.AssertValid();

            //Signs of the two numeric operands
            int Sign1;
            int Sign2;

            int iFinalResult;   //Final result of comparison: positive = greater
                                //than, 0 = equal, negative = less than

            //Initialize the sign values to be 1(positive) or -1(negative)
            Sign1 = IsPositive ? 1 : -1;
            Sign2 = snumOp.IsPositive ? 1 : -1;

            if (Sign1 != Sign2) //If different sign, the positive one is greater
                return Sign1 == 1 ? EComparison.GT : EComparison.LT;

            else
            { //same sign, have to compare absolute values
                //Temporary memory to hold the operand since it is const
                //but its scale may get adjusted during comparison
                int ScaleDiff;
                SqlDecimal snumArg1 = this;
                SqlDecimal snumArg2 = snumOp;

                //First make the two operands the same scale if necessary
                ScaleDiff = _bScale - snumOp._bScale;

                if (ScaleDiff < 0)
                {
                    //If increasing the scale of operand1 caused an overflow,
                    //then its absolute value is greater than that of operand2.
                    try
                    {
                        snumArg1.AdjustScale(-ScaleDiff, true);
                    }
                    catch (OverflowException)
                    {
                        return (Sign1 > 0) ? EComparison.GT : EComparison.LT;
                    }
                }
                else if (ScaleDiff > 0)
                {
                    //If increasing the scale of operand2 caused an overflow, then
                    //operand1's absolute value is less than that of operand2.
                    try
                    {
                        snumArg2.AdjustScale(ScaleDiff, true);
                    }
                    catch (OverflowException)
                    {
                        return (Sign1 > 0) ? EComparison.LT : EComparison.GT;
                    }
                }

                //Compare the absolute value of the two numerics
                //Note: We are sure that scale of arguments is the same,
                //      so LAbsCmp() will not modify its argument.
                int lResult = snumArg1.LAbsCmp(snumArg2);
                if (0 == lResult)
                    return EComparison.EQ;

                //if both positive, result same as result from LAbsCmp;
                //if both negative, result reverse of result from LAbsCmp
                iFinalResult = Sign1 * lResult;

                if (iFinalResult < 0)
                    return EComparison.LT;
                else
                    return EComparison.GT;
            }
        }

        private static void CheckValidPrecScale(byte bPrec, byte bScale)
        {
            if (bPrec < 1 || bPrec > MaxPrecision || bScale > MaxScale || bScale > bPrec)
                throw new SqlTypeException(SQLResource.InvalidPrecScaleMessage);
        }

        private static void CheckValidPrecScale(int iPrec, int iScale)
        {
            if (iPrec < 1 || iPrec > MaxPrecision || iScale < 0 || iScale > MaxScale || iScale > iPrec)
                throw new SqlTypeException(SQLResource.InvalidPrecScaleMessage);
        }

        // Overloading comparison operators
        public static SqlBoolean operator ==(SqlDecimal x, SqlDecimal y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.CompareNm(y) == EComparison.EQ);
        }

        public static SqlBoolean operator !=(SqlDecimal x, SqlDecimal y)
        {
            return !(x == y);
        }

        public static SqlBoolean operator <(SqlDecimal x, SqlDecimal y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.CompareNm(y) == EComparison.LT);
        }

        public static SqlBoolean operator >(SqlDecimal x, SqlDecimal y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.CompareNm(y) == EComparison.GT);
        }

        public static SqlBoolean operator <=(SqlDecimal x, SqlDecimal y)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;
            else
            {
                EComparison result = x.CompareNm(y);
                return new SqlBoolean(result == EComparison.LT || result == EComparison.EQ);
            }
        }

        public static SqlBoolean operator >=(SqlDecimal x, SqlDecimal y)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;
            else
            {
                EComparison result = x.CompareNm(y);
                return new SqlBoolean(result == EComparison.GT || result == EComparison.EQ);
            }
        }


        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator +
        public static SqlDecimal Add(SqlDecimal x, SqlDecimal y)
        {
            return x + y;
        }
        // Alternative method for operator -
        public static SqlDecimal Subtract(SqlDecimal x, SqlDecimal y)
        {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlDecimal Multiply(SqlDecimal x, SqlDecimal y)
        {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlDecimal Divide(SqlDecimal x, SqlDecimal y)
        {
            return x / y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlDecimal x, SqlDecimal y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlDecimal x, SqlDecimal y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlDecimal x, SqlDecimal y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlDecimal x, SqlDecimal y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlDecimal x, SqlDecimal y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlDecimal x, SqlDecimal y)
        {
            return (x >= y);
        }

        // Alternative method for conversions.

        public SqlBoolean ToSqlBoolean()
        {
            return (SqlBoolean)this;
        }

        public SqlByte ToSqlByte()
        {
            return (SqlByte)this;
        }

        public SqlDouble ToSqlDouble()
        {
            return this;
        }

        public SqlInt16 ToSqlInt16()
        {
            return (SqlInt16)this;
        }

        public SqlInt32 ToSqlInt32()
        {
            return (SqlInt32)this;
        }

        public SqlInt64 ToSqlInt64()
        {
            return (SqlInt64)this;
        }

        public SqlMoney ToSqlMoney()
        {
            return (SqlMoney)this;
        }

        public SqlSingle ToSqlSingle()
        {
            return this;
        }

        public SqlString ToSqlString()
        {
            return (SqlString)this;
        }

        private static char ChFromDigit(uint uiDigit)
        {
            Debug.Assert(uiDigit < 10);
            return (char)(uiDigit + '0');
        }

        // Store data back from rguiData[] to m_data*
        private void StoreFromWorkingArray(ReadOnlySpan<uint> rguiData)
        {
            Debug.Assert(rguiData.Length == 4);
            _data1 = rguiData[0];
            _data2 = rguiData[1];
            _data3 = rguiData[2];
            _data4 = rguiData[3];
        }

        private void SetToZero()
        {
            Debug.Assert(_bPrec >= 1);
            _bLen = 1;
            _data1 =
            _data2 =
            _data3 =
            _data4 = 0;
            _bStatus = (byte)(s_bNotNull | s_bPositive);
            AssertValid();
        }

        // Truncate to integer
        private void MakeInteger(out bool fFraction)
        {
            uint ulRem;
            int iAdjust = _bScale;

            fFraction = false;

            while (iAdjust > 0)
            {
                if (iAdjust >= 9)
                {
                    ulRem = DivByULong(s_rgulShiftBase[8]);
                    iAdjust -= 9;
                }
                else
                {
                    ulRem = DivByULong(s_rgulShiftBase[iAdjust - 1]);
                    iAdjust = 0;
                }

                // Check for remainder and set fFraction flag.
                if (ulRem != 0)
                    fFraction = true;
            }

            _bScale = 0;
            AssertValid();
        }

        // Builtin functions

        // Abs - absolute value
        public static SqlDecimal Abs(SqlDecimal n)
        {
            n.AssertValid();

            if (n.IsNull)
                return SqlDecimal.Null;

            n.SetPositive();
            n.AssertValid();
            return n;
        }

        // Ceiling - next smallest integer greater than or equal to the numeric
        public static SqlDecimal Ceiling(SqlDecimal n)
        {
            n.AssertValid();

            if (n.IsNull)
                return SqlDecimal.Null;

            if (n._bScale == 0)
                return n;

            bool fFraction;    //Fractional flag

            n.MakeInteger(out fFraction);

            //When the numeric has fraction and is positive, adjust by adding 1.
            //Otherwise return the integral part.
            if (fFraction && n.IsPositive)
            {
                n.AddULong(1);
            }

            if (n.FZero())//if result is zero, sign should be positive
                n.SetPositive();
            n.AssertValid();
            return n;
        }

        // Floor - next largest integer smaller or equal to the numeric
        public static SqlDecimal Floor(SqlDecimal n)
        {
            n.AssertValid();

            if (n.IsNull)
                return SqlDecimal.Null;

            if (n._bScale == 0)
                return n;

            bool fFraction;    //Fractional flag

            n.MakeInteger(out fFraction);

            //When the numeric has fraction and is negative, subtract 1 by calling AddULong(1)
            //Otherwise return the integral part.
            if (fFraction && !n.IsPositive)
            {
                n.AddULong(1);
            }

            if (n.FZero())//if result is zero, sign should be positive
                n.SetPositive();
            n.AssertValid();
            return n;
        }

        // Sign -   1 if positive, -1 if negative
        public static SqlInt32 Sign(SqlDecimal n)
        {
            n.AssertValid();

            if (n.IsNull)
                return SqlInt32.Null;

            if (n == new SqlDecimal(0))
                return SqlInt32.Zero;
            else
                return n.IsNull ? SqlInt32.Null :
                    (n.IsPositive ? new SqlInt32(1) : new SqlInt32(-1));
        }

        private static SqlDecimal Round(SqlDecimal n, int lPosition, bool fTruncate)
        {
            if (n.IsNull)
                return SqlDecimal.Null;

            if (lPosition >= 0)
            {
                //If round to the right of decimal number
                lPosition = Math.Min(s_NUMERIC_MAX_PRECISION, lPosition);
                if (lPosition >= n._bScale)
                    return n;   //No need to round
            }
            else
            {
                //If round to the left of the decimal point
                lPosition = Math.Max(-s_NUMERIC_MAX_PRECISION, lPosition);

                //Return +0.00 if truncation of integer part
                if (lPosition < n._bScale - n._bPrec)
                {
                    n.SetToZero();
                    return n;
                }
            }

            uint ulRem = 0;                                         // Remainder: the highest significant digit to be truncated
            int lAdjust = Math.Abs(lPosition - n._bScale);    // Precision adjustment
            uint ulLastDivBase = 1;                                 //

            //Compute the integral part of the numeric
            while (lAdjust > 0)
            {
                if (lAdjust >= 9)
                {
                    ulRem = n.DivByULong(s_rgulShiftBase[8]);
                    ulLastDivBase = s_rgulShiftBase[8];
                    lAdjust -= 9;
                }
                else
                {
                    ulRem = n.DivByULong(s_rgulShiftBase[lAdjust - 1]);
                    ulLastDivBase = s_rgulShiftBase[lAdjust - 1];
                    lAdjust = 0;
                }
            }

            // The rounding only depends on the first digit after the rounding position
            if (ulLastDivBase > 1)
            {
                ulRem /= (ulLastDivBase / 10);
            }

            //If result is zero, return
            if (n.FZero() && (fTruncate || ulRem < 5))
            {
                n.SetPositive();
                n.AssertValid();
                return n;
            }

            // Adjust by adding 1 if remainder is larger than 5
            if (ulRem >= 5 && !fTruncate)
                n.AddULong(1);

            // Convert back to original scale
            lAdjust = Math.Abs(lPosition - n._bScale);

            while (lAdjust-- > 0)
            {
                n.MultByULong(s_ulBase10);
            }

            n.AssertValid();
            return n;
        }

        // Round - Round the numeric to a specific digit
        public static SqlDecimal Round(SqlDecimal n, int position)
        {
            n.AssertValid();
            return Round(n, position, false);
        }

        // Truncate - Truncate the numeric to a specific digit
        public static SqlDecimal Truncate(SqlDecimal n, int position)
        {
            n.AssertValid();
            return Round(n, position, true);
        }

        // Power - Compute the power of a numeric
        public static SqlDecimal Power(SqlDecimal n, double exp)
        {
            n.AssertValid();

            if (n.IsNull)
                return SqlDecimal.Null;

            int scale = n.Scale;
            double dBaseNum = n.ToDouble();

            n = new SqlDecimal(Math.Pow(dBaseNum, exp));
            n.AdjustScale(scale - n.Scale, true);

            n._bPrec = MaxPrecision;

            return n;
        }


        // IComparable
        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this < object, zero if this = object,
        // or a value greater than zero if this > object.
        // null is considered to be less than any instance.
        // If object is not of same type, this method throws an ArgumentException.
        public int CompareTo(object? value)
        {
            if (value is SqlDecimal i)
            {
                return CompareTo(i);
            }
            throw ADP.WrongType(value!.GetType(), typeof(SqlDecimal));
        }

        public int CompareTo(SqlDecimal value)
        {
            // If both Null, consider them equal.
            // Otherwise, Null is less than anything.
            if (IsNull)
                return value.IsNull ? 0 : -1;
            else if (value.IsNull)
                return 1;

            if (this < value) return -1;
            if (this > value) return 1;
            return 0;
        }

        // Compares this instance with a specified object
        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is SqlDecimal other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(SqlDecimal other) =>
            other.IsNull || IsNull ? other.IsNull && IsNull :
            (this == other).Value;

        // For hashing purpose
        public override int GetHashCode()
        {
            if (IsNull)
                return 0;

            SqlDecimal ssnumTemp;
            int lActualPrec;

            // First, "normalize" numeric, so that values with different
            // scale/precision will have the same representation.
            ssnumTemp = this;
            lActualPrec = ssnumTemp.CalculatePrecision();
            ssnumTemp.AdjustScale(s_NUMERIC_MAX_PRECISION - lActualPrec, true);

            // Now evaluate the hash
            int cDwords = ssnumTemp._bLen;
            int ulValue = 0;
            int ulHi;

            // Size of CRC window (hashing bytes, ssstr, sswstr, numeric)
            const int x_cbCrcWindow = 4;
            // const int iShiftVal = (sizeof ulValue) * (8*sizeof(char)) - x_cbCrcWindow;
            const int iShiftVal = 4 * 8 - x_cbCrcWindow;

            int[] rgiData = ssnumTemp.Data;

            for (int i = 0; i < cDwords; i++)
            {
                ulHi = (ulValue >> iShiftVal) & 0xff;
                ulValue <<= x_cbCrcWindow;
                ulValue = ulValue ^ rgiData[i] ^ ulHi;
            }
            return ulValue;
        }

        XmlSchema? IXmlSerializable.GetSchema() { return null; }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            string? isNull = reader.GetAttribute("nil", XmlSchema.InstanceNamespace);
            if (isNull != null && XmlConvert.ToBoolean(isNull))
            {
                // Read the next value.
                reader.ReadElementString();
                _bStatus = (byte)(s_bReverseNullMask & _bStatus);
            }
            else
            {
                SqlDecimal dec = Parse(reader.ReadElementString());
                _bStatus = dec._bStatus;
                _bLen = dec._bLen;
                _bPrec = dec._bPrec;
                _bScale = dec._bScale;
                _data1 = dec._data1;
                _data2 = dec._data2;
                _data3 = dec._data3;
                _data4 = dec._data4;
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (IsNull)
            {
                writer.WriteAttributeString("xsi", "nil", XmlSchema.InstanceNamespace, "true");
            }
            else
            {
                writer.WriteString(ToString());
            }
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("decimal", XmlSchema.Namespace);
        }

        // These values are defined at last, because they need to call ctors, which use other
        // constant values. Those constants must be defined before callint ctor.

        public static readonly SqlDecimal Null = new SqlDecimal(true);

        public static readonly SqlDecimal MinValue = SqlDecimal.Parse("-99999999999999999999999999999999999999");
        public static readonly SqlDecimal MaxValue = SqlDecimal.Parse("99999999999999999999999999999999999999");
    } // SqlDecimal
} // namespace System.Data.SqlTypes
