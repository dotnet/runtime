// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes
{
    /// <summary>
    /// Represents a currency value ranging from
    /// -2<superscript term='63'/> (or -922,337,203,685,477.5808) to 2<superscript term='63'/> -1 (or
    /// +922,337,203,685,477.5807) with an accuracy to
    /// a ten-thousandth of currency unit to be stored in or retrieved from a
    /// database.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    public struct SqlMoney : INullable, IComparable, IXmlSerializable
    {
        // NOTE: If any instance fields change, update SqlTypeWorkarounds type in System.Data.SqlClient.
        private bool _fNotNull; // false if null
        private long _value;

        // SQL Server stores money8 as ticks of 1/10000.
        internal static readonly int s_iMoneyScale = 4;
        private static readonly long s_lTickBase = 10000;
        private static readonly double s_dTickBase = s_lTickBase;

        private static readonly long s_minLong = unchecked((long)0x8000000000000000L) / s_lTickBase;
        private static readonly long s_maxLong = 0x7FFFFFFFFFFFFFFFL / s_lTickBase;

        // constructor
        // construct a Null
        private SqlMoney(bool fNull)
        {
            _fNotNull = false;
            _value = 0;
        }

        // Constructs from a long value without scaling. The ignored parameter exists
        // only to distinguish this constructor from the constructor that takes a long.
        // Used only internally.
        internal SqlMoney(long value, int ignored)
        {
            _value = value;
            _fNotNull = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='SqlMoney'/> class with the value given.
        /// </summary>
        public SqlMoney(int value)
        {
            _value = value * s_lTickBase;
            _fNotNull = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='SqlMoney'/> class with the value given.
        /// </summary>
        public SqlMoney(long value)
        {
            if (value < s_minLong || value > s_maxLong)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            _value = value * s_lTickBase;
            _fNotNull = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='SqlMoney'/> class with the value given.
        /// </summary>
        public SqlMoney(decimal value)
        {
            // Since Decimal is a value type, operate directly on value, don't worry about changing it.
            SqlDecimal snum = new SqlDecimal(value);
            snum.AdjustScale(s_iMoneyScale - snum.Scale, true);
            Debug.Assert(snum.Scale == s_iMoneyScale);

            if (snum._data3 != 0 || snum._data4 != 0)
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            bool fPositive = snum.IsPositive;
            ulong ulValue = snum._data1 + (((ulong)snum._data2) << 32);
            if (fPositive && ulValue > long.MaxValue ||
                !fPositive && ulValue > unchecked((ulong)(long.MinValue)))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            _value = fPositive ? (long)ulValue : unchecked(-(long)ulValue);
            _fNotNull = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='SqlMoney'/> class with the value given.
        /// </summary>
        public SqlMoney(double value) : this(new decimal(value))
        {
        }


        /// <summary>
        /// Gets a value indicating whether the <see cref='Value'/>
        /// property is assigned to null.
        /// </summary>
        public bool IsNull
        {
            get { return !_fNotNull; }
        }

        /// <summary>
        /// Gets or sets the monetary value of an instance of the <see cref='SqlMoney'/> class.
        /// </summary>
        public decimal Value
        {
            get
            {
                if (_fNotNull)
                    return ToDecimal();
                else
                    throw new SqlNullValueException();
            }
        }

        public decimal ToDecimal()
        {
            if (IsNull)
                throw new SqlNullValueException();

            bool fNegative = false;
            long value = _value;
            if (_value < 0)
            {
                fNegative = true;
                value = unchecked(-_value);
            }

            return new decimal(unchecked((int)value), unchecked((int)(value >> 32)), 0, fNegative, (byte)s_iMoneyScale);
        }

        public long ToInt64()
        {
            if (IsNull)
                throw new SqlNullValueException();

            long ret = _value / (s_lTickBase / 10);
            bool fPositive = (ret >= 0);
            long remainder = ret % 10;
            ret = ret / 10;

            if (remainder >= 5)
            {
                if (fPositive)
                    ret++;
                else
                    ret--;
            }

            return ret;
        }

        internal long ToSqlInternalRepresentation()
        {
            if (IsNull)
                throw new SqlNullValueException();

            return _value;
        }

        public int ToInt32()
        {
            return checked((int)(ToInt64()));
        }

        public double ToDouble()
        {
            return decimal.ToDouble(ToDecimal());
        }

        // Implicit conversion from Decimal to SqlMoney
        public static implicit operator SqlMoney(decimal x)
        {
            return new SqlMoney(x);
        }

        // Explicit conversion from Double to SqlMoney
        public static explicit operator SqlMoney(double x)
        {
            return new SqlMoney(x);
        }

        // Implicit conversion from long to SqlMoney
        public static implicit operator SqlMoney(long x)
        {
            return new SqlMoney(new decimal(x));
        }

        // Explicit conversion from SqlMoney to Decimal. Throw exception if x is Null.
        public static explicit operator decimal (SqlMoney x)
        {
            return x.Value;
        }

        public override string ToString()
        {
            if (IsNull)
            {
                return SQLResource.NullString;
            }
            decimal money = ToDecimal();
            // Formatting of SqlMoney: At least two digits after decimal point
            return money.ToString("#0.00##", null);
        }

        public static SqlMoney Parse(string s)
        {
            // Try parsing the format of '#0.00##' generated by ToString() by using the
            // culture invariant NumberFormatInfo as well as the current culture's format
            //
            decimal d;
            SqlMoney money;

            const NumberStyles SqlNumberStyle =
                     NumberStyles.AllowCurrencySymbol |
                     NumberStyles.AllowDecimalPoint |
                     NumberStyles.AllowParentheses |
                     NumberStyles.AllowTrailingSign |
                     NumberStyles.AllowLeadingSign |
                     NumberStyles.AllowTrailingWhite |
                     NumberStyles.AllowLeadingWhite;

            if (s == SQLResource.NullString)
            {
                money = SqlMoney.Null;
            }
            else if (decimal.TryParse(s, SqlNumberStyle, NumberFormatInfo.InvariantInfo, out d))
            {
                money = new SqlMoney(d);
            }
            else
            {
                money = new SqlMoney(decimal.Parse(s, NumberStyles.Currency, NumberFormatInfo.CurrentInfo));
            }

            return money;
        }

        // Unary operators
        public static SqlMoney operator -(SqlMoney x)
        {
            if (x.IsNull)
                return Null;
            if (x._value == s_minLong)
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            return new SqlMoney(-x._value, 0);
        }


        // Binary operators

        // Arithmetic operators
        public static SqlMoney operator +(SqlMoney x, SqlMoney y)
        {
            try
            {
                return (x.IsNull || y.IsNull) ? Null : new SqlMoney(checked(x._value + y._value), 0);
            }
            catch (OverflowException)
            {
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            }
        }

        public static SqlMoney operator -(SqlMoney x, SqlMoney y)
        {
            try
            {
                return (x.IsNull || y.IsNull) ? Null : new SqlMoney(checked(x._value - y._value), 0);
            }
            catch (OverflowException)
            {
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            }
        }

        public static SqlMoney operator *(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? Null :
        new SqlMoney(decimal.Multiply(x.ToDecimal(), y.ToDecimal()));
        }

        public static SqlMoney operator /(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? Null :
        new SqlMoney(decimal.Divide(x.ToDecimal(), y.ToDecimal()));
        }


        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlMoney
        public static explicit operator SqlMoney(SqlBoolean x)
        {
            return x.IsNull ? Null : new SqlMoney(x.ByteValue);
        }

        // Implicit conversion from SqlByte to SqlMoney
        public static implicit operator SqlMoney(SqlByte x)
        {
            return x.IsNull ? Null : new SqlMoney(x.Value);
        }

        // Implicit conversion from SqlInt16 to SqlMoney
        public static implicit operator SqlMoney(SqlInt16 x)
        {
            return x.IsNull ? Null : new SqlMoney(x.Value);
        }

        // Implicit conversion from SqlInt32 to SqlMoney
        public static implicit operator SqlMoney(SqlInt32 x)
        {
            return x.IsNull ? Null : new SqlMoney(x.Value);
        }

        // Implicit conversion from SqlInt64 to SqlMoney
        public static implicit operator SqlMoney(SqlInt64 x)
        {
            return x.IsNull ? Null : new SqlMoney(x.Value);
        }


        // Explicit conversions

        // Explicit conversion from SqlSingle to SqlMoney
        public static explicit operator SqlMoney(SqlSingle x)
        {
            return x.IsNull ? Null : new SqlMoney(x.Value);
        }

        // Explicit conversion from SqlDouble to SqlMoney
        public static explicit operator SqlMoney(SqlDouble x)
        {
            return x.IsNull ? Null : new SqlMoney(x.Value);
        }

        // Explicit conversion from SqlDecimal to SqlMoney
        public static explicit operator SqlMoney(SqlDecimal x)
        {
            return x.IsNull ? SqlMoney.Null : new SqlMoney(x.Value);
        }

        // Explicit conversion from SqlString to SqlMoney
        // Throws FormatException or OverflowException if necessary.
        public static explicit operator SqlMoney(SqlString x)
        {
            return x.IsNull ? Null : new SqlMoney(decimal.Parse(x.Value, NumberStyles.Currency, null));
        }


        // Builtin functions

        // Overloading comparison operators
        public static SqlBoolean operator ==(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value == y._value);
        }

        public static SqlBoolean operator !=(SqlMoney x, SqlMoney y)
        {
            return !(x == y);
        }

        public static SqlBoolean operator <(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value < y._value);
        }

        public static SqlBoolean operator >(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value > y._value);
        }

        public static SqlBoolean operator <=(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value <= y._value);
        }

        public static SqlBoolean operator >=(SqlMoney x, SqlMoney y)
        {
            return (x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x._value >= y._value);
        }


        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator +
        public static SqlMoney Add(SqlMoney x, SqlMoney y)
        {
            return x + y;
        }
        // Alternative method for operator -
        public static SqlMoney Subtract(SqlMoney x, SqlMoney y)
        {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlMoney Multiply(SqlMoney x, SqlMoney y)
        {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlMoney Divide(SqlMoney x, SqlMoney y)
        {
            return x / y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlMoney x, SqlMoney y)
        {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlMoney x, SqlMoney y)
        {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlMoney x, SqlMoney y)
        {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlMoney x, SqlMoney y)
        {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlMoney x, SqlMoney y)
        {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlMoney x, SqlMoney y)
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
        public int CompareTo(object value)
        {
            if (value is SqlMoney)
            {
                SqlMoney i = (SqlMoney)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlMoney));
        }

        public int CompareTo(SqlMoney value)
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
        public override bool Equals(object value)
        {
            if (!(value is SqlMoney))
            {
                return false;
            }

            SqlMoney i = (SqlMoney)value;

            if (i.IsNull || IsNull)
                return (i.IsNull && IsNull);
            else
                return (this == i).Value;
        }

        // For hashing purpose
        public override int GetHashCode()
        {
            // Don't use Value property, because Value will convert to Decimal, which is not necessary.
            return IsNull ? 0 : _value.GetHashCode();
        }

        XmlSchema IXmlSerializable.GetSchema() { return null; }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            string isNull = reader.GetAttribute("nil", XmlSchema.InstanceNamespace);
            if (isNull != null && XmlConvert.ToBoolean(isNull))
            {
                // Read the next value.
                reader.ReadElementString();
                _fNotNull = false;
            }
            else
            {
                SqlMoney money = new SqlMoney(XmlConvert.ToDecimal(reader.ReadElementString()));
                _fNotNull = money._fNotNull;
                _value = money._value;
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
                writer.WriteString(XmlConvert.ToString(ToDecimal()));
            }
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("decimal", XmlSchema.Namespace);
        }

        /// <summary>
        /// Represents a null value that can be assigned to
        /// the <see cref='Value'/> property of an instance of
        /// the <see cref='SqlMoney'/>class.
        /// </summary>
        public static readonly SqlMoney Null = new SqlMoney(true);

        /// <summary>
        /// Represents the zero value that can be assigned to the <see cref='Value'/> property of an instance of
        /// the <see cref='SqlMoney'/> class.
        /// </summary>
        public static readonly SqlMoney Zero = new SqlMoney(0);

        /// <summary>
        /// Represents the minimum value that can be assigned
        /// to <see cref='Value'/> property of an instance of
        /// the <see cref='SqlMoney'/>
        /// class.
        /// </summary>
        public static readonly SqlMoney MinValue = new SqlMoney(unchecked((long)0x8000000000000000L), 0);

        /// <summary>
        /// Represents the maximum value that can be assigned to
        /// the <see cref='Value'/> property of an instance of
        /// the <see cref='SqlMoney'/>
        /// class.
        /// </summary>
        public static readonly SqlMoney MaxValue = new SqlMoney(0x7FFFFFFFFFFFFFFFL, 0);
    } // SqlMoney
} // namespace System.Data.SqlTypes
