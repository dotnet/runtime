// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.SqlTypes
{
    /// <summary>
    /// Represents a floating-point number within the range of -1.79E
    /// +308 through 1.79E +308 to be stored in or retrieved from a database.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public struct SqlDouble : INullable, IComparable, IXmlSerializable, IEquatable<SqlDouble>
    {
        private bool m_fNotNull; // false if null. Do not rename (binary serialization)
        private double m_value; // Do not rename (binary serialization)

        // constructor
        // construct a Null
        private SqlDouble(bool fNull)
        {
            m_fNotNull = false;
            m_value = 0.0;
        }

        public SqlDouble(double value)
        {
            if (!double.IsFinite(value))
            {
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            }
            else
            {
                m_value = value;
                m_fNotNull = true;
            }
        }

        // INullable
        public bool IsNull
        {
            get { return !m_fNotNull; }
        }

        // property: Value
        public double Value
        {
            get
            {
                if (m_fNotNull)
                    return m_value;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from double to SqlDouble
        public static implicit operator SqlDouble(double x)
        {
            return new SqlDouble(x);
        }

        // Explicit conversion from SqlDouble to double. Throw exception if x is Null.
        public static explicit operator double(SqlDouble x)
        {
            return x.Value;
        }

        public override string ToString()
        {
            return IsNull ? SQLResource.NullString : m_value.ToString((IFormatProvider)null!);
        }

        public static SqlDouble Parse(string s)
        {
            if (s == SQLResource.NullString)
                return SqlDouble.Null;
            else
                return new SqlDouble(double.Parse(s, CultureInfo.InvariantCulture));
        }


        // Unary operators
        public static SqlDouble operator -(SqlDouble x)
        {
            return x.IsNull ? Null : new SqlDouble(-x.m_value);
        }


        // Binary operators

        // Arithmetic operators
        public static SqlDouble operator +(SqlDouble x, SqlDouble y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            double value = x.m_value + y.m_value;

            if (double.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlDouble(value);
        }

        public static SqlDouble operator -(SqlDouble x, SqlDouble y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            double value = x.m_value - y.m_value;

            if (double.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlDouble(value);
        }

        public static SqlDouble operator *(SqlDouble x, SqlDouble y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            double value = x.m_value * y.m_value;

            if (double.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlDouble(value);
        }

        public static SqlDouble operator /(SqlDouble x, SqlDouble y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value == 0.0)
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);

            double value = x.m_value / y.m_value;

            if (double.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlDouble(value);
        }

        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlDouble
        public static explicit operator SqlDouble(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlDouble(x.ByteValue);
        }

        // Implicit conversion from SqlByte to SqlDouble
        public static implicit operator SqlDouble(SqlByte x)
        {
            return x.IsNull ? Null : new SqlDouble(x.Value);
        }

        // Implicit conversion from SqlInt16 to SqlDouble
        public static implicit operator SqlDouble(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlDouble(x.Value);
        }

        // Implicit conversion from SqlInt32 to SqlDouble
        public static implicit operator SqlDouble(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlDouble(x.Value);
        }

        // Implicit conversion from SqlInt64 to SqlDouble
        public static implicit operator SqlDouble(SqlInt64 x)
        {
            return x.IsNull ? Null : new SqlDouble(x.Value);
        }

        // Implicit conversion from SqlSingle to SqlDouble
        public static implicit operator SqlDouble(SqlSingle x)
        {
            return x.IsNull ? Null : new SqlDouble(x.Value);
        }

        // Implicit conversion from SqlMoney to SqlDouble
        public static implicit operator SqlDouble(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlDouble(x.ToDouble());
        }

        // Implicit conversion from SqlDecimal to SqlDouble
        public static implicit operator SqlDouble(SqlDecimal x)
        {
            return x.IsNull ? Null : new SqlDouble(x.ToDouble());
        }

        // Explicit conversions

        // Explicit conversion from SqlString to SqlDouble
        // Throws FormatException or OverflowException if necessary.
        public static explicit operator SqlDouble(SqlString x)
        {
            if (x.IsNull)
                return SqlDouble.Null;

            return Parse(x.Value);
        }

        // Overloading comparison operators
        public static SqlBoolean operator ==(SqlDouble x, SqlDouble y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value == y.m_value);
        }

        public static SqlBoolean operator !=(SqlDouble x, SqlDouble y)
        {
            return !(x == y);
        }

        public static SqlBoolean operator <(SqlDouble x, SqlDouble y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value < y.m_value);
        }

        public static SqlBoolean operator >(SqlDouble x, SqlDouble y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value > y.m_value);
        }

        public static SqlBoolean operator <=(SqlDouble x, SqlDouble y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value <= y.m_value);
        }

        public static SqlBoolean operator >=(SqlDouble x, SqlDouble y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value >= y.m_value);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator +
        public static SqlDouble Add(SqlDouble x, SqlDouble y)
        {
            return x + y;
        }
        // Alternative method for operator -
        public static SqlDouble Subtract(SqlDouble x, SqlDouble y)
        {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlDouble Multiply(SqlDouble x, SqlDouble y)
        {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlDouble Divide(SqlDouble x, SqlDouble y)
        {
            return x / y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlDouble x, SqlDouble y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlDouble x, SqlDouble y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlDouble x, SqlDouble y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlDouble x, SqlDouble y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlDouble x, SqlDouble y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlDouble x, SqlDouble y)
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
        public int CompareTo(object? value)
        {
            if (value is SqlDouble i)
            {
                return CompareTo(i);
            }
            throw ADP.WrongType(value!.GetType(), typeof(SqlDouble));
        }

        public int CompareTo(SqlDouble value)
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
            value is SqlDouble other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(SqlDouble other) =>
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
                m_value = XmlConvert.ToDouble(reader.ReadElementString());
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
            return new XmlQualifiedName("double", XmlSchema.Namespace);
        }

        public static readonly SqlDouble Null = new SqlDouble(true);
        public static readonly SqlDouble Zero = new SqlDouble(0.0);
        public static readonly SqlDouble MinValue = new SqlDouble(double.MinValue);
        public static readonly SqlDouble MaxValue = new SqlDouble(double.MaxValue);
    } // SqlDouble
} // namespace System.Data.SqlTypes
