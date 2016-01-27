// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using Microsoft.SqlServer.Server;
using System.Diagnostics;
using System.Data.SqlTypes;

namespace System.Data.SqlClient
{
    // TdsRecordBufferSetter handles writing a structured value out to a TDS stream
    internal class TdsRecordBufferSetter : SmiRecordBuffer
    {
        #region Fields (private)

        private TdsValueSetter[] _fieldSetters;      // setters for individual fields

        private TdsParserStateObject _stateObj;          // target to write to
        private SmiMetaData _metaData;          // metadata describing value
#if DEBUG
        private const int ReadyForToken = -1;         // must call new/end element next
        private const int EndElementsCalled = -2;     // already called EndElements, can only call Close

        private int _currentField;      // validate that caller sets columns in correct order.
#endif

        #endregion

        #region Exposed Construct and control methods/properties

        internal TdsRecordBufferSetter(TdsParserStateObject stateObj, SmiMetaData md)
        {
            Debug.Assert(SqlDbType.Structured == md.SqlDbType, "Unsupported SqlDbType: " + md.SqlDbType);
            _fieldSetters = new TdsValueSetter[md.FieldMetaData.Count];
            for (int i = 0; i < md.FieldMetaData.Count; i++)
            {
                _fieldSetters[i] = new TdsValueSetter(stateObj, md.FieldMetaData[i]);
            }
            _stateObj = stateObj;
            _metaData = md;
#if DEBUG
            _currentField = ReadyForToken;
#endif
        }

        // TdsRecordBufferSetter supports Setting only
        internal override bool CanGet
        {
            get
            {
                return false;
            }
        }

        internal override bool CanSet
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Setters

        // Set value to null
        //  valid for all types
        public override void SetDBNull(SmiEventSink sink, int ordinal)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetDBNull();
        }

        //  valid for SqlDbType.Bit
        public override void SetBoolean(SmiEventSink sink, int ordinal, Boolean value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetBoolean(value);
        }

        //  valid for SqlDbType.TinyInt
        public override void SetByte(SmiEventSink sink, int ordinal, Byte value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetByte(value);
        }

