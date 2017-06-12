// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.IO;

using MSS = Microsoft.SqlServer.Server;

namespace System.Data.SqlClient
{
    internal sealed class MetaType
    {
        internal readonly Type ClassType;   // com+ type
        internal readonly Type SqlType;

        internal readonly int FixedLength; // fixed length size in bytes (-1 for variable)
        internal readonly bool IsFixed;     // true if fixed length, note that sqlchar and sqlbinary are not considered fixed length
        internal readonly bool IsLong;      // true if long
        internal readonly bool IsPlp;       // Column is Partially Length Prefixed (MAX)
        internal readonly byte Precision;   // maximum precision for numeric types 
        internal readonly byte Scale;
        internal readonly byte TDSType;
        internal readonly byte NullableType;

        internal readonly string TypeName;    // string name of this type
        internal readonly SqlDbType SqlDbType;
        internal readonly DbType DbType;

        //  holds count of property bytes expected for a SQLVariant structure
        internal readonly byte PropBytes;


        // pre-computed fields
        internal readonly bool IsAnsiType;
        internal readonly bool IsBinType;
        internal readonly bool IsCharType;
        internal readonly bool IsNCharType;
        internal readonly bool IsSizeInCharacters;
        internal readonly bool IsNewKatmaiType;
        internal readonly bool IsVarTime;

        internal readonly bool Is70Supported;
        internal readonly bool Is80Supported;
        internal readonly bool Is90Supported;
        internal readonly bool Is100Supported;

        public MetaType(byte precision, byte scale, int fixedLength, bool isFixed, bool isLong, bool isPlp, byte tdsType, byte nullableTdsType, string typeName, Type classType, Type sqlType, SqlDbType sqldbType, DbType dbType, byte propBytes)
        {
            this.Precision = precision;
            this.Scale = scale;
            this.FixedLength = fixedLength;
            this.IsFixed = isFixed;
            this.IsLong = isLong;
            this.IsPlp = isPlp;
            // can we get rid of this (?just have a mapping?)
            this.TDSType = tdsType;
            this.NullableType = nullableTdsType;
            this.TypeName = typeName;
            this.SqlDbType = sqldbType;
            this.DbType = dbType;

            this.ClassType = classType;
            this.SqlType = sqlType;
            this.PropBytes = propBytes;

            IsAnsiType = _IsAnsiType(sqldbType);
            IsBinType = _IsBinType(sqldbType);
            IsCharType = _IsCharType(sqldbType);
            IsNCharType = _IsNCharType(sqldbType);
            IsSizeInCharacters = _IsSizeInCharacters(sqldbType);
            IsNewKatmaiType = _IsNewKatmaiType(sqldbType);
            IsVarTime = _IsVarTime(sqldbType);

            Is70Supported = _Is70Supported(SqlDbType);
            Is80Supported = _Is80Supported(SqlDbType);
            Is90Supported = _Is90Supported(SqlDbType);
            Is100Supported = _Is100Supported(SqlDbType);
        }

        // properties should be inlined so there should be no perf penalty for using these accessor functions
        public int TypeId
        {             // partial length prefixed (xml, nvarchar(max),...)
            get { return 0; }
        }

        private static bool _IsAnsiType(SqlDbType type)
        {
            return (type == SqlDbType.Char ||
                   type == SqlDbType.VarChar ||
                   type == SqlDbType.Text);
        }

        // is this type size expressed as count of characters or bytes?
        private static bool _IsSizeInCharacters(SqlDbType type)
        {
            return (type == SqlDbType.NChar ||
                   type == SqlDbType.NVarChar ||
                   type == SqlDbType.Xml ||
                   type == SqlDbType.NText);
        }

        private static bool _IsCharType(SqlDbType type)
        {
            return (type == SqlDbType.NChar ||
                   type == SqlDbType.NVarChar ||
                   type == SqlDbType.NText ||
                   type == SqlDbType.Char ||
                   type == SqlDbType.VarChar ||
                   type == SqlDbType.Text ||
                   type == SqlDbType.Xml);
        }

        private static bool _IsNCharType(SqlDbType type)
        {
            return (type == SqlDbType.NChar ||
                   type == SqlDbType.NVarChar ||
                   type == SqlDbType.NText ||
                   type == SqlDbType.Xml);
        }

        private static bool _IsBinType(SqlDbType type)
        {
            return (type == SqlDbType.Image ||
                   type == SqlDbType.Binary ||
                   type == SqlDbType.VarBinary ||
                   type == SqlDbType.Timestamp ||
                   type == SqlDbType.Udt ||
                   (int)type == 24 /*SqlSmallVarBinary*/);
        }

        private static bool _Is70Supported(SqlDbType type)
        {
            return ((type != SqlDbType.BigInt) && ((int)type > 0) &&
                   ((int)type <= (int)SqlDbType.VarChar));
        }

        private static bool _Is80Supported(SqlDbType type)
        {
            return ((int)type >= 0 &&
                ((int)type <= (int)SqlDbType.Variant));
        }

        private static bool _Is90Supported(SqlDbType type)
        {
            return _Is80Supported(type) ||
                    SqlDbType.Xml == type ||
                    SqlDbType.Udt == type;
        }

        private static bool _Is100Supported(SqlDbType type)
        {
            return _Is90Supported(type) ||
                    SqlDbType.Date == type ||
                    SqlDbType.Time == type ||
                    SqlDbType.DateTime2 == type ||
                    SqlDbType.DateTimeOffset == type;
        }

