// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.SqlTypes
{
    [XmlSchemaProvider("GetXsdType")]
    public struct SqlBinary : INullable, IComparable, IXmlSerializable, IEquatable<SqlBinary>
    {
        // NOTE: If any instance fields change, update SqlTypeWorkarounds type in System.Data.SqlClient.
        private byte[]? _value;

        private SqlBinary(bool fNull)
        {
            _value = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='SqlBinary'/> class with a binary object to be stored.
        /// </summary>
        public SqlBinary(byte[]? value)
        {
            // if value is null, this generates a SqlBinary.Null
            if (value == null)
            {
                _value = null;
            }
            else
            {
                _value = new byte[value.Length];
                value.CopyTo(_value, 0);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='SqlBinary'/> class with a binary object to be stored.  This constructor will not copy the value.
        /// </summary>
        internal SqlBinary(byte[]? value, bool ignored)
        {
            // if value is null, this generates a SqlBinary.Null
            _value = value;
        }

        // INullable
        /// <summary>
        /// Gets whether or not <see cref='Value'/> is null.
        /// </summary>
        public bool IsNull => _value is null;

        // property: Value
        /// <summary>
        /// Gets or sets the  value of the SQL binary object retrieved.
        /// </summary>
        public byte[] Value
        {
            get
            {
                if (_value is null)
                {
                    throw new SqlNullValueException();
                }

                var value = new byte[_value.Length];
                _value.CopyTo(value, 0);
                return value;
            }
        }

        // class indexer
        public byte this[int index]
        {
            get
            {
                if (_value is null)
                {
                    throw new SqlNullValueException();
                }
                return _value[index];
            }
        }

        // property: Length
        /// <summary>
        /// Gets the length in bytes of <see cref='Value'/>.
        /// </summary>
        public int Length
        {
            get
            {
                if (_value != null)
                {
                    return _value.Length;
                }
                throw new SqlNullValueException();
            }
        }

        // Implicit conversion from byte[] to SqlBinary
        // Alternative: constructor SqlBinary(bytep[])
        /// <summary>
        /// Converts a binary object to a <see cref='SqlBinary'/>.
        /// </summary>
        public static implicit operator SqlBinary(byte[] x) => new SqlBinary(x);

        // Explicit conversion from SqlBinary to byte[]. Throw exception if x is Null.
        // Alternative: Value property
        /// <summary>
        /// Converts a <see cref='SqlBinary'/> to a binary object.
        /// </summary>
        public static explicit operator byte[]?(SqlBinary x) => x.Value;

        /// <summary>
        /// Returns a string describing a <see cref='SqlBinary'/> object.
        /// </summary>
        public override string ToString() => _value is null ? SQLResource.NullString : $"SqlBinary({_value.Length})";

        // Unary operators

        // Binary operators

        // Arithmetic operators
        /// <summary>
        /// Adds two instances of <see cref='SqlBinary'/> together.
        /// </summary>
        // Alternative method: SqlBinary.Concat
        public static SqlBinary operator +(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
                return Null;

            byte[] rgbResult = new byte[x.Value.Length + y.Value.Length];
            x.Value.CopyTo(rgbResult, 0);
            y.Value.CopyTo(rgbResult, x.Value.Length);

            return new SqlBinary(rgbResult);
        }


        // Comparisons

        private static EComparison PerformCompareByte(byte[] x, byte[] y)
        {
            // the smaller length of two arrays
            int len = (x.Length < y.Length) ? x.Length : y.Length;
            int i;

            for (i = 0; i < len; ++i)
            {
                if (x[i] != y[i])
                {
                    if (x[i] < y[i])
                        return EComparison.LT;
                    else
                        return EComparison.GT;
                }
            }

            if (x.Length == y.Length)
                return EComparison.EQ;
            else
            {
                // if the remaining bytes are all zeroes, they are still equal.

                byte bZero = 0;

                if (x.Length < y.Length)
                {
                    // array X is shorter
                    for (i = len; i < y.Length; ++i)
                    {
                        if (y[i] != bZero)
                            return EComparison.LT;
                    }
                }
                else
                {
                    // array Y is shorter
                    for (i = len; i < x.Length; ++i)
                    {
                        if (x[i] != bZero)
                            return EComparison.GT;
                    }
                }

                return EComparison.EQ;
            }
        }

        // Explicit conversion from SqlGuid to SqlBinary
        /// <summary>
        /// Converts a <see cref='System.Data.SqlTypes.SqlGuid'/> to a <see cref='SqlBinary'/>.
        /// </summary>
        public static explicit operator SqlBinary(SqlGuid x) // Alternative method: SqlGuid.ToSqlBinary
        {
            return x.IsNull ? SqlBinary.Null : new SqlBinary(x.ToByteArray());
        }

        // Builtin functions

        // Overloading comparison operators
        /// <summary>
        /// Compares two instances of <see cref='SqlBinary'/> for equality.
        /// </summary>
        public static SqlBoolean operator ==(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            return new SqlBoolean(PerformCompareByte(x.Value, y.Value) == EComparison.EQ);
        }

        /// <summary>
        /// Compares two instances of <see cref='SqlBinary'/>
        /// for equality.
        /// </summary>
        public static SqlBoolean operator !=(SqlBinary x, SqlBinary y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Compares the first <see cref='SqlBinary'/> for being less than the
        /// second <see cref='SqlBinary'/>.
        /// </summary>
        public static SqlBoolean operator <(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            return new SqlBoolean(PerformCompareByte(x.Value, y.Value) == EComparison.LT);
        }

        /// <summary>
        /// Compares the first <see cref='SqlBinary'/> for being greater than the second <see cref='SqlBinary'/>.
        /// </summary>
        public static SqlBoolean operator >(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            return new SqlBoolean(PerformCompareByte(x.Value, y.Value) == EComparison.GT);
        }

        /// <summary>
        /// Compares the first <see cref='SqlBinary'/> for being less than or equal to the second <see cref='SqlBinary'/>.
        /// </summary>
        public static SqlBoolean operator <=(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            EComparison cmpResult = PerformCompareByte(x.Value, y.Value);
            return new SqlBoolean(cmpResult == EComparison.LT || cmpResult == EComparison.EQ);
        }

        /// <summary>
        /// Compares the first <see cref='SqlBinary'/> for being greater than or equal the second <see cref='SqlBinary'/>.
        /// </summary>
        public static SqlBoolean operator >=(SqlBinary x, SqlBinary y)
        {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            EComparison cmpResult = PerformCompareByte(x.Value, y.Value);
            return new SqlBoolean(cmpResult == EComparison.GT || cmpResult == EComparison.EQ);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator +
        public static SqlBinary Add(SqlBinary x, SqlBinary y)
        {
            return x + y;
        }

        public static SqlBinary Concat(SqlBinary x, SqlBinary y)
        {
            return x + y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlBinary x, SqlBinary y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlBinary x, SqlBinary y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlBinary x, SqlBinary y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlBinary x, SqlBinary y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlBinary x, SqlBinary y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlBinary x, SqlBinary y)
        {
            return (x >= y);
        }

        // Alternative method for conversions.
        public SqlGuid ToSqlGuid()
        {
            return (SqlGuid)this;
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
            if (value is SqlBinary i)
            {
                return CompareTo(i);
            }
            throw ADP.WrongType(value!.GetType(), typeof(SqlBinary));
        }

        public int CompareTo(SqlBinary value)
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
            value is SqlBinary other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(SqlBinary other) =>
            other.IsNull || IsNull ? other.IsNull && IsNull :
            (this == other).Value;

        // Hash a byte array.
        // Trailing zeroes/spaces would affect the hash value, so caller needs to
        // perform trimming as necessary.
        internal static int HashByteArray(byte[] rgbValue, int length)
        {
            Debug.Assert(length >= 0);

            if (length <= 0)
                return 0;

            Debug.Assert(rgbValue.Length >= length);

            int ulValue = 0;
            int ulHi;

            // Size of CRC window (hashing bytes, ssstr, sswstr, numeric)
            const int x_cbCrcWindow = 4;
            // const int iShiftVal = (sizeof ulValue) * (8*sizeof(char)) - x_cbCrcWindow;
            const int iShiftVal = 4 * 8 - x_cbCrcWindow;

            for (int i = 0; i < length; i++)
            {
                ulHi = (ulValue >> iShiftVal) & 0xff;
                ulValue <<= x_cbCrcWindow;
                ulValue = ulValue ^ rgbValue[i] ^ ulHi;
            }

            return ulValue;
        }
        // For hashing purpose
        public override int GetHashCode()
        {
            if (_value is null)
                return 0;

            //First trim off extra '\0's
            int cbLen = _value.Length;
            while (cbLen > 0 && _value[cbLen - 1] == 0)
                --cbLen;

            return HashByteArray(_value, cbLen);
        }

        XmlSchema? IXmlSerializable.GetSchema() { return null; }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            string? isNull = reader.GetAttribute("nil", XmlSchema.InstanceNamespace);
            if (isNull != null && XmlConvert.ToBoolean(isNull))
            {
                // Read the next value.
                reader.ReadElementString();
                _value = null;
            }
            else
            {
                string base64 = reader.ReadElementString();
                if (base64 == null)
                {
                    _value = Array.Empty<byte>();
                }
                else
                {
                    base64 = base64.Trim();

                    if (base64.Length == 0)
                    {
                        _value = Array.Empty<byte>();
                    }
                    else
                    {
                        _value = Convert.FromBase64String(base64);
                    }
                }
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (_value is null)
            {
                writer.WriteAttributeString("xsi", "nil", XmlSchema.InstanceNamespace, "true");
            }
            else
            {
                writer.WriteString(Convert.ToBase64String(_value));
            }
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("base64Binary", XmlSchema.Namespace);
        }

        public static SqlBinary WrapBytes(byte[] bytes)
        {
            return new SqlBinary(bytes, ignored: true);
        }

        /// <summary>
        /// Represents a null value that can be assigned to the <see cref='Value'/> property of an
        /// instance of the <see cref='SqlBinary'/> class.
        /// </summary>
        public static readonly SqlBinary Null = new SqlBinary(true);
    } // SqlBinary
} // namespace System.Data.SqlTypes
