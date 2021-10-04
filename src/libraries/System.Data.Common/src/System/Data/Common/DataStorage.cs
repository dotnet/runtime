// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common
{
    internal enum StorageType
    {
        Empty = TypeCode.Empty, // 0
        Object = TypeCode.Object,
        DBNull = TypeCode.DBNull,
        Boolean = TypeCode.Boolean,
        Char = TypeCode.Char,
        SByte = TypeCode.SByte,
        Byte = TypeCode.Byte,
        Int16 = TypeCode.Int16,
        UInt16 = TypeCode.UInt16,
        Int32 = TypeCode.Int32,
        UInt32 = TypeCode.UInt32,
        Int64 = TypeCode.Int64,
        UInt64 = TypeCode.UInt64,
        Single = TypeCode.Single,
        Double = TypeCode.Double,
        Decimal = TypeCode.Decimal, // 15
        DateTime = TypeCode.DateTime, // 16
        TimeSpan = 17,
        String = TypeCode.String, // 18
        Guid = 19,

        ByteArray = 20,
        CharArray = 21,
        Type = 22,
        DateTimeOffset = 23,
        BigInteger = 24,
        Uri = 25,

        SqlBinary, // SqlTypes should remain at the end for IsSqlType checking
        SqlBoolean,
        SqlByte,
        SqlBytes,
        SqlChars,
        SqlDateTime,
        SqlDecimal,
        SqlDouble,
        SqlGuid,
        SqlInt16,
        SqlInt32,
        SqlInt64,
        SqlMoney,
        SqlSingle,
        SqlString,
        //        SqlXml,
    };

    internal abstract class DataStorage
    {
        // for Whidbey 40426, searching down the Type[] is about 20% faster than using a Dictionary
        // must keep in same order as enum StorageType
        private static readonly Type?[] s_storageClassType = new Type?[] {
            null,
            typeof(object),
            typeof(DBNull),
            typeof(bool),
            typeof(char),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(string),
            typeof(Guid),

            typeof(byte[]),
            typeof(char[]),
            typeof(Type),
            typeof(DateTimeOffset),
            typeof(System.Numerics.BigInteger),
            typeof(Uri),

            typeof(SqlBinary),
            typeof(SqlBoolean),
            typeof(SqlByte),
            typeof(SqlBytes),
            typeof(SqlChars),
            typeof(SqlDateTime),
            typeof(SqlDecimal),
            typeof(SqlDouble),
            typeof(SqlGuid),
            typeof(SqlInt16),
            typeof(SqlInt32),
            typeof(SqlInt64),
            typeof(SqlMoney),
            typeof(SqlSingle),
            typeof(SqlString),
//            typeof(SqlXml),
        };

        internal readonly DataColumn _column;
        internal readonly DataTable _table;
        internal readonly Type _dataType;
        internal readonly StorageType _storageTypeCode;
        private System.Collections.BitArray _dbNullBits = default!;  // Late-initialized

        private readonly object? _defaultValue;
        internal readonly object _nullValue;

        internal readonly bool _isCloneable;
        internal readonly bool _isCustomDefinedType;
        internal readonly bool _isStringType;
        internal readonly bool _isValueType;

        private static readonly Func<Type, Tuple<bool, bool, bool, bool>> s_inspectTypeForInterfaces = InspectTypeForInterfaces;
        private static readonly ConcurrentDictionary<Type, Tuple<bool, bool, bool, bool>> s_typeImplementsInterface = new ConcurrentDictionary<Type, Tuple<bool, bool, bool, bool>>();

        protected DataStorage(DataColumn column, Type type, object? defaultValue, StorageType storageType)
            : this(column, type, defaultValue, DBNull.Value, false, storageType)
        {
        }

        protected DataStorage(DataColumn column, Type type, object? defaultValue, object nullValue, StorageType storageType)
            : this(column, type, defaultValue, nullValue, false, storageType)
        {
        }

        protected DataStorage(DataColumn column, Type type, object? defaultValue, object nullValue, bool isICloneable, StorageType storageType)
        {
            Debug.Assert(storageType == GetStorageType(type), "Incorrect storage type specified");
            _column = column;
            _table = column.Table!;
            _dataType = type;
            _storageTypeCode = storageType;
            _defaultValue = defaultValue;
            _nullValue = nullValue;
            _isCloneable = isICloneable;
            _isCustomDefinedType = IsTypeCustomType(_storageTypeCode);
            _isStringType = ((StorageType.String == _storageTypeCode) || (StorageType.SqlString == _storageTypeCode));
            _isValueType = DetermineIfValueType(_storageTypeCode, type);
        }

        internal DataSetDateTime DateTimeMode
        {
            get
            {
                return _column.DateTimeMode;
            }
        }

        internal IFormatProvider FormatProvider
        {
            get
            {
                return _table.FormatProvider;
            }
        }

        public virtual object Aggregate(int[] recordNos, AggregateType kind)
        {
            if (AggregateType.Count == kind)
            {
                return AggregateCount(recordNos);
            }
            // Overridden implementation never return null, except for First which isn't actually implemented.
            return null!;
        }

        public object AggregateCount(int[] recordNos)
        {
            int count = 0;
            for (int i = 0; i < recordNos.Length; i++)
            {
                if (!_dbNullBits.Get(recordNos[i]))
                    count++;
            }
            return count;
        }

        protected int CompareBits(int recordNo1, int recordNo2)
        {
            bool recordNo1Null = _dbNullBits.Get(recordNo1);
            bool recordNo2Null = _dbNullBits.Get(recordNo2);
            if (recordNo1Null ^ recordNo2Null)
            {
                if (recordNo1Null)
                    return -1;
                else
                    return 1;
            }

            return 0;
        }

        public abstract int Compare(int recordNo1, int recordNo2);

        // only does comparision, expect value to be of the correct type
        public abstract int CompareValueTo(int recordNo1, object? value);

        // only does conversion with support for reference null
        public virtual object? ConvertValue(object? value)
        {
            return value;
        }

        protected void CopyBits(int srcRecordNo, int dstRecordNo)
        {
            _dbNullBits.Set(dstRecordNo, _dbNullBits.Get(srcRecordNo));
        }

        public abstract void Copy(int recordNo1, int recordNo2);

        public abstract object Get(int recordNo);

        protected object GetBits(int recordNo)
        {
            if (_dbNullBits.Get(recordNo))
            {
                return _nullValue;
            }
            // _defaultValue is null only for ObjectStorage, which does not call this method
            return _defaultValue!;
        }

        public virtual int GetStringLength(int record)
        {
            Debug.Fail("not a String or SqlString column");
            return int.MaxValue;
        }

        protected bool HasValue(int recordNo)
        {
            return !_dbNullBits!.Get(recordNo);
        }

        public virtual bool IsNull(int recordNo)
        {
            return _dbNullBits.Get(recordNo);
        }

        // convert (may not support reference null) and store the value
        public abstract void Set(int recordNo, object value);

        protected void SetNullBit(int recordNo, bool flag)
        {
            _dbNullBits.Set(recordNo, flag);
        }

        public virtual void SetCapacity(int capacity)
        {
            if (null == _dbNullBits)
            {
                _dbNullBits = new BitArray(capacity);
            }
            else
            {
                _dbNullBits.Length = capacity;
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public abstract object ConvertXmlToObject(string s);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public virtual object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute? xmlAttrib)
        {
            return ConvertXmlToObject(xmlReader.Value);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public abstract string ConvertObjectToXml(object value);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public virtual void ConvertObjectToXml(object value, XmlWriter xmlWriter, XmlRootAttribute? xmlAttrib)
        {
            xmlWriter.WriteString(ConvertObjectToXml(value)); // should it be NO OP?
        }

        public static DataStorage CreateStorage(DataColumn column, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type dataType, StorageType typeCode)
        {
            Debug.Assert(typeCode == GetStorageType(dataType), "Incorrect storage type specified");
            if ((StorageType.Empty == typeCode) && (null != dataType))
            {
                if (typeof(INullable).IsAssignableFrom(dataType))
                { // Udt, OracleTypes
                    return new SqlUdtStorage(column, dataType);
                }
                else
                {
                    return new ObjectStorage(column, dataType); // non-nullable non-primitives
                }
            }

            switch (typeCode)
            {
                case StorageType.Empty: throw ExceptionBuilder.InvalidStorageType(TypeCode.Empty);
                case StorageType.DBNull: throw ExceptionBuilder.InvalidStorageType((TypeCode)2); // TypeCode.DBNull);
                case StorageType.Object: return new ObjectStorage(column, dataType!);
                case StorageType.Boolean: return new BooleanStorage(column);
                case StorageType.Char: return new CharStorage(column);
                case StorageType.SByte: return new SByteStorage(column);
                case StorageType.Byte: return new ByteStorage(column);
                case StorageType.Int16: return new Int16Storage(column);
                case StorageType.UInt16: return new UInt16Storage(column);
                case StorageType.Int32: return new Int32Storage(column);
                case StorageType.UInt32: return new UInt32Storage(column);
                case StorageType.Int64: return new Int64Storage(column);
                case StorageType.UInt64: return new UInt64Storage(column);
                case StorageType.Single: return new SingleStorage(column);
                case StorageType.Double: return new DoubleStorage(column);
                case StorageType.Decimal: return new DecimalStorage(column);
                case StorageType.DateTime: return new DateTimeStorage(column);
                case StorageType.TimeSpan: return new TimeSpanStorage(column);
                case StorageType.String: return new StringStorage(column);
                case StorageType.Guid: return new ObjectStorage(column, dataType!);

                case StorageType.ByteArray: return new ObjectStorage(column, dataType!);
                case StorageType.CharArray: return new ObjectStorage(column, dataType!);
                case StorageType.Type: return new ObjectStorage(column, dataType!);
                case StorageType.DateTimeOffset: return new DateTimeOffsetStorage(column);
                case StorageType.BigInteger: return new BigIntegerStorage(column);
                case StorageType.Uri: return new ObjectStorage(column, dataType!);

                case StorageType.SqlBinary: return new SqlBinaryStorage(column);
                case StorageType.SqlBoolean: return new SqlBooleanStorage(column);
                case StorageType.SqlByte: return new SqlByteStorage(column);
                case StorageType.SqlBytes: return new SqlBytesStorage(column);
                case StorageType.SqlChars: return new SqlCharsStorage(column);
                case StorageType.SqlDateTime: return new SqlDateTimeStorage(column);
                case StorageType.SqlDecimal: return new SqlDecimalStorage(column);
                case StorageType.SqlDouble: return new SqlDoubleStorage(column);
                case StorageType.SqlGuid: return new SqlGuidStorage(column);
                case StorageType.SqlInt16: return new SqlInt16Storage(column);
                case StorageType.SqlInt32: return new SqlInt32Storage(column);
                case StorageType.SqlInt64: return new SqlInt64Storage(column);
                case StorageType.SqlMoney: return new SqlMoneyStorage(column);
                case StorageType.SqlSingle: return new SqlSingleStorage(column);
                case StorageType.SqlString: return new SqlStringStorage(column);

                default:
                    Debug.Fail("shouldn't be here");
                    goto case StorageType.Object;
            }
        }

        internal static StorageType GetStorageType(Type? dataType)
        {
            for (int i = 0; i < s_storageClassType.Length; ++i)
            {
                if (dataType == s_storageClassType[i])
                {
                    return (StorageType)i;
                }
            }
            TypeCode tcode = Type.GetTypeCode(dataType);
            if (TypeCode.Object != tcode)
            { // enum -> Int64/Int32/Int16/Byte
                return (StorageType)tcode;
            }
            return StorageType.Empty;
        }

        internal static Type GetTypeStorage(StorageType storageType)
        {
            Debug.Assert(storageType != StorageType.Empty);
            return s_storageClassType[(int)storageType]!;
        }

        internal static bool IsTypeCustomType(Type type)
        {
            return IsTypeCustomType(GetStorageType(type));
        }

        internal static bool IsTypeCustomType(StorageType typeCode)
        {
            return ((StorageType.Object == typeCode) || (StorageType.Empty == typeCode) || (StorageType.CharArray == typeCode));
        }

        internal static bool IsSqlType(StorageType storageType)
        {
            return (StorageType.SqlBinary <= storageType);
        }

        public static bool IsSqlType(Type dataType)
        {
            for (int i = (int)StorageType.SqlBinary; i < s_storageClassType.Length; ++i)
            {
                if (dataType == s_storageClassType[i])
                {
                    return true;
                }
            }
            return false;
        }

        private static bool DetermineIfValueType(StorageType typeCode, Type dataType)
        {
            bool result;
            switch (typeCode)
            {
                case StorageType.Boolean:
                case StorageType.Char:
                case StorageType.SByte:
                case StorageType.Byte:
                case StorageType.Int16:
                case StorageType.UInt16:
                case StorageType.Int32:
                case StorageType.UInt32:
                case StorageType.Int64:
                case StorageType.UInt64:
                case StorageType.Single:
                case StorageType.Double:
                case StorageType.Decimal:
                case StorageType.DateTime:
                case StorageType.DateTimeOffset:
                case StorageType.BigInteger:
                case StorageType.TimeSpan:
                case StorageType.Guid:
                case StorageType.SqlBinary:
                case StorageType.SqlBoolean:
                case StorageType.SqlByte:
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
                    result = true;
                    break;

                case StorageType.String:
                case StorageType.ByteArray:
                case StorageType.CharArray:
                case StorageType.Type:
                case StorageType.Uri:
                case StorageType.SqlBytes:
                case StorageType.SqlChars:
                    result = false;
                    break;

                default:
                    result = dataType.IsValueType;
                    break;
            }
            Debug.Assert(result == dataType.IsValueType, "typeCode mismatches dataType");
            return result;
        }

        internal static void ImplementsInterfaces(
                                    StorageType typeCode,
                                    Type dataType,
                                    out bool sqlType,
                                    out bool nullable,
                                    out bool xmlSerializable,
                                    out bool changeTracking,
                                    out bool revertibleChangeTracking)
        {
            Debug.Assert(typeCode == GetStorageType(dataType), "typeCode mismatches dataType");
            if (IsSqlType(typeCode))
            {
                sqlType = true;
                nullable = true;
                changeTracking = false;
                revertibleChangeTracking = false;
                xmlSerializable = true;
            }
            else if (StorageType.Empty != typeCode)
            {
                sqlType = false;
                nullable = false;
                changeTracking = false;
                revertibleChangeTracking = false;
                xmlSerializable = false;
            }
            else
            {
                // Non-standard type - look it up in the dictionary or add it if not found
                Tuple<bool, bool, bool, bool> interfaces = s_typeImplementsInterface.GetOrAdd(dataType, s_inspectTypeForInterfaces);
                sqlType = false;
                nullable = interfaces.Item1;
                changeTracking = interfaces.Item2;
                revertibleChangeTracking = interfaces.Item3;
                xmlSerializable = interfaces.Item4;
            }
            Debug.Assert(nullable == typeof(INullable).IsAssignableFrom(dataType), "INullable");
            Debug.Assert(changeTracking == typeof(System.ComponentModel.IChangeTracking).IsAssignableFrom(dataType), "IChangeTracking");
            Debug.Assert(revertibleChangeTracking == typeof(System.ComponentModel.IRevertibleChangeTracking).IsAssignableFrom(dataType), "IRevertibleChangeTracking");
            Debug.Assert(xmlSerializable == typeof(IXmlSerializable).IsAssignableFrom(dataType), "IXmlSerializable");
        }

        private static Tuple<bool, bool, bool, bool> InspectTypeForInterfaces(Type dataType)
        {
            Debug.Assert(dataType != null, "Type should not be null");

            return new Tuple<bool, bool, bool, bool>(
                typeof(INullable).IsAssignableFrom(dataType),
                typeof(System.ComponentModel.IChangeTracking).IsAssignableFrom(dataType),
                typeof(System.ComponentModel.IRevertibleChangeTracking).IsAssignableFrom(dataType),
                typeof(IXmlSerializable).IsAssignableFrom(dataType));
        }

        internal static bool ImplementsINullableValue(StorageType typeCode, Type dataType)
        {
            Debug.Assert(typeCode == GetStorageType(dataType), "typeCode mismatches dataType");
            return ((StorageType.Empty == typeCode) && dataType.IsGenericType && (dataType.GetGenericTypeDefinition() == typeof(System.Nullable<>)));
        }

        public static bool IsObjectNull(object value)
        {
            return ((null == value) || (DBNull.Value == value) || IsObjectSqlNull(value));
        }

        public static bool IsObjectSqlNull(object value)
        {
            INullable? inullable = (value as INullable);
            return ((null != inullable) && inullable.IsNull);
        }

        internal object GetEmptyStorageInternal(int recordCount)
        {
            return GetEmptyStorage(recordCount);
        }

        internal void CopyValueInternal(int record, object store, BitArray nullbits, int storeIndex)
        {
            CopyValue(record, store, nullbits, storeIndex);
        }

        internal void SetStorageInternal(object store, BitArray nullbits)
        {
            SetStorage(store, nullbits);
        }

        protected abstract object GetEmptyStorage(int recordCount);
        protected abstract void CopyValue(int record, object store, BitArray nullbits, int storeIndex);
        protected abstract void SetStorage(object store, BitArray nullbits);
        protected void SetNullStorage(BitArray nullbits)
        {
            _dbNullBits = nullbits;
        }

        /// <summary>wrapper around Type.GetType</summary>
        /// <param name="value">assembly qualified type name or one of the special known types</param>
        /// <returns>Type or null if not found</returns>
        /// <exception cref="InvalidOperationException">when type implements IDynamicMetaObjectProvider and not IXmlSerializable</exception>
        /// <remarks>
        /// Types like "System.Guid" will load regardless of AssemblyQualifiedName because they are special
        /// Types like "System.Data.SqlTypes.SqlString" will load because they are in the same assembly as this code
        /// Types like "System.Numerics.BigInteger" won't load because they are not special and not same assembly as this code
        /// </remarks>
        [RequiresUnreferencedCode("Calls Type.GetType")]
        internal static Type GetType(string value)
        {
            Type? dataType = Type.GetType(value); // throwOnError=false, ignoreCase=fase
            if (null == dataType)
            {
                if ("System.Numerics.BigInteger" == value)
                {
                    dataType = typeof(System.Numerics.BigInteger);
                }
            }

            // prevent reading type from schema which implements IDynamicMetaObjectProvider and not IXmlSerializable
            // the check here prevents the type from being loaded in schema or as instance data (when DataType is object)
            ObjectStorage.VerifyIDynamicMetaObjectProvider(dataType!);
            return dataType!;
        }

        /// <summary>wrapper around Type.AssemblyQualifiedName</summary>
        /// <param name="type"></param>
        /// <returns>qualified name when writing in xml</returns>
        /// <exception cref="InvalidOperationException">when type implements IDynamicMetaObjectProvider and not IXmlSerializable</exception>
        internal static string GetQualifiedName(Type type)
        {
            Debug.Assert(null != type, "null type");
            ObjectStorage.VerifyIDynamicMetaObjectProvider(type);
            return type.AssemblyQualifiedName!;
        }
    }
}
