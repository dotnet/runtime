// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    using System;


    public interface IDataRecord
    {

        int FieldCount { get; }

        object this[int i] { get; }

        object this[String name] { get; }

        String GetName(int i);

        String GetDataTypeName(int i);

        Type GetFieldType(int i);

        Object GetValue(int i);

        int GetValues(object[] values);

        int GetOrdinal(string name);

        bool GetBoolean(int i);

        byte GetByte(int i);

        long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length);

        char GetChar(int i);

        long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length);

        Guid GetGuid(int i);

        Int16 GetInt16(int i);

        Int32 GetInt32(int i);

        Int64 GetInt64(int i);

        float GetFloat(int i);

        double GetDouble(int i);

        String GetString(int i);

        Decimal GetDecimal(int i);

        DateTime GetDateTime(int i);

        IDataReader GetData(int i);

        bool IsDBNull(int i);
    }
}