        private static bool _IsNewKatmaiType(SqlDbType type)
        {
            return SqlDbType.Structured == type;
        }

        internal static bool _IsVarTime(SqlDbType type)
        {
            return (type == SqlDbType.Time || type == SqlDbType.DateTime2 || type == SqlDbType.DateTimeOffset);
        }

        //
        // map SqlDbType to MetaType class
        //
        internal static MetaType GetMetaTypeFromSqlDbType(SqlDbType target, bool isMultiValued)
        { // WebData 113289
            switch (target)
            {
                case SqlDbType.BigInt: return s_metaBigInt;
                case SqlDbType.Binary: return s_metaBinary;
                case SqlDbType.Bit: return s_metaBit;
                case SqlDbType.Char: return s_metaChar;
                case SqlDbType.DateTime: return s_metaDateTime;
                case SqlDbType.Decimal: return MetaDecimal;
                case SqlDbType.Float: return s_metaFloat;
                case SqlDbType.Image: return MetaImage;
                case SqlDbType.Int: return s_metaInt;
                case SqlDbType.Money: return s_metaMoney;
                case SqlDbType.NChar: return s_metaNChar;
                case SqlDbType.NText: return MetaNText;
                case SqlDbType.NVarChar: return MetaNVarChar;
                case SqlDbType.Real: return s_metaReal;
                case SqlDbType.UniqueIdentifier: return s_metaUniqueId;
                case SqlDbType.SmallDateTime: return s_metaSmallDateTime;
                case SqlDbType.SmallInt: return s_metaSmallInt;
                case SqlDbType.SmallMoney: return s_metaSmallMoney;
                case SqlDbType.Text: return MetaText;
                case SqlDbType.Timestamp: return s_metaTimestamp;
                case SqlDbType.TinyInt: return s_metaTinyInt;
                case SqlDbType.VarBinary: return MetaVarBinary;
                case SqlDbType.VarChar: return s_metaVarChar;
                case SqlDbType.Variant: return s_metaVariant;
                case (SqlDbType)TdsEnums.SmallVarBinary: return s_metaSmallVarBinary;
                case SqlDbType.Xml: return MetaXml;
                case SqlDbType.Udt: return MetaUdt;
                case SqlDbType.Structured:
                    if (isMultiValued)
                    {
                        return s_metaTable;
                    }
                    else
                    {
                        return s_metaSUDT;
                    }
                case SqlDbType.Date: return s_metaDate;
                case SqlDbType.Time: return MetaTime;
                case SqlDbType.DateTime2: return s_metaDateTime2;
                case SqlDbType.DateTimeOffset: return MetaDateTimeOffset;
                default: throw SQL.InvalidSqlDbType(target);
            }
        }

        //
        // map DbType to MetaType class
        //
        internal static MetaType GetMetaTypeFromDbType(DbType target)
        {
            // if we can't map it, we need to throw
            switch (target)
            {
                case DbType.AnsiString: return s_metaVarChar;
                case DbType.AnsiStringFixedLength: return s_metaChar;
                case DbType.Binary: return MetaVarBinary;
                case DbType.Byte: return s_metaTinyInt;
                case DbType.Boolean: return s_metaBit;
                case DbType.Currency: return s_metaMoney;
                case DbType.Date:
                case DbType.DateTime: return s_metaDateTime;
                case DbType.Decimal: return MetaDecimal;
                case DbType.Double: return s_metaFloat;
                case DbType.Guid: return s_metaUniqueId;
                case DbType.Int16: return s_metaSmallInt;
                case DbType.Int32: return s_metaInt;
                case DbType.Int64: return s_metaBigInt;
                case DbType.Object: return s_metaVariant;
                case DbType.Single: return s_metaReal;
                case DbType.String: return MetaNVarChar;
                case DbType.StringFixedLength: return s_metaNChar;
                case DbType.Time: return s_metaDateTime;
                case DbType.Xml: return MetaXml;
                case DbType.DateTime2: return s_metaDateTime2;
                case DbType.DateTimeOffset: return MetaDateTimeOffset;
                case DbType.SByte:                  // unsupported
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                case DbType.VarNumeric:
                default: throw ADP.DbTypeNotSupported(target, typeof(SqlDbType)); // no direct mapping, error out
            }
        }

        internal static MetaType GetMaxMetaTypeFromMetaType(MetaType mt)
        {
            // if we can't map it, we need to throw
            switch (mt.SqlDbType)
            {
                case SqlDbType.VarBinary:
                case SqlDbType.Binary:
                    return MetaMaxVarBinary;
                case SqlDbType.VarChar:
                case SqlDbType.Char:
                    return MetaMaxVarChar;
                case SqlDbType.NVarChar:
                case SqlDbType.NChar:
                    return MetaMaxNVarChar;
                case SqlDbType.Udt:
                    return s_metaMaxUdt;
                default:
                    return mt;
            }
        }

