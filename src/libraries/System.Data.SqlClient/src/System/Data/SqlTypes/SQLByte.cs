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
    ///       Represents an 8-bit unsigned integer to be stored in
    ///       or retrieved from a database.
    ///    </para>
    /// </devdoc>

    [StructLayout(LayoutKind.Sequential)]
    public struct SqlByte : INullable, IComparable
    {
        private bool _fNotNull; // false if null
        private byte _value;

        private static readonly int s_iBitNotByteMax = ~0xff;

        // constructor
        // construct a Null
        private SqlByte(bool fNull)
        {
            _fNotNull = false;
            _value = 0;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlByte(byte value)
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
        public byte Value
        {
            get
            {
                if (_fNotNull)
                    return _value;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from byte to SqlByte
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlByte(byte x)
        {
            return new SqlByte(x);
        }

        // Explicit conversion from SqlByte to byte. Throw exception if x is Null.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator byte (SqlByte x)
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
        public static SqlByte Parse(String s)
        {
            if (s == SQLResource.NullString)
                return SqlByte.Null;
            else
                return new SqlByte(Byte.Parse(s, (IFormatProvider)null));
        }

        // Unary operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlByte operator ~(SqlByte x)
        {
            return x.IsNull ? Null : new SqlByte((byte)~x._value);
        }


        // Binary operators

        // Arithmetic operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlByte operator +(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = (int)x._value + (int)y._value;
            if ((iResult & s_iBitNotByteMax) != 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlByte((byte)iResult);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlByte operator -(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = (int)x._value - (int)y._value;
            if ((iResult & s_iBitNotByteMax) != 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlByte((byte)iResult);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlByte operator *(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = (int)x._value * (int)y._value;
            if ((iResult & s_iBitNotByteMax) != 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlByte((byte)iResult);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlByte operator /(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y._value != 0)
            {
                return new SqlByte((byte)(x._value / y._value));
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlByte operator %(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y._value != 0)
            {
                return new SqlByte((byte)(x._value % y._value));
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        // Bitwise operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlByte operator &(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlByte((byte)(x._value & y._value));
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlByte operator |(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlByte((byte)(x._value | y._value));
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlByte operator ^(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlByte((byte)(x._value ^ y._value));
        }



        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlByte
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlByte(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlByte((byte)(x.ByteValue));
        }


        // Explicit conversions

        // Explicit conversion from SqlMoney to SqlByte
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlByte(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlByte(checked((byte)x.ToInt32()));
        }

        // Explicit conversion from SqlInt16 to SqlByte
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlByte(SqlInt16 x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > (short)Byte.MaxValue || x.Value < (short)Byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlInt32 to SqlByte
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlByte(SqlInt32 x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > (int)Byte.MaxValue || x.Value < (int)Byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlInt64 to SqlByte
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlByte(SqlInt64 x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > (long)Byte.MaxValue || x.Value < (long)Byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlSingle to SqlByte
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlByte(SqlSingle x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > (float)Byte.MaxValue || x.Value < (float)Byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlDouble to SqlByte
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlByte(SqlDouble x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > (double)Byte.MaxValue || x.Value < (double)Byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlDecimal to SqlByte
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlByte(SqlDecimal x)
        {
            return (SqlByte)(SqlInt32)x;
        }

        // Implicit conversion from SqlString to SqlByte
        // Throws FormatException or OverflowException if necessary.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlByte(SqlString x)
        {
            return x.IsNull ? Null : new SqlByte(Byte.Parse(x.Value, (IFormatProvider)null));
        }

        // Overloading comparison operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator ==(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value == y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator !=(SqlByte x, SqlByte y)
        {
            return !(x == y);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value < y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value > y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <=(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value <= y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >=(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value >= y._value);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator ~
        public static SqlByte OnesComplement(SqlByte x)
        {
            return ~x;
        }

        // Alternative method for operator +
        public static SqlByte Add(SqlByte x, SqlByte y)
        {
            return x + y;
        }

        // Alternative method for operator -
        public static SqlByte Subtract(SqlByte x, SqlByte y)
        {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlByte Multiply(SqlByte x, SqlByte y)
        {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlByte Divide(SqlByte x, SqlByte y)
        {
            return x / y;
        }

        // Alternative method for operator %
        public static SqlByte Mod(SqlByte x, SqlByte y)
        {
            return x % y;
        }

        public static SqlByte Modulus(SqlByte x, SqlByte y)
        {
            return x % y;
        }

        // Alternative method for operator &
        public static SqlByte BitwiseAnd(SqlByte x, SqlByte y)
        {
            return x & y;
        }

        // Alternative method for operator |
        public static SqlByte BitwiseOr(SqlByte x, SqlByte y)
        {
            return x | y;
        }

        // Alternative method for operator ^
        public static SqlByte Xor(SqlByte x, SqlByte y)
        {
            return x ^ y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlByte x, SqlByte y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlByte x, SqlByte y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlByte x, SqlByte y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlByte x, SqlByte y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlByte x, SqlByte y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlByte x, SqlByte y)
        {
            return (x >= y);
        }

        // Alternative method for conversions.

        public SqlBoolean ToSqlBoolean()
        {
            return (SqlBoolean)this;
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
            if (value is SqlByte)
            {
                SqlByte i = (SqlByte)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlByte));
        }

        public int CompareTo(SqlByte value)
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
            if (!(value is SqlByte))
            {
                return false;
            }

            SqlByte i = (SqlByte)value;

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
        public static readonly SqlByte Null = new SqlByte(true);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlByte Zero = new SqlByte(0);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlByte MinValue = new SqlByte(Byte.MinValue);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlByte MaxValue = new SqlByte(Byte.MaxValue);
    } // SqlByte
} // namespace System
