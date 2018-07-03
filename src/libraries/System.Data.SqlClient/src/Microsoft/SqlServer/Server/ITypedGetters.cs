// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.SqlTypes;

namespace Microsoft.SqlServer.Server
{
    // Interface for strongly-typed value getters
    internal interface ITypedGetters
    {
        // Null test
        bool IsDBNull(int ordinal);

        // Check what type current sql_variant value is
        SqlDbType GetVariantType(int ordinal);

        // By value accessors  (data copy across the interface boundary implied)
        bool GetBoolean(int ordinal);

        byte GetByte(int ordinal);

        long GetBytes(int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length);

        char GetChar(int ordinal);

        long GetChars(int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length);

        short GetInt16(int ordinal);

        int GetInt32(int ordinal);

        long GetInt64(int ordinal);

        float GetFloat(int ordinal);

        double GetDouble(int ordinal);

        string GetString(int ordinal);

        decimal GetDecimal(int ordinal);

        DateTime GetDateTime(int ordinal);

        Guid GetGuid(int ordinal);

        SqlBoolean GetSqlBoolean(int ordinal);

        SqlByte GetSqlByte(int ordinal);

        SqlInt16 GetSqlInt16(int ordinal);

        SqlInt32 GetSqlInt32(int ordinal);

        SqlInt64 GetSqlInt64(int ordinal);

        SqlSingle GetSqlSingle(int ordinal);

        SqlDouble GetSqlDouble(int ordinal);

        SqlMoney GetSqlMoney(int ordinal);

        SqlDateTime GetSqlDateTime(int ordinal);

        SqlDecimal GetSqlDecimal(int ordinal);

        SqlString GetSqlString(int ordinal);

        SqlBinary GetSqlBinary(int ordinal);

        SqlGuid GetSqlGuid(int ordinal);

        SqlChars GetSqlChars(int ordinal);

        SqlBytes GetSqlBytes(int ordinal);

        SqlXml GetSqlXml(int ordinal);


        // "By reference" accessors
        //    May hook to buffer.
        //    Semantics guarantee is that as long as the object exposing the accessor is not logically
        //    moved to a new set of values and the overall state (open/closed) isn't changed, it will not 
        //    change the underlying value returned and continue to allow access to it.
        //
        //    Example: GetSqlCharsRef called on an event stream value.  The back-end optimizes by re-using
        //        the buffer wrapped by the returned SqlChars on the next row event, but guarantees that it
        //        won't change the value until the next event is consumed.
        //
        //    Simplest way to guarantee this behavior is to simply call the corresponding by-value accessor.
        SqlBytes GetSqlBytesRef(int ordinal);

        SqlChars GetSqlCharsRef(int ordinal);

        SqlXml GetSqlXmlRef(int ordinal);
    }
}