        //
        // map COM+ Type to MetaType class
        //
        internal static MetaType GetMetaTypeFromType(Type dataType, bool streamAllowed = true)
        {
            if (dataType == typeof(System.Byte[]))
                return MetaVarBinary;
            else if (dataType == typeof(System.Guid))
                return s_metaUniqueId;
            else if (dataType == typeof(System.Object))
                return s_metaVariant;
            else if (dataType == typeof(SqlBinary))
                return MetaVarBinary;
            else if (dataType == typeof(SqlBoolean))
                return s_metaBit;
            else if (dataType == typeof(SqlByte))
                return s_metaTinyInt;
            else if (dataType == typeof(SqlBytes))
                return MetaVarBinary;
            else if (dataType == typeof(SqlChars))
                return MetaNVarChar;
            else if (dataType == typeof(SqlDateTime))
                return s_metaDateTime;
            else if (dataType == typeof(SqlDouble))
                return s_metaFloat;
            else if (dataType == typeof(SqlGuid))
                return s_metaUniqueId;
            else if (dataType == typeof(SqlInt16))
                return s_metaSmallInt;
            else if (dataType == typeof(SqlInt32))
                return s_metaInt;
            else if (dataType == typeof(SqlInt64))
                return s_metaBigInt;
            else if (dataType == typeof(SqlMoney))
                return s_metaMoney;
            else if (dataType == typeof(SqlDecimal))
                return MetaDecimal;
            else if (dataType == typeof(SqlSingle))
                return s_metaReal;
            else if (dataType == typeof(SqlXml))
                return MetaXml;
            else if (dataType == typeof(SqlString))
                return MetaNVarChar;
            else if (dataType == typeof(IEnumerable<DbDataRecord>))
                return s_metaTable;
            else if (dataType == typeof(TimeSpan))
                return MetaTime;
            else if (dataType == typeof(DateTimeOffset))
                return MetaDateTimeOffset;
            else if (dataType == typeof(DBNull))
                throw ADP.InvalidDataType(nameof(DBNull));
            else if (dataType == typeof(Boolean))
                return s_metaBit;
            else if (dataType == typeof(Char))
                throw ADP.InvalidDataType(nameof(Char));
            else if (dataType == typeof(SByte))
                throw ADP.InvalidDataType(nameof(SByte));
            else if (dataType == typeof(Byte))
                return s_metaTinyInt;
            else if (dataType == typeof(Int16))
                return s_metaSmallInt;
            else if (dataType == typeof(UInt16))
                throw ADP.InvalidDataType(nameof(UInt16));
            else if (dataType == typeof(Int32))
                return s_metaInt;
            else if (dataType == typeof(UInt32))
                throw ADP.InvalidDataType(nameof(UInt32));
            else if (dataType == typeof(Int64))
                return s_metaBigInt;
            else if (dataType == typeof(UInt64))
                throw ADP.InvalidDataType(nameof(UInt64));
            else if (dataType == typeof(Single))
                return s_metaReal;
            else if (dataType == typeof(Double))
                return s_metaFloat;
            else if (dataType == typeof(Decimal))
                return MetaDecimal;
            else if (dataType == typeof(DateTime))
                return s_metaDateTime;
            else if (dataType == typeof(String))
                return MetaNVarChar;
            else
                throw ADP.UnknownDataType(dataType);
        }

        internal static MetaType GetMetaTypeFromValue(object value, bool inferLen = true, bool streamAllowed = true)
        {
            if (value == null)
            {
                throw ADP.InvalidDataType("null");
            }

            if (value is DBNull)
            {
                throw ADP.InvalidDataType(nameof(DBNull));
            }
            
            Type dataType = value.GetType();
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Empty:
                    throw ADP.InvalidDataType(nameof(TypeCode.Empty));
                case TypeCode.Object:
                    
                    if (dataType == typeof (System.Byte[]))
                    {
                        if (!inferLen || ((byte[]) value).Length <= TdsEnums.TYPE_SIZE_LIMIT)
                        {
                            return MetaVarBinary;
                        }
                        else
                        {
                            return MetaImage;
                        }
                    }
                    if (dataType == typeof (System.Guid))
                    {
                        return s_metaUniqueId;
                    }
                    if (dataType == typeof (System.Object))
                    {
                        return s_metaVariant;
                    } 
                    // check sql types now
                    if (dataType == typeof (SqlBinary))
                        return MetaVarBinary;
                    if (dataType == typeof (SqlBoolean))
                        return s_metaBit;
                    if (dataType == typeof (SqlByte))
                        return s_metaTinyInt;
                    if (dataType == typeof (SqlBytes))
                        return MetaVarBinary;
                    if (dataType == typeof (SqlChars))
                        return MetaNVarChar;
                    if (dataType == typeof (SqlDateTime))
                        return s_metaDateTime;
                    if (dataType == typeof (SqlDouble))
                        return s_metaFloat;
                    if (dataType == typeof (SqlGuid))
                        return s_metaUniqueId;
                    if (dataType == typeof (SqlInt16))
                        return s_metaSmallInt;
                    if (dataType == typeof (SqlInt32))
                        return s_metaInt;
                    if (dataType == typeof (SqlInt64))
                        return s_metaBigInt;
                    if (dataType == typeof (SqlMoney))
                        return s_metaMoney;
                    if (dataType == typeof (SqlDecimal))
                        return MetaDecimal;
                    if (dataType == typeof (SqlSingle))
                        return s_metaReal;
                    if (dataType == typeof (SqlXml))
                        return MetaXml;
                    if (dataType == typeof (SqlString))
                    {
                        return ((inferLen && !((SqlString) value).IsNull)
                            ? PromoteStringType(((SqlString) value).Value)
                            : MetaNVarChar);
                    }

                    if (dataType == typeof (IEnumerable<DbDataRecord>) || dataType == typeof (DataTable))
                    {
                        return s_metaTable;
                    }

                    if (dataType == typeof (TimeSpan))
                    {
                        return MetaTime;
                    }

                    if (dataType == typeof (DateTimeOffset))
                    {
                        return MetaDateTimeOffset;
                    }
                    
                    if (streamAllowed)
                    {
                        // Derived from Stream ?
                        if (value is Stream)
                        {
                            return MetaVarBinary;
                        }
                        // Derived from TextReader ?
                        if (value is TextReader)
                        {
                            return MetaNVarChar;
                        }
                        // Derived from XmlReader ? 
                        if (value is XmlReader)
                        {
                            return MetaXml;
                        }
                    }
                    
                     throw ADP.UnknownDataType(dataType);                    
                case TypeCode.Boolean:
                    return s_metaBit;
                case TypeCode.Char:
                    throw ADP.InvalidDataType(nameof(TypeCode.Char));
                case TypeCode.SByte:
                    throw ADP.InvalidDataType(nameof(TypeCode.SByte));
                case TypeCode.Byte:
                    return s_metaTinyInt;
                case TypeCode.Int16:
                    return s_metaSmallInt;
                case TypeCode.UInt16:
                    throw ADP.InvalidDataType(nameof(TypeCode.UInt16));
                case TypeCode.Int32:
                    return s_metaInt;
                case TypeCode.UInt32:
                    throw ADP.InvalidDataType(nameof(TypeCode.UInt32));
                case TypeCode.Int64:
                    return s_metaBigInt;
                case TypeCode.UInt64:
                    throw ADP.InvalidDataType(nameof(TypeCode.UInt64));
                case TypeCode.Single:
                    return s_metaReal;
                case TypeCode.Double:
                    return s_metaFloat;
                case TypeCode.Decimal:
                    return MetaDecimal;
                case TypeCode.DateTime:
                    return s_metaDateTime;
                case TypeCode.String:
                    return (inferLen ? PromoteStringType((string) value) : MetaNVarChar);
                default:
                    throw ADP.UnknownDataType(dataType);
            }
        }

