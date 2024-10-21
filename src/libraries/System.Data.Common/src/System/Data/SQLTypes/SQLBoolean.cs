// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes
{
    /// <summary>
    /// Represents an integer value that is either 1 or 0.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public struct SqlBoolean : INullable, IComparable, IXmlSerializable, IEquatable<SqlBoolean>
    {
        // m_value: 2 (true), 1 (false), 0 (unknown/Null)
        private byte m_value; // Do not rename (binary serialization)

        private const byte x_Null = 0;
        private const byte x_False = 1;
        private const byte x_True = 2;

        // constructor

        /// <summary>
        /// Initializes a new instance of the <see cref='SqlBoolean'/> class.
        /// </summary>
        public SqlBoolean(bool value)
        {
            m_value = value ? x_True : x_False;
        }

        public SqlBoolean(int value) : this(value, false)
        {
        }

        private SqlBoolean(int value, bool fNull)
        {
            if (fNull)
                m_value = x_Null;
            else
                m_value = (value != 0) ? x_True : x_False;
        }


        // INullable
        /// <summary>
        /// Gets whether the current <see cref='Value'/> is <see cref='SqlBoolean.Null'/>.
        /// </summary>
        public bool IsNull
        {
            get { return m_value == x_Null; }
        }

        // property: Value
        /// <summary>
        /// Gets or sets the <see cref='SqlBoolean'/> to be <see langword='true'/> or <see langword='false'/>.
        /// </summary>
        public bool Value
        {
            get
            {
                return m_value switch
                {
                    x_True => true,
                    x_False => false,
                    _ => throw new SqlNullValueException(),
                };
            }
        }

        /// <summary>
        /// Gets whether the current <see cref='Value'/> is <see cref='SqlBoolean.True'/>.
        /// </summary>
        public bool IsTrue
        {
            get { return m_value == x_True; }
        }

        /// <summary>
        /// Gets whether the current <see cref='Value'/> is <see cref='SqlBoolean.False'/>.
        /// </summary>
        public bool IsFalse
        {
            get { return m_value == x_False; }
        }


        // Implicit conversion from bool to SqlBoolean
        /// <summary>
        /// Converts a boolean to a <see cref='SqlBoolean'/>.
        /// </summary>
        public static implicit operator SqlBoolean(bool x)
        {
            return new SqlBoolean(x);
        }

        // Explicit conversion from SqlBoolean to bool. Throw exception if x is Null.
        /// <summary>
        /// Converts a <see cref='SqlBoolean'/> to a boolean.
        /// </summary>
        public static explicit operator bool(SqlBoolean x)
        {
            return x.Value;
        }


        // Unary operators

        /// <summary>
        /// Performs a NOT operation on a <see cref='SqlBoolean'/>.
        /// </summary>
        public static SqlBoolean operator !(SqlBoolean x)
        {
            switch (x.m_value)
            {
                case x_True:
                    return SqlBoolean.False;

                case x_False:
                    return SqlBoolean.True;

                default:
                    Debug.Assert(x.m_value == x_Null);
                    return SqlBoolean.Null;
            }
        }

        public static bool operator true(SqlBoolean x)
        {
            return x.IsTrue;
        }

        public static bool operator false(SqlBoolean x)
        {
            return x.IsFalse;
        }

        // Binary operators

        /// <summary>
        /// Performs a bitwise AND operation on two instances of <see cref='SqlBoolean'/>.
        /// </summary>
        public static SqlBoolean operator &(SqlBoolean x, SqlBoolean y)
        {
            if (x.m_value == x_False || y.m_value == x_False)
                return SqlBoolean.False;
            else if (x.m_value == x_True && y.m_value == x_True)
                return SqlBoolean.True;
            else
                return SqlBoolean.Null;
        }

        /// <summary>
        /// Performs a bitwise OR operation on two instances of a <see cref='SqlBoolean'/>.
        /// </summary>
        public static SqlBoolean operator |(SqlBoolean x, SqlBoolean y)
        {
            if (x.m_value == x_True || y.m_value == x_True)
                return SqlBoolean.True;
            else if (x.m_value == x_False && y.m_value == x_False)
                return SqlBoolean.False;
            else
                return SqlBoolean.Null;
        }



        // property: ByteValue
        public byte ByteValue
        {
            get
            {
                if (!IsNull)
                    return (m_value == x_True) ? (byte)1 : (byte)0;
                else
                    throw new SqlNullValueException();
            }
        }

        public override string ToString()
        {
            return IsNull ? SQLResource.NullString : Value.ToString();
        }

        public static SqlBoolean Parse(string s)
        {
            if (null == s)
                // Let Boolean.Parse throw exception
                return new SqlBoolean(bool.Parse(s!));
            if (s == SQLResource.NullString)
                return SqlBoolean.Null;

            s = s.TrimStart();
            char wchFirst = s[0];
            if (char.IsNumber(wchFirst) || ('-' == wchFirst) || ('+' == wchFirst))
            {
                return new SqlBoolean(int.Parse(s, null));
            }
            else
            {
                return new SqlBoolean(bool.Parse(s));
            }
        }


        // Unary operators
        public static SqlBoolean operator ~(SqlBoolean x)
        {
            return (!x);
        }


        // Binary operators

        public static SqlBoolean operator ^(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? Null : new SqlBoolean(x.m_value != y.m_value);
        }



        // Implicit conversions


        // Explicit conversions

        // Explicit conversion from SqlByte to SqlBoolean
        public static explicit operator SqlBoolean(SqlByte x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0);
        }

        // Explicit conversion from SqlInt16 to SqlBoolean
        public static explicit operator SqlBoolean(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0);
        }

        // Explicit conversion from SqlInt32 to SqlBoolean
        public static explicit operator SqlBoolean(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0);
        }

        // Explicit conversion from SqlInt64 to SqlBoolean
        public static explicit operator SqlBoolean(SqlInt64 x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0);
        }

        // Explicit conversion from SqlDouble to SqlBoolean
        public static explicit operator SqlBoolean(SqlDouble x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0.0);
        }

        // Explicit conversion from SqlSingle to SqlBoolean
        public static explicit operator SqlBoolean(SqlSingle x)
        {
            return x.IsNull ? Null : new SqlBoolean(x.Value != 0.0);
        }

        // Explicit conversion from SqlMoney to SqlBoolean
        public static explicit operator SqlBoolean(SqlMoney x)
        {
            return x.IsNull ? Null : (x != SqlMoney.Zero);
        }

        // Explicit conversion from SqlDecimal to SqlBoolean
        public static explicit operator SqlBoolean(SqlDecimal x)
        {
            return x.IsNull ? SqlBoolean.Null : new SqlBoolean(x._data1 != 0 || x._data2 != 0 ||
                                                       x._data3 != 0 || x._data4 != 0);
        }

        // Explicit conversion from SqlString to SqlBoolean
        // Throws FormatException or OverflowException if necessary.
        public static explicit operator SqlBoolean(SqlString x)
        {
            return x.IsNull ? Null : SqlBoolean.Parse(x.Value);
        }

        // Overloading comparison operators
        public static SqlBoolean operator ==(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value == y.m_value);
        }

        public static SqlBoolean operator !=(SqlBoolean x, SqlBoolean y)
        {
            return !(x == y);
        }

        public static SqlBoolean operator <(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value < y.m_value);
        }

        public static SqlBoolean operator >(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value > y.m_value);
        }

        public static SqlBoolean operator <=(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value <= y.m_value);
        }

        public static SqlBoolean operator >=(SqlBoolean x, SqlBoolean y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value >= y.m_value);
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
        public int CompareTo(object? value)
        {
            if (value is SqlBoolean i)
            {
                return CompareTo(i);
            }
            throw ADP.WrongType(value!.GetType(), typeof(SqlBoolean));
        }

        public int CompareTo(SqlBoolean value)
        {
            // If both Null, consider them equal.
            // Otherwise, Null is less than anything.
            if (IsNull)
                return value.IsNull ? 0 : -1;
            else if (value.IsNull)
                return 1;

            if (ByteValue < value.ByteValue) return -1;
            if (ByteValue > value.ByteValue) return 1;
            return 0;
        }

        // Compares this instance with a specified object
        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is SqlBoolean other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(SqlBoolean other) =>
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
                m_value = x_Null;
            }
            else
            {
                m_value = XmlConvert.ToBoolean(reader.ReadElementString()) ? x_True : x_False;
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
                writer.WriteString(m_value == x_True ? "true" : "false");
            }
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("boolean", XmlSchema.Namespace);
        }

        /// <summary>
        /// Represents a true value that can be assigned to the
        /// <see cref='Value'/> property of an instance of the <see cref='SqlBoolean'/> class.
        /// </summary>
        public static readonly SqlBoolean True = new SqlBoolean(true);
        /// <summary>
        /// Represents a false value that can be assigned to the <see cref='Value'/> property of an instance of
        /// the <see cref='SqlBoolean'/> class.
        /// </summary>
        public static readonly SqlBoolean False = new SqlBoolean(false);
        /// <summary>
        /// Represents a null value that can be assigned to the <see cref='Value'/> property of an instance of
        /// the <see cref='SqlBoolean'/> class.
        /// </summary>
        public static readonly SqlBoolean Null = new SqlBoolean(0, true);

        public static readonly SqlBoolean Zero = new SqlBoolean(0);
        public static readonly SqlBoolean One = new SqlBoolean(1);
    } // SqlBoolean
} // namespace System.Data.SqlTypes
