// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




//------------------------------------------------------------------------------

using System.Diagnostics;
using System.Data.SqlTypes;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Data.SqlClient
{
    internal sealed class SqlBuffer
    {
        internal enum StorageType
        {
            Empty = 0,
            Boolean,
            Byte,
            DateTime,
            Decimal,
            Double,
            Int16,
            Int32,
            Int64,
            Money,
            Single,
            String,
            SqlBinary,
            SqlCachedBuffer,
            SqlGuid,
            SqlXml,
            Date,
            DateTime2,
            DateTimeOffset,
            Time,
        }

        internal struct DateTimeInfo
        {
            // This is used to store DateTime
            internal Int32 daypart;
            internal Int32 timepart;
        }

        internal struct NumericInfo
        {
            // This is used to store Decimal data
            internal Int32 data1;
            internal Int32 data2;
            internal Int32 data3;
            internal Int32 data4;
            internal Byte precision;
            internal Byte scale;
            internal Boolean positive;
        }

        internal struct TimeInfo
        {
            internal Int64 ticks;
            internal byte scale;
        }

        internal struct DateTime2Info
        {
            internal Int32 date;
            internal TimeInfo timeInfo;
        }

        internal struct DateTimeOffsetInfo
        {
            internal DateTime2Info dateTime2Info;
            internal Int16 offset;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Storage
        {
            [FieldOffset(0)]
            internal Boolean _boolean;
            [FieldOffset(0)]
            internal Byte _byte;
            [FieldOffset(0)]
            internal DateTimeInfo _dateTimeInfo;
            [FieldOffset(0)]
            internal Double _double;
            [FieldOffset(0)]
            internal NumericInfo _numericInfo;
            [FieldOffset(0)]
            internal Int16 _int16;
            [FieldOffset(0)]
            internal Int32 _int32;
            [FieldOffset(0)]
            internal Int64 _int64;     // also used to store Money, UtcDateTime, Date , and Time
            [FieldOffset(0)]
            internal Single _single;
            [FieldOffset(0)]
            internal TimeInfo _timeInfo;
            [FieldOffset(0)]
            internal DateTime2Info _dateTime2Info;
            [FieldOffset(0)]
            internal DateTimeOffsetInfo _dateTimeOffsetInfo;
        }

        private bool _isNull;
        private StorageType _type;
        private Storage _value;
        private object _object;    // String, SqlBinary, SqlCachedBuffer, SqlGuid, SqlString, SqlXml

        internal SqlBuffer()
        {
        }

        private SqlBuffer(SqlBuffer value)
        { // Clone
            // value types
            _isNull = value._isNull;
            _type = value._type;
            // ref types - should also be read only unless at some point we allow this data
            // to be mutable, then we will need to copy
            _value = value._value;
            _object = value._object;
        }

        internal bool IsEmpty
        {
            get
            {
                return (StorageType.Empty == _type);
            }
        }

        internal bool IsNull
        {
            get
            {
                return _isNull;
            }
        }

        internal StorageType VariantInternalStorageType
        {
            get { return _type; }
        }

        internal Boolean Boolean
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Boolean == _type)
                {
                    return _value._boolean;
                }
                return (Boolean)this.Value; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _value._boolean = value;
                _type = StorageType.Boolean;
                _isNull = false;
            }
        }

        internal Byte Byte
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Byte == _type)
                {
                    return _value._byte;
                }
                return (Byte)this.Value; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _value._byte = value;
                _type = StorageType.Byte;
                _isNull = false;
            }
        }

        internal Byte[] ByteArray
        {
            get
            {
                ThrowIfNull();
                return this.SqlBinary.Value;
            }
        }

        internal DateTime DateTime
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Date == _type)
                {
                    return DateTime.MinValue.AddDays(_value._int32);
                }
                if (StorageType.DateTime2 == _type)
                {
                    return new DateTime(GetTicksFromDateTime2Info(_value._dateTime2Info));
                }
                if (StorageType.DateTime == _type)
                {
                    return SqlTypeWorkarounds.SqlDateTimeToDateTime(_value._dateTimeInfo.daypart, _value._dateTimeInfo.timepart);
                }
                return (DateTime)this.Value; // anything else we haven't thought of goes through boxing.
            }
        }

        internal Decimal Decimal
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Decimal == _type)
                {
                    if (_value._numericInfo.data4 != 0 || _value._numericInfo.scale > 28)
                    {
                        throw new OverflowException(SQLResource.ConversionOverflowMessage);
                    }
                    return new Decimal(_value._numericInfo.data1, _value._numericInfo.data2, _value._numericInfo.data3, !_value._numericInfo.positive, _value._numericInfo.scale);
                }
                if (StorageType.Money == _type)
                {
                    long l = _value._int64;
                    bool isNegative = false;
                    if (l < 0)
                    {
                        isNegative = true;
                        l = -l;
                    }
                    return new Decimal((int)(l & 0xffffffff), (int)(l >> 32), 0, isNegative, 4);
                }
                return (Decimal)this.Value; // anything else we haven't thought of goes through boxing.
            }
        }

        internal Double Double
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Double == _type)
                {
                    return _value._double;
                }
                return (Double)this.Value; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _value._double = value;
                _type = StorageType.Double;
                _isNull = false;
            }
        }

        internal Guid Guid
        {
            get
            {
                ThrowIfNull();
                return this.SqlGuid.Value;
            }
        }

        internal Int16 Int16
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Int16 == _type)
                {
                    return _value._int16;
                }
                return (Int16)this.Value; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _value._int16 = value;
                _type = StorageType.Int16;
                _isNull = false;
            }
        }

        internal Int32 Int32
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Int32 == _type)
                {
                    return _value._int32;
                }
                return (Int32)this.Value; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _value._int32 = value;
                _type = StorageType.Int32;
                _isNull = false;
            }
        }

        internal Int64 Int64
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Int64 == _type)
                {
                    return _value._int64;
                }
                return (Int64)this.Value; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _value._int64 = value;
                _type = StorageType.Int64;
                _isNull = false;
            }
        }

        internal Single Single
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Single == _type)
                {
                    return _value._single;
                }
                return (Single)this.Value; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _value._single = value;
                _type = StorageType.Single;
                _isNull = false;
            }
        }

        internal String String
        {
            get
            {
                ThrowIfNull();

                if (StorageType.String == _type)
                {
                    return (String)_object;
                }
                else if (StorageType.SqlCachedBuffer == _type)
                {
                    return ((SqlCachedBuffer)(_object)).ToString();
                }
                return (String)this.Value; // anything else we haven't thought of goes through boxing.
            }
        }

        // use static list of format strings indexed by scale for perf
        private static string[] s_katmaiDateTimeOffsetFormatByScale = new string[] {
                "yyyy-MM-dd HH:mm:ss zzz",
                "yyyy-MM-dd HH:mm:ss.f zzz",
                "yyyy-MM-dd HH:mm:ss.ff zzz",
                "yyyy-MM-dd HH:mm:ss.fff zzz",
                "yyyy-MM-dd HH:mm:ss.ffff zzz",
                "yyyy-MM-dd HH:mm:ss.fffff zzz",
                "yyyy-MM-dd HH:mm:ss.ffffff zzz",
                "yyyy-MM-dd HH:mm:ss.fffffff zzz",
        };

        private static string[] s_katmaiDateTime2FormatByScale = new string[] {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss.f",
                "yyyy-MM-dd HH:mm:ss.ff",
                "yyyy-MM-dd HH:mm:ss.fff",
                "yyyy-MM-dd HH:mm:ss.ffff",
                "yyyy-MM-dd HH:mm:ss.fffff",
                "yyyy-MM-dd HH:mm:ss.ffffff",
                "yyyy-MM-dd HH:mm:ss.fffffff",
        };

        private static string[] s_katmaiTimeFormatByScale = new string[] {
                "HH:mm:ss",
                "HH:mm:ss.f",
                "HH:mm:ss.ff",
                "HH:mm:ss.fff",
                "HH:mm:ss.ffff",
                "HH:mm:ss.fffff",
                "HH:mm:ss.ffffff",
                "HH:mm:ss.fffffff",
        };

        internal string KatmaiDateTimeString
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Date == _type)
                {
                    return this.DateTime.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo);
                }
                if (StorageType.Time == _type)
                {
                    byte scale = _value._timeInfo.scale;
                    return new DateTime(_value._timeInfo.ticks).ToString(s_katmaiTimeFormatByScale[scale], DateTimeFormatInfo.InvariantInfo);
                }
                if (StorageType.DateTime2 == _type)
                {
                    byte scale = _value._dateTime2Info.timeInfo.scale;
                    return this.DateTime.ToString(s_katmaiDateTime2FormatByScale[scale], DateTimeFormatInfo.InvariantInfo);
                }
                if (StorageType.DateTimeOffset == _type)
                {
                    DateTimeOffset dto = this.DateTimeOffset;
                    byte scale = _value._dateTimeOffsetInfo.dateTime2Info.timeInfo.scale;
                    return dto.ToString(s_katmaiDateTimeOffsetFormatByScale[scale], DateTimeFormatInfo.InvariantInfo);
                }
                return (String)this.Value; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlString KatmaiDateTimeSqlString
        {
            get
            {
                if (StorageType.Date == _type ||
                    StorageType.Time == _type ||
                    StorageType.DateTime2 == _type ||
                    StorageType.DateTimeOffset == _type)
                {
                    if (IsNull)
                    {
                        return SqlString.Null;
                    }
                    return new SqlString(KatmaiDateTimeString);
                }
                return (SqlString)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal TimeSpan Time
        {
            get
            {
                ThrowIfNull();

                if (StorageType.Time == _type)
                {
                    return new TimeSpan(_value._timeInfo.ticks);
                }

                return (TimeSpan)this.Value; // anything else we haven't thought of goes through boxing.
            }
        }

        internal DateTimeOffset DateTimeOffset
        {
            get
            {
                ThrowIfNull();

                if (StorageType.DateTimeOffset == _type)
                {
                    TimeSpan offset = new TimeSpan(0, _value._dateTimeOffsetInfo.offset, 0);
                    // datetime part presents time in UTC
                    return new DateTimeOffset(GetTicksFromDateTime2Info(_value._dateTimeOffsetInfo.dateTime2Info) + offset.Ticks, offset);
                }

                return (DateTimeOffset)this.Value; // anything else we haven't thought of goes through boxing.
            }
        }

        private static long GetTicksFromDateTime2Info(DateTime2Info dateTime2Info)
        {
            return (dateTime2Info.date * TimeSpan.TicksPerDay + dateTime2Info.timeInfo.ticks);
        }

        internal SqlBinary SqlBinary
        {
            get
            {
                if (StorageType.SqlBinary == _type)
                {
                    return (SqlBinary)_object;
                }
                return (SqlBinary)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _object = value;
                _type = StorageType.SqlBinary;
                _isNull = value.IsNull;
            }
        }

        internal SqlBoolean SqlBoolean
        {
            get
            {
                if (StorageType.Boolean == _type)
                {
                    if (IsNull)
                    {
                        return SqlBoolean.Null;
                    }
                    return new SqlBoolean(_value._boolean);
                }
                return (SqlBoolean)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlByte SqlByte
        {
            get
            {
                if (StorageType.Byte == _type)
                {
                    if (IsNull)
                    {
                        return SqlByte.Null;
                    }
                    return new SqlByte(_value._byte);
                }
                return (SqlByte)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlCachedBuffer SqlCachedBuffer
        {
            get
            {
                if (StorageType.SqlCachedBuffer == _type)
                {
                    if (IsNull)
                    {
                        return SqlCachedBuffer.Null;
                    }
                    return (SqlCachedBuffer)_object;
                }
                return (SqlCachedBuffer)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _object = value;
                _type = StorageType.SqlCachedBuffer;
                _isNull = value.IsNull;
            }
        }

        internal SqlXml SqlXml
        {
            get
            {
                if (StorageType.SqlXml == _type)
                {
                    if (IsNull)
                    {
                        return SqlXml.Null;
                    }
                    return (SqlXml)_object;
                }
                return (SqlXml)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _object = value;
                _type = StorageType.SqlXml;
                _isNull = value.IsNull;
            }
        }

        internal SqlDateTime SqlDateTime
        {
            get
            {
                if (StorageType.DateTime == _type)
                {
                    if (IsNull)
                    {
                        return SqlDateTime.Null;
                    }
                    return new SqlDateTime(_value._dateTimeInfo.daypart, _value._dateTimeInfo.timepart);
                }
                return (SqlDateTime)SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlDecimal SqlDecimal
        {
            get
            {
                if (StorageType.Decimal == _type)
                {
                    if (IsNull)
                    {
                        return SqlDecimal.Null;
                    }
                    return new SqlDecimal(_value._numericInfo.precision,
                                          _value._numericInfo.scale,
                                          _value._numericInfo.positive,
                                          _value._numericInfo.data1,
                                          _value._numericInfo.data2,
                                          _value._numericInfo.data3,
                                          _value._numericInfo.data4
                                          );
                }
                return (SqlDecimal)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlDouble SqlDouble
        {
            get
            {
                if (StorageType.Double == _type)
                {
                    if (IsNull)
                    {
                        return SqlDouble.Null;
                    }
                    return new SqlDouble(_value._double);
                }
                return (SqlDouble)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlGuid SqlGuid
        {
            get
            {
                if (StorageType.SqlGuid == _type)
                {
                    return (SqlGuid)_object;
                }
                return (SqlGuid)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
            set
            {
                Debug.Assert(IsEmpty, "setting value a second time?");
                _object = value;
                _type = StorageType.SqlGuid;
                _isNull = value.IsNull;
            }
        }

        internal SqlInt16 SqlInt16
        {
            get
            {
                if (StorageType.Int16 == _type)
                {
                    if (IsNull)
                    {
                        return SqlInt16.Null;
                    }
                    return new SqlInt16(_value._int16);
                }
                return (SqlInt16)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlInt32 SqlInt32
        {
            get
            {
                if (StorageType.Int32 == _type)
                {
                    if (IsNull)
                    {
                        return SqlInt32.Null;
                    }
                    return new SqlInt32(_value._int32);
                }
                return (SqlInt32)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlInt64 SqlInt64
        {
            get
            {
                if (StorageType.Int64 == _type)
                {
                    if (IsNull)
                    {
                        return SqlInt64.Null;
                    }
                    return new SqlInt64(_value._int64);
                }
                return (SqlInt64)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlMoney SqlMoney
        {
            get
            {
                if (StorageType.Money == _type)
                {
                    if (IsNull)
                    {
                        return SqlMoney.Null;
                    }
                    return SqlTypeWorkarounds.SqlMoneyCtor(_value._int64, 1/*ignored*/);
                }
                return (SqlMoney)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlSingle SqlSingle
        {
            get
            {
                if (StorageType.Single == _type)
                {
                    if (IsNull)
                    {
                        return SqlSingle.Null;
                    }
                    return new SqlSingle(_value._single);
                }
                return (SqlSingle)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal SqlString SqlString
        {
            get
            {
                if (StorageType.String == _type)
                {
                    if (IsNull)
                    {
                        return SqlString.Null;
                    }
                    return new SqlString((String)_object);
                }
                else if (StorageType.SqlCachedBuffer == _type)
                {
                    SqlCachedBuffer data = (SqlCachedBuffer)(_object);
                    if (data.IsNull)
                    {
                        return SqlString.Null;
                    }
                    return data.ToSqlString();
                }
                return (SqlString)this.SqlValue; // anything else we haven't thought of goes through boxing.
            }
        }

        internal object SqlValue
        {
            get
            {
                switch (_type)
                {
                    case StorageType.Empty: return DBNull.Value;
                    case StorageType.Boolean: return SqlBoolean;
                    case StorageType.Byte: return SqlByte;
                    case StorageType.DateTime: return SqlDateTime;
                    case StorageType.Decimal: return SqlDecimal;
                    case StorageType.Double: return SqlDouble;
                    case StorageType.Int16: return SqlInt16;
                    case StorageType.Int32: return SqlInt32;
                    case StorageType.Int64: return SqlInt64;
                    case StorageType.Money: return SqlMoney;
                    case StorageType.Single: return SqlSingle;
                    case StorageType.String: return SqlString;
                    case StorageType.SqlCachedBuffer:
                        {
                            SqlCachedBuffer data = (SqlCachedBuffer)(_object);
                            if (data.IsNull)
                            {
                                return SqlXml.Null;
                            }
                            return data.ToSqlXml();
                        }

                    case StorageType.SqlBinary:
                    case StorageType.SqlGuid:
                        return _object;

                    case StorageType.SqlXml:
                        {
                            if (_isNull)
                            {
                                return SqlXml.Null;
                            }
                            Debug.Assert(null != _object);
                            return (SqlXml)_object;
                        }
                    case StorageType.Date:
                    case StorageType.DateTime2:
                        if (_isNull)
                        {
                            return DBNull.Value;
                        }
                        return DateTime;
                    case StorageType.DateTimeOffset:
                        if (_isNull)
                        {
                            return DBNull.Value;
                        }
                        return DateTimeOffset;
                    case StorageType.Time:
                        if (_isNull)
                        {
                            return DBNull.Value;
                        }
                        return Time;
                }
                return null; // need to return the value as an object of some SQL type
            }
        }

        internal object Value
        {
            get
            {
                if (IsNull)
                {
                    return DBNull.Value;
                }
                switch (_type)
                {
                    case StorageType.Empty: return DBNull.Value;
                    case StorageType.Boolean: return Boolean;
                    case StorageType.Byte: return Byte;
                    case StorageType.DateTime: return DateTime;
                    case StorageType.Decimal: return Decimal;
                    case StorageType.Double: return Double;
                    case StorageType.Int16: return Int16;
                    case StorageType.Int32: return Int32;
                    case StorageType.Int64: return Int64;
                    case StorageType.Money: return Decimal;
                    case StorageType.Single: return Single;
                    case StorageType.String: return String;
                    case StorageType.SqlBinary: return ByteArray;
                    case StorageType.SqlCachedBuffer:
                        {
                            // If we have a CachedBuffer, it's because it's an XMLTYPE column
                            // and we have to return a string when they're asking for the CLS
                            // value of the column.
                            return ((SqlCachedBuffer)(_object)).ToString();
                        }
                    case StorageType.SqlGuid: return Guid;
                    case StorageType.SqlXml:
                        {
                            // XMLTYPE columns must be returned as string when asking for the CLS value
                            SqlXml data = (SqlXml)_object;
                            string s = data.Value;
                            return s;
                        }
                    case StorageType.Date: return DateTime;
                    case StorageType.DateTime2: return DateTime;
                    case StorageType.DateTimeOffset: return DateTimeOffset;
                    case StorageType.Time: return Time;
                }
                return null; // need to return the value as an object of some CLS type
            }
        }

        internal Type GetTypeFromStorageType(bool isSqlType)
        {
            if (isSqlType)
            {
                switch (_type)
                {
                    case SqlBuffer.StorageType.Empty: return null;
                    case SqlBuffer.StorageType.Boolean: return typeof(SqlBoolean);
                    case SqlBuffer.StorageType.Byte: return typeof(SqlByte);
                    case SqlBuffer.StorageType.DateTime: return typeof(SqlDateTime);
                    case SqlBuffer.StorageType.Decimal: return typeof(SqlDecimal);
                    case SqlBuffer.StorageType.Double: return typeof(SqlDouble);
                    case SqlBuffer.StorageType.Int16: return typeof(SqlInt16);
                    case SqlBuffer.StorageType.Int32: return typeof(SqlInt32);
                    case SqlBuffer.StorageType.Int64: return typeof(SqlInt64);
                    case SqlBuffer.StorageType.Money: return typeof(SqlMoney);
                    case SqlBuffer.StorageType.Single: return typeof(SqlSingle);
                    case SqlBuffer.StorageType.String: return typeof(SqlString);
                    case SqlBuffer.StorageType.SqlCachedBuffer: return typeof(SqlString);
                    case SqlBuffer.StorageType.SqlBinary: return typeof(object);
                    case SqlBuffer.StorageType.SqlGuid: return typeof(object);
                    case SqlBuffer.StorageType.SqlXml: return typeof(SqlXml);
                }
            }
            else
            { //Is CLR Type
                switch (_type)
                {
                    case SqlBuffer.StorageType.Empty: return null;
                    case SqlBuffer.StorageType.Boolean: return typeof(Boolean);
                    case SqlBuffer.StorageType.Byte: return typeof(Byte);
                    case SqlBuffer.StorageType.DateTime: return typeof(DateTime);
                    case SqlBuffer.StorageType.Decimal: return typeof(Decimal);
                    case SqlBuffer.StorageType.Double: return typeof(Double);
                    case SqlBuffer.StorageType.Int16: return typeof(Int16);
                    case SqlBuffer.StorageType.Int32: return typeof(Int32);
                    case SqlBuffer.StorageType.Int64: return typeof(Int64);
                    case SqlBuffer.StorageType.Money: return typeof(Decimal);
                    case SqlBuffer.StorageType.Single: return typeof(Single);
                    case SqlBuffer.StorageType.String: return typeof(String);
                    case SqlBuffer.StorageType.SqlBinary: return typeof(Byte[]);
                    case SqlBuffer.StorageType.SqlCachedBuffer: return typeof(string);
                    case SqlBuffer.StorageType.SqlGuid: return typeof(Guid);
                    case SqlBuffer.StorageType.SqlXml: return typeof(string);
                }
            }

            return null; // need to return the value as an object of some CLS type            
        }

        internal static SqlBuffer[] CreateBufferArray(int length)
        {
            SqlBuffer[] buffers = new SqlBuffer[length];
            for (int i = 0; i < buffers.Length; ++i)
            {
                buffers[i] = new SqlBuffer();
            }
            return buffers;
        }

        internal static SqlBuffer[] CloneBufferArray(SqlBuffer[] values)
        {
            SqlBuffer[] copy = new SqlBuffer[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                copy[i] = new SqlBuffer(values[i]);
            }
            return copy;
        }

        internal static void Clear(SqlBuffer[] values)
        {
            if (null != values)
            {
                for (int i = 0; i < values.Length; ++i)
                {
                    values[i].Clear();
                }
            }
        }

        internal void Clear()
        {
            _isNull = false;
            _type = StorageType.Empty;
            _object = null;
        }

        internal void SetToDateTime(int daypart, int timepart)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");
            _value._dateTimeInfo.daypart = daypart;
            _value._dateTimeInfo.timepart = timepart;
            _type = StorageType.DateTime;
            _isNull = false;
        }

        internal void SetToDecimal(byte precision, byte scale, bool positive, int[] bits)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");
            _value._numericInfo.precision = precision;
            _value._numericInfo.scale = scale;
            _value._numericInfo.positive = positive;
            _value._numericInfo.data1 = bits[0];
            _value._numericInfo.data2 = bits[1];
            _value._numericInfo.data3 = bits[2];
            _value._numericInfo.data4 = bits[3];
            _type = StorageType.Decimal;
            _isNull = false;
        }

        internal void SetToMoney(long value)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");
            _value._int64 = value;
            _type = StorageType.Money;
            _isNull = false;
        }

        internal void SetToNullOfType(StorageType storageType)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");
            _type = storageType;
            _isNull = true;
            _object = null;
        }

        internal void SetToString(string value)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");
            _object = value;
            _type = StorageType.String;
            _isNull = false;
        }

        internal void SetToDate(byte[] bytes)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.Date;
            _value._int32 = GetDateFromByteArray(bytes, 0);
            _isNull = false;
        }

        internal void SetToDate(DateTime date)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.Date;
            _value._int32 = date.Subtract(DateTime.MinValue).Days;
            _isNull = false;
        }

        internal void SetToTime(byte[] bytes, int length, byte scale)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.Time;
            FillInTimeInfo(ref _value._timeInfo, bytes, length, scale);
            _isNull = false;
        }

        internal void SetToTime(TimeSpan timeSpan, byte scale)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.Time;
            _value._timeInfo.ticks = timeSpan.Ticks;
            _value._timeInfo.scale = scale;
            _isNull = false;
        }

        internal void SetToDateTime2(byte[] bytes, int length, byte scale)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.DateTime2;
            FillInTimeInfo(ref _value._dateTime2Info.timeInfo, bytes, length - 3, scale); // remaining 3 bytes is for date
            _value._dateTime2Info.date = GetDateFromByteArray(bytes, length - 3); // 3 bytes for date
            _isNull = false;
        }

        internal void SetToDateTime2(DateTime dateTime, byte scale)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.DateTime2;
            _value._dateTime2Info.timeInfo.ticks = dateTime.TimeOfDay.Ticks;
            _value._dateTime2Info.timeInfo.scale = scale;
            _value._dateTime2Info.date = dateTime.Subtract(DateTime.MinValue).Days;
            _isNull = false;
        }

        internal void SetToDateTimeOffset(byte[] bytes, int length, byte scale)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.DateTimeOffset;
            FillInTimeInfo(ref _value._dateTimeOffsetInfo.dateTime2Info.timeInfo, bytes, length - 5, scale); // remaining 5 bytes are for date and offset
            _value._dateTimeOffsetInfo.dateTime2Info.date = GetDateFromByteArray(bytes, length - 5); // 3 bytes for date
            _value._dateTimeOffsetInfo.offset = (Int16)(bytes[length - 2] + (bytes[length - 1] << 8)); // 2 bytes for offset (Int16)
            _isNull = false;
        }

        internal void SetToDateTimeOffset(DateTimeOffset dateTimeOffset, byte scale)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.DateTimeOffset;
            DateTime utcDateTime = dateTimeOffset.UtcDateTime; // timeInfo stores the utc datetime of a datatimeoffset
            _value._dateTimeOffsetInfo.dateTime2Info.timeInfo.ticks = utcDateTime.TimeOfDay.Ticks;
            _value._dateTimeOffsetInfo.dateTime2Info.timeInfo.scale = scale;
            _value._dateTimeOffsetInfo.dateTime2Info.date = utcDateTime.Subtract(DateTime.MinValue).Days;
            _value._dateTimeOffsetInfo.offset = (Int16)dateTimeOffset.Offset.TotalMinutes;
            _isNull = false;
        }

        private static void FillInTimeInfo(ref TimeInfo timeInfo, byte[] timeBytes, int length, byte scale)
        {
            Debug.Assert(3 <= length && length <= 5, "invalid data length for timeInfo: " + length);
            Debug.Assert(0 <= scale && scale <= 7, "invalid scale: " + scale);

            Int64 tickUnits = (Int64)timeBytes[0] + ((Int64)timeBytes[1] << 8) + ((Int64)timeBytes[2] << 16);
            if (length > 3)
            {
                tickUnits += ((Int64)timeBytes[3] << 24);
            }
            if (length > 4)
            {
                tickUnits += ((Int64)timeBytes[4] << 32);
            }
            timeInfo.ticks = tickUnits * TdsEnums.TICKS_FROM_SCALE[scale];
            timeInfo.scale = scale;
        }

        private static Int32 GetDateFromByteArray(byte[] buf, int offset)
        {
            return buf[offset] + (buf[offset + 1] << 8) + (buf[offset + 2] << 16);
        }

        private void ThrowIfNull()
        {
            if (IsNull)
            {
                throw new SqlNullValueException();
            }
        }
    }
}// namespace
