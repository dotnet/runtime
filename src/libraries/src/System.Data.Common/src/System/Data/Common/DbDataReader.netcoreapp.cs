// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;

namespace System.Data.Common
{
    public abstract partial class DbDataReader
    {
        public bool GetBoolean(string name) => GetBoolean(GetOrdinal(name));

        public byte GetByte(string name) => GetByte(GetOrdinal(name));

        public long GetBytes(string name, long dataOffset, byte[] buffer, int bufferOffset, int length) =>
            GetBytes(GetOrdinal(name), dataOffset, buffer, bufferOffset, length);

        public char GetChar(string name) => GetChar(GetOrdinal(name));

        public long GetChars(string name, long dataOffset, char[] buffer, int bufferOffset, int length) =>
            GetChars(GetOrdinal(name), dataOffset, buffer, bufferOffset, length);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public DbDataReader GetData(string name) => GetData(GetOrdinal(name));

        public string GetDataTypeName(string name) => GetDataTypeName(GetOrdinal(name));

        public DateTime GetDateTime(string name) => GetDateTime(GetOrdinal(name));

        public decimal GetDecimal(string name) => GetDecimal(GetOrdinal(name));

        public double GetDouble(string name) => GetDouble(GetOrdinal(name));

        public Type GetFieldType(string name) => GetFieldType(GetOrdinal(name));

        public T GetFieldValue<T>(string name) => GetFieldValue<T>(GetOrdinal(name));

        public float GetFloat(string name) => GetFloat(GetOrdinal(name));

        public Guid GetGuid(string name) => GetGuid(GetOrdinal(name));

        public short GetInt16(string name) => GetInt16(GetOrdinal(name));

        public int GetInt32(string name) => GetInt32(GetOrdinal(name));

        public long GetInt64(string name) => GetInt64(GetOrdinal(name));

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Type GetProviderSpecificFieldType(string name) => GetProviderSpecificFieldType(GetOrdinal(name));

        [EditorBrowsable(EditorBrowsableState.Never)]
        public object GetProviderSpecificValue(string name) => GetProviderSpecificValue(GetOrdinal(name));

        public Stream GetStream(string name) => GetStream(GetOrdinal(name));

        public string GetString(string name) => GetString(GetOrdinal(name));

        public TextReader GetTextReader(string name) => GetTextReader(GetOrdinal(name));

        public object GetValue(string name) => GetValue(GetOrdinal(name));

        public bool IsDBNull(string name) => IsDBNull(GetOrdinal(name));
    }
}
