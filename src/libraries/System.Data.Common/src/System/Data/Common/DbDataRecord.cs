// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Data.Common
{
    public abstract class DbDataRecord : IDataRecord
    {
        protected DbDataRecord()
        {
        }

        public abstract int FieldCount
        {
            get;
        }

        public abstract object this[int i]
        {
            get;
        }

        public abstract object this[string name]
        {
            get;
        }

        public abstract bool GetBoolean(int i);

        public abstract byte GetByte(int i);

        public abstract long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length);

        public abstract char GetChar(int i);

        public abstract long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length);


        virtual protected DbDataReader GetDbDataReader(int i)
        {
            // NOTE: This method is virtual because we're required to implement
            //       it however most providers won't support it. Only the OLE DB 
            //       provider supports it right now, and they can override it.
            throw ADP.NotSupported();
        }

        public abstract string GetDataTypeName(int i);

        public abstract DateTime GetDateTime(int i);

        public abstract Decimal GetDecimal(int i);

        public abstract double GetDouble(int i);

        public abstract Type GetFieldType(int i);

        public abstract float GetFloat(int i);

        public abstract Guid GetGuid(int i);

        public abstract Int16 GetInt16(int i);

        public abstract Int32 GetInt32(int i);

        public abstract Int64 GetInt64(int i);

        public abstract string GetName(int i);

        public abstract int GetOrdinal(string name);

        public abstract string GetString(int i);

        public abstract object GetValue(int i);

        public abstract int GetValues(object[] values);

        public abstract bool IsDBNull(int i);

        public IDataReader GetData(int i)
        {
            return GetDbDataReader(i);
        }
    }
}
