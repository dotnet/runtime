// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    public interface IDataRecord
    {
        int FieldCount { get; }
        object this[int i] { get; }
        object this[string name] { get; }
        string GetName(int i);
        string GetDataTypeName(int i);
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        Type GetFieldType(int i);
        object GetValue(int i);
        int GetValues(object[] values);
        int GetOrdinal(string name);
        bool GetBoolean(int i);
        byte GetByte(int i);
        long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length);
        char GetChar(int i);
        long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length);
        Guid GetGuid(int i);
        short GetInt16(int i);
        int GetInt32(int i);
        long GetInt64(int i);
        float GetFloat(int i);
        double GetDouble(int i);
        string GetString(int i);
        decimal GetDecimal(int i);
        DateTime GetDateTime(int i);
        IDataReader GetData(int i);
        bool IsDBNull(int i);
    }
}