        // Semantics for SetBytes are to modify existing value, not overwrite
        //  Use in combination with SetLength to ensure overwriting when necessary
        // valid for SqlDbTypes: Binary, VarBinary, Image, Udt, Xml
        //      (VarBinary assumed for variants)
        public override int SetBytes(SmiEventSink sink, int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            CheckWritingToColumn(ordinal);
            return _fieldSetters[ordinal].SetBytes(fieldOffset, buffer, bufferOffset, length);
        }
        public override void SetBytesLength(SmiEventSink sink, int ordinal, long length)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetBytesLength(length);
        }

        // Semantics for SetChars are to modify existing value, not overwrite
        //  Use in combination with SetLength to ensure overwriting when necessary
        // valid for character types: Char, VarChar, Text, NChar, NVarChar, NText
        //      (NVarChar and global clr collation assumed for variants)
        public override int SetChars(SmiEventSink sink, int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            CheckWritingToColumn(ordinal);
            return _fieldSetters[ordinal].SetChars(fieldOffset, buffer, bufferOffset, length);
        }

        public override void SetCharsLength(SmiEventSink sink, int ordinal, long length)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetCharsLength(length);
        }

        // valid for character types: Char, VarChar, Text, NChar, NVarChar, NText
        public override void SetString(SmiEventSink sink, int ordinal, string value, int offset, int length)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetString(value, offset, length);
        }

        // valid for SqlDbType.SmallInt
        public override void SetInt16(SmiEventSink sink, int ordinal, Int16 value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetInt16(value);
        }

        // valid for SqlDbType.Int
        public override void SetInt32(SmiEventSink sink, int ordinal, Int32 value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetInt32(value);
        }

        // valid for SqlDbType.BigInt, SqlDbType.Money, SqlDbType.SmallMoney
        public override void SetInt64(SmiEventSink sink, int ordinal, Int64 value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetInt64(value);
        }

        // valid for SqlDbType.Real
        public override void SetSingle(SmiEventSink sink, int ordinal, Single value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetSingle(value);
        }

        // valid for SqlDbType.Float
        public override void SetDouble(SmiEventSink sink, int ordinal, Double value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetDouble(value);
        }

        // valid for SqlDbType.Numeric (uses SqlDecimal since Decimal cannot hold full range)
        public override void SetSqlDecimal(SmiEventSink sink, int ordinal, SqlDecimal value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetSqlDecimal(value);
        }

        // valid for DateTime, SmallDateTime, Date, DateTime2
        public override void SetDateTime(SmiEventSink sink, int ordinal, DateTime value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetDateTime(value);
        }

        // valid for UniqueIdentifier
        public override void SetGuid(SmiEventSink sink, int ordinal, Guid value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetGuid(value);
        }

        // valid for SqlDbType.Time
        public override void SetTimeSpan(SmiEventSink sink, int ordinal, TimeSpan value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetTimeSpan(value);
        }

        // valid for DateTimeOffset
        public override void SetDateTimeOffset(SmiEventSink sink, int ordinal, DateTimeOffset value)
        {
            CheckSettingColumn(ordinal);
            _fieldSetters[ordinal].SetDateTimeOffset(value);
        }

        // valid for SqlDbType.Variant
        public override void SetVariantMetaData(SmiEventSink sink, int ordinal, SmiMetaData metaData)
        {
            CheckWritingToColumn(ordinal);
            _fieldSetters[ordinal].SetVariantType(metaData);
        }

        // valid for multi-valued types
        internal override void NewElement(SmiEventSink sink)
        {
#if DEBUG
            SkipPossibleDefaultedColumns(ReadyForToken);
            Debug.Assert(ReadyForToken == _currentField, "Not on first or last column!");
#endif

            // For TVP types, write new-row token
            Debug.Assert(_metaData.IsMultiValued, "Unsupported call for single-valued types");
            _stateObj.WriteByte(TdsEnums.TVP_ROW_TOKEN);
#if DEBUG
            _currentField = 0;
#endif
        }

        internal override void EndElements(SmiEventSink sink)
        {
#if DEBUG
            SkipPossibleDefaultedColumns(ReadyForToken);
            Debug.Assert(ReadyForToken == _currentField, "Not on first or last column!");
            Debug.Assert(_metaData.IsMultiValued, "Unsupported call for single-valued types");
#endif
            // For TVP types, write no-more-rows token
            _stateObj.WriteByte(TdsEnums.TVP_END_TOKEN);
#if DEBUG
            _currentField = EndElementsCalled;
#endif
        }


        #endregion

        #region private methods
        [Conditional("DEBUG")]
        private void CheckWritingToColumn(int ordinal)
        {
#if DEBUG
            Debug.Assert(0 <= ordinal, "TdsRecordBufferSetter.CheckWritingToColumn: Targeting invalid column: " + ordinal);
            SkipPossibleDefaultedColumns(ordinal);

            Debug.Assert(0 <= _currentField && _metaData.FieldMetaData.Count > _currentField, "_currentField out of range for setting a column:" + _currentField);
            Debug.Assert(ordinal == _currentField, "Setter called out of order.  Should be " + _currentField + ", but was " + ordinal);
            // Must not write to field with a DefaultFieldsProperty set to true
            Debug.Assert(!((SmiDefaultFieldsProperty)_metaData.ExtendedProperties[SmiPropertySelector.DefaultFields])[ordinal],
                "Attempt to write to a default-valued field: " + ordinal);
#endif
        }

        // Handle logic of skipping default columns
        [Conditional("DEBUG")]
        private void SkipPossibleDefaultedColumns(int targetColumn)
        {
#if DEBUG
            Debug.Assert(targetColumn < _metaData.FieldMetaData.Count && targetColumn >= ReadyForToken, "TdsRecordBufferSetter.SkipPossibleDefaultedColumns: Invalid target column: " + targetColumn);

            // special setup for ReadyForToken as the target
            if (targetColumn == ReadyForToken)
            {
                if (ReadyForToken == _currentField)
                {
                    return;
                }

                // Handle readyfortoken by using count of columns in the loop.            
                targetColumn = _metaData.FieldMetaData.Count;
            }

            // Handle skipping default-valued fields
            while (targetColumn > _currentField)
            {
                // All intermediate fields must be default fields (i.e. have a "true" entry in SmiDefaultFieldsProperty
                Debug.Assert(((SmiDefaultFieldsProperty)_metaData.ExtendedProperties[SmiPropertySelector.DefaultFields])[_currentField],
                    "Skipping a field that was not default: " + _currentField);
                _currentField++;
            }

            if (_metaData.FieldMetaData.Count == _currentField)
            {
                _currentField = ReadyForToken;
            }
#endif
        }

        [Conditional("DEBUG")]
        internal void CheckSettingColumn(int ordinal)
        {
#if DEBUG
            // Make sure target column can be written to.
            CheckWritingToColumn(ordinal);

            _currentField++;
            if (_metaData.FieldMetaData.Count == _currentField)
            {
                _currentField = ReadyForToken;
            }
#endif
        }
        #endregion
    }
}
