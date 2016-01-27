// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------


using System.Data.Common;
using System.Runtime.InteropServices;
using System.Globalization;


namespace System.Data.SqlTypes
{
    /// <devdoc>
    ///    <para>
    ///       Represents a floating point number within the range of -3.40E +38 through
    ///       3.40E +38 to be stored in or retrieved from a database.
    ///    </para>
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public struct SqlSingle : INullable, IComparable
    {
        private bool _fNotNull; // false if null
        private float _value;
        // constructor
        // construct a Null
        private SqlSingle(bool fNull)
        {
            _fNotNull = false;
            _value = (float)0.0;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlSingle(float value)
        {
            if (Single.IsInfinity(value) || Single.IsNaN(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
            {
                _fNotNull = true;
                _value = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlSingle(double value) : this(checked((float)value))
        {
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
        public float Value
        {
            get
            {
                if (_fNotNull)
                    return _value;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from float to SqlSingle
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlSingle(float x)
        {
            return new SqlSingle(x);
        }

        // Explicit conversion from SqlSingle to float. Throw exception if x is Null.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator float (SqlSingle x)
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
        public static SqlSingle Parse(String s)
        {
            if (s == SQLResource.NullString)
                return SqlSingle.Null;
            else
                return new SqlSingle(Single.Parse(s, CultureInfo.InvariantCulture));
        }


        // Unary operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlSingle operator -(SqlSingle x)
        {
            return x.IsNull ? Null : new SqlSingle(-x._value);
        }


        // Binary operators

        // Arithmetic operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlSingle operator +(SqlSingle x, SqlSingle y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            float value = x._value + y._value;

            if (Single.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlSingle(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlSingle operator -(SqlSingle x, SqlSingle y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            float value = x._value - y._value;

            if (Single.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlSingle(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlSingle operator *(SqlSingle x, SqlSingle y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            float value = x._value * y._value;

            if (Single.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlSingle(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlSingle operator /(SqlSingle x, SqlSingle y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y._value == (float)0.0)
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);

            float value = x._value / y._value;

            if (Single.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlSingle(value);
        }



        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlSingle
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlSingle(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlSingle(x.ByteValue);
        }

        // Implicit conversion from SqlByte to SqlSingle
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlSingle(SqlByte x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle((float)(x.Value));
        }

        // Implicit conversion from SqlInt16 to SqlSingle
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlSingle(SqlInt16 x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle((float)(x.Value));
        }

        // Implicit conversion from SqlInt32 to SqlSingle
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlSingle(SqlInt32 x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle((float)(x.Value));
        }

        // Implicit conversion from SqlInt64 to SqlSingle
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlSingle(SqlInt64 x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle((float)(x.Value));
        }

        // Implicit conversion from SqlMoney to SqlSingle
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlSingle(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlSingle(x.ToDouble());
        }

        // Implicit conversion from SqlDecimal to SqlSingle
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlSingle(SqlDecimal x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle(x.ToDouble());
        }


        // Explicit conversions


        // Explicit conversion from SqlDouble to SqlSingle
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlSingle(SqlDouble x)
        {
            return x.IsNull ? Null : new SqlSingle(x.Value);
        }

        // Explicit conversion from SqlString to SqlSingle
        // Throws FormatException or OverflowException if necessary.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlSingle(SqlString x)
        {
            if (x.IsNull)
                return SqlSingle.Null;
            return Parse(x.Value);
        }

        // Overloading comparison operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator ==(SqlSingle x, SqlSingle y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value == y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator !=(SqlSingle x, SqlSingle y)
        {
            return !(x == y);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <(SqlSingle x, SqlSingle y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value < y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >(SqlSingle x, SqlSingle y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value > y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <=(SqlSingle x, SqlSingle y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value <= y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >=(SqlSingle x, SqlSingle y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value >= y._value);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator +
        public static SqlSingle Add(SqlSingle x, SqlSingle y)
        {
            return x + y;
        }
        // Alternative method for operator -
        public static SqlSingle Subtract(SqlSingle x, SqlSingle y)
        {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlSingle Multiply(SqlSingle x, SqlSingle y)
        {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlSingle Divide(SqlSingle x, SqlSingle y)
        {
            return x / y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlSingle x, SqlSingle y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlSingle x, SqlSingle y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlSingle x, SqlSingle y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlSingle x, SqlSingle y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlSingle x, SqlSingle y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlSingle x, SqlSingle y)
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

        public SqlMoney ToSqlMoney()
        {
            return (SqlMoney)this;
        }

        public SqlDecimal ToSqlDecimal()
        {
            return (SqlDecimal)this;
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
            if (value is SqlSingle)
            {
                SqlSingle i = (SqlSingle)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlSingle));
        }

        public int CompareTo(SqlSingle value)
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
            if (!(value is SqlSingle))
            {
                return false;
            }

            SqlSingle i = (SqlSingle)value;

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
        public static readonly SqlSingle Null = new SqlSingle(true);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlSingle Zero = new SqlSingle((float)0.0);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlSingle MinValue = new SqlSingle(Single.MinValue);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlSingle MaxValue = new SqlSingle(Single.MaxValue);
    } // SqlSingle
} // namespace System.Data.SqlTypes
