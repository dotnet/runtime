// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.





//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Microsoft.SqlServer.Server
{
    public class SqlDataRecord : IDataRecord
    {
        private SmiRecordBuffer _recordBuffer;
        private SmiExtendedMetaData[] _columnSmiMetaData;
        private SmiEventSink_Default _eventSink;
        private SqlMetaData[] _columnMetaData;
        private FieldNameLookup _fieldNameLookup;
        private bool _usesStringStorageForXml;

        private static readonly SmiMetaData s_maxNVarCharForXml = new SmiMetaData(SqlDbType.NVarChar, SmiMetaData.UnlimitedMaxLengthIndicator,
                                        SmiMetaData.DefaultNVarChar_NoCollation.Precision,
                                        SmiMetaData.DefaultNVarChar_NoCollation.Scale,
                                        SmiMetaData.DefaultNVarChar.LocaleId,
                                        SmiMetaData.DefaultNVarChar.CompareOptions,
                                        null);

        public virtual int FieldCount
        {
            get
            {
                EnsureSubclassOverride();
                return _columnMetaData.Length;
            }
        }

        public virtual string GetName(int ordinal)
        {
            EnsureSubclassOverride();
            return GetSqlMetaData(ordinal).Name;
        }

        public virtual string GetDataTypeName(int ordinal)
        {
            EnsureSubclassOverride();
            SqlMetaData metaData = GetSqlMetaData(ordinal);
            if (SqlDbType.Udt == metaData.SqlDbType)
            {
                return metaData.UdtTypeName;
            }
            else
            {
                return MetaType.GetMetaTypeFromSqlDbType(metaData.SqlDbType, false).TypeName;
            }
        }

        public virtual Type GetFieldType(int ordinal)
        {
            EnsureSubclassOverride();
            {
                SqlMetaData md = GetSqlMetaData(ordinal);
                return MetaType.GetMetaTypeFromSqlDbType(md.SqlDbType, false).ClassType;
            }
        }

        public virtual object GetValue(int ordinal)
        {
            EnsureSubclassOverride();
            SmiMetaData metaData = GetSmiMetaData(ordinal);

            return ValueUtilsSmi.GetValue200(
                            _eventSink,
                            _recordBuffer,
                            ordinal,
                            metaData
                                );
        }

        public virtual int GetValues(object[] values)
        {
            EnsureSubclassOverride();
            if (null == values)
            {
                throw ADP.ArgumentNull(nameof(values));
            }

            int copyLength = (values.Length < FieldCount) ? values.Length : FieldCount;
            for (int i = 0; i < copyLength; i++)
            {
                values[i] = GetValue(i);
            }

            return copyLength;
        }

        public virtual int GetOrdinal(string name)
        {
            EnsureSubclassOverride();
            if (null == _fieldNameLookup)
            {
                string[] names = new string[FieldCount];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = GetSqlMetaData(i).Name;
                }

                _fieldNameLookup = new FieldNameLookup(names, -1);
            }

            return _fieldNameLookup.GetOrdinal(name);
        }

        public virtual object this[int ordinal]
        {
            get
            {
                EnsureSubclassOverride();
                return GetValue(ordinal);
            }
        }

        public virtual object this[string name]
        {
            get
            {
                EnsureSubclassOverride();
                return GetValue(GetOrdinal(name));
            }
        }

        public virtual bool GetBoolean(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetBoolean(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual byte GetByte(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetByte(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual long GetBytes(int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetBytes(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), fieldOffset, buffer, bufferOffset, length, true);
        }

        public virtual char GetChar(int ordinal)
        {
            EnsureSubclassOverride();
            throw ADP.NotSupported();
        }

        public virtual long GetChars(int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetChars(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), fieldOffset, buffer, bufferOffset, length);
        }

        public virtual Guid GetGuid(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetGuid(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual short GetInt16(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetInt16(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual int GetInt32(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetInt32(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual long GetInt64(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetInt64(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual float GetFloat(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSingle(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual double GetDouble(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetDouble(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual string GetString(int ordinal)
        {
            EnsureSubclassOverride();
            SmiMetaData colMeta = GetSmiMetaData(ordinal);
            if (_usesStringStorageForXml && SqlDbType.Xml == colMeta.SqlDbType)
            {
                return ValueUtilsSmi.GetString(_eventSink, _recordBuffer, ordinal, s_maxNVarCharForXml);
            }
            else
            {
                return ValueUtilsSmi.GetString(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
            }
        }

        public virtual decimal GetDecimal(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetDecimal(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual DateTime GetDateTime(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetDateTime(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual DateTimeOffset GetDateTimeOffset(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetDateTimeOffset(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual TimeSpan GetTimeSpan(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetTimeSpan(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }


        public virtual bool IsDBNull(int ordinal)
        {
            EnsureSubclassOverride();
            ThrowIfInvalidOrdinal(ordinal);
            return ValueUtilsSmi.IsDBNull(_eventSink, _recordBuffer, ordinal);
        }

        //
        //  ISqlRecord implementation
        //
        public virtual SqlMetaData GetSqlMetaData(int ordinal)
        {
            EnsureSubclassOverride();
            return _columnMetaData[ordinal];
        }

        public virtual Type GetSqlFieldType(int ordinal)
        {
            EnsureSubclassOverride();
            SqlMetaData md = GetSqlMetaData(ordinal);
            return MetaType.GetMetaTypeFromSqlDbType(md.SqlDbType, false).SqlType;
        }

        public virtual object GetSqlValue(int ordinal)
        {
            EnsureSubclassOverride();
            SmiMetaData metaData = GetSmiMetaData(ordinal);
            return ValueUtilsSmi.GetSqlValue200(_eventSink, _recordBuffer, ordinal, metaData);
        }

        public virtual int GetSqlValues(object[] values)
        {
            EnsureSubclassOverride();
            if (null == values)
            {
                throw ADP.ArgumentNull(nameof(values));
            }


            int copyLength = (values.Length < FieldCount) ? values.Length : FieldCount;
            for (int i = 0; i < copyLength; i++)
            {
                values[i] = GetSqlValue(i);
            }

            return copyLength;
        }

        public virtual SqlBinary GetSqlBinary(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlBinary(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlBytes GetSqlBytes(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlBytes(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlXml GetSqlXml(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlXml(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlBoolean GetSqlBoolean(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlBoolean(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlByte GetSqlByte(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlByte(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlChars GetSqlChars(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlChars(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlInt16 GetSqlInt16(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlInt16(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlInt32 GetSqlInt32(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlInt32(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlInt64 GetSqlInt64(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlInt64(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlSingle GetSqlSingle(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlSingle(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlDouble GetSqlDouble(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlDouble(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlMoney GetSqlMoney(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlMoney(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlDateTime GetSqlDateTime(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlDateTime(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlDecimal GetSqlDecimal(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlDecimal(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlString GetSqlString(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlString(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        public virtual SqlGuid GetSqlGuid(int ordinal)
        {
            EnsureSubclassOverride();
            return ValueUtilsSmi.GetSqlGuid(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal));
        }

        //
        // ISqlUpdateableRecord Implementation
        //
        public virtual int SetValues(params object[] values)
        {
            EnsureSubclassOverride();
            if (null == values)
            {
                throw ADP.ArgumentNull(nameof(values));
            }

            // Allow values array longer than FieldCount, just ignore the extra cells.
            int copyLength = (values.Length > FieldCount) ? FieldCount : values.Length;

            ExtendedClrTypeCode[] typeCodes = new ExtendedClrTypeCode[copyLength];

            // Verify all data values as acceptable before changing current state.
            for (int i = 0; i < copyLength; i++)
            {
                SqlMetaData metaData = GetSqlMetaData(i);
                typeCodes[i] = MetaDataUtilsSmi.DetermineExtendedTypeCodeForUseWithSqlDbType(
                    metaData.SqlDbType, false /* isMultiValued */, values[i], metaData.Type);
                if (ExtendedClrTypeCode.Invalid == typeCodes[i])
                {
                    throw ADP.InvalidCast();
                }
            }

            // Now move the data (it'll only throw if someone plays with the values array between
            //      the validation loop and here, or if an invalid UDT was sent).
            for (int i = 0; i < copyLength; i++)
            {
                ValueUtilsSmi.SetCompatibleValueV200(_eventSink, _recordBuffer, i, GetSmiMetaData(i), values[i], typeCodes[i], 0, 0, null);
            }

            return copyLength;
        }

        public virtual void SetValue(int ordinal, object value)
        {
            EnsureSubclassOverride();
            SqlMetaData metaData = GetSqlMetaData(ordinal);
            ExtendedClrTypeCode typeCode = MetaDataUtilsSmi.DetermineExtendedTypeCodeForUseWithSqlDbType(
                        metaData.SqlDbType, false /* isMultiValued */, value, metaData.Type);
            if (ExtendedClrTypeCode.Invalid == typeCode)
            {
                throw ADP.InvalidCast();
            }

            ValueUtilsSmi.SetCompatibleValueV200(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value, typeCode, 0, 0, null);
        }

        public virtual void SetBoolean(int ordinal, bool value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetBoolean(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetByte(int ordinal, byte value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetByte(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetBytes(int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetBytes(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), fieldOffset, buffer, bufferOffset, length);
        }

        public virtual void SetChar(int ordinal, char value)
        {
            EnsureSubclassOverride();
            throw ADP.NotSupported();
        }

        public virtual void SetChars(int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetChars(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), fieldOffset, buffer, bufferOffset, length);
        }

        public virtual void SetInt16(int ordinal, short value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetInt16(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetInt32(int ordinal, int value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetInt32(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetInt64(int ordinal, long value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetInt64(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetFloat(int ordinal, float value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSingle(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetDouble(int ordinal, double value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetDouble(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetString(int ordinal, string value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetString(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetDecimal(int ordinal, decimal value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetDecimal(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetDateTime(int ordinal, DateTime value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetDateTime(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetTimeSpan(int ordinal, TimeSpan value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetTimeSpan(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetDateTimeOffset(int ordinal, DateTimeOffset value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetDateTimeOffset(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetDBNull(int ordinal)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetDBNull(_eventSink, _recordBuffer, ordinal, true);
        }

        public virtual void SetGuid(int ordinal, Guid value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetGuid(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlBoolean(int ordinal, SqlBoolean value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlBoolean(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlByte(int ordinal, SqlByte value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlByte(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlInt16(int ordinal, SqlInt16 value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlInt16(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlInt32(int ordinal, SqlInt32 value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlInt32(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlInt64(int ordinal, SqlInt64 value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlInt64(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlSingle(int ordinal, SqlSingle value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlSingle(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlDouble(int ordinal, SqlDouble value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlDouble(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlMoney(int ordinal, SqlMoney value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlMoney(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlDateTime(int ordinal, SqlDateTime value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlDateTime(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlXml(int ordinal, SqlXml value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlXml(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlDecimal(int ordinal, SqlDecimal value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlDecimal(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlString(int ordinal, SqlString value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlString(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlBinary(int ordinal, SqlBinary value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlBinary(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlGuid(int ordinal, SqlGuid value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlGuid(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlChars(int ordinal, SqlChars value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlChars(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }

        public virtual void SetSqlBytes(int ordinal, SqlBytes value)
        {
            EnsureSubclassOverride();
            ValueUtilsSmi.SetSqlBytes(_eventSink, _recordBuffer, ordinal, GetSmiMetaData(ordinal), value);
        }


        //
        //  SqlDataRecord public API
        //

        public SqlDataRecord(params SqlMetaData[] metaData)
        {
            // Initial consistency check
            if (null == metaData)
            {
                throw ADP.ArgumentNull(nameof(metaData));
            }

            _columnMetaData = new SqlMetaData[metaData.Length];
            _columnSmiMetaData = new SmiExtendedMetaData[metaData.Length];
            for (int i = 0; i < _columnSmiMetaData.Length; i++)
            {
                if (null == metaData[i])
                {
                    throw ADP.ArgumentNull($"{nameof(metaData)}[{i}]");
                }
                _columnMetaData[i] = metaData[i];
                _columnSmiMetaData[i] = MetaDataUtilsSmi.SqlMetaDataToSmiExtendedMetaData(_columnMetaData[i]);
            }

            _eventSink = new SmiEventSink_Default();

            _recordBuffer = new MemoryRecordBuffer(_columnSmiMetaData);
            _usesStringStorageForXml = true;
            _eventSink.ProcessMessagesAndThrow();
        }

        internal SqlDataRecord(SmiRecordBuffer recordBuffer, params SmiExtendedMetaData[] metaData)
        {
            Debug.Assert(null != recordBuffer, "invalid attempt to instantiate SqlDataRecord with null SmiRecordBuffer");
            Debug.Assert(null != metaData, "invalid attempt to instantiate SqlDataRecord with null SmiExtendedMetaData[]");

            _columnMetaData = new SqlMetaData[metaData.Length];
            _columnSmiMetaData = new SmiExtendedMetaData[metaData.Length];
            for (int i = 0; i < _columnSmiMetaData.Length; i++)
            {
                _columnSmiMetaData[i] = metaData[i];
                _columnMetaData[i] = MetaDataUtilsSmi.SmiExtendedMetaDataToSqlMetaData(_columnSmiMetaData[i]);
            }

            _eventSink = new SmiEventSink_Default();

            _recordBuffer = recordBuffer;
            _eventSink.ProcessMessagesAndThrow();
        }

        //
        //  SqlDataRecord private members
        //
        internal SmiRecordBuffer RecordBuffer
        {  // used by SqlPipe
            get
            {
                return _recordBuffer;
            }
        }


        internal SqlMetaData[] InternalGetMetaData()
        {
            return _columnMetaData;
        }

        internal SmiExtendedMetaData[] InternalGetSmiMetaData()
        {
            return _columnSmiMetaData;
        }

        internal SmiExtendedMetaData GetSmiMetaData(int ordinal)
        {
            return _columnSmiMetaData[ordinal];
        }

        internal void ThrowIfInvalidOrdinal(int ordinal)
        {
            if (0 > ordinal || FieldCount <= ordinal)
            {
                throw ADP.IndexOutOfRange(ordinal);
            }
        }
        private void EnsureSubclassOverride()
        {
            if (null == _recordBuffer)
            {
                throw SQL.SubclassMustOverride();
            }
        }

        IDataReader System.Data.IDataRecord.GetData(int ordinal)
        {
            throw ADP.NotSupported();
        }
    }
}

