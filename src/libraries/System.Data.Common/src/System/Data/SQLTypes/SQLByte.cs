// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.SqlTypes
{
    /// <summary>
    /// Represents an 8-bit unsigned integer to be stored in
    /// or retrieved from a database.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public struct SqlByte : INullable, IComparable, IXmlSerializable, IEquatable<SqlByte>
    {
        private bool m_fNotNull; // false if null. Do not rename (binary serialization)
        private byte m_value; // Do not rename (binary serialization)

        private const int s_iBitNotByteMax = ~0xff;

        // constructor
        // construct a Null
        private SqlByte(bool fNull)
        {
            m_fNotNull = false;
            m_value = 0;
        }

        public SqlByte(byte value)
        {
            m_value = value;
            m_fNotNull = true;
        }

        // INullable
        public bool IsNull
        {
            get { return !m_fNotNull; }
        }

        // property: Value
        public byte Value
        {
            get
            {
                if (m_fNotNull)
                    return m_value;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from byte to SqlByte
        public static implicit operator SqlByte(byte x)
        {
            return new SqlByte(x);
        }

        // Explicit conversion from SqlByte to byte. Throw exception if x is Null.
        public static explicit operator byte(SqlByte x)
        {
            return x.Value;
        }

        public override string ToString()
        {
            return IsNull ? SQLResource.NullString : m_value.ToString((IFormatProvider)null!);
        }

        public static SqlByte Parse(string s)
        {
            if (s == SQLResource.NullString)
                return SqlByte.Null;
            else
                return new SqlByte(byte.Parse(s, null));
        }

        // Unary operators
        public static SqlByte operator ~(SqlByte x)
        {
            return x.IsNull ? Null : new SqlByte(unchecked((byte)~x.m_value));
        }


        // Binary operators

        // Arithmetic operators
        public static SqlByte operator +(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = x.m_value + y.m_value;
            if ((iResult & s_iBitNotByteMax) != 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlByte((byte)iResult);
        }

        public static SqlByte operator -(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = x.m_value - y.m_value;
            if ((iResult & s_iBitNotByteMax) != 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlByte((byte)iResult);
        }

        public static SqlByte operator *(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = x.m_value * y.m_value;
            if ((iResult & s_iBitNotByteMax) != 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlByte((byte)iResult);
        }

        public static SqlByte operator /(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value != 0)
            {
                return new SqlByte((byte)(x.m_value / y.m_value));
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        public static SqlByte operator %(SqlByte x, SqlByte y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value != 0)
            {
                return new SqlByte((byte)(x.m_value % y.m_value));
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        // Bitwise operators
        public static SqlByte operator &(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlByte((byte)(x.m_value & y.m_value));
        }

        public static SqlByte operator |(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlByte((byte)(x.m_value | y.m_value));
        }

        public static SqlByte operator ^(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlByte((byte)(x.m_value ^ y.m_value));
        }



        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlByte
        public static explicit operator SqlByte(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlByte(x.ByteValue);
        }


        // Explicit conversions

        // Explicit conversion from SqlMoney to SqlByte
        public static explicit operator SqlByte(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlByte(checked((byte)x.ToInt32()));
        }

        // Explicit conversion from SqlInt16 to SqlByte
        public static explicit operator SqlByte(SqlInt16 x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > byte.MaxValue || x.Value < byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlInt32 to SqlByte
        public static explicit operator SqlByte(SqlInt32 x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > byte.MaxValue || x.Value < byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlInt64 to SqlByte
        public static explicit operator SqlByte(SqlInt64 x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > byte.MaxValue || x.Value < byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlSingle to SqlByte
        public static explicit operator SqlByte(SqlSingle x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > byte.MaxValue || x.Value < byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlDouble to SqlByte
        public static explicit operator SqlByte(SqlDouble x)
        {
            if (x.IsNull)
                return Null;

            if (x.Value > byte.MaxValue || x.Value < byte.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return x.IsNull ? Null : new SqlByte((byte)(x.Value));
        }

        // Explicit conversion from SqlDecimal to SqlByte
        public static explicit operator SqlByte(SqlDecimal x)
        {
            return (SqlByte)(SqlInt32)x;
        }

        // Implicit conversion from SqlString to SqlByte
        // Throws FormatException or OverflowException if necessary.
        public static explicit operator SqlByte(SqlString x)
        {
            return x.IsNull ? Null : new SqlByte(byte.Parse(x.Value, null));
        }

        // Overloading comparison operators
        public static SqlBoolean operator ==(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value == y.m_value);
        }

        public static SqlBoolean operator !=(SqlByte x, SqlByte y)
        {
            return !(x == y);
        }

        public static SqlBoolean operator <(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value < y.m_value);
        }

        public static SqlBoolean operator >(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value > y.m_value);
        }

        public static SqlBoolean operator <=(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value <= y.m_value);
        }

        public static SqlBoolean operator >=(SqlByte x, SqlByte y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value >= y.m_value);
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
            return this;
        }

        public SqlInt16 ToSqlInt16()
        {
            return this;
        }

        public SqlInt32 ToSqlInt32()
        {
            return this;
        }

        public SqlInt64 ToSqlInt64()
        {
            return this;
        }

        public SqlMoney ToSqlMoney()
        {
            return this;
        }

        public SqlDecimal ToSqlDecimal()
        {
            return this;
        }

        public SqlSingle ToSqlSingle()
        {
            return this;
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
        public int CompareTo(object? value)
        {
            if (value is SqlByte i)
            {
                return CompareTo(i);
            }
            throw ADP.WrongType(value!.GetType(), typeof(SqlByte));
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
        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is SqlByte other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(SqlByte other) =>
            other.IsNull || IsNull ? other.IsNull && IsNull :
            (this == other).Value;

        // For hashing purpose
        public override int GetHashCode() => IsNull ? 0 : Value.GetHashCode();

        XmlSchema? IXmlSerializable.GetSchema() { return null; }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            string? isNull = reader.GetAttribute("nil", XmlSchema.InstanceNamespace);
            if (isNull != null && XmlConvert.ToBoolean(isNull))
            {
                // Read the next value.
                reader.ReadElementString();
                m_fNotNull = false;
            }
            else
            {
                m_value = XmlConvert.ToByte(reader.ReadElementString());
                m_fNotNull = true;
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
                writer.WriteString(XmlConvert.ToString(m_value));
            }
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("unsignedByte", XmlSchema.Namespace);
        }

        public static readonly SqlByte Null = new SqlByte(true);
        public static readonly SqlByte Zero = new SqlByte(0);
        public static readonly SqlByte MinValue = new SqlByte(byte.MinValue);
        public static readonly SqlByte MaxValue = new SqlByte(byte.MaxValue);
    } // SqlByte
} // namespace System
