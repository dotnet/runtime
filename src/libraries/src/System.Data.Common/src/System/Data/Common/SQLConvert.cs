// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlTypes;
using System.Xml;
using System.Diagnostics;

namespace System.Data.Common
{
    internal static class SqlConvert
    {
        public static SqlByte ConvertToSqlByte(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlByte");
            if ((value == DBNull.Value))
            { // null is not valid, SqlByte is struct
                return SqlByte.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlByte:
                    return (SqlByte)value;
                case StorageType.Byte:
                    return (byte)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlByte));
            }
        }

        public static SqlInt16 ConvertToSqlInt16(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlInt16");
            if (value == DBNull.Value)
            {
                return SqlInt16.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);
            switch (stype)
            {
                case StorageType.Byte:
                    return (byte)value;
                case StorageType.Int16:
                    return (short)value;
                case StorageType.SqlByte:
                    return (SqlByte)value;
                case StorageType.SqlInt16:
                    return (SqlInt16)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlInt16));
            }
        }

        public static SqlInt32 ConvertToSqlInt32(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlInt32");
            if (value == DBNull.Value)
            {
                return SqlInt32.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);
            switch (stype)
            {
                case StorageType.SqlInt32:
                    return (SqlInt32)value;
                case StorageType.Int32:
                    return (int)value;
                case StorageType.SqlInt16:
                    return (SqlInt16)value;
                case StorageType.Int16:
                    return (short)value;
                case StorageType.UInt16:
                    return (ushort)value;
                case StorageType.SqlByte:
                    return (SqlByte)value;
                case StorageType.Byte:
                    return (byte)value;

                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlInt32));
            }
        }

        public static SqlInt64 ConvertToSqlInt64(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlInt64");
            if (value == DBNull.Value)
            {
                return SqlInt32.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);
            switch (stype)
            {
                case StorageType.SqlInt64:
                    return (SqlInt64)value;
                case StorageType.Int64:
                    return (long)value;
                case StorageType.SqlInt16:
                    return (SqlInt16)value;
                case StorageType.Int16:
                    return (short)value;
                case StorageType.UInt16:
                    return (ushort)value;
                case StorageType.SqlInt32:
                    return (SqlInt32)value;
                case StorageType.Int32:
                    return (int)value;
                case StorageType.UInt32:
                    return (uint)value;
                case StorageType.SqlByte:
                    return (SqlByte)value;
                case StorageType.Byte:
                    return (byte)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlInt64));
            }
        }

        public static SqlDouble ConvertToSqlDouble(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlDouble");
            if (value == DBNull.Value)
            {
                return SqlDouble.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlDouble:
                    return (SqlDouble)value;
                case StorageType.Double:
                    return (double)value;
                case StorageType.SqlInt64:
                    return (SqlInt64)value;
                case StorageType.Int64:
                    return (long)value;
                case StorageType.UInt64:
                    return (ulong)value;
                case StorageType.SqlInt16:
                    return (SqlInt16)value;
                case StorageType.Int16:
                    return (short)value;
                case StorageType.UInt16:
                    return (ushort)value;
                case StorageType.SqlInt32:
                    return (SqlInt32)value;
                case StorageType.Int32:
                    return (int)value;
                case StorageType.UInt32:
                    return (uint)value;
                case StorageType.SqlByte:
                    return (SqlByte)value;
                case StorageType.Byte:
                    return (byte)value;
                case StorageType.SqlSingle:
                    return (SqlSingle)value;
                case StorageType.Single:
                    return (float)value;
                case StorageType.SqlMoney:
                    return (SqlMoney)value;
                case StorageType.SqlDecimal:
                    return (SqlDecimal)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlDouble));
            }
        }

        public static SqlDecimal ConvertToSqlDecimal(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlDecimal");
            if (value == DBNull.Value)
            {
                return SqlDecimal.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlDecimal:
                    return (SqlDecimal)value;
                case StorageType.Decimal:
                    return (decimal)value;
                case StorageType.SqlInt64:
                    return (SqlInt64)value;
                case StorageType.Int64:
                    return (long)value;
                case StorageType.UInt64:
                    return (ulong)value;
                case StorageType.SqlInt16:
                    return (SqlInt16)value;
                case StorageType.Int16:
                    return (short)value;
                case StorageType.UInt16:
                    return (ushort)value;
                case StorageType.SqlInt32:
                    return (SqlInt32)value;
                case StorageType.Int32:
                    return (int)value;
                case StorageType.UInt32:
                    return (uint)value;
                case StorageType.SqlByte:
                    return (SqlByte)value;
                case StorageType.Byte:
                    return (byte)value;
                case StorageType.SqlMoney:
                    return (SqlMoney)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlDecimal));
            }
        }

        public static SqlSingle ConvertToSqlSingle(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlSingle");
            if (value == DBNull.Value)
            {
                return SqlSingle.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlSingle:
                    return (SqlSingle)value;
                case StorageType.Single:
                    return (float)value;
                case StorageType.SqlInt64:
                    return (SqlInt64)value;
                case StorageType.Int64:
                    return (long)value;
                case StorageType.UInt64:
                    return (ulong)value;
                case StorageType.SqlInt16:
                    return (SqlInt16)value;
                case StorageType.Int16:
                    return (short)value;
                case StorageType.UInt16:
                    return (ushort)value;
                case StorageType.SqlInt32:
                    return (SqlInt32)value;
                case StorageType.Int32:
                    return (int)value;
                case StorageType.UInt32:
                    return (uint)value;
                case StorageType.SqlByte:
                    return (SqlByte)value;
                case StorageType.Byte:
                    return (byte)value;
                case StorageType.SqlMoney:
                    return (SqlMoney)value;
                case StorageType.SqlDecimal:
                    return (SqlDecimal)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlSingle));
            }
        }

        public static SqlMoney ConvertToSqlMoney(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlMoney");
            if (value == DBNull.Value)
            {
                return SqlMoney.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlMoney:
                    return (SqlMoney)value;
                case StorageType.Decimal:
                    return (decimal)value;
                case StorageType.SqlInt64:
                    return (SqlInt64)value;
                case StorageType.Int64:
                    return (long)value;
                case StorageType.UInt64:
                    return (ulong)value;
                case StorageType.SqlInt16:
                    return (SqlInt16)value;
                case StorageType.Int16:
                    return (short)value;
                case StorageType.UInt16:
                    return (ushort)value;
                case StorageType.SqlInt32:
                    return (SqlInt32)value;
                case StorageType.Int32:
                    return (int)value;
                case StorageType.UInt32:
                    return (uint)value;
                case StorageType.SqlByte:
                    return (SqlByte)value;
                case StorageType.Byte:
                    return (byte)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlMoney));
            }
        }


        public static SqlDateTime ConvertToSqlDateTime(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlDateTime");
            if (value == DBNull.Value)
            {
                return SqlDateTime.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlDateTime:
                    return (SqlDateTime)value;
                case StorageType.DateTime:
                    return (DateTime)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlDateTime));
            }
        }

        public static SqlBoolean ConvertToSqlBoolean(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlBoolean");
            if ((value == DBNull.Value) || (value == null))
            {
                return SqlBoolean.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlBoolean:
                    return (SqlBoolean)value;
                case StorageType.Boolean:
                    return (bool)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlBoolean));
            }
        }

        public static SqlGuid ConvertToSqlGuid(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlGuid");
            if (value == DBNull.Value)
            {
                return SqlGuid.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlGuid:
                    return (SqlGuid)value;
                case StorageType.Guid:
                    return (Guid)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlGuid));
            }
        }

        public static SqlBinary ConvertToSqlBinary(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlBinary");
            if (value == DBNull.Value)
            {
                return SqlBinary.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlBinary:
                    return (SqlBinary)value;
                case StorageType.ByteArray:
                    return (byte[])value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlBinary));
            }
        }

        public static SqlString ConvertToSqlString(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlString");
            if ((value == DBNull.Value) || (value == null))
            {
                return SqlString.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);

            switch (stype)
            {
                case StorageType.SqlString:
                    return (SqlString)value;
                case StorageType.String:
                    return (string)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlString));
            }
        }

        public static SqlChars ConvertToSqlChars(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlChars");
            if (value == DBNull.Value)
            {
                return SqlChars.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);
            switch (stype)
            {
                case StorageType.SqlChars:
                    return (SqlChars)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlChars));
            }
        }

        public static SqlBytes ConvertToSqlBytes(object value)
        {
            Debug.Assert(value != null, "null argument in ConvertToSqlBytes");
            if (value == DBNull.Value)
            {
                return SqlBytes.Null;
            }
            Type valueType = value.GetType();
            StorageType stype = DataStorage.GetStorageType(valueType);
            switch (stype)
            {
                case StorageType.SqlBytes:
                    return (SqlBytes)value;
                default:
                    throw ExceptionBuilder.ConvertFailed(valueType, typeof(SqlBytes));
            }
        }

        public static DateTimeOffset ConvertStringToDateTimeOffset(string value, IFormatProvider formatProvider)
        {
            return DateTimeOffset.Parse(value, formatProvider);
        }
        // this should not be called for XmlSerialization
        public static object ChangeTypeForDefaultValue(object value, Type type, IFormatProvider formatProvider)
        {
            if (type == typeof(System.Numerics.BigInteger))
            {
                if ((DBNull.Value == value) || (null == value)) { return DBNull.Value; }
                return BigIntegerStorage.ConvertToBigInteger(value, formatProvider);
            }
            else if (value is System.Numerics.BigInteger)
            {
                return BigIntegerStorage.ConvertFromBigInteger((System.Numerics.BigInteger)value, type, formatProvider);
            }

            return ChangeType2(value, DataStorage.GetStorageType(type), type, formatProvider);
        }

        // this should not be called for XmlSerialization
        public static object ChangeType2(object value, StorageType stype, Type type, IFormatProvider formatProvider)
        {
            switch (stype)
            { // if destination is SQL type
                case StorageType.SqlBinary:
                    return (SqlConvert.ConvertToSqlBinary(value));
                case StorageType.SqlBoolean:
                    return (SqlConvert.ConvertToSqlBoolean(value));
                case StorageType.SqlByte:
                    return (SqlConvert.ConvertToSqlByte(value));
                case StorageType.SqlBytes:
                    return (SqlConvert.ConvertToSqlBytes(value));
                case StorageType.SqlChars:
                    return (SqlConvert.ConvertToSqlChars(value));
                case StorageType.SqlDateTime:
                    return (SqlConvert.ConvertToSqlDateTime(value));
                case StorageType.SqlDecimal:
                    return (SqlConvert.ConvertToSqlDecimal(value));
                case StorageType.SqlDouble:
                    return (SqlConvert.ConvertToSqlDouble(value));
                case StorageType.SqlGuid:
                    return (SqlConvert.ConvertToSqlGuid(value));
                case StorageType.SqlInt16:
                    return (SqlConvert.ConvertToSqlInt16(value));
                case StorageType.SqlInt32:
                    return (SqlConvert.ConvertToSqlInt32(value));
                case StorageType.SqlInt64:
                    return (SqlConvert.ConvertToSqlInt64(value));
                case StorageType.SqlMoney:
                    return (SqlConvert.ConvertToSqlMoney(value));
                case StorageType.SqlSingle:
                    return (SqlConvert.ConvertToSqlSingle(value));
                case StorageType.SqlString:
                    return (SqlConvert.ConvertToSqlString(value));
                /*            case StorageType.SqlXml:
                                if (DataStorage.IsObjectNull(value)) {
                                    return SqlXml.Null;
                                }
                                goto default;
                */
                default: // destination is CLR
                    if ((DBNull.Value == value) || (null == value))
                    {
                        return DBNull.Value;
                    }
                    Type valueType = value.GetType();
                    StorageType vtype = DataStorage.GetStorageType(valueType);
                    // destination is CLR
                    switch (vtype)
                    {// and source is SQL type
                        case StorageType.SqlBinary:
                        case StorageType.SqlBoolean:
                        case StorageType.SqlByte:
                        case StorageType.SqlBytes:
                        case StorageType.SqlChars:
                        case StorageType.SqlDateTime:
                        case StorageType.SqlDecimal:
                        case StorageType.SqlDouble:
                        case StorageType.SqlGuid:
                        case StorageType.SqlInt16:
                        case StorageType.SqlInt32:
                        case StorageType.SqlInt64:
                        case StorageType.SqlMoney:
                        case StorageType.SqlSingle:
                        case StorageType.SqlString:
                            throw ExceptionBuilder.ConvertFailed(valueType, type);
                        default: // source is CLR type
                            if (StorageType.String == stype)
                            { // destination is string
                                switch (vtype)
                                { // source's  type
                                    case StorageType.Boolean:
                                        return ((IConvertible)(bool)value).ToString(formatProvider);
                                    case StorageType.Char:
                                        return ((IConvertible)(char)value).ToString(formatProvider);
                                    case StorageType.SByte:
                                        return ((sbyte)value).ToString(formatProvider);
                                    case StorageType.Byte:
                                        return ((byte)value).ToString(formatProvider);
                                    case StorageType.Int16:
                                        return ((short)value).ToString(formatProvider);
                                    case StorageType.UInt16:
                                        return ((ushort)value).ToString(formatProvider);
                                    case StorageType.Int32:
                                        return ((int)value).ToString(formatProvider);
                                    case StorageType.UInt32:
                                        return ((uint)value).ToString(formatProvider);
                                    case StorageType.Int64:
                                        return ((long)value).ToString(formatProvider);
                                    case StorageType.UInt64:
                                        return ((ulong)value).ToString(formatProvider);
                                    case StorageType.Single:
                                        return ((float)value).ToString(formatProvider);
                                    case StorageType.Double:
                                        return ((double)value).ToString(formatProvider);
                                    case StorageType.Decimal:
                                        return ((decimal)value).ToString(formatProvider);
                                    case StorageType.DateTime:
                                        return ((DateTime)value).ToString(formatProvider);
                                    //return  XmlConvert.ToString((DateTime) value, XmlDateTimeSerializationMode.RoundtripKind);
                                    case StorageType.TimeSpan:
                                        return XmlConvert.ToString((TimeSpan)value);
                                    case StorageType.Guid:
                                        return XmlConvert.ToString((Guid)value);
                                    case StorageType.String:
                                        return (string)value;
                                    case StorageType.CharArray:
                                        return new string((char[])value);
                                    case StorageType.DateTimeOffset:
                                        return ((DateTimeOffset)value).ToString(formatProvider);
                                    case StorageType.BigInteger:
                                        break;
                                    default:
                                        IConvertible iconvertible = (value as IConvertible);
                                        if (null != iconvertible)
                                        {
                                            return iconvertible.ToString(formatProvider);
                                        }
                                        // catch additional classes like Guid
                                        IFormattable iformattable = (value as IFormattable);
                                        if (null != iformattable)
                                        {
                                            return iformattable.ToString(null, formatProvider);
                                        }
                                        return value.ToString();
                                }
                            }
                            else if (StorageType.TimeSpan == stype)
                            {
                                // destination is TimeSpan
                                switch (vtype)
                                {
                                    case StorageType.String:
                                        return XmlConvert.ToTimeSpan((string)value);
                                    case StorageType.Int32:
                                        return new TimeSpan((int)value);
                                    case StorageType.Int64:
                                        return new TimeSpan((long)value);
                                    default:
                                        return (TimeSpan)value;
                                }
                            }
                            else if (StorageType.DateTimeOffset == stype)
                            { // destination is DateTimeOffset
                                return (DateTimeOffset)value;
                            }
                            else if (StorageType.String == vtype)
                            { // if source is string
                                switch (stype)
                                { // type of destination
                                    case StorageType.String:
                                        return (string)value;
                                    case StorageType.Boolean:
                                        if ("1" == (string)value) return true;
                                        if ("0" == (string)value) return false;
                                        break;
                                    case StorageType.Char:
                                        return ((IConvertible)(string)value).ToChar(formatProvider);
                                    case StorageType.SByte:
                                        return ((IConvertible)(string)value).ToSByte(formatProvider);
                                    case StorageType.Byte:
                                        return ((IConvertible)(string)value).ToByte(formatProvider);
                                    case StorageType.Int16:
                                        return ((IConvertible)(string)value).ToInt16(formatProvider);
                                    case StorageType.UInt16:
                                        return ((IConvertible)(string)value).ToUInt16(formatProvider);
                                    case StorageType.Int32:
                                        return ((IConvertible)(string)value).ToInt32(formatProvider);
                                    case StorageType.UInt32:
                                        return ((IConvertible)(string)value).ToUInt32(formatProvider);
                                    case StorageType.Int64:
                                        return ((IConvertible)(string)value).ToInt64(formatProvider);
                                    case StorageType.UInt64:
                                        return ((IConvertible)(string)value).ToUInt64(formatProvider);
                                    case StorageType.Single:
                                        return ((IConvertible)(string)value).ToSingle(formatProvider);
                                    case StorageType.Double:
                                        return ((IConvertible)(string)value).ToDouble(formatProvider);
                                    case StorageType.Decimal:
                                        return ((IConvertible)(string)value).ToDecimal(formatProvider);
                                    case StorageType.DateTime:
                                        return ((IConvertible)(string)value).ToDateTime(formatProvider);
                                    //return  XmlConvert.ToDateTime((string) value, XmlDateTimeSerializationMode.RoundtripKind);
                                    case StorageType.TimeSpan:
                                        return XmlConvert.ToTimeSpan((string)value);
                                    case StorageType.Guid:
                                        return XmlConvert.ToGuid((string)value);
                                    case StorageType.Uri:
                                        return new Uri((string)value);
                                    default: // other clr types,
                                        break;
                                }
                            }
                            return Convert.ChangeType(value, type, formatProvider);
                    }
            }
        }

        // this should be called for XmlSerialization
        public static object ChangeTypeForXML(object value, Type type)
        {
            Debug.Assert(value is string || type == typeof(string), "invalid call to ChangeTypeForXML");
            StorageType destinationType = DataStorage.GetStorageType(type);
            Type valueType = value.GetType();
            StorageType vtype = DataStorage.GetStorageType(valueType);

            switch (destinationType)
            { // if destination is not string
                case StorageType.SqlBinary:
                    return new SqlBinary(Convert.FromBase64String((string)value));
                case StorageType.SqlBoolean:
                    return new SqlBoolean(XmlConvert.ToBoolean((string)value));
                case StorageType.SqlByte:
                    return new SqlByte(XmlConvert.ToByte((string)value));
                case StorageType.SqlBytes:
                    return new SqlBytes(Convert.FromBase64String((string)value));
                case StorageType.SqlChars:
                    return new SqlChars(((string)value).ToCharArray());
                case StorageType.SqlDateTime:
                    return new SqlDateTime(XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.RoundtripKind));
                case StorageType.SqlDecimal:
                    return SqlDecimal.Parse((string)value); // parses invariant format and is larger has larger range then Decimal
                case StorageType.SqlDouble:
                    return new SqlDouble(XmlConvert.ToDouble((string)value));
                case StorageType.SqlGuid:
                    return new SqlGuid(XmlConvert.ToGuid((string)value));
                case StorageType.SqlInt16:
                    return new SqlInt16(XmlConvert.ToInt16((string)value));
                case StorageType.SqlInt32:
                    return new SqlInt32(XmlConvert.ToInt32((string)value));
                case StorageType.SqlInt64:
                    return new SqlInt64(XmlConvert.ToInt64((string)value));
                case StorageType.SqlMoney:
                    return new SqlMoney(XmlConvert.ToDecimal((string)value));
                case StorageType.SqlSingle:
                    return new SqlSingle(XmlConvert.ToSingle((string)value));
                case StorageType.SqlString:
                    return new SqlString((string)value);
                //                case StorageType.SqlXml: // What to do
                //                    if (DataStorage.IsObjectNull(value)) {
                //                        return SqlXml.Null;
                //                    }
                //                    goto default;
                case StorageType.Boolean:
                    if ("1" == (string)value) return true;
                    if ("0" == (string)value) return false;
                    return XmlConvert.ToBoolean((string)value);
                case StorageType.Char:
                    return XmlConvert.ToChar((string)value);
                case StorageType.SByte:
                    return XmlConvert.ToSByte((string)value);
                case StorageType.Byte:
                    return XmlConvert.ToByte((string)value);
                case StorageType.Int16:
                    return XmlConvert.ToInt16((string)value);
                case StorageType.UInt16:
                    return XmlConvert.ToUInt16((string)value);
                case StorageType.Int32:
                    return XmlConvert.ToInt32((string)value);
                case StorageType.UInt32:
                    return XmlConvert.ToUInt32((string)value);
                case StorageType.Int64:
                    return XmlConvert.ToInt64((string)value);
                case StorageType.UInt64:
                    return XmlConvert.ToUInt64((string)value);
                case StorageType.Single:
                    return XmlConvert.ToSingle((string)value);
                case StorageType.Double:
                    return XmlConvert.ToDouble((string)value);
                case StorageType.Decimal:
                    return XmlConvert.ToDecimal((string)value);
                case StorageType.DateTime:
                    return XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.RoundtripKind);
                case StorageType.Guid:
                    return XmlConvert.ToGuid((string)value);
                case StorageType.Uri:
                    return new Uri((string)value);
                case StorageType.DateTimeOffset:
                    return XmlConvert.ToDateTimeOffset((string)value);
                case StorageType.TimeSpan:
                    switch (vtype)
                    {
                        case StorageType.String:
                            return XmlConvert.ToTimeSpan((string)value);
                        case StorageType.Int32:
                            return new TimeSpan((int)value);
                        case StorageType.Int64:
                            return new TimeSpan((long)value);
                        default:
                            return (TimeSpan)value;
                    }
                default:
                    {
                        if ((DBNull.Value == value) || (null == value))
                        {
                            return DBNull.Value;
                        }

                        switch (vtype)
                        { // To String
                            case StorageType.SqlBinary:
                                return Convert.ToBase64String(((SqlBinary)value).Value);
                            case StorageType.SqlBoolean:
                                return XmlConvert.ToString(((SqlBoolean)value).Value);
                            case StorageType.SqlByte:
                                return XmlConvert.ToString(((SqlByte)value).Value);
                            case StorageType.SqlBytes:
                                return Convert.ToBase64String(((SqlBytes)value).Value);
                            case StorageType.SqlChars:
                                return new string(((SqlChars)value).Value);
                            case StorageType.SqlDateTime:
                                return XmlConvert.ToString(((SqlDateTime)value).Value, XmlDateTimeSerializationMode.RoundtripKind);
                            case StorageType.SqlDecimal:
                                return ((SqlDecimal)value).ToString(); // converts using invariant format and is larger has larger range then Decimal
                            case StorageType.SqlDouble:
                                return XmlConvert.ToString(((SqlDouble)value).Value);
                            case StorageType.SqlGuid:
                                return XmlConvert.ToString(((SqlGuid)value).Value);
                            case StorageType.SqlInt16:
                                return XmlConvert.ToString(((SqlInt16)value).Value);
                            case StorageType.SqlInt32:
                                return XmlConvert.ToString(((SqlInt32)value).Value);
                            case StorageType.SqlInt64:
                                return XmlConvert.ToString(((SqlInt64)value).Value);
                            case StorageType.SqlMoney:
                                return XmlConvert.ToString(((SqlMoney)value).Value);
                            case StorageType.SqlSingle:
                                return XmlConvert.ToString(((SqlSingle)value).Value);
                            case StorageType.SqlString:
                                return ((SqlString)value).Value;
                            case StorageType.Boolean:
                                return XmlConvert.ToString((bool)value);
                            case StorageType.Char:
                                return XmlConvert.ToString((char)value);
                            case StorageType.SByte:
                                return XmlConvert.ToString((sbyte)value);
                            case StorageType.Byte:
                                return XmlConvert.ToString((byte)value);
                            case StorageType.Int16:
                                return XmlConvert.ToString((short)value);
                            case StorageType.UInt16:
                                return XmlConvert.ToString((ushort)value);
                            case StorageType.Int32:
                                return XmlConvert.ToString((int)value);
                            case StorageType.UInt32:
                                return XmlConvert.ToString((uint)value);
                            case StorageType.Int64:
                                return XmlConvert.ToString((long)value);
                            case StorageType.UInt64:
                                return XmlConvert.ToString((ulong)value);
                            case StorageType.Single:
                                return XmlConvert.ToString((float)value);
                            case StorageType.Double:
                                return XmlConvert.ToString((double)value);
                            case StorageType.Decimal:
                                return XmlConvert.ToString((decimal)value);
                            case StorageType.DateTime:
                                return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
                            case StorageType.TimeSpan:
                                return XmlConvert.ToString((TimeSpan)value);
                            case StorageType.Guid:
                                return XmlConvert.ToString((Guid)value);
                            case StorageType.String:
                                return (string)value;
                            case StorageType.CharArray:
                                return new string((char[])value);
                            case StorageType.DateTimeOffset:
                                return XmlConvert.ToString((DateTimeOffset)value);
                            default:
                                IConvertible iconvertible = (value as IConvertible);
                                if (null != iconvertible)
                                {
                                    return iconvertible.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }
                                // catch additional classes like Guid
                                IFormattable iformattable = (value as IFormattable);
                                if (null != iformattable)
                                {
                                    return iformattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
                                }
                                return value.ToString();
                        }
                    }
            }
        }
    }
}
