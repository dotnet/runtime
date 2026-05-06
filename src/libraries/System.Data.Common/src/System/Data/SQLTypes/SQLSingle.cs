// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes
{
    /// <summary>
    /// Represents a floating point number within the range of -3.40E +38 through
    /// 3.40E +38 to be stored in or retrieved from a database.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    public struct SqlSingle : INullable, IComparable, IXmlSerializable, IEquatable<SqlSingle>
    {
        private bool _fNotNull; // false if null
        private float _value;
        // constructor
        // construct a Null
        private SqlSingle(bool _)
        {
            _fNotNull = false;
            _value = (float)0.0;
        }

        public SqlSingle(float value)
        {
            if (!float.IsFinite(value))
            {
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            }
            else
            {
                _fNotNull = true;
                _value = value;
            }
        }

        public SqlSingle(double value) : this(checked((float)value))
        {
        }

        // INullable
        public bool IsNull
        {
            get { return !_fNotNull; }
        }

        // property: Value
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
        public static implicit operator SqlSingle(float x)
        {
            return new SqlSingle(x);
        }

        // Explicit conversion from SqlSingle to float. Throw exception if x is Null.
        public static explicit operator float(SqlSingle x)
        {
            return x.Value;
        }

        public override string ToString()
        {
            return IsNull ? SQLResource.NullString : _value.ToString((IFormatProvider)null!);
        }

        public static SqlSingle Parse(string s)
        {
            if (s == SQLResource.NullString)
                return SqlSingle.Null;
            else
                return new SqlSingle(float.Parse(s, CultureInfo.InvariantCulture));
        }


        // Unary operators
        public static SqlSingle operator -(SqlSingle x)
        {
            return x.IsNull ? Null : new SqlSingle(-x._value);
        }


        // Binary operators

        // Arithmetic operators
        public static SqlSingle operator +(SqlSingle x, SqlSingle y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            float value = x._value + y._value;

            if (float.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlSingle(value);
        }

        public static SqlSingle operator -(SqlSingle x, SqlSingle y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            float value = x._value - y._value;

            if (float.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlSingle(value);
        }

        public static SqlSingle operator *(SqlSingle x, SqlSingle y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            float value = x._value * y._value;

            if (float.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlSingle(value);
        }

        public static SqlSingle operator /(SqlSingle x, SqlSingle y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y._value == (float)0.0)
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);

            float value = x._value / y._value;

            if (float.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlSingle(value);
        }



        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlSingle
        public static explicit operator SqlSingle(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlSingle(x.ByteValue);
        }

        // Implicit conversion from SqlByte to SqlSingle
        public static implicit operator SqlSingle(SqlByte x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle(x.Value);
        }

        // Implicit conversion from SqlInt16 to SqlSingle
        public static implicit operator SqlSingle(SqlInt16 x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle(x.Value);
        }

        // Implicit conversion from SqlInt32 to SqlSingle
        public static implicit operator SqlSingle(SqlInt32 x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle(x.Value);
        }

        // Implicit conversion from SqlInt64 to SqlSingle
        public static implicit operator SqlSingle(SqlInt64 x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle(x.Value);
        }

        // Implicit conversion from SqlMoney to SqlSingle
        public static implicit operator SqlSingle(SqlMoney x)
        {
            return x.IsNull ? Null : new SqlSingle(x.ToDouble());
        }

        // Implicit conversion from SqlDecimal to SqlSingle
        public static implicit operator SqlSingle(SqlDecimal x)
        {
            // Will not overflow
            return x.IsNull ? Null : new SqlSingle(x.ToDouble());
        }


        // Explicit conversions


        // Explicit conversion from SqlDouble to SqlSingle
        public static explicit operator SqlSingle(SqlDouble x)
        {
            return x.IsNull ? Null : new SqlSingle(x.Value);
        }

        // Explicit conversion from SqlString to SqlSingle
        // Throws FormatException or OverflowException if necessary.
        public static explicit operator SqlSingle(SqlString x)
        {
            if (x.IsNull)
                return SqlSingle.Null;
            return Parse(x.Value);
        }

        // Overloading comparison operators
        public static SqlBoolean operator ==(SqlSingle x, SqlSingle y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value == y._value);
        }

        public static SqlBoolean operator !=(SqlSingle x, SqlSingle y)
        {
            return !(x == y);
        }

        public static SqlBoolean operator <(SqlSingle x, SqlSingle y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value < y._value);
        }

        public static SqlBoolean operator >(SqlSingle x, SqlSingle y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value > y._value);
        }

        public static SqlBoolean operator <=(SqlSingle x, SqlSingle y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value <= y._value);
        }

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
        public int CompareTo(object? value)
        {
            if (value is SqlSingle i)
            {
                return CompareTo(i);
            }
            throw ADP.WrongType(value!.GetType(), typeof(SqlSingle));
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
        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is SqlSingle other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(SqlSingle other) =>
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
                _fNotNull = false;
            }
            else
            {
                _value = XmlConvert.ToSingle(reader.ReadElementString());
                _fNotNull = true;
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
                writer.WriteString(XmlConvert.ToString(_value));
            }
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("float", XmlSchema.Namespace);
        }

        public static readonly SqlSingle Null = new SqlSingle(true);
        public static readonly SqlSingle Zero = new SqlSingle((float)0.0);
        public static readonly SqlSingle MinValue = new SqlSingle(float.MinValue);
        public static readonly SqlSingle MaxValue = new SqlSingle(float.MaxValue);
    } // SqlSingle
} // namespace System.Data.SqlTypes
