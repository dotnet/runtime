// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------


using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace System.Data.SqlTypes
{
    /// <devdoc>
    ///    <para>
    ///       Represents an integer value that is either 1 or 0.
    ///    </para>
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public struct SqlBoolean : INullable, IComparable
    {
        // m_value: 2 (true), 1 (false), 0 (unknown/Null)
        private byte _value;

        private const byte x_Null = 0;
        private const byte x_False = 1;
        private const byte x_True = 2;

        // constructor

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlBoolean'/> class.
        ///    </para>
        /// </devdoc>
        public SqlBoolean(bool value)
        {
            _value = (byte)(value ? x_True : x_False);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlBoolean(int value) : this(value, false)
        {
        }

        private SqlBoolean(int value, bool fNull)
        {
            if (fNull)
                _value = x_Null;
            else
                _value = (value != 0) ? x_True : x_False;
        }


        // INullable
        /// <devdoc>
        ///    <para>
        ///       Gets whether the current <see cref='System.Data.SqlTypes.SqlBoolean.Value'/> is <see cref='System.Data.SqlTypes.SqlBoolean.Null'/>.
        ///    </para>
        /// </devdoc>
        public bool IsNull
        {
            get { return _value == x_Null; }
        }

        // property: Value
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the <see cref='System.Data.SqlTypes.SqlBoolean'/> to be <see langword='true'/> or
        ///    <see langword='false'/>.
        ///    </para>
        /// </devdoc>
        public bool Value
        {
            get
            {
                switch (_value)
                {
                    case x_True:
                        return true;

                    case x_False:
                        return false;

                    default:
                        throw new SqlNullValueException();
                }
            }
        }

        // property: IsTrue
        /// <devdoc>
        ///    <para>
        ///       Gets whether the current <see cref='System.Data.SqlTypes.SqlBoolean.Value'/> is <see cref='System.Data.SqlTypes.SqlBoolean.True'/>.
        ///    </para>
        /// </devdoc>
        public bool IsTrue
        {
            get { return _value == x_True; }
        }

        // property: IsFalse
        /// <devdoc>
        ///    <para>
        ///       Gets whether the current <see cref='System.Data.SqlTypes.SqlBoolean.Value'/> is <see cref='System.Data.SqlTypes.SqlBoolean.False'/>.
        ///    </para>
        /// </devdoc>
        public bool IsFalse
        {
            get { return _value == x_False; }
        }


        // Implicit conversion from bool to SqlBoolean
        /// <devdoc>
        ///    <para>
        ///       Converts a boolean to a <see cref='System.Data.SqlTypes.SqlBoolean'/>.
        ///    </para>
        /// </devdoc>
        public static implicit operator SqlBoolean(bool x)
        {
            return new SqlBoolean(x);
        }

        // Explicit conversion from SqlBoolean to bool. Throw exception if x is Null.
        /// <devdoc>
        ///    <para>
        ///       Converts a <see cref='System.Data.SqlTypes.SqlBoolean'/>
        ///       to a boolean.
        ///    </para>
        /// </devdoc>
        public static explicit operator bool (SqlBoolean x)
        {
            return x.Value;
        }


        // Unary operators

        /// <devdoc>
        ///    <para>
        ///       Performs a NOT operation on a <see cref='System.Data.SqlTypes.SqlBoolean'/>
        ///       .
        ///    </para>
        /// </devdoc>
        public static SqlBoolean operator !(SqlBoolean x)
        {
            switch (x._value)
            {
                case x_True:
                    return SqlBoolean.False;

                case x_False:
                    return SqlBoolean.True;

                default:
                    Debug.Assert(x._value == x_Null);
                    return SqlBoolean.Null;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static bool operator true(SqlBoolean x)
        {
            return x.IsTrue;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static bool operator false(SqlBoolean x)
        {
            return x.IsFalse;
        }

        // Binary operators

        /// <devdoc>
        ///    <para>
        ///       Performs a bitwise AND operation on two instances of
        ///    <see cref='System.Data.SqlTypes.SqlBoolean'/>
        ///    .
        /// </para>
        /// </devdoc>
        public static SqlBoolean operator &(SqlBoolean x, SqlBoolean y)
        {
            if (x._value == x_False || y._value == x_False)
                return SqlBoolean.False;
            else if (x._value == x_True && y._value == x_True)
                return SqlBoolean.True;
            else
                return SqlBoolean.Null;
        }

        /// <devdoc>
        ///    <para>
        ///       Performs
        ///       a bitwise OR operation on two instances of a
        ///    <see cref='System.Data.SqlTypes.SqlBoolean'/>
        ///    .
        /// </para>
        /// </devdoc>
        public static SqlBoolean operator |(SqlBoolean x, SqlBoolean y)
        {
            if (x._value == x_True || y._value == x_True)
                return SqlBoolean.True;
            else if (x._value == x_False && y._value == x_False)
                return SqlBoolean.False;
            else
                return SqlBoolean.Null;
        }



        // property: ByteValue
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public byte ByteValue
        {
            get
            {
                if (!IsNull)
                    return (_value == x_True) ? (byte)1 : (byte)0;
                else
                    throw new SqlNullValueException();
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override String ToString()
        {
            return IsNull ?
                SQLResource.NullString :
                Value.ToString();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean Parse(String s)
        {
            if (null == s)
                // Let Boolean.Parse throw exception
                return new SqlBoolean(Boolean.Parse(s));
            if (s == SQLResource.NullString)
                return SqlBoolean.Null;

            s = s.TrimStart();
            char wchFirst = s[0];
            if (Char.IsNumber(wchFirst) || ('-' == wchFirst) || ('+' == wchFirst))
            {
                return new SqlBoolean(Int32.Parse(s, (IFormatProvider)null));
            }
            else
            {
                return new SqlBoolean(Boolean.Parse(s));
            }
        }


        // Unary operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator ~(SqlBoolean x)
        {
            return (!x);
        }


        // Binary operators

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator ^(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlBoolean(x._value != y._value);
        }



        // Implicit conversions


        // Explicit conversions

        // Explicit conversion from SqlByte to SqlBoolean
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlBoolean(SqlByte x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0);
        }

        // Explicit conversion from SqlInt16 to SqlBoolean
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlBoolean(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0);
        }

        // Explicit conversion from SqlInt32 to SqlBoolean
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlBoolean(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0);
        }

        // Explicit conversion from SqlInt64 to SqlBoolean
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlBoolean(SqlInt64 x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0);
        }

        // Explicit conversion from SqlDouble to SqlBoolean
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlBoolean(SqlDouble x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0.0);
        }

        // Explicit conversion from SqlSingle to SqlBoolean
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlBoolean(SqlSingle x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0.0);
        }

        // Explicit conversion from SqlMoney to SqlBoolean
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlBoolean(SqlMoney x)
        {
            return x.IsNull ? Null : (x != SqlMoney.Zero);
        }

        // Explicit conversion from SqlDecimal to SqlBoolean
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlBoolean(SqlDecimal x)
        {
            return x.IsNull ? SqlBoolean.Null : new SqlBoolean(x.m_data1 != 0 || x.m_data2 != 0 ||
                                                       x.m_data3 != 0 || x.m_data4 != 0);
        }

        // Explicit conversion from SqlString to SqlBoolean
        // Throws FormatException or OverflowException if necessary.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlBoolean(SqlString x)
        {
            return x.IsNull ? Null : SqlBoolean.Parse(x.Value);
        }

        // Overloading comparison operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator ==(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value == y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator !=(SqlBoolean x, SqlBoolean y)
        {
            return !(x == y);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value < y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value > y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <=(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value <= y._value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >=(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value >= y._value);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator ~
        public static SqlBoolean OnesComplement(SqlBoolean x)
        {
            return ~x;
        }

        // Alternative method for operator &
        public static SqlBoolean And(SqlBoolean x, SqlBoolean y)
        {
            return x & y;
        }

        // Alternative method for operator |
        public static SqlBoolean Or(SqlBoolean x, SqlBoolean y)
        {
            return x | y;
        }

        // Alternative method for operator ^
        public static SqlBoolean Xor(SqlBoolean x, SqlBoolean y)
        {
            return x ^ y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlBoolean x, SqlBoolean y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlBoolean x, SqlBoolean y)
        {
            return (x != y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlBoolean x, SqlBoolean y)
        {
            return (x > y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlBoolean x, SqlBoolean y)
        {
            return (x < y);
        }

        // Alternative method for operator <=
        public static SqlBoolean GreaterThanOrEquals(SqlBoolean x, SqlBoolean y)
        {
            return (x >= y);
        }

        // Alternative method for operator !=
        public static SqlBoolean LessThanOrEquals(SqlBoolean x, SqlBoolean y)
        {
            return (x <= y);
        }

        // Alternative method for conversions.

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
            if (value is SqlBoolean)
            {
                SqlBoolean i = (SqlBoolean)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlBoolean));
        }

        public int CompareTo(SqlBoolean value)
        {
            // If both Null, consider them equal.
            // Otherwise, Null is less than anything.
            if (IsNull)
                return value.IsNull ? 0 : -1;
            else if (value.IsNull)
                return 1;

            if (this.ByteValue < value.ByteValue) return -1;
            if (this.ByteValue > value.ByteValue) return 1;
            return 0;
        }

        // Compares this instance with a specified object
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override bool Equals(Object value)
        {
            if (!(value is SqlBoolean))
            {
                return false;
            }

            SqlBoolean i = (SqlBoolean)value;

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
        ///    <para>
        ///       Represents a true value that can be assigned to the
        ///    <see cref='System.Data.SqlTypes.SqlBoolean.Value'/> property of an instance of
        ///       the <see cref='System.Data.SqlTypes.SqlBoolean'/> class.
        ///    </para>
        /// </devdoc>
        public static readonly SqlBoolean True = new SqlBoolean(true);
        /// <devdoc>
        ///    <para>
        ///       Represents a false value that can be assigned to the
        ///    <see cref='System.Data.SqlTypes.SqlBoolean.Value'/> property of an instance of
        ///       the <see cref='System.Data.SqlTypes.SqlBoolean'/> class.
        ///    </para>
        /// </devdoc>
        public static readonly SqlBoolean False = new SqlBoolean(false);
        /// <devdoc>
        ///    <para>
        ///       Represents a null value that can be assigned to the <see cref='System.Data.SqlTypes.SqlBoolean.Value'/> property of an instance of
        ///       the <see cref='System.Data.SqlTypes.SqlBoolean'/> class.
        ///    </para>
        /// </devdoc>
        public static readonly SqlBoolean Null = new SqlBoolean(0, true);

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlBoolean Zero = new SqlBoolean(0);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlBoolean One = new SqlBoolean(1);
    } // SqlBoolean
} // namespace System.Data.SqlTypes
