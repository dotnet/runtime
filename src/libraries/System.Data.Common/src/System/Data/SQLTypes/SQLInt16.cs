// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes
{
    /// <summary>
    /// Represents a 16-bit signed integer to be stored in or retrieved from a database.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public struct SqlInt16 : INullable, IComparable, IXmlSerializable, IEquatable<SqlInt16>
    {
        private bool m_fNotNull; // false if null. Do not rename (binary serialization)
        private short m_value; // Do not rename (binary serialization)

        private const int MaskI2 = ~0x00007fff;

        // constructor
        // construct a Null
        private SqlInt16(bool _)
        {
            m_fNotNull = false;
            m_value = 0;
        }

        public SqlInt16(short value)
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
        public short Value
        {
            get
            {
                if (m_fNotNull)
                    return m_value;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from short to SqlInt16
        public static implicit operator SqlInt16(short x)
        {
            return new SqlInt16(x);
        }

        // Explicit conversion from SqlInt16 to short. Throw exception if x is Null.
        public static explicit operator short(SqlInt16 x)
        {
            return x.Value;
        }

        public override string ToString()
        {
            return IsNull ? SQLResource.NullString : m_value.ToString((IFormatProvider)null!);
        }

        public static SqlInt16 Parse(string s)
        {
            if (s == SQLResource.NullString)
                return SqlInt16.Null;
            else
                return new SqlInt16(short.Parse(s, null));
        }

        // Unary operators
        public static SqlInt16 operator -(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlInt16((short)-x.m_value);
        }

        public static SqlInt16 operator ~(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlInt16((short)~x.m_value);
        }

        // Binary operators

        // Arithmetic operators
        public static SqlInt16 operator +(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = x.m_value + y.m_value;
            if ((((iResult >> 15) ^ (iResult >> 16)) & 1) != 0) // Bit 15 != bit 16
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)iResult);
        }

        public static SqlInt16 operator -(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = x.m_value - y.m_value;
            if ((((iResult >> 15) ^ (iResult >> 16)) & 1) != 0) // Bit 15 != bit 16
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)iResult);
        }

        public static SqlInt16 operator *(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = x.m_value * y.m_value;
            int iTemp = iResult & MaskI2;
            if (iTemp != 0 && iTemp != MaskI2)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)iResult);
        }

        public static SqlInt16 operator /(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value != 0)
            {
                if ((x.m_value == short.MinValue) && (y.m_value == -1))
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                return new SqlInt16((short)(x.m_value / y.m_value));
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        public static SqlInt16 operator %(SqlInt16 x, SqlInt16 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value != 0)
            {
                if ((x.m_value == short.MinValue) && (y.m_value == -1))
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                return new SqlInt16((short)(x.m_value % y.m_value));
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        // Bitwise operators
        public static SqlInt16 operator &(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt16((short)(x.m_value & y.m_value));
        }

        public static SqlInt16 operator |(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt16(unchecked((short)((ushort)x.m_value | (ushort)y.m_value)));
        }

        public static SqlInt16 operator ^(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt16((short)(x.m_value ^ y.m_value));
        }

        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlInt16
        public static explicit operator SqlInt16(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlInt16(x.ByteValue);
        }

        // Implicit conversion from SqlByte to SqlInt16
        public static implicit operator SqlInt16(SqlByte x)
        {
            return x.IsNull ? Null : new SqlInt16(x.Value);
        }

        // Explicit conversions

        // Explicit conversion from SqlInt32 to SqlInt16
        public static explicit operator SqlInt16(SqlInt32 x)
        {
            if (x.IsNull)
                return Null;

            int value = x.Value;
            if (value > short.MaxValue || value < short.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)value);
        }

        // Explicit conversion from SqlInt64 to SqlInt16
        public static explicit operator SqlInt16(SqlInt64 x)
        {
            if (x.IsNull)
                return Null;

            long value = x.Value;
            if (value > short.MaxValue || value < short.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)value);
        }

        // Explicit conversion from SqlSingle to SqlInt16
        public static explicit operator SqlInt16(SqlSingle x)
        {
            if (x.IsNull)
                return Null;

            float value = x.Value;
            if (value < short.MinValue || value > short.MaxValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)value);
        }

        // Explicit conversion from SqlDouble to SqlInt16
        public static explicit operator SqlInt16(SqlDouble x)
        {
            if (x.IsNull)
                return Null;

            double value = x.Value;
            if (value < short.MinValue || value > short.MaxValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt16((short)value);
        }

        // Explicit conversion from SqlMoney to SqlInt16
        public static explicit operator SqlInt16(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlInt16(checked((short)x.ToInt32()));
        }

        // Explicit conversion from SqlDecimal to SqlInt16
        public static explicit operator SqlInt16(SqlDecimal x)
        {
            return (SqlInt16)(SqlInt32)x;
        }

        // Explicit conversion from SqlString to SqlInt16
        public static explicit operator SqlInt16(SqlString x)
        {
            return x.IsNull ? Null : new SqlInt16(short.Parse(x.Value, null));
        }

        // Overloading comparison operators
        public static SqlBoolean operator ==(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value == y.m_value);
        }

        public static SqlBoolean operator !=(SqlInt16 x, SqlInt16 y)
        {
            return !(x == y);
        }

        public static SqlBoolean operator <(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value < y.m_value);
        }

        public static SqlBoolean operator >(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value > y.m_value);
        }

        public static SqlBoolean operator <=(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value <= y.m_value);
        }

        public static SqlBoolean operator >=(SqlInt16 x, SqlInt16 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value >= y.m_value);
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
            if (value is SqlInt16 i)
            {
                return CompareTo(i);
            }
            throw ADP.WrongType(value!.GetType(), typeof(SqlInt16));
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
        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is SqlInt16 other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(SqlInt16 other) =>
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
                m_value = XmlConvert.ToInt16(reader.ReadElementString());
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
            return new XmlQualifiedName("short", XmlSchema.Namespace);
        }

        public static readonly SqlInt16 Null = new SqlInt16(true);
        public static readonly SqlInt16 Zero = new SqlInt16(0);
        public static readonly SqlInt16 MinValue = new SqlInt16(short.MinValue);
        public static readonly SqlInt16 MaxValue = new SqlInt16(short.MaxValue);
    } // SqlInt16
} // namespace System.Data.SqlTypes