        internal static object GetNullSqlValue(Type sqlType)
        {
            if (sqlType == typeof(SqlSingle)) return SqlSingle.Null;
            else if (sqlType == typeof(SqlString)) return SqlString.Null;
            else if (sqlType == typeof(SqlDouble)) return SqlDouble.Null;
            else if (sqlType == typeof(SqlBinary)) return SqlBinary.Null;
            else if (sqlType == typeof(SqlGuid)) return SqlGuid.Null;
            else if (sqlType == typeof(SqlBoolean)) return SqlBoolean.Null;
            else if (sqlType == typeof(SqlByte)) return SqlByte.Null;
            else if (sqlType == typeof(SqlInt16)) return SqlInt16.Null;
            else if (sqlType == typeof(SqlInt32)) return SqlInt32.Null;
            else if (sqlType == typeof(SqlInt64)) return SqlInt64.Null;
            else if (sqlType == typeof(SqlDecimal)) return SqlDecimal.Null;
            else if (sqlType == typeof(SqlDateTime)) return SqlDateTime.Null;
            else if (sqlType == typeof(SqlMoney)) return SqlMoney.Null;
            else if (sqlType == typeof(SqlXml)) return SqlXml.Null;
            else if (sqlType == typeof(object)) return DBNull.Value;
            else if (sqlType == typeof(IEnumerable<DbDataRecord>)) return DBNull.Value;
            else if (sqlType == typeof(DataTable)) return DBNull.Value;
            else if (sqlType == typeof(DateTime)) return DBNull.Value;
            else if (sqlType == typeof(TimeSpan)) return DBNull.Value;
            else if (sqlType == typeof(DateTimeOffset)) return DBNull.Value;
            else
            {
                Debug.Assert(false, "Unknown SqlType!");
                return DBNull.Value;
            }
        }

        internal static MetaType PromoteStringType(string s)
        {
            int len = s.Length;

            if ((len << 1) > TdsEnums.TYPE_SIZE_LIMIT)
            {
                return s_metaVarChar; // try as var char since we can send a 8K characters
            }
            return MetaNVarChar; // send 4k chars, but send as unicode
        }

        internal static object GetComValueFromSqlVariant(object sqlVal)
        {
            object comVal = null;

            if (ADP.IsNull(sqlVal))
                return comVal;

            if (sqlVal is SqlSingle)
                comVal = ((SqlSingle)sqlVal).Value;
            else if (sqlVal is SqlString)
                comVal = ((SqlString)sqlVal).Value;
            else if (sqlVal is SqlDouble)
                comVal = ((SqlDouble)sqlVal).Value;
            else if (sqlVal is SqlBinary)
                comVal = ((SqlBinary)sqlVal).Value;
            else if (sqlVal is SqlGuid)
                comVal = ((SqlGuid)sqlVal).Value;
            else if (sqlVal is SqlBoolean)
                comVal = ((SqlBoolean)sqlVal).Value;
            else if (sqlVal is SqlByte)
                comVal = ((SqlByte)sqlVal).Value;
            else if (sqlVal is SqlInt16)
                comVal = ((SqlInt16)sqlVal).Value;
            else if (sqlVal is SqlInt32)
                comVal = ((SqlInt32)sqlVal).Value;
            else if (sqlVal is SqlInt64)
                comVal = ((SqlInt64)sqlVal).Value;
            else if (sqlVal is SqlDecimal)
                comVal = ((SqlDecimal)sqlVal).Value;
            else if (sqlVal is SqlDateTime)
                comVal = ((SqlDateTime)sqlVal).Value;
            else if (sqlVal is SqlMoney)
                comVal = ((SqlMoney)sqlVal).Value;
            else if (sqlVal is SqlXml)
                comVal = ((SqlXml)sqlVal).Value;
            else
            {
                Debug.Assert(false, "unknown SqlType class stored in sqlVal");
            }


