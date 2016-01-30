// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------


using System.Data.Common;
using System.Runtime.InteropServices;


namespace System.Data.SqlTypes
{
    /// <devdoc>
    ///    <para>
    ///       Represents a 16-bit signed integer to be stored in
    ///       or retrieved from a database.
    ///    </para>
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public struct SqlInt16 : INullable, IComparable
    {
        private bool _fNotNull; // false if null
        private short _value;

        private static readonly int s_MASKI2 = ~0x00007fff;

        // constructor
        // construct a Null
        private SqlInt16(bool fNull)
        {
            _fNotNull = false;
            _value = 0;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlInt16(short value)
        {
            _value = value;
            _fNotNull = true;
        }

        // INullable
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsNull
        {
            get { return !_fNotNull; }
        }

        // property: Value
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public short Value
        {
            get
            {
                if (_fNotNull)
                    return _value;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from short to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlInt16(short x)
        {
            return new SqlInt16(x);
        }

        // Explicit conversion from SqlInt16 to short. Throw exception if x is Null.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator short (SqlInt16 x)
        {
            return x.Value;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override String ToString()
        {
            return IsNull ? SQLResource.NullString : _value.ToString((IFormatProvider)null);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 Parse(String s)
        {
            if (s == SQLResource.NullString)
                return SqlInt16.Null;
            else
                return new SqlInt16(Int16.Parse(s, (IFormatProvider)null));
        }


        // Unary operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator -(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlInt16((short)-x._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator ~(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlInt16((short)~x._value);
        }


        // Binary operators

        // Arithmetic operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator +(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = (int)x._value + (int)y._value;
            if ((((iResult >> 15) ^ (iResult >> 16)) & 1) != 0) // Bit 15 != bit 16
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)iResult);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator -(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = (int)x._value - (int)y._value;
            if ((((iResult >> 15) ^ (iResult >> 16)) & 1) != 0) // Bit 15 != bit 16
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)iResult);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator *(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = (int)x._value * (int)y._value;
            int iTemp = iResult & s_MASKI2;
            if (iTemp != 0 && iTemp != s_MASKI2)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)iResult);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator /(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y._value != 0)
            {
                if ((x._value == Int16.MinValue) && (y._value == -1))
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                return new SqlInt16((short)(x._value / y._value));
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator %(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y._value != 0)
            {
                if ((x._value == Int16.MinValue) && (y._value == -1))
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                return new SqlInt16((short)(x._value % y._value));
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        // Bitwise operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator &(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt16((short)(x._value & y._value));
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator |(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt16((short)((ushort)x._value | (ushort)y._value));
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlInt16 operator ^(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt16((short)(x._value ^ y._value));
        }



        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlInt16(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlInt16((short)(x.ByteValue));
        }

        // Implicit conversion from SqlByte to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlInt16(SqlByte x)
        {
            return x.IsNull ? Null : new SqlInt16((short)(x.Value));
        }

        // Explicit conversions

        // Explicit conversion from SqlInt32 to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlInt16(SqlInt32 x)
        {
            if (x.IsNull)
                return Null;

            int value = x.Value;
            if (value > (int)Int16.MaxValue || value < (int)Int16.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)value);
        }

        // Explicit conversion from SqlInt64 to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlInt16(SqlInt64 x)
        {
            if (x.IsNull)
                return Null;

            long value = x.Value;
            if (value > (long)Int16.MaxValue || value < (long)Int16.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)value);
        }

        // Explicit conversion from SqlSingle to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlInt16(SqlSingle x)
        {
            if (x.IsNull)
                return Null;

            float value = x.Value;
            if (value < (float)Int16.MinValue || value > (float)Int16.MaxValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)value);
        }

        // Explicit conversion from SqlDouble to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlInt16(SqlDouble x)
        {
            if (x.IsNull)
                return Null;

            double value = x.Value;
            if (value < (double)Int16.MinValue || value > (double)Int16.MaxValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)value);
        }

        // Explicit conversion from SqlMoney to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlInt16(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlInt16(checked((short)x.ToInt32()));
        }

        // Explicit conversion from SqlDecimal to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlInt16(SqlDecimal x)
        {
            return (SqlInt16)(SqlInt32)x;
        }

        // Explicit conversion from SqlString to SqlInt16
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlInt16(SqlString x)
        {
            return x.IsNull ? Null : new SqlInt16(Int16.Parse(x.Value, (IFormatProvider)null));
        }

        // Overloading comparison operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator ==(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value == y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator !=(SqlInt16 x, SqlInt16 y)
        {
            return !(x == y);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value < y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value > y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <=(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value <= y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >=(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value >= y._value);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator ~
        public static SqlInt16 OnesComplement(SqlInt16 x)
        {
            return ~x;
        }

        // Alternative method for operator +
        public static SqlInt16 Add(SqlInt16 x, SqlInt16 y)
        {
            return x + y;
        }
        // Alternative method for operator -
        public static SqlInt16 Subtract(SqlInt16 x, SqlInt16 y)
        {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlInt16 Multiply(SqlInt16 x, SqlInt16 y)
        {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlInt16 Divide(SqlInt16 x, SqlInt16 y)
        {
            return x / y;
        }

        // Alternative method for operator %
        public static SqlInt16 Mod(SqlInt16 x, SqlInt16 y)
        {
            return x % y;
        }

        public static SqlInt16 Modulus(SqlInt16 x, SqlInt16 y)
        {
            return x % y;
        }

        // Alternative method for operator &
        public static SqlInt16 BitwiseAnd(SqlInt16 x, SqlInt16 y)
        {
            return x & y;
        }

        // Alternative method for operator |
        public static SqlInt16 BitwiseOr(SqlInt16 x, SqlInt16 y)
        {
            return x | y;
        }

        // Alternative method for operator ^
        public static SqlInt16 Xor(SqlInt16 x, SqlInt16 y)
        {
            return x ^ y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlInt16 x, SqlInt16 y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlInt16 x, SqlInt16 y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlInt16 x, SqlInt16 y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlInt16 x, SqlInt16 y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlInt16 x, SqlInt16 y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlInt16 x, SqlInt16 y)
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
            if (value is SqlInt16)
            {
                SqlInt16 i = (SqlInt16)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlInt16));
        }

        public int CompareTo(SqlInt16 value)
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
            if (!(value is SqlInt16))
            {
                return false;
            }

            SqlInt16 i = (SqlInt16)value;

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
            return IsNull ? 0 : Value.GetHashCode();
        }


        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlInt16 Null = new SqlInt16(true);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlInt16 Zero = new SqlInt16(0);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlInt16 MinValue = new SqlInt16(Int16.MinValue);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlInt16 MaxValue = new SqlInt16(Int16.MaxValue);
    } // SqlInt16
} // namespace System.Data.SqlTypes
