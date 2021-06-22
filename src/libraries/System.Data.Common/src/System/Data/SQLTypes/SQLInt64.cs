// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.SqlTypes
{
    /// <summary>
    /// Represents a 64-bit signed integer to be stored in or retrieved from a database.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public struct SqlInt64 : INullable, IComparable, IXmlSerializable
    {
        private bool m_fNotNull; // false if null. Do not rename (binary serialization)
        private long m_value; // Do not rename (binary serialization)

        private const long s_lLowIntMask = 0xffffffff;
        private const long s_lHighIntMask = unchecked((long)0xffffffff00000000);

        // constructor
        // construct a Null
        private SqlInt64(bool fNull)
        {
            m_fNotNull = false;
            m_value = 0;
        }

        public SqlInt64(long value)
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
        public long Value
        {
            get
            {
                if (m_fNotNull)
                    return m_value;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from long to SqlInt64
        public static implicit operator SqlInt64(long x)
        {
            return new SqlInt64(x);
        }

        // Explicit conversion from SqlInt64 to long. Throw exception if x is Null.
        public static explicit operator long(SqlInt64 x)
        {
            return x.Value;
        }

        public override string ToString()
        {
            return IsNull ? SQLResource.NullString : m_value.ToString((IFormatProvider)null!);
        }

        public static SqlInt64 Parse(string s)
        {
            if (s == SQLResource.NullString)
                return SqlInt64.Null;
            else
                return new SqlInt64(long.Parse(s, null));
        }

        // Unary operators
        public static SqlInt64 operator -(SqlInt64 x)
        {
            return x.IsNull ? Null : new SqlInt64(-x.m_value);
        }

        public static SqlInt64 operator ~(SqlInt64 x)
        {
            return x.IsNull ? Null : new SqlInt64(~x.m_value);
        }

        // Binary operators

        // Arithmetic operators
        public static SqlInt64 operator +(SqlInt64 x, SqlInt64 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            long lResult = x.m_value + y.m_value;
            if (SameSignLong(x.m_value, y.m_value) && !SameSignLong(x.m_value, lResult))
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt64(lResult);
        }

        public static SqlInt64 operator -(SqlInt64 x, SqlInt64 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            long lResult = x.m_value - y.m_value;
            if (!SameSignLong(x.m_value, y.m_value) && SameSignLong(y.m_value, lResult))
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt64(lResult);
        }

        public static SqlInt64 operator *(SqlInt64 x, SqlInt64 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            bool fNeg = false;

            long lOp1 = x.m_value;
            long lOp2 = y.m_value;
            long lResult;
            long lPartialResult = 0;

            if (lOp1 < 0)
            {
                fNeg = true;
                lOp1 = -lOp1;
            }

            if (lOp2 < 0)
            {
                fNeg = !fNeg;
                lOp2 = -lOp2;
            }

            long lLow1 = lOp1 & s_lLowIntMask;
            long lHigh1 = (lOp1 >> 32) & s_lLowIntMask;
            long lLow2 = lOp2 & s_lLowIntMask;
            long lHigh2 = (lOp2 >> 32) & s_lLowIntMask;

            // if both of the high order dwords are non-zero then overflow results
            if (lHigh1 != 0 && lHigh2 != 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            lResult = lLow1 * lLow2;

            if (lResult < 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            if (lHigh1 != 0)
            {
                Debug.Assert(lHigh2 == 0);
                lPartialResult = lHigh1 * lLow2;
                if (lPartialResult < 0 || lPartialResult > long.MaxValue)
                    throw new OverflowException(SQLResource.ArithOverflowMessage);
            }
            else if (lHigh2 != 0)
            {
                Debug.Assert(lHigh1 == 0);
                lPartialResult = lLow1 * lHigh2;
                if (lPartialResult < 0 || lPartialResult > long.MaxValue)
                    throw new OverflowException(SQLResource.ArithOverflowMessage);
            }

            lResult += lPartialResult << 32;
            if (lResult < 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            if (fNeg)
                lResult = -lResult;

            return new SqlInt64(lResult);
        }

        public static SqlInt64 operator /(SqlInt64 x, SqlInt64 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value != 0)
            {
                if ((x.m_value == long.MinValue) && (y.m_value == -1))
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                return new SqlInt64(x.m_value / y.m_value);
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        public static SqlInt64 operator %(SqlInt64 x, SqlInt64 y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value != 0)
            {
                if ((x.m_value == long.MinValue) && (y.m_value == -1))
                    throw new OverflowException(SQLResource.ArithOverflowMessage);

                return new SqlInt64(x.m_value % y.m_value);
            }
            else
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
        }

        // Bitwise operators
        public static SqlInt64 operator &(SqlInt64 x, SqlInt64 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt64(x.m_value & y.m_value);
        }

        public static SqlInt64 operator |(SqlInt64 x, SqlInt64 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt64(x.m_value | y.m_value);
        }

        public static SqlInt64 operator ^(SqlInt64 x, SqlInt64 y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlInt64(x.m_value ^ y.m_value);
        }

        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlInt64
        public static explicit operator SqlInt64(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlInt64(x.ByteValue);
        }

        // Implicit conversion from SqlByte to SqlInt64
        public static implicit operator SqlInt64(SqlByte x)
        {
            return x.IsNull ? Null : new SqlInt64(x.Value);
        }

        // Implicit conversion from SqlInt16 to SqlInt64
        public static implicit operator SqlInt64(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlInt64(x.Value);
        }

        // Implicit conversion from SqlInt32 to SqlInt64
        public static implicit operator SqlInt64(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlInt64(x.Value);
        }

        // Explicit conversions

        // Explicit conversion from SqlSingle to SqlInt64
        public static explicit operator SqlInt64(SqlSingle x)
        {
            if (x.IsNull)
                return Null;

            float value = x.Value;
            if (value > long.MaxValue || value < long.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt64((long)value);
        }

        // Explicit conversion from SqlDouble to SqlInt64
        public static explicit operator SqlInt64(SqlDouble x)
        {
            if (x.IsNull)
                return Null;

            double value = x.Value;
            if (value > long.MaxValue || value < long.MinValue)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else
                return new SqlInt64((long)value);
        }

        // Explicit conversion from SqlMoney to SqlInt64
        public static explicit operator SqlInt64(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlInt64(x.ToInt64());
        }

        // Explicit conversion from SqlDecimal to SqlInt64
        public static explicit operator SqlInt64(SqlDecimal x)
        {
            if (x.IsNull)
                return SqlInt64.Null;

            SqlDecimal ssnumTemp = x;
            long llRetVal;

            // Throw away decimal portion
            ssnumTemp.AdjustScale(-ssnumTemp._bScale, false);

            // More than 8 bytes of data will always overflow
            if (ssnumTemp._bLen > 2)
                throw new OverflowException(SQLResource.ConversionOverflowMessage);

            // If 8 bytes of data, see if fits in LONGLONG
            if (ssnumTemp._bLen == 2)
            {
                ulong dwl = SqlDecimal.DWL(ssnumTemp._data1, ssnumTemp._data2);
                if (dwl > SqlDecimal.s_llMax && (ssnumTemp.IsPositive || dwl != 1 + SqlDecimal.s_llMax))
                    throw new OverflowException(SQLResource.ConversionOverflowMessage);
                llRetVal = (long)dwl;
            }
            // 4 bytes of data always fits in a LONGLONG
            else
                llRetVal = ssnumTemp._data1;

            //negate result if ssnumTemp negative
            if (!ssnumTemp.IsPositive)
                llRetVal = -llRetVal;

            return new SqlInt64(llRetVal);
        }

        // Explicit conversion from SqlString to SqlInt
        // Throws FormatException or OverflowException if necessary.
        public static explicit operator SqlInt64(SqlString x)
        {
            return x.IsNull ? Null : new SqlInt64(long.Parse(x.Value, null));
        }

        // Utility functions
        private static bool SameSignLong(long x, long y)
        {
            return ((x ^ y) & unchecked((long)0x8000000000000000L)) == 0;
        }

        // Overloading comparison operators
        public static SqlBoolean operator ==(SqlInt64 x, SqlInt64 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value == y.m_value);
        }

        public static SqlBoolean operator !=(SqlInt64 x, SqlInt64 y)
        {
            return !(x == y);
        }

        public static SqlBoolean operator <(SqlInt64 x, SqlInt64 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value < y.m_value);
        }

        public static SqlBoolean operator >(SqlInt64 x, SqlInt64 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value > y.m_value);
        }

        public static SqlBoolean operator <=(SqlInt64 x, SqlInt64 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value <= y.m_value);
        }

        public static SqlBoolean operator >=(SqlInt64 x, SqlInt64 y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value >= y.m_value);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator ~
        public static SqlInt64 OnesComplement(SqlInt64 x)
        {
            return ~x;
        }

        // Alternative method for operator +
        public static SqlInt64 Add(SqlInt64 x, SqlInt64 y)
        {
            return x + y;
        }
        // Alternative method for operator -
        public static SqlInt64 Subtract(SqlInt64 x, SqlInt64 y)
        {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlInt64 Multiply(SqlInt64 x, SqlInt64 y)
        {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlInt64 Divide(SqlInt64 x, SqlInt64 y)
        {
            return x / y;
        }

        // Alternative method for operator %
        public static SqlInt64 Mod(SqlInt64 x, SqlInt64 y)
        {
            return x % y;
        }

        public static SqlInt64 Modulus(SqlInt64 x, SqlInt64 y)
        {
            return x % y;
        }

        // Alternative method for operator &
        public static SqlInt64 BitwiseAnd(SqlInt64 x, SqlInt64 y)
        {
            return x & y;
        }

        // Alternative method for operator |
        public static SqlInt64 BitwiseOr(SqlInt64 x, SqlInt64 y)
        {
            return x | y;
        }

        // Alternative method for operator ^
        public static SqlInt64 Xor(SqlInt64 x, SqlInt64 y)
        {
            return x ^ y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlInt64 x, SqlInt64 y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlInt64 x, SqlInt64 y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlInt64 x, SqlInt64 y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlInt64 x, SqlInt64 y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlInt64 x, SqlInt64 y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlInt64 x, SqlInt64 y)
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

        public SqlInt32 ToSqlInt32()
        {
            return (SqlInt32)this;
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
            if (value is SqlInt64)
            {
                SqlInt64 i = (SqlInt64)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value!.GetType(), typeof(SqlInt64));
        }

        public int CompareTo(SqlInt64 value)
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
        public override bool Equals([NotNullWhen(true)] object? value)
        {
            if (!(value is SqlInt64))
            {
                return false;
            }

            SqlInt64 i = (SqlInt64)value;

            if (i.IsNull || IsNull)
                return (i.IsNull && IsNull);
            else
                return (this == i).Value;
        }

        // For hashing purpose
        public override int GetHashCode()
        {
            return IsNull ? 0 : Value.GetHashCode();
        }

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
                m_value = XmlConvert.ToInt64(reader.ReadElementString());
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
            return new XmlQualifiedName("long", XmlSchema.Namespace);
        }

        public static readonly SqlInt64 Null = new SqlInt64(true);
        public static readonly SqlInt64 Zero = new SqlInt64(0);
        public static readonly SqlInt64 MinValue = new SqlInt64(long.MinValue);
        public static readonly SqlInt64 MaxValue = new SqlInt64(long.MaxValue);
    } // SqlInt64
} // namespace System.Data.SqlTypes
