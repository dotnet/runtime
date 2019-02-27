// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Data.Common
{
    public abstract partial class DbDataReader
    {
        public bool GetBoolean(string name) => throw null;

        public byte GetByte(string name) => throw null;

        public long GetBytes(string name, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw null;

        public char GetChar(string name) => throw null;

        public long GetChars(string name, long dataOffset, char[] buffer, int bufferOffset, int length) => throw null;

        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public System.Data.Common.DbDataReader GetData(string name) => throw null;

        public string GetDataTypeName(string name) => throw null;

        public System.DateTime GetDateTime(string name) => throw null;

        public decimal GetDecimal(string name) => throw null;

        public double GetDouble(string name) => throw null;

        public System.Type GetFieldType(string name) => throw null;

        public T GetFieldValue<T>(string name) => throw null;

        public float GetFloat(string name) => throw null;

        public System.Guid GetGuid(string name) => throw null;

        public short GetInt16(string name) => throw null;

        public int GetInt32(string name) => throw null;

        public long GetInt64(string name) => throw null;

        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public System.Type GetProviderSpecificFieldType(string name) => throw null;

        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public object GetProviderSpecificValue(string name) => throw null;

        public System.IO.Stream GetStream(string name) => throw null;

        public string GetString(string name) => throw null;

        public System.IO.TextReader GetTextReader(string name) => throw null;

        public object GetValue(string name) => throw null;

        public bool IsDBNull(string name) => throw null;
    }
}
