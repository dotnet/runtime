// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------


using System.Data.Common;


namespace System.Data.SqlTypes
{
    /// <devdoc>
    ///    <para>
    ///       Represents a globally unique identifier to be stored in
    ///       or retrieved from a database.
    ///    </para>
    /// </devdoc>
    public struct SqlGuid : INullable, IComparable
    {
        private static readonly int s_sizeOfGuid = 16;

        // Comparison orders.
        private static readonly int[] s_rgiGuidOrder = new int[16]
        {10, 11, 12, 13, 14, 15, 8, 9, 6, 7, 4, 5, 0, 1, 2, 3};

        private byte[] _value; // the SqlGuid is null if m_value is null

        // constructor
        // construct a SqlGuid.Null
        private SqlGuid(bool fNull)
        {
            _value = null;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlGuid(byte[] value)
        {
            if (value == null || value.Length != s_sizeOfGuid)
                throw new ArgumentException(SQLResource.InvalidArraySizeMessage);

            _value = new byte[s_sizeOfGuid];
            value.CopyTo(_value, 0);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal SqlGuid(byte[] value, bool ignored)
        {
            if (value == null || value.Length != s_sizeOfGuid)
                throw new ArgumentException(SQLResource.InvalidArraySizeMessage);

            _value = value;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlGuid(String s)
        {
            _value = (new Guid(s)).ToByteArray();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlGuid(Guid g)
        {
            _value = g.ToByteArray();
        }

        public SqlGuid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
            : this(new Guid(a, b, c, d, e, f, g, h, i, j, k))
        {
        }


        // INullable
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsNull
        {
            get { return (_value == null); }
        }

        // property: Value
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Guid Value
        {
            get
            {
                if (IsNull)
                    throw new SqlNullValueException();
                else
                    return new Guid(_value);
            }
        }

        // Implicit conversion from Guid to SqlGuid
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlGuid(Guid x)
        {
            return new SqlGuid(x);
        }

        // Explicit conversion from SqlGuid to Guid. Throw exception if x is Null.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator Guid(SqlGuid x)
        {
            return x.Value;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public byte[] ToByteArray()
        {
            byte[] ret = new byte[s_sizeOfGuid];
            _value.CopyTo(ret, 0);
            return ret;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override String ToString()
        {
            if (IsNull)
                return SQLResource.NullString;

            Guid g = new Guid(_value);
            return g.ToString();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlGuid Parse(String s)
        {
            if (s == SQLResource.NullString)
                return SqlGuid.Null;
            else
                return new SqlGuid(s);
        }


        // Comparison operators
        private static EComparison Compare(SqlGuid x, SqlGuid y)
        {
            //Swap to the correct order to be compared
            for (int i = 0; i < s_sizeOfGuid; i++)
            {
                byte b1, b2;

                b1 = x._value[s_rgiGuidOrder[i]];
                b2 = y._value[s_rgiGuidOrder[i]];
                if (b1 != b2)
                    return (b1 < b2) ? EComparison.LT : EComparison.GT;
            }
            return EComparison.EQ;
        }



        // Implicit conversions

        // Explicit conversions

        // Explicit conversion from SqlString to SqlGuid
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlGuid(SqlString x)
        {
            return x.IsNull ? Null : new SqlGuid(x.Value);
        }

        // Explicit conversion from SqlBinary to SqlGuid
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlGuid(SqlBinary x)
        {
            return x.IsNull ? Null : new SqlGuid(x.Value);
        }

        // Overloading comparison operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator ==(SqlGuid x, SqlGuid y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(Compare(x, y) == EComparison.EQ);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator !=(SqlGuid x, SqlGuid y)
        {
            return !(x == y);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <(SqlGuid x, SqlGuid y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(Compare(x, y) == EComparison.LT);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >(SqlGuid x, SqlGuid y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(Compare(x, y) == EComparison.GT);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator <=(SqlGuid x, SqlGuid y)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            EComparison cmp = Compare(x, y);
            return new SqlBoolean(cmp == EComparison.LT || cmp == EComparison.EQ);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator >=(SqlGuid x, SqlGuid y)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            EComparison cmp = Compare(x, y);
            return new SqlBoolean(cmp == EComparison.GT || cmp == EComparison.EQ);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlGuid x, SqlGuid y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlGuid x, SqlGuid y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlGuid x, SqlGuid y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlGuid x, SqlGuid y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlGuid x, SqlGuid y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlGuid x, SqlGuid y)
        {
            return (x >= y);
        }

        // Alternative method for conversions.

        public SqlString ToSqlString()
        {
            return (SqlString)this;
        }

        public SqlBinary ToSqlBinary()
        {
            return (SqlBinary)this;
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
            if (value is SqlGuid)
            {
                SqlGuid i = (SqlGuid)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlGuid));
        }

        public int CompareTo(SqlGuid value)
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
            if (!(value is SqlGuid))
            {
                return false;
            }

            SqlGuid i = (SqlGuid)value;

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
        public static readonly SqlGuid Null = new SqlGuid(true);
    } // SqlGuid
} // namespace System.Data.SqlTypes

