// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------


using System.Data.Common;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Text;

namespace System.Data.SqlTypes
{
    // Options that are used in comparison
    [Flags]
    public enum SqlCompareOptions
    {
        None = 0x00000000,
        IgnoreCase = 0x00000001,
        IgnoreNonSpace = 0x00000002,
        IgnoreKanaType = 0x00000008, // ignore kanatype
        IgnoreWidth = 0x00000010, // ignore width
        BinarySort = 0x00008000, // binary sorting
        BinarySort2 = 0x00004000, // binary sorting 2
    }

    /// <devdoc>
    ///    <para>
    ///       Represents a variable-length stream of characters to be stored in or retrieved from the database.
    ///    </para>
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public struct SqlString : INullable, IComparable
    {
        private String _value;
        private CompareInfo _cmpInfo;
        private int _lcid;     // Locale Id
        private SqlCompareOptions _flag;     // Compare flags
        private bool _fNotNull; // false if null

        /// <devdoc>
        ///    <para>
        ///       Represents a null value that can be assigned to the <see cref='System.Data.SqlTypes.SqlString.Value'/> property of an instance of
        ///       the <see cref='System.Data.SqlTypes.SqlString'/> class.
        ///    </para>
        /// </devdoc>
        public static readonly SqlString Null = new SqlString(true);

        internal static readonly UnicodeEncoding x_UnicodeEncoding = new UnicodeEncoding();

        /// <devdoc>
        /// </devdoc>
        public static readonly int IgnoreCase = 0x1;
        /// <devdoc>
        /// </devdoc>
        public static readonly int IgnoreWidth = 0x10;
        /// <devdoc>
        /// </devdoc>
        public static readonly int IgnoreNonSpace = 0x2;
        /// <devdoc>
        /// </devdoc>
        public static readonly int IgnoreKanaType = 0x8;
        /// <devdoc>
        /// </devdoc>
        public static readonly int BinarySort = 0x8000;
        /// <devdoc>
        /// </devdoc>
        public static readonly int BinarySort2 = 0x4000;

        private static readonly SqlCompareOptions s_iDefaultFlag =
                    SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType |
                    SqlCompareOptions.IgnoreWidth;
        private static readonly CompareOptions s_iValidCompareOptionMask =
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreWidth |
                    CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreKanaType;

        internal static readonly SqlCompareOptions x_iValidSqlCompareOptionMask =
                    SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreWidth |
                    SqlCompareOptions.IgnoreNonSpace | SqlCompareOptions.IgnoreKanaType |
                    SqlCompareOptions.BinarySort | SqlCompareOptions.BinarySort2;

        internal static readonly int x_lcidUSEnglish = 0x00000409;
        private static readonly int s_lcidBinary = 0x00008200;


        // constructor
        // construct a Null
        private SqlString(bool fNull)
        {
            _value = null;
            _cmpInfo = null;
            _lcid = 0;
            _flag = SqlCompareOptions.None;
            _fNotNull = false;
        }

        // Constructor: Construct from both Unicode and NonUnicode data, according to fUnicode
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlString'/> class.
        ///    </para>
        /// </devdoc>
        public SqlString(int lcid, SqlCompareOptions compareOptions, byte[] data, int index, int count, bool fUnicode)
        {
            _lcid = lcid;
            ValidateSqlCompareOptions(compareOptions);
            _flag = compareOptions;
            if (data == null)
            {
                _fNotNull = false;
                _value = null;
                _cmpInfo = null;
            }
            else
            {
                _fNotNull = true;

                // m_cmpInfo is set lazily, so that we don't need to pay the cost
                // unless the string is used in comparison.
                _cmpInfo = null;

                if (fUnicode)
                {
                    _value = x_UnicodeEncoding.GetString(data, index, count);
                }
                else
                {
                    Encoding cpe = Locale.GetEncodingForLcid(_lcid);
                    _value = cpe.GetString(data, index, count);
                }
            }
        }

        // Constructor: Construct from both Unicode and NonUnicode data, according to fUnicode
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlString'/> class.
        ///    </para>
        /// </devdoc>
        public SqlString(int lcid, SqlCompareOptions compareOptions, byte[] data, bool fUnicode)
        : this(lcid, compareOptions, data, 0, data.Length, fUnicode)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlString'/> class.
        ///    </para>
        /// </devdoc>
        public SqlString(int lcid, SqlCompareOptions compareOptions, byte[] data, int index, int count)
            : this(lcid, compareOptions, data, index, count, true)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlString'/> class.
        ///    </para>
        /// </devdoc>
        public SqlString(int lcid, SqlCompareOptions compareOptions, byte[] data)
            : this(lcid, compareOptions, data, 0, data.Length, true)
        {
        }


        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlString'/> class.
        ///    </para>
        /// </devdoc>
        public SqlString(String data, int lcid, SqlCompareOptions compareOptions)
        {
            _lcid = lcid;
            ValidateSqlCompareOptions(compareOptions);
            _flag = compareOptions;
            _cmpInfo = null;
            if (data == null)
            {
                _fNotNull = false;
                _value = null;
            }
            else
            {
                _fNotNull = true;
                _value = data; // PERF: do not String.Copy
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlString'/> class.
        ///    </para>
        /// </devdoc>
        public SqlString(String data, int lcid) : this(data, lcid, s_iDefaultFlag)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlString'/> class.
        ///    </para>
        /// </devdoc>
        public SqlString(String data) : this(data, Locale.GetCurrentCultureLcid(), s_iDefaultFlag)
        {
        }

        private SqlString(int lcid, SqlCompareOptions compareOptions, String data, CompareInfo cmpInfo)
        {
            _lcid = lcid;
            ValidateSqlCompareOptions(compareOptions);
            _flag = compareOptions;
            if (data == null)
            {
                _fNotNull = false;
                _value = null;
                _cmpInfo = null;
            }
            else
            {
                _value = data;
                _cmpInfo = cmpInfo;
                _fNotNull = true;
            }
        }


        // INullable
        /// <devdoc>
        ///    <para>
        ///       Gets whether the <see cref='System.Data.SqlTypes.SqlString.Value'/> of the <see cref='System.Data.SqlTypes.SqlString'/> is <see cref='System.Data.SqlTypes.SqlString.Null'/>.
        ///    </para>
        /// </devdoc>
        public bool IsNull
        {
            get { return !_fNotNull; }
        }

        // property: Value
        /// <devdoc>
        ///    <para>
        ///       Gets the string that is to be stored.
        ///    </para>
        /// </devdoc>
        public String Value
        {
            get
            {
                if (!IsNull)
                    return _value;
                else
                    throw new SqlNullValueException();
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int LCID
        {
            get
            {
                if (!IsNull)
                    return _lcid;
                else
                    throw new SqlNullValueException();
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CultureInfo CultureInfo
        {
            get
            {
                if (!IsNull)
                    return new CultureInfo(Locale.GetLocaleNameForLcid(_lcid));
                else
                    throw new SqlNullValueException();
            }
        }

        private void SetCompareInfo()
        {
            Debug.Assert(!IsNull);
            if (_cmpInfo == null)
                _cmpInfo = (new CultureInfo(Locale.GetLocaleNameForLcid(_lcid))).CompareInfo;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CompareInfo CompareInfo
        {
            get
            {
                if (!IsNull)
                {
                    SetCompareInfo();
                    return _cmpInfo;
                }
                else
                    throw new SqlNullValueException();
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlCompareOptions SqlCompareOptions
        {
            get
            {
                if (!IsNull)
                    return _flag;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from String to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlString(String x)
        {
            return new SqlString(x);
        }

        // Explicit conversion from SqlString to String. Throw exception if x is Null.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator String(SqlString x)
        {
            return x.Value;
        }

        /// <devdoc>
        ///    <para>
        ///       Converts a <see cref='System.Data.SqlTypes.SqlString'/> object to a string.
        ///    </para>
        /// </devdoc>
        public override String ToString()
        {
            return IsNull ? SQLResource.NullString : _value;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public byte[] GetUnicodeBytes()
        {
            if (IsNull)
                return null;

            return x_UnicodeEncoding.GetBytes(_value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public byte[] GetNonUnicodeBytes()
        {
            if (IsNull)
                return null;

            // Get the CultureInfo
            Encoding cpe = Locale.GetEncodingForLcid(_lcid);
            return cpe.GetBytes(_value);
        }

        /*
                internal int GetSQLCID() {
                    if (IsNull)
                        throw new SqlNullValueException();

                    return MAKECID(m_lcid, m_flag);
                }
        */

        // Binary operators

        // Concatenation
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlString operator +(SqlString x, SqlString y)
        {
            if (x.IsNull || y.IsNull)
                return SqlString.Null;

            if (x._lcid != y._lcid || x._flag != y._flag)
                throw new SqlTypeException(SQLResource.ConcatDiffCollationMessage);

            return new SqlString(x._lcid, x._flag, x._value + y._value,
                    (x._cmpInfo == null) ? y._cmpInfo : x._cmpInfo);
        }

        // StringCompare: Common compare function which is used by Compare and CompareTo
        //  In the case of Compare (used by comparison operators) the int result needs to be converted to SqlBoolean type
        //  while CompareTo needs the result in int type
        //  Pre-requisite: the null condition of the both string needs to be checked and handled by the caller of this function
        private static int StringCompare(SqlString x, SqlString y)
        {
            Debug.Assert(!x.IsNull && !y.IsNull, "Null condition should be handled by the caller of StringCompare method");

            if (x._lcid != y._lcid || x._flag != y._flag)
                throw new SqlTypeException(SQLResource.CompareDiffCollationMessage);

            x.SetCompareInfo();
            y.SetCompareInfo();
            Debug.Assert(x.FBinarySort() || (x._cmpInfo != null && y._cmpInfo != null));

            int iCmpResult;

            if ((x._flag & SqlCompareOptions.BinarySort) != 0)
                iCmpResult = CompareBinary(x, y);
            else if ((x._flag & SqlCompareOptions.BinarySort2) != 0)
                iCmpResult = CompareBinary2(x, y);
            else
            {
                // SqlString can be padded with spaces (Padding is turn on by default in SQL Server 2008
                // Trim the trailing space for comparison
                //  Avoid using String.TrimEnd function to avoid extra string allocations

                string rgchX = x._value;
                string rgchY = y._value;
                int cwchX = rgchX.Length;
                int cwchY = rgchY.Length;

                while (cwchX > 0 && rgchX[cwchX - 1] == ' ')
                    cwchX--;
                while (cwchY > 0 && rgchY[cwchY - 1] == ' ')
                    cwchY--;

                CompareOptions options = CompareOptionsFromSqlCompareOptions(x._flag);

                iCmpResult = x._cmpInfo.Compare(x._value, 0, cwchX, y._value, 0, cwchY, options);
            }

            return iCmpResult;
        }

        // Comparison operators
        private static SqlBoolean Compare(SqlString x, SqlString y, EComparison ecExpectedResult)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            int iCmpResult = StringCompare(x, y);

            bool fResult = false;

            switch (ecExpectedResult)
            {
                case EComparison.EQ:
                    fResult = (iCmpResult == 0);
                    break;

                case EComparison.LT:
                    fResult = (iCmpResult < 0);
                    break;

                case EComparison.LE:
                    fResult = (iCmpResult <= 0);
                    break;

                case EComparison.GT:
                    fResult = (iCmpResult > 0);
                    break;

                case EComparison.GE:
                    fResult = (iCmpResult >= 0);
                    break;

                default:
                    Debug.Assert(false, "Invalid ecExpectedResult");
                    return SqlBoolean.Null;
            }

            return new SqlBoolean(fResult);
        }



        // Implicit conversions



        // Explicit conversions

        // Explicit conversion from SqlBoolean to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlString((x.Value).ToString());
        }

        // Explicit conversion from SqlByte to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlByte x)
        {
            return x.IsNull ? Null : new SqlString((x.Value).ToString((IFormatProvider)null));
        }

        // Explicit conversion from SqlInt16 to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlString((x.Value).ToString((IFormatProvider)null));
        }

        // Explicit conversion from SqlInt32 to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlString((x.Value).ToString((IFormatProvider)null));
        }

        // Explicit conversion from SqlInt64 to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlInt64 x)
        {
            return x.IsNull ? Null : new SqlString((x.Value).ToString((IFormatProvider)null));
        }

        // Explicit conversion from SqlSingle to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlSingle x)
        {
            return x.IsNull ? Null : new SqlString((x.Value).ToString((IFormatProvider)null));
        }

        // Explicit conversion from SqlDouble to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlDouble x)
        {
            return x.IsNull ? Null : new SqlString((x.Value).ToString((IFormatProvider)null));
        }

        // Explicit conversion from SqlDecimal to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlDecimal x)
        {
            return x.IsNull ? Null : new SqlString(x.ToString());
        }

        // Explicit conversion from SqlMoney to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlString(x.ToString());
        }

        // Explicit conversion from SqlDateTime to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlDateTime x)
        {
            return x.IsNull ? Null : new SqlString(x.ToString());
        }

        // Explicit conversion from SqlGuid to SqlString
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlString(SqlGuid x)
        {
            return x.IsNull ? Null : new SqlString(x.ToString());
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlString Clone()
        {
            if (IsNull)
                return new SqlString(true);
            else
            {
                SqlString ret = new SqlString(_value, _lcid, _flag);
                return ret;
            }
        }

        // Overloading comparison operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator ==(SqlString x, SqlString y)
        {
            return Compare(x, y, EComparison.EQ);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator !=(SqlString x, SqlString y)
        {
            return !(x == y);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <(SqlString x, SqlString y)
        {
            return Compare(x, y, EComparison.LT);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >(SqlString x, SqlString y)
        {
            return Compare(x, y, EComparison.GT);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <=(SqlString x, SqlString y)
        {
            return Compare(x, y, EComparison.LE);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >=(SqlString x, SqlString y)
        {
            return Compare(x, y, EComparison.GE);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator +
        public static SqlString Concat(SqlString x, SqlString y)
        {
            return x + y;
        }

        public static SqlString Add(SqlString x, SqlString y)
        {
            return x + y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlString x, SqlString y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlString x, SqlString y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlString x, SqlString y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlString x, SqlString y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlString x, SqlString y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlString x, SqlString y)
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

        public SqlDateTime ToSqlDateTime()
        {
            return (SqlDateTime)this;
        }

        public SqlDouble ToSqlDouble()
        {
            return (SqlDouble)this;
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

        public SqlDecimal ToSqlDecimal()
        {
            return (SqlDecimal)this;
        }

        public SqlSingle ToSqlSingle()
        {
            return (SqlSingle)this;
        }

        public SqlGuid ToSqlGuid()
        {
            return (SqlGuid)this;
        }




        // Utility functions and constants

        private static void ValidateSqlCompareOptions(SqlCompareOptions compareOptions)
        {
            if ((compareOptions & x_iValidSqlCompareOptionMask) != compareOptions)
                throw new ArgumentOutOfRangeException(nameof(compareOptions));
        }

        public static CompareOptions CompareOptionsFromSqlCompareOptions(SqlCompareOptions compareOptions)
        {
            CompareOptions options = CompareOptions.None;

            ValidateSqlCompareOptions(compareOptions);

            if ((compareOptions & (SqlCompareOptions.BinarySort | SqlCompareOptions.BinarySort2)) != 0)
                throw ADP.ArgumentOutOfRange("compareOptions");
            else
            {
                if ((compareOptions & SqlCompareOptions.IgnoreCase) != 0)
                    options |= CompareOptions.IgnoreCase;
                if ((compareOptions & SqlCompareOptions.IgnoreNonSpace) != 0)
                    options |= CompareOptions.IgnoreNonSpace;
                if ((compareOptions & SqlCompareOptions.IgnoreKanaType) != 0)
                    options |= CompareOptions.IgnoreKanaType;
                if ((compareOptions & SqlCompareOptions.IgnoreWidth) != 0)
                    options |= CompareOptions.IgnoreWidth;
            }

            return options;
        }

        /*
        private static SqlCompareOptions SqlCompareOptionsFromCompareOptions(CompareOptions compareOptions) {
            SqlCompareOptions sqlOptions = SqlCompareOptions.None;

            if ((compareOptions & x_iValidCompareOptionMask) != compareOptions)
                throw new ArgumentOutOfRangeException ("compareOptions");
            else {
                if ((compareOptions & CompareOptions.IgnoreCase) != 0)
                    sqlOptions |= SqlCompareOptions.IgnoreCase;
                if ((compareOptions & CompareOptions.IgnoreNonSpace) != 0)
                    sqlOptions |= SqlCompareOptions.IgnoreNonSpace;
                if ((compareOptions & CompareOptions.IgnoreKanaType) != 0)
                    sqlOptions |= SqlCompareOptions.IgnoreKanaType;
                if ((compareOptions & CompareOptions.IgnoreWidth) != 0)
                    sqlOptions |= SqlCompareOptions.IgnoreWidth;
            }

            return  sqlOptions;
        }
        */

        private bool FBinarySort()
        {
            return (!IsNull && (_flag & (SqlCompareOptions.BinarySort | SqlCompareOptions.BinarySort2)) != 0);
        }

        //    Wide-character string comparison for Binary Unicode Collation
        //    Return values:
        //        -1 : wstr1 < wstr2
        //        0  : wstr1 = wstr2
        //        1  : wstr1 > wstr2
        //
        //    Does a memory comparison.
        private static int CompareBinary(SqlString x, SqlString y)
        {
            byte[] rgDataX = x_UnicodeEncoding.GetBytes(x._value);
            byte[] rgDataY = x_UnicodeEncoding.GetBytes(y._value);
            int cbX = rgDataX.Length;
            int cbY = rgDataY.Length;
            int cbMin = cbX < cbY ? cbX : cbY;
            int i;

            Debug.Assert(cbX % 2 == 0);
            Debug.Assert(cbY % 2 == 0);

            for (i = 0; i < cbMin; i++)
            {
                if (rgDataX[i] < rgDataY[i])
                    return -1;
                else if (rgDataX[i] > rgDataY[i])
                    return 1;
            }

            i = cbMin;

            int iCh;
            int iSpace = (int)' ';

            if (cbX < cbY)
            {
                for (; i < cbY; i += 2)
                {
                    iCh = ((int)rgDataY[i + 1]) << 8 + rgDataY[i];
                    if (iCh != iSpace)
                        return (iSpace > iCh) ? 1 : -1;
                }
            }
            else
            {
                for (; i < cbX; i += 2)
                {
                    iCh = ((int)rgDataX[i + 1]) << 8 + rgDataX[i];
                    if (iCh != iSpace)
                        return (iCh > iSpace) ? 1 : -1;
                }
            }

            return 0;
        }

        //    Wide-character string comparison for Binary2 Unicode Collation
        //    Return values:
        //        -1 : wstr1 < wstr2
        //        0  : wstr1 = wstr2
        //        1  : wstr1 > wstr2
        //
        //    Does a wchar comparison (different from memcmp of BinarySort).
        private static int CompareBinary2(SqlString x, SqlString y)
        {
            Debug.Assert(!x.IsNull && !y.IsNull);

            string rgDataX = x._value;
            string rgDataY = y._value;
            int cwchX = rgDataX.Length;
            int cwchY = rgDataY.Length;
            int cwchMin = cwchX < cwchY ? cwchX : cwchY;
            int i;

            for (i = 0; i < cwchMin; i++)
            {
                if (rgDataX[i] < rgDataY[i])
                    return -1;
                else if (rgDataX[i] > rgDataY[i])
                    return 1;
            }

            // If compares equal up to one of the string terminates,
            // pad it with spaces and compare with the rest of the other one.
            //
            char chSpace = ' ';

            if (cwchX < cwchY)
            {
                for (i = cwchMin; i < cwchY; i++)
                {
                    if (rgDataY[i] != chSpace)
                        return (chSpace > rgDataY[i]) ? 1 : -1;
                }
            }
            else
            {
                for (i = cwchMin; i < cwchX; i++)
                {
                    if (rgDataX[i] != chSpace)
                        return (rgDataX[i] > chSpace) ? 1 : -1;
                }
            }

            return 0;
        }

        /*
                private void Print() {
                    Debug.WriteLine("SqlString - ");
                    Debug.WriteLine("\tlcid = " + m_lcid.ToString());
                    Debug.Write("\t");
                    if ((m_flag & SqlCompareOptions.IgnoreCase) != 0)
                        Debug.Write("IgnoreCase, ");
                    if ((m_flag & SqlCompareOptions.IgnoreNonSpace) != 0)
                        Debug.Write("IgnoreNonSpace, ");
                    if ((m_flag & SqlCompareOptions.IgnoreKanaType) != 0)
                        Debug.Write("IgnoreKanaType, ");
                    if ((m_flag & SqlCompareOptions.IgnoreWidth) != 0)
                        Debug.Write("IgnoreWidth, ");
                    Debug.WriteLine("");
                    Debug.WriteLine("\tvalue = " + m_value);
                    Debug.WriteLine("\tcmpinfo = " + m_cmpInfo);
                }
        */
        // IComparable
        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this < object, zero if this = object,
        // or a value greater than zero if this > object.
        // null is considered to be less than any instance.
        // If object is not of same type, this method throws an ArgumentException.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int CompareTo(Object value)
        {
            if (value is SqlString)
            {
                SqlString i = (SqlString)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlString));
        }

        public int CompareTo(SqlString value)
        {
            // If both Null, consider them equal.
            // Otherwise, Null is less than anything.
            if (IsNull)
                return value.IsNull ? 0 : -1;
            else if (value.IsNull)
                return 1;

            int returnValue = StringCompare(this, value);

            // Conver the result into -1, 0, or 1 as this method never returned any other values
            //  This is to ensure the backcompat
            if (returnValue < 0)
            {
                return -1;
            }
            if (returnValue > 0)
            {
                return 1;
            }

            return 0;
        }

        // Compares this instance with a specified object
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override bool Equals(Object value)
        {
            if (!(value is SqlString))
            {
                return false;
            }

            SqlString i = (SqlString)value;

            if (i.IsNull || IsNull)
                return (i.IsNull && IsNull);
            else
                return (this == i).Value;
        }

        // For hashing purpose
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode()
        {
            if (IsNull)
                return 0;

            byte[] rgbSortKey;
            if (FBinarySort())
                rgbSortKey = x_UnicodeEncoding.GetBytes(_value.TrimEnd());
            else
            {
                // VSDevDiv 479660
                //  GetHashCode should not throw just because this instance has an invalid LCID or compare options.
                CompareInfo cmpInfo;
                CompareOptions options;
                try
                {
                    SetCompareInfo();
                    cmpInfo = _cmpInfo;
                    options = CompareOptionsFromSqlCompareOptions(_flag);
                }
                catch (ArgumentException)
                {
                    // SetCompareInfo throws this when instance's LCID is unsupported
                    // CompareOptionsFromSqlCompareOptions throws this when instance's options are invalid
                    cmpInfo = CultureInfo.InvariantCulture.CompareInfo;
                    options = CompareOptions.None;
                }
                return cmpInfo.GetHashCode(_value.TrimEnd(), options);
            }

            return SqlBinary.HashByteArray(rgbSortKey, rgbSortKey.Length);
        }
    } // SqlString
      /*
          internal struct SLocaleMapItem {
              public int      lcid;           // the primary key, not nullable
              public String   name;           // unique, nullable
              public int      idCodePage;     // the ANSI default code page of the locale

              public SLocaleMapItem(int lid, String str, int cpid) {
                  lcid = lid;
                  name = str;
                  idCodePage = cpid;
              }
          }

          // Struct to map lcid to ordinal
          internal struct SLcidOrdMapItem {
              internal int    lcid;
              internal int    uiOrd;
          };

          // Class to store map of lcids to ordinal
          internal class CBuildLcidOrdMap {
              internal SLcidOrdMapItem[] m_rgLcidOrdMap;
              internal int m_cValidLocales;
              internal int m_uiPosEnglish; // Start binary searches here - this is index in array, not ordinal

              // Constructor builds the array sorted by lcid
              // We use a simple n**2 sort because the array is mostly sorted anyway
              // and objects of this class will be const, hence this will be called
              // only by VC compiler
              public CBuildLcidOrdMap() {
                  int i,j;

                  m_rgLcidOrdMap = new SLcidOrdMapItem[SqlString.x_cLocales];

                  // Compact the array
                  for (i=0,j=0; i < SqlString.x_cLocales; i++) {
                      if (SqlString.x_rgLocaleMap[i].lcid != SqlString.x_lcidUnused) {
                          m_rgLcidOrdMap[j].lcid = SqlString.x_rgLocaleMap[i].lcid;
                          m_rgLcidOrdMap[j].uiOrd = i;
                          j++;
                      }
                  }

                  m_cValidLocales = j;

                  // Set the rest to invalid
                  while (j < SqlString.x_cLocales) {
                      m_rgLcidOrdMap[j].lcid = SqlString.x_lcidUnused;
                      m_rgLcidOrdMap[j].uiOrd = 0;
                      j++;
                  }

                  // Now sort in place
                  // Algo:
                  // Start from 1, assume list before i is sorted, if next item
                  // violates this assumption, exchange with prev items until the
                  // item is in its correct place
                  for (i=1; i<m_cValidLocales; i++) {
                      for (j=i; j>0 &&
                          m_rgLcidOrdMap[j].lcid < m_rgLcidOrdMap[j-1].lcid; j--) {
                          // Swap with prev element
                          int lcidTemp = m_rgLcidOrdMap[j-1].lcid;
                          int uiOrdTemp = m_rgLcidOrdMap[j-1].uiOrd;
                          m_rgLcidOrdMap[j-1].lcid = m_rgLcidOrdMap[j].lcid;
                          m_rgLcidOrdMap[j-1].uiOrd = m_rgLcidOrdMap[j].uiOrd;
                          m_rgLcidOrdMap[j].lcid = lcidTemp;
                          m_rgLcidOrdMap[j].uiOrd = uiOrdTemp;
                      }
                  }

                  // Set the position of the US_English LCID (Latin1_General)
                  for (i=0; i<m_cValidLocales && m_rgLcidOrdMap[i].lcid != SqlString.x_lcidUSEnglish; i++)
                      ; // Deliberately empty

                  SQLDebug.Check(i<m_cValidLocales);  // Latin1_General better be present
                  m_uiPosEnglish = i;     // This is index in array, not ordinal
              }

          } // CBuildLcidOrdMap
      */
} // namespace System.Data.SqlTypes