            return comVal;
        }


        // devnote: This method should not be used with SqlDbType.Date and SqlDbType.DateTime2. 
        //          With these types the values should be used directly as CLR types instead of being converted to a SqlValue
        internal static object GetSqlValueFromComVariant(object comVal)
        {
            object sqlVal = null;
            if ((null != comVal) && (DBNull.Value != comVal))
            {
                if (comVal is float)
                    sqlVal = new SqlSingle((float)comVal);
                else if (comVal is string)
                    sqlVal = new SqlString((string)comVal);
                else if (comVal is double)
                    sqlVal = new SqlDouble((double)comVal);
                else if (comVal is System.Byte[])
                    sqlVal = new SqlBinary((byte[])comVal);
                else if (comVal is System.Char)
                    sqlVal = new SqlString(((char)comVal).ToString());
                else if (comVal is System.Char[])
                    sqlVal = new SqlChars((System.Char[])comVal);
                else if (comVal is System.Guid)
                    sqlVal = new SqlGuid((Guid)comVal);
                else if (comVal is bool)
                    sqlVal = new SqlBoolean((bool)comVal);
                else if (comVal is byte)
                    sqlVal = new SqlByte((byte)comVal);
                else if (comVal is Int16)
                    sqlVal = new SqlInt16((Int16)comVal);
                else if (comVal is Int32)
                    sqlVal = new SqlInt32((Int32)comVal);
                else if (comVal is Int64)
                    sqlVal = new SqlInt64((Int64)comVal);
                else if (comVal is Decimal)
                    sqlVal = new SqlDecimal((Decimal)comVal);
                else if (comVal is DateTime)
                {
                    // devnote: Do not use with SqlDbType.Date and SqlDbType.DateTime2. See comment at top of method.
                    sqlVal = new SqlDateTime((DateTime)comVal);
                }
                else if (comVal is XmlReader)
                    sqlVal = new SqlXml((XmlReader)comVal);
                else if (comVal is TimeSpan || comVal is DateTimeOffset)
                    sqlVal = comVal;
#if DEBUG
                else
                    Debug.Assert(false, "unknown SqlType class stored in sqlVal");
#endif
            }
            return sqlVal;
        }

        internal static SqlDbType GetSqlDbTypeFromOleDbType(short dbType, string typeName)
        {
            // OleDbTypes not supported
            return SqlDbType.Variant;
        }

        internal static MetaType GetSqlDataType(int tdsType, UInt32 userType, int length)
        {
            switch (tdsType)
            {
                case TdsEnums.SQLMONEYN: return ((4 == length) ? s_metaSmallMoney : s_metaMoney);
                case TdsEnums.SQLDATETIMN: return ((4 == length) ? s_metaSmallDateTime : s_metaDateTime);
                case TdsEnums.SQLINTN: return ((4 <= length) ? ((4 == length) ? s_metaInt : s_metaBigInt) : ((2 == length) ? s_metaSmallInt : s_metaTinyInt));
                case TdsEnums.SQLFLTN: return ((4 == length) ? s_metaReal : s_metaFloat);
                case TdsEnums.SQLTEXT: return MetaText;
                case TdsEnums.SQLVARBINARY: return s_metaSmallVarBinary;
                case TdsEnums.SQLBIGVARBINARY: return MetaVarBinary;

                case TdsEnums.SQLVARCHAR:           //goto TdsEnums.SQLBIGVARCHAR;
                case TdsEnums.SQLBIGVARCHAR: return s_metaVarChar;

                case TdsEnums.SQLBINARY:            //goto TdsEnums.SQLBIGBINARY;
                case TdsEnums.SQLBIGBINARY: return ((TdsEnums.SQLTIMESTAMP == userType) ? s_metaTimestamp : s_metaBinary);

                case TdsEnums.SQLIMAGE: return MetaImage;

                case TdsEnums.SQLCHAR:              //goto TdsEnums.SQLBIGCHAR;
                case TdsEnums.SQLBIGCHAR: return s_metaChar;

                case TdsEnums.SQLINT1: return s_metaTinyInt;

                case TdsEnums.SQLBIT:               //goto TdsEnums.SQLBITN;
                case TdsEnums.SQLBITN: return s_metaBit;

                case TdsEnums.SQLINT2: return s_metaSmallInt;
                case TdsEnums.SQLINT4: return s_metaInt;
                case TdsEnums.SQLINT8: return s_metaBigInt;
                case TdsEnums.SQLMONEY: return s_metaMoney;
                case TdsEnums.SQLDATETIME: return s_metaDateTime;
                case TdsEnums.SQLFLT8: return s_metaFloat;
                case TdsEnums.SQLFLT4: return s_metaReal;
                case TdsEnums.SQLMONEY4: return s_metaSmallMoney;
                case TdsEnums.SQLDATETIM4: return s_metaSmallDateTime;

                case TdsEnums.SQLDECIMALN:          //goto TdsEnums.SQLNUMERICN;
                case TdsEnums.SQLNUMERICN: return MetaDecimal;

                case TdsEnums.SQLUNIQUEID: return s_metaUniqueId;
                case TdsEnums.SQLNCHAR: return s_metaNChar;
                case TdsEnums.SQLNVARCHAR: return MetaNVarChar;
                case TdsEnums.SQLNTEXT: return MetaNText;
                case TdsEnums.SQLVARIANT: return s_metaVariant;
                case TdsEnums.SQLUDT: return MetaUdt;
                case TdsEnums.SQLXMLTYPE: return MetaXml;
                case TdsEnums.SQLTABLE: return s_metaTable;
                case TdsEnums.SQLDATE: return s_metaDate;
                case TdsEnums.SQLTIME: return MetaTime;
                case TdsEnums.SQLDATETIME2: return s_metaDateTime2;
                case TdsEnums.SQLDATETIMEOFFSET: return MetaDateTimeOffset;

                case TdsEnums.SQLVOID:
                default:
                    Debug.Assert(false, "Unknown type " + tdsType.ToString(CultureInfo.InvariantCulture));
                    throw SQL.InvalidSqlDbType((SqlDbType)tdsType);
            }// case
        }

