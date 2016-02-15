// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.ProviderBase;
using System.Diagnostics;

namespace System.Data.Common
{
    internal sealed class DataRecordInternal : DbDataRecord
    {
        private SchemaInfo[] _schemaInfo;
        private object[] _values;
        private FieldNameLookup _fieldNameLookup;

        // copy all runtime data information
        internal DataRecordInternal(SchemaInfo[] schemaInfo, object[] values, FieldNameLookup fieldNameLookup)
        {
            Debug.Assert(null != schemaInfo, "invalid attempt to instantiate DataRecordInternal with null schema information");
            Debug.Assert(null != values, "invalid attempt to instantiate DataRecordInternal with null value[]");
            _schemaInfo = schemaInfo;
            _values = values;
            _fieldNameLookup = fieldNameLookup;
        }

        internal void SetSchemaInfo(SchemaInfo[] schemaInfo)
        {
            Debug.Assert(null == _schemaInfo, "invalid attempt to override DataRecordInternal schema information");
            _schemaInfo = schemaInfo;
        }

        public override int FieldCount
        {
            get
            {
                return _schemaInfo.Length;
            }
        }

        public override int GetValues(object[] values)
        {
            if (null == values)
            {
                throw ADP.ArgumentNull("values");
            }

            int copyLen = (values.Length < _schemaInfo.Length) ? values.Length : _schemaInfo.Length;
            for (int i = 0; i < copyLen; i++)
            {
                values[i] = _values[i];
            }
            return copyLen;
        }

        public override string GetName(int i)
        {
            return _schemaInfo[i].name;
        }


        public override object GetValue(int i)
        {
            return _values[i];
        }

        public override string GetDataTypeName(int i)
        {
            return _schemaInfo[i].typeName;
        }

        public override Type GetFieldType(int i)
        {
            return _schemaInfo[i].type;
        }

        public override int GetOrdinal(string name)
        {
            return _fieldNameLookup.GetOrdinal(name);
        }

        public override object this[int i]
        {
            get
            {
                return GetValue(i);
            }
        }

        public override object this[string name]
        {
            get
            {
                return GetValue(GetOrdinal(name));
            }
        }

        public override bool GetBoolean(int i)
        {
            return ((bool)_values[i]);
        }

        public override byte GetByte(int i)
        {
            return ((byte)_values[i]);
        }

        public override long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length)
        {
            int cbytes = 0;
            int ndataIndex;

            byte[] data = (byte[])_values[i];

            cbytes = data.Length;

            // since arrays can't handle 64 bit values and this interface doesn't
            // allow chunked access to data, a dataIndex outside the rang of Int32
            // is invalid
            if (dataIndex > Int32.MaxValue)
            {
                throw ADP.InvalidSourceBufferIndex(cbytes, dataIndex, "dataIndex");
            }

            ndataIndex = (int)dataIndex;

            // if no buffer is passed in, return the number of characters we have
            if (null == buffer)
                return cbytes;

            try
            {
                if (ndataIndex < cbytes)
                {
                    // help the user out in the case where there's less data than requested
                    if ((ndataIndex + length) > cbytes)
                        cbytes = cbytes - ndataIndex;
                    else
                        cbytes = length;
                }

                // until arrays are 64 bit, we have to do these casts
                Buffer.BlockCopy(data, ndataIndex, buffer, bufferIndex, (int)cbytes);
            }
            catch (Exception e)
            {
                if (ADP.IsCatchableExceptionType(e))
                {
                    cbytes = data.Length;

                    if (length < 0)
                        throw ADP.InvalidDataLength(length);

                    // if bad buffer index, throw
                    if (bufferIndex < 0 || bufferIndex >= buffer.Length)
                        throw ADP.InvalidDestinationBufferIndex(length, bufferIndex, "bufferIndex");

                    // if bad data index, throw
                    if (dataIndex < 0 || dataIndex >= cbytes)
                        throw ADP.InvalidSourceBufferIndex(length, dataIndex, "dataIndex");

                    // if there is not enough room in the buffer for data
                    if (cbytes + bufferIndex > buffer.Length)
                        throw ADP.InvalidBufferSizeOrIndex(cbytes, bufferIndex);
                }

                throw;
            }

            return cbytes;
        }

        public override char GetChar(int i)
        {
            return ((string)_values[i])[0];
        }

        public override long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length)
        {
            var s = (string)_values[i];
            int cchars = s.Length;

            // since arrays can't handle 64 bit values and this interface doesn't
            // allow chunked access to data, a dataIndex outside the rang of Int32
            // is invalid
            if (dataIndex > Int32.MaxValue)
            {
                throw ADP.InvalidSourceBufferIndex(cchars, dataIndex, "dataIndex");
            }

            int ndataIndex = (int)dataIndex;

            // if no buffer is passed in, return the number of characters we have
            if (null == buffer)
                return cchars;

            try
            {
                if (ndataIndex < cchars)
                {
                    // help the user out in the case where there's less data than requested
                    if ((ndataIndex + length) > cchars)
                        cchars = cchars - ndataIndex;
                    else
                        cchars = length;
                }

                s.CopyTo(ndataIndex, buffer, bufferIndex, cchars);
            }
            catch (Exception e)
            {
                if (ADP.IsCatchableExceptionType(e))
                {
                    cchars = s.Length;

                    if (length < 0)
                        throw ADP.InvalidDataLength(length);

                    // if bad buffer index, throw
                    if (bufferIndex < 0 || bufferIndex >= buffer.Length)
                        throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex");

                    // if bad data index, throw
                    if (ndataIndex < 0 || ndataIndex >= cchars)
                        throw ADP.InvalidSourceBufferIndex(cchars, dataIndex, "dataIndex");

                    // if there is not enough room in the buffer for data
                    if (cchars + bufferIndex > buffer.Length)
                        throw ADP.InvalidBufferSizeOrIndex(cchars, bufferIndex);
                }

                throw;
            }

            return cchars;
        }

        public override Guid GetGuid(int i)
        {
            return ((Guid)_values[i]);
        }


        public override Int16 GetInt16(int i)
        {
            return ((Int16)_values[i]);
        }

        public override Int32 GetInt32(int i)
        {
            return ((Int32)_values[i]);
        }

        public override Int64 GetInt64(int i)
        {
            return ((Int64)_values[i]);
        }

        public override float GetFloat(int i)
        {
            return ((float)_values[i]);
        }

        public override double GetDouble(int i)
        {
            return ((double)_values[i]);
        }

        public override string GetString(int i)
        {
            return ((string)_values[i]);
        }

        public override Decimal GetDecimal(int i)
        {
            return ((Decimal)_values[i]);
        }

        public override DateTime GetDateTime(int i)
        {
            return ((DateTime)_values[i]);
        }

        public override bool IsDBNull(int i)
        {
            object o = _values[i];
            return (null == o || o is DBNull);
        }

        //
        // ICustomTypeDescriptor
        //
    }

    // this doesn't change per record, only alloc once
    internal struct SchemaInfo
    {
        public string name;
        public string typeName;
        public Type type;
    }
}
