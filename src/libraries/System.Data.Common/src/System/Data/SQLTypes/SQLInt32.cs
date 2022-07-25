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
    /// Represents a 32-bit signed integer to be stored in or retrieved from a database.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public struct SqlInt32 : INullable, IComparable, IXmlSerializable, IEquatable<SqlInt32>
    {
        private bool m_fNotNull; // false if null, the default ctor (plain 0) will make it Null. Do not rename (binary serialization)
        private int m_value; // Do not rename (binary serialization)

        private const long s_iIntMin = int.MinValue;   // minimum (signed) int value
        private const long s_lBitNotIntMax = ~int.MaxValue;

        // constructor
        // construct a Null
        private SqlInt32(bool fNull)
        {
            m_fNotNull = false;
            m_value = 0;
        }

        public SqlInt32(int value)
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
        public int Value
        {
            get
            {
                if (IsNull)
                    throw new SqlNullValueException();
                else
                    return m_value;
            }
        }

        // Implicit conversion from int to SqlInt32
        public static implicit operator SqlInt32(int x)
        {
            return new SqlInt32(x);
        }

        // Explicit conversion from SqlInt32 to int. Throw exception if x is Null.
        public static explicit operator int(SqlInt32 x)
        {
            return x.Value;
        }

        public override string ToString()
        {
            return IsNull ? SQLResource.NullString : m_value.ToString((IFormatProvider)null!);
        }

        public static SqlInt32 Parse(string s)
        {
            if (s == SQLResource.NullString)
                return SqlInt32.Null;
            else
                return new SqlInt32(int.Parse(s, null));
        }


        // Unary operators
        public static SqlInt32 operator -(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlInt32(-x.m_value);
        }

        public static SqlInt32 operator ~(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlInt32(~x.m_value);
        }

        // Binary operators

        // Arithmetic operators
        public static SqlInt32 operator +(SqlInt32 x, SqlInt32 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = x.m_value + y.m_value;
            if (SameSignInt(x.m_value, y.m_value) && !SameSignInt(x.m_value, iResult))
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt32(iResult);
        }

        public static SqlInt32 operator -(SqlInt32 x, SqlInt32 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            int iResult = x.m_value - y.m_value;
            if (!SameSignInt(x.m_value, y.m_value) && SameSignInt(y.m_value, iResult))
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt32(iResult);
        }

        public static SqlInt32 operator *(SqlInt32 x, SqlInt32 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            long lResult = x.m_value * (long)y.m_value;
            long lTemp = lResult & s_lBitNotIntMax;
            if (lTemp != 0 && lTemp != s_lBitNotIntMax)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt32((int)lResult);
        }

        public static SqlInt32 operator /(SqlInt32 x, SqlInt32 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value != 0)
            {
                if ((x.m_value == s_iIntMin) && (y.m_value == -1))
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                return new SqlInt32(x.m_value / y.m_value);
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        public static SqlInt32 operator %(SqlInt32 x, SqlInt32 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value != 0)
            {
                if ((x.m_value == s_iIntMin) && (y.m_value == -1))
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                return new SqlInt32(x.m_value % y.m_value);
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        // Bitwise operators
        public static SqlInt32 operator &(SqlInt32 x, SqlInt32 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt32(x.m_value & y.m_value);
        }

        public static SqlInt32 operator |(SqlInt32 x, SqlInt32 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt32(x.m_value | y.m_value);
        }

        public static SqlInt32 operator ^(SqlInt32 x, SqlInt32 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt32(x.m_value ^ y.m_value);
        }


        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlInt32
        public static explicit operator SqlInt32(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlInt32(x.ByteValue);
        }

        // Implicit conversion from SqlByte to SqlInt32
        public static implicit operator SqlInt32(SqlByte x)
        {
            return x.IsNull ? Null : new SqlInt32(x.Value);
        }

        // Implicit conversion from SqlInt16 to SqlInt32
        public static implicit operator SqlInt32(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlInt32(x.Value);
        }

        // Explicit conversions

        // Explicit conversion from SqlInt64 to SqlInt32
        public static explicit operator SqlInt32(SqlInt64 x)
        {
            if (x.IsNull)
                return Null;

            long value = x.Value;
            if (value > int.MaxValue || value < int.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt32((int)value);
        }

        // Explicit conversion from SqlSingle to SqlInt32
        public static explicit operator SqlInt32(SqlSingle x)
        {
            if (x.IsNull)
                return Null;

            float value = x.Value;
            if (value > int.MaxValue || value < int.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt32((int)value);
        }

        // Explicit conversion from SqlDouble to SqlInt32
        public static explicit operator SqlInt32(SqlDouble x)
        {
            if (x.IsNull)
                return Null;

            double value = x.Value;
            if (value > int.MaxValue || value < int.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt32((int)value);
        }

        // Explicit conversion from SqlMoney to SqlInt32
        public static explicit operator SqlInt32(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlInt32(x.ToInt32());
        }

        // Explicit conversion from SqlDecimal to SqlInt32
        public static explicit operator SqlInt32(SqlDecimal x)
        {
            if (x.IsNull)
                return SqlInt32.Null;

            x.AdjustScale(-x.Scale, true);

            long ret = x._data1;
            if (!x.IsPositive)
                ret = -ret;

            if (x._bLen > 1 || ret > int.MaxValue || ret < int.MinValue)
                throw new OverflowException(SQLResource.ConversionOverflowMessage);

            return new SqlInt32((int)ret);
        }

        // Explicit conversion from SqlString to SqlInt
        // Throws FormatException or OverflowException if necessary.
        public static explicit operator SqlInt32(SqlString x)
        {
            return x.IsNull ? SqlInt32.Null : new SqlInt32(int.Parse(x.Value, null));
        }

        // Utility functions
        private static bool SameSignInt(int x, int y)
        {
            return ((x ^ y) & 0x80000000) == 0;
        }

        // Overloading comparison operators
        public static SqlBoolean operator ==(SqlInt32 x, SqlInt32 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value == y.m_value);
        }

        public static SqlBoolean operator !=(SqlInt32 x, SqlInt32 y)
        {
            return !(x == y);
        }

        public static SqlBoolean operator <(SqlInt32 x, SqlInt32 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value < y.m_value);
        }

        public static SqlBoolean operator >(SqlInt32 x, SqlInt32 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value > y.m_value);
        }

        public static SqlBoolean operator <=(SqlInt32 x, SqlInt32 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value <= y.m_value);
        }

        public static SqlBoolean operator >=(SqlInt32 x, SqlInt32 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value >= y.m_value);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator ~
        public static SqlInt32 OnesComplement(SqlInt32 x)
        {
            return ~x;
        }

        // Alternative method for operator +
        public static SqlInt32 Add(SqlInt32 x, SqlInt32 y)
        {
            return x + y;
        }
        // Alternative method for operator -
        public static SqlInt32 Subtract(SqlInt32 x, SqlInt32 y)
        {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlInt32 Multiply(SqlInt32 x, SqlInt32 y)
        {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlInt32 Divide(SqlInt32 x, SqlInt32 y)
        {
            return x / y;
        }

        // Alternative method for operator %
        public static SqlInt32 Mod(SqlInt32 x, SqlInt32 y)
        {
            return x % y;
        }

        public static SqlInt32 Modulus(SqlInt32 x, SqlInt32 y)
        {
            return x % y;
        }

        // Alternative method for operator &
        public static SqlInt32 BitwiseAnd(SqlInt32 x, SqlInt32 y)
        {
            return x & y;
        }

        // Alternative method for operator |
        public static SqlInt32 BitwiseOr(SqlInt32 x, SqlInt32 y)
        {
            return x | y;
        }

        // Alternative method for operator ^
        public static SqlInt32 Xor(SqlInt32 x, SqlInt32 y)
        {
            return x ^ y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlInt32 x, SqlInt32 y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlInt32 x, SqlInt32 y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlInt32 x, SqlInt32 y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlInt32 x, SqlInt32 y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlInt32 x, SqlInt32 y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlInt32 x, SqlInt32 y)
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
            if (value is SqlInt32 i)
            {
                return CompareTo(i);
            }
            throw ADP.WrongType(value!.GetType(), typeof(SqlInt32));
        }

        public int CompareTo(SqlInt32 value)
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
            value is SqlInt32 other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(SqlInt32 other) =>
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
                m_value = XmlConvert.ToInt32(reader.ReadElementString());
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
            return new XmlQualifiedName("int", XmlSchema.Namespace);
        }

        public static readonly SqlInt32 Null = new SqlInt32(true);
        public static readonly SqlInt32 Zero = new SqlInt32(0);
        public static readonly SqlInt32 MinValue = new SqlInt32(int.MinValue);
        public static readonly SqlInt32 MaxValue = new SqlInt32(int.MaxValue);
    } // SqlInt32
} // namespace System.Data.SqlTypes