        internal static MetaType GetDefaultMetaType()
        {
            return MetaNVarChar;
        }

        // Converts an XmlReader into String
        internal static String GetStringFromXml(XmlReader xmlreader)
        {
            SqlXml sxml = new SqlXml(xmlreader);
            return sxml.Value;
        }

        private static readonly MetaType s_metaBigInt = new MetaType
            (19, 255, 8, true, false, false, TdsEnums.SQLINT8, TdsEnums.SQLINTN, MetaTypeName.BIGINT, typeof(System.Int64), typeof(SqlInt64), SqlDbType.BigInt, DbType.Int64, 0);

        private static readonly MetaType s_metaFloat = new MetaType
            (15, 255, 8, true, false, false, TdsEnums.SQLFLT8, TdsEnums.SQLFLTN, MetaTypeName.FLOAT, typeof(System.Double), typeof(SqlDouble), SqlDbType.Float, DbType.Double, 0);

        private static readonly MetaType s_metaReal = new MetaType
            (7, 255, 4, true, false, false, TdsEnums.SQLFLT4, TdsEnums.SQLFLTN, MetaTypeName.REAL, typeof(System.Single), typeof(SqlSingle), SqlDbType.Real, DbType.Single, 0);

        // MetaBinary has two bytes of properties for binary and varbinary
        // 2 byte maxlen
        private static readonly MetaType s_metaBinary = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGBINARY, TdsEnums.SQLBIGBINARY, MetaTypeName.BINARY, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.Binary, DbType.Binary, 2);

        // Syntactic sugar for the user...timestamps are 8-byte fixed length binary columns
        private static readonly MetaType s_metaTimestamp = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGBINARY, TdsEnums.SQLBIGBINARY, MetaTypeName.TIMESTAMP, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.Timestamp, DbType.Binary, 2);

        internal static readonly MetaType MetaVarBinary = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGVARBINARY, TdsEnums.SQLBIGVARBINARY, MetaTypeName.VARBINARY, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.VarBinary, DbType.Binary, 2);

        internal static readonly MetaType MetaMaxVarBinary = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLBIGVARBINARY, TdsEnums.SQLBIGVARBINARY, MetaTypeName.VARBINARY, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.VarBinary, DbType.Binary, 2);

        // HACK!!!  We have an internal type for smallvarbinarys stored on TdsEnums.  We
        // store on TdsEnums instead of SqlDbType because we do not want to expose
        // this type to the user!
        private static readonly MetaType s_metaSmallVarBinary = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLVARBINARY, TdsEnums.SQLBIGBINARY, ADP.StrEmpty, typeof(System.Byte[]), typeof(SqlBinary), TdsEnums.SmallVarBinary, DbType.Binary, 2);

        internal static readonly MetaType MetaImage = new MetaType
            (255, 255, -1, false, true, false, TdsEnums.SQLIMAGE, TdsEnums.SQLIMAGE, MetaTypeName.IMAGE, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.Image, DbType.Binary, 0);

        private static readonly MetaType s_metaBit = new MetaType
            (255, 255, 1, true, false, false, TdsEnums.SQLBIT, TdsEnums.SQLBITN, MetaTypeName.BIT, typeof(System.Boolean), typeof(SqlBoolean), SqlDbType.Bit, DbType.Boolean, 0);

        private static readonly MetaType s_metaTinyInt = new MetaType
            (3, 255, 1, true, false, false, TdsEnums.SQLINT1, TdsEnums.SQLINTN, MetaTypeName.TINYINT, typeof(System.Byte), typeof(SqlByte), SqlDbType.TinyInt, DbType.Byte, 0);

        private static readonly MetaType s_metaSmallInt = new MetaType
            (5, 255, 2, true, false, false, TdsEnums.SQLINT2, TdsEnums.SQLINTN, MetaTypeName.SMALLINT, typeof(System.Int16), typeof(SqlInt16), SqlDbType.SmallInt, DbType.Int16, 0);

        private static readonly MetaType s_metaInt = new MetaType
            (10, 255, 4, true, false, false, TdsEnums.SQLINT4, TdsEnums.SQLINTN, MetaTypeName.INT, typeof(System.Int32), typeof(SqlInt32), SqlDbType.Int, DbType.Int32, 0);

        // MetaVariant has seven bytes of properties for MetaChar and MetaVarChar
        // 5 byte tds collation
        // 2 byte maxlen
        private static readonly MetaType s_metaChar = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGCHAR, TdsEnums.SQLBIGCHAR, MetaTypeName.CHAR, typeof(System.String), typeof(SqlString), SqlDbType.Char, DbType.AnsiStringFixedLength, 7);

        private static readonly MetaType s_metaVarChar = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGVARCHAR, TdsEnums.SQLBIGVARCHAR, MetaTypeName.VARCHAR, typeof(System.String), typeof(SqlString), SqlDbType.VarChar, DbType.AnsiString, 7);

        internal static readonly MetaType MetaMaxVarChar = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLBIGVARCHAR, TdsEnums.SQLBIGVARCHAR, MetaTypeName.VARCHAR, typeof(System.String), typeof(SqlString), SqlDbType.VarChar, DbType.AnsiString, 7);

        internal static readonly MetaType MetaText = new MetaType
            (255, 255, -1, false, true, false, TdsEnums.SQLTEXT, TdsEnums.SQLTEXT, MetaTypeName.TEXT, typeof(System.String), typeof(SqlString), SqlDbType.Text, DbType.AnsiString, 0);

        // MetaVariant has seven bytes of properties for MetaNChar and MetaNVarChar
        // 5 byte tds collation
        // 2 byte maxlen
        private static readonly MetaType s_metaNChar = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLNCHAR, TdsEnums.SQLNCHAR, MetaTypeName.NCHAR, typeof(System.String), typeof(SqlString), SqlDbType.NChar, DbType.StringFixedLength, 7);

        internal static readonly MetaType MetaNVarChar = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLNVARCHAR, TdsEnums.SQLNVARCHAR, MetaTypeName.NVARCHAR, typeof(System.String), typeof(SqlString), SqlDbType.NVarChar, DbType.String, 7);

        internal static readonly MetaType MetaMaxNVarChar = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLNVARCHAR, TdsEnums.SQLNVARCHAR, MetaTypeName.NVARCHAR, typeof(System.String), typeof(SqlString), SqlDbType.NVarChar, DbType.String, 7);

        internal static readonly MetaType MetaNText = new MetaType
            (255, 255, -1, false, true, false, TdsEnums.SQLNTEXT, TdsEnums.SQLNTEXT, MetaTypeName.NTEXT, typeof(System.String), typeof(SqlString), SqlDbType.NText, DbType.String, 7);

        // MetaVariant has two bytes of properties for numeric/decimal types
        // 1 byte precision
        // 1 byte scale
        internal static readonly MetaType MetaDecimal = new MetaType
            (38, 4, 17, true, false, false, TdsEnums.SQLNUMERICN, TdsEnums.SQLNUMERICN, MetaTypeName.DECIMAL, typeof(System.Decimal), typeof(SqlDecimal), SqlDbType.Decimal, DbType.Decimal, 2);

        internal static readonly MetaType MetaXml = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLXMLTYPE, TdsEnums.SQLXMLTYPE, MetaTypeName.XML, typeof(System.String), typeof(SqlXml), SqlDbType.Xml, DbType.Xml, 0);

        private static readonly MetaType s_metaDateTime = new MetaType
            (23, 3, 8, true, false, false, TdsEnums.SQLDATETIME, TdsEnums.SQLDATETIMN, MetaTypeName.DATETIME, typeof(System.DateTime), typeof(SqlDateTime), SqlDbType.DateTime, DbType.DateTime, 0);

        private static readonly MetaType s_metaSmallDateTime = new MetaType
            (16, 0, 4, true, false, false, TdsEnums.SQLDATETIM4, TdsEnums.SQLDATETIMN, MetaTypeName.SMALLDATETIME, typeof(System.DateTime), typeof(SqlDateTime), SqlDbType.SmallDateTime, DbType.DateTime, 0);

        private static readonly MetaType s_metaMoney = new MetaType
            (19, 255, 8, true, false, false, TdsEnums.SQLMONEY, TdsEnums.SQLMONEYN, MetaTypeName.MONEY, typeof(System.Decimal), typeof(SqlMoney), SqlDbType.Money, DbType.Currency, 0);

        private static readonly MetaType s_metaSmallMoney = new MetaType
            (10, 255, 4, true, false, false, TdsEnums.SQLMONEY4, TdsEnums.SQLMONEYN, MetaTypeName.SMALLMONEY, typeof(System.Decimal), typeof(SqlMoney), SqlDbType.SmallMoney, DbType.Currency, 0);

        private static readonly MetaType s_metaUniqueId = new MetaType
            (255, 255, 16, true, false, false, TdsEnums.SQLUNIQUEID, TdsEnums.SQLUNIQUEID, MetaTypeName.ROWGUID, typeof(System.Guid), typeof(SqlGuid), SqlDbType.UniqueIdentifier, DbType.Guid, 0);

        private static readonly MetaType s_metaVariant = new MetaType
            (255, 255, -1, true, false, false, TdsEnums.SQLVARIANT, TdsEnums.SQLVARIANT, MetaTypeName.VARIANT, typeof(System.Object), typeof(System.Object), SqlDbType.Variant, DbType.Object, 0);

        internal static readonly MetaType MetaUdt = new MetaType
           (255, 255, -1, false, false, true, TdsEnums.SQLUDT, TdsEnums.SQLUDT, MetaTypeName.UDT, typeof(System.Object), typeof(System.Object), SqlDbType.Udt, DbType.Object, 0);

        private static readonly MetaType s_metaMaxUdt = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLUDT, TdsEnums.SQLUDT, MetaTypeName.UDT, typeof(System.Object), typeof(System.Object), SqlDbType.Udt, DbType.Object, 0);

        private static readonly MetaType s_metaTable = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLTABLE, TdsEnums.SQLTABLE, MetaTypeName.TABLE, typeof(IEnumerable<DbDataRecord>), typeof(IEnumerable<DbDataRecord>), SqlDbType.Structured, DbType.Object, 0);

        private static readonly MetaType s_metaSUDT = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLVOID, TdsEnums.SQLVOID, "", typeof(MSS.SqlDataRecord), typeof(MSS.SqlDataRecord), SqlDbType.Structured, DbType.Object, 0);

        private static readonly MetaType s_metaDate = new MetaType
            (255, 255, 3, true, false, false, TdsEnums.SQLDATE, TdsEnums.SQLDATE, MetaTypeName.DATE, typeof(System.DateTime), typeof(System.DateTime), SqlDbType.Date, DbType.Date, 0);

        internal static readonly MetaType MetaTime = new MetaType
            (255, 7, -1, false, false, false, TdsEnums.SQLTIME, TdsEnums.SQLTIME, MetaTypeName.TIME, typeof(System.TimeSpan), typeof(System.TimeSpan), SqlDbType.Time, DbType.Time, 1);

        private static readonly MetaType s_metaDateTime2 = new MetaType
            (255, 7, -1, false, false, false, TdsEnums.SQLDATETIME2, TdsEnums.SQLDATETIME2, MetaTypeName.DATETIME2, typeof(System.DateTime), typeof(System.DateTime), SqlDbType.DateTime2, DbType.DateTime2, 1);

        internal static readonly MetaType MetaDateTimeOffset = new MetaType
            (255, 7, -1, false, false, false, TdsEnums.SQLDATETIMEOFFSET, TdsEnums.SQLDATETIMEOFFSET, MetaTypeName.DATETIMEOFFSET, typeof(System.DateTimeOffset), typeof(System.DateTimeOffset), SqlDbType.DateTimeOffset, DbType.DateTimeOffset, 1);

        public static TdsDateTime FromDateTime(DateTime dateTime, byte cb)
        {
            SqlDateTime sqlDateTime;
            TdsDateTime tdsDateTime = new TdsDateTime();

            Debug.Assert(cb == 8 || cb == 4, "Invalid date time size!");

            if (cb == 8)
            {
                sqlDateTime = new SqlDateTime(dateTime);
                tdsDateTime.time = sqlDateTime.TimeTicks;
            }
            else
            {
                // note that smalldatetime is days & minutes.
                // Adding 30 seconds ensures proper roundup if the seconds are >= 30
                // The AddSeconds function handles eventual carryover
                sqlDateTime = new SqlDateTime(dateTime.AddSeconds(30));
                tdsDateTime.time = sqlDateTime.TimeTicks / SqlDateTime.SQLTicksPerMinute;
            }
            tdsDateTime.days = sqlDateTime.DayTicks;
            return tdsDateTime;
        }


        public static DateTime ToDateTime(int sqlDays, int sqlTime, int length)
        {
            if (length == 4)
            {
                return new SqlDateTime(sqlDays, sqlTime * SqlDateTime.SQLTicksPerMinute).Value;
            }
            else
            {
                Debug.Assert(length == 8, "invalid length for DateTime");
                return new SqlDateTime(sqlDays, sqlTime).Value;
            }
        }

        internal static int GetTimeSizeFromScale(byte scale)
        {
            if (scale <= 2)
                return 3;

            if (scale <= 4)
                return 4;

            return 5;
        }

        //
        // please leave string sorted alphabetically
        // note that these names should only be used in the context of parameters.  We always send over BIG* and nullable types for SQL Server
        //
        private static class MetaTypeName
        {
            public const string BIGINT = "bigint";
            public const string BINARY = "binary";
            public const string BIT = "bit";
            public const string CHAR = "char";
            public const string DATETIME = "datetime";
            public const string DECIMAL = "decimal";
            public const string FLOAT = "float";
            public const string IMAGE = "image";
            public const string INT = "int";
            public const string MONEY = "money";
            public const string NCHAR = "nchar";
            public const string NTEXT = "ntext";
            public const string NVARCHAR = "nvarchar";
            public const string REAL = "real";
            public const string ROWGUID = "uniqueidentifier";
            public const string SMALLDATETIME = "smalldatetime";
            public const string SMALLINT = "smallint";
            public const string SMALLMONEY = "smallmoney";
            public const string TEXT = "text";
            public const string TIMESTAMP = "timestamp";
            public const string TINYINT = "tinyint";
            public const string UDT = "udt";
            public const string VARBINARY = "varbinary";
            public const string VARCHAR = "varchar";
            public const string VARIANT = "sql_variant";
            public const string XML = "xml";
            public const string TABLE = "table";
            public const string DATE = "date";
            public const string TIME = "time";
            public const string DATETIME2 = "datetime2";
            public const string DATETIMEOFFSET = "datetimeoffset";
        }
    }

    //
    // note: it is the client's responsibility to know what size date time he is working with
    //
    internal struct TdsDateTime
    {
        public int days;  // offset in days from 1/1/1900
        //     private UInt32 time;  // if smalldatetime, this is # of minutes since midnight
        // otherwise: # of 1/300th of a second since midnight
        public int time;
    }
}

