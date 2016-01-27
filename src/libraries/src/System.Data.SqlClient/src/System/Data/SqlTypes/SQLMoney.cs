// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------


using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;


namespace System.Data.SqlTypes
{
    /// <devdoc>
    ///    <para>
    ///       Represents a currency value ranging from
    ///       -2<superscript term='63'/> (or -922,337,203,685,477.5808) to 2<superscript term='63'/> -1 (or
    ///       +922,337,203,685,477.5807) with an accuracy to
    ///       a ten-thousandth of currency unit to be stored in or retrieved from a
    ///       database.
    ///    </para>
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public struct SqlMoney : INullable, IComparable
    {
        private bool _fNotNull; // false if null
        private long _value;

        // SQL Server stores money8 as ticks of 1/10000.
        internal static readonly int x_iMoneyScale = 4;
        private static readonly long s_lTickBase = 10000;
        private static readonly double s_dTickBase = (double)s_lTickBase;

        private static readonly long s_minLong = unchecked((long)0x8000000000000000L) / s_lTickBase;
        private static readonly long s_maxLong = 0x7FFFFFFFFFFFFFFFL / s_lTickBase;

        // constructor
        // construct a Null
        private SqlMoney(bool fNull)
        {
            _fNotNull = false;
            _value = 0;
        }

        // Constructs from a long value without scaling. The ignored parameter exists
        // only to distinguish this constructor from the constructor that takes a long.
        // Used only internally.
        internal SqlMoney(long value, int ignored)
        {
            _value = value;
            _fNotNull = true;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlMoney'/> class with the value given.
        ///    </para>
        /// </devdoc>
        public SqlMoney(int value)
        {
            _value = (long)value * s_lTickBase;
            _fNotNull = true;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlMoney'/> class with the value given.
        ///    </para>
        /// </devdoc>
        public SqlMoney(long value)
        {
            if (value < s_minLong || value > s_maxLong)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            _value = value * s_lTickBase;
            _fNotNull = true;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlMoney'/> class with the value given.
        ///    </para>
        /// </devdoc>
        public SqlMoney(Decimal value)
        {
            // Since Decimal is a value type, operate directly on value, don't worry about changing it.
            SqlDecimal snum = new SqlDecimal(value);
            snum.AdjustScale(x_iMoneyScale - snum.Scale, true);
            Debug.Assert(snum.Scale == x_iMoneyScale);

            if (snum.m_data3 != 0 || snum.m_data4 != 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            bool fPositive = snum.IsPositive;
            ulong ulValue = (ulong)snum.m_data1 + (((ulong)snum.m_data2) << 32);
            if (fPositive && ulValue > (ulong)(Int64.MaxValue) ||
                !fPositive && ulValue > unchecked((ulong)(Int64.MinValue)))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            _value = fPositive ? (long)ulValue : unchecked(-(long)ulValue);
            _fNotNull = true;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlMoney'/> class with the value given.
        ///    </para>
        /// </devdoc>
        public SqlMoney(double value) : this(new Decimal(value))
        {
        }


        // INullable
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether the <see cref='System.Data.SqlTypes.SqlMoney.Value'/>
        ///       property is assigned to null.
        ///    </para>
        /// </devdoc>
        public bool IsNull
        {
            get { return !_fNotNull; }
        }

        // property: Value
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the monetary value of an instance of the <see cref='System.Data.SqlTypes.SqlMoney'/>
        ///       class.
        ///    </para>
        /// </devdoc>
        public Decimal Value
        {
            get
            {
                if (_fNotNull)
                    return ToDecimal();
                else
                    throw new SqlNullValueException();
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Decimal ToDecimal()
        {
            if (IsNull)
                throw new SqlNullValueException();

            bool fNegative = false;
            long value = _value;
            if (_value < 0)
            {
                fNegative = true;
                value = -_value;
            }

            return new Decimal(unchecked((int)value), unchecked((int)(value >> 32)), 0, fNegative, (byte)x_iMoneyScale);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public long ToInt64()
        {
            if (IsNull)
                throw new SqlNullValueException();

            long ret = _value / (s_lTickBase / 10);
            bool fPositive = (ret >= 0);
            long remainder = ret % 10;
            ret = ret / 10;

            if (remainder >= 5)
            {
                if (fPositive)
                    ret++;
                else
                    ret--;
            }

            return ret;
        }

        internal long ToSqlInternalRepresentation()
        {
            if (IsNull)
                throw new SqlNullValueException();

            return _value;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int ToInt32()
        {
            return checked((int)(ToInt64()));
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public double ToDouble()
        {
            return Decimal.ToDouble(ToDecimal());
        }

        // Implicit conversion from Decimal to SqlMoney
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlMoney(Decimal x)
        {
            return new SqlMoney(x);
        }

        // Explicit conversion from Double to SqlMoney
        public static explicit operator SqlMoney(double x)
        {
            return new SqlMoney(x);
        }

        // Implicit conversion from long to SqlMoney
        public static implicit operator SqlMoney(long x)
        {
            return new SqlMoney(new Decimal(x));
        }

        // Explicit conversion from SqlMoney to Decimal. Throw exception if x is Null.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator Decimal(SqlMoney x)
        {
            return x.Value;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override String ToString()
        {
            if (this.IsNull)
            {
                return SQLResource.NullString;
            }
            Decimal money = ToDecimal();
            // Formatting of SqlMoney: At least two digits after decimal point
            return money.ToString("#0.00##", (IFormatProvider)null);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlMoney Parse(String s)
        {
            // Try parsing the format of '#0.00##' generated by ToString() by using the
            // culture invariant NumberFormatInfo as well as the current culture's format
            //
            Decimal d;
            SqlMoney money;

            const NumberStyles SqlNumberStyle =
                     NumberStyles.AllowCurrencySymbol |
                     NumberStyles.AllowDecimalPoint |
                     NumberStyles.AllowParentheses |
                     NumberStyles.AllowTrailingSign |
                     NumberStyles.AllowLeadingSign |
                     NumberStyles.AllowTrailingWhite |
                     NumberStyles.AllowLeadingWhite;

            if (s == SQLResource.NullString)
            {
                money = SqlMoney.Null;
            }
            else if (Decimal.TryParse(s, SqlNumberStyle, NumberFormatInfo.InvariantInfo, out d))
            {
                money = new SqlMoney(d);
            }
            else
            {
                money = new SqlMoney(Decimal.Parse(s, NumberStyles.Currency, NumberFormatInfo.CurrentInfo));
            }

            return money;
        }

        // Unary operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlMoney operator -(SqlMoney x)
        {
            if (x.IsNull)
                return Null;
            if (x._value == s_minLong)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            return new SqlMoney(-x._value, 0);
        }


        // Binary operators

        // Arithmetic operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlMoney operator +(SqlMoney x, SqlMoney y)
        {
            try
            {
                return (x.IsNull || y.IsNull) ? Null : new SqlMoney(checked(x._value + y._value), 0);
            }
            catch (OverflowException)
            {
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlMoney operator -(SqlMoney x, SqlMoney y)
        {
            try
            {
                return (x.IsNull || y.IsNull) ? Null : new SqlMoney(checked(x._value - y._value), 0);
            }
            catch (OverflowException)
            {
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlMoney operator *(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? Null :
        new SqlMoney(Decimal.Multiply(x.ToDecimal(), y.ToDecimal()));
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlMoney operator /(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? Null :
        new SqlMoney(Decimal.Divide(x.ToDecimal(), y.ToDecimal()));
        }


        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlMoney
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlMoney(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlMoney((int)x.ByteValue);
        }

        // Implicit conversion from SqlByte to SqlMoney
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlMoney(SqlByte x)
        {
            return x.IsNull ? Null : new SqlMoney((int)x.Value);
        }

        // Implicit conversion from SqlInt16 to SqlMoney
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlMoney(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlMoney((int)x.Value);
        }

        // Implicit conversion from SqlInt32 to SqlMoney
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlMoney(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlMoney(x.Value);
        }

        // Implicit conversion from SqlInt64 to SqlMoney
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlMoney(SqlInt64 x)
        {
            return x.IsNull ? Null : new SqlMoney(x.Value);
        }


        // Explicit conversions

        // Explicit conversion from SqlSingle to SqlMoney
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlMoney(SqlSingle x)
        {
            return x.IsNull ? Null : new SqlMoney((double)x.Value);
        }

        // Explicit conversion from SqlDouble to SqlMoney
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlMoney(SqlDouble x)
        {
            return x.IsNull ? Null : new SqlMoney(x.Value);
        }

        // Explicit conversion from SqlDecimal to SqlMoney
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlMoney(SqlDecimal x)
        {
            return x.IsNull ? SqlMoney.Null : new SqlMoney(x.Value);
        }

        // Explicit conversion from SqlString to SqlMoney
        // Throws FormatException or OverflowException if necessary.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlMoney(SqlString x)
        {
            return x.IsNull ? Null : new SqlMoney(Decimal.Parse(x.Value, NumberStyles.Currency, null));
        }


        // Builtin functions

        // Overloading comparison operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator ==(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value == y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator !=(SqlMoney x, SqlMoney y)
        {
            return !(x == y);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value < y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value > y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <=(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value <= y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >=(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value >= y._value);
        }


        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator +
        public static SqlMoney Add(SqlMoney x, SqlMoney y)
        {
            return x + y;
        }
        // Alternative method for operator -
        public static SqlMoney Subtract(SqlMoney x, SqlMoney y)
        {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlMoney Multiply(SqlMoney x, SqlMoney y)
        {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlMoney Divide(SqlMoney x, SqlMoney y)
        {
            return x / y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlMoney x, SqlMoney y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlMoney x, SqlMoney y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlMoney x, SqlMoney y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlMoney x, SqlMoney y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlMoney x, SqlMoney y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlMoney x, SqlMoney y)
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

        public SqlDecimal ToSqlDecimal()
        {
            return (SqlDecimal)this;
        }

        public SqlSingle ToSqlSingle()
        {
            return (SqlSingle)this;
        }

        public SqlString ToSqlString()
        {
            return (SqlString)this;
        }


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
            if (value is SqlMoney)
            {
                SqlMoney i = (SqlMoney)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlMoney));
        }

        public int CompareTo(SqlMoney value)
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
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override bool Equals(Object value)
        {
            if (!(value is SqlMoney))
            {
                return false;
            }

            SqlMoney i = (SqlMoney)value;

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
            // Don't use Value property, because Value will convert to Decimal, which is not necessary.
            return IsNull ? 0 : _value.GetHashCode();
        }


        /// <devdoc>
        ///    <para>
        ///       Represents a null value that can be assigned to
        ///       the <see cref='System.Data.SqlTypes.SqlMoney.Value'/> property of an instance of
        ///       the <see cref='System.Data.SqlTypes.SqlMoney'/>class.
        ///    </para>
        /// </devdoc>
        public static readonly SqlMoney Null = new SqlMoney(true);

        /// <devdoc>
        ///    <para>
        ///       Represents the zero value that can be assigned to the <see cref='System.Data.SqlTypes.SqlMoney.Value'/> property of an instance of
        ///       the <see cref='System.Data.SqlTypes.SqlMoney'/> class.
        ///    </para>
        /// </devdoc>
        public static readonly SqlMoney Zero = new SqlMoney(0);

        /// <devdoc>
        ///    <para>
        ///       Represents the minimum value that can be assigned
        ///       to <see cref='System.Data.SqlTypes.SqlMoney.Value'/> property of an instance of
        ///       the <see cref='System.Data.SqlTypes.SqlMoney'/>
        ///       class.
        ///    </para>
        /// </devdoc>
        public static readonly SqlMoney MinValue = new SqlMoney(unchecked((long)0x8000000000000000L), 0);

        /// <devdoc>
        ///    <para>
        ///       Represents the maximum value that can be assigned to
        ///       the <see cref='System.Data.SqlTypes.SqlMoney.Value'/> property of an instance of
        ///       the <see cref='System.Data.SqlTypes.SqlMoney'/>
        ///       class.
        ///    </para>
        /// </devdoc>
        public static readonly SqlMoney MaxValue = new SqlMoney(0x7FFFFFFFFFFFFFFFL, 0);
    } // SqlMoney
} // namespace System.Data.SqlTypes
