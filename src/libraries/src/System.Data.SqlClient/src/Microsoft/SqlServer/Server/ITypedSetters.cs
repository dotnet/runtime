// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Data.SqlTypes;

namespace Microsoft.SqlServer.Server
{
    // interface for strongly-typed value setters
    internal interface ITypedSetters
    {
        // By value setters (data copy across the interface boundary implied)
        void SetDBNull(int ordinal);

        void SetBoolean(int ordinal, Boolean value);

        void SetByte(int ordinal, Byte value);

        void SetBytes(int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length);

        void SetChar(int ordinal, char value);

        void SetChars(int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length);

        void SetInt16(int ordinal, Int16 value);

        void SetInt32(int ordinal, Int32 value);

        void SetInt64(int ordinal, Int64 value);

        void SetFloat(int ordinal, Single value);

        void SetDouble(int ordinal, Double value);

        [ObsoleteAttribute("Not supported as of SMI v2.  Will be removed when v1 support dropped.  Use setter with offset.")]
        void SetString(int ordinal, string value);

        // Method introduced as of SMI V2
        void SetString(int ordinal, string value, int offset);

        void SetDecimal(int ordinal, Decimal value);

        void SetDateTime(int ordinal, DateTime value);

        void SetGuid(int ordinal, Guid value);

        void SetSqlBoolean(int ordinal, SqlBoolean value);

        void SetSqlByte(int ordinal, SqlByte value);

        void SetSqlInt16(int ordinal, SqlInt16 value);

        void SetSqlInt32(int ordinal, SqlInt32 value);

        void SetSqlInt64(int ordinal, SqlInt64 value);

        void SetSqlSingle(int ordinal, SqlSingle value);

        void SetSqlDouble(int ordinal, SqlDouble value);

        void SetSqlMoney(int ordinal, SqlMoney value);

        void SetSqlDateTime(int ordinal, SqlDateTime value);

        void SetSqlDecimal(int ordinal, SqlDecimal value);

        [ObsoleteAttribute("Not supported as of SMI v2.  Will be removed when v1 support dropped.  Use setter with offset.")]
        void SetSqlString(int ordinal, SqlString value);

        // Method introduced as of SMI V2
        void SetSqlString(int ordinal, SqlString value, int offset);

        [ObsoleteAttribute("Not supported as of SMI v2.  Will be removed when v1 support dropped.  Use setter with offset.")]
        void SetSqlBinary(int ordinal, SqlBinary value);

        // Method introduced as of SMI V2
        void SetSqlBinary(int ordinal, SqlBinary value, int offset);

        void SetSqlGuid(int ordinal, SqlGuid value);

        [ObsoleteAttribute("Not supported as of SMI v2.  Will be removed when v1 support dropped.  Use setter with offset.")]
        void SetSqlChars(int ordinal, SqlChars value);

        // Method introduced as of SMI V2
        void SetSqlChars(int ordinal, SqlChars value, int offset);

        [ObsoleteAttribute("Not supported as of SMI v2.  Will be removed when v1 support dropped.  Use setter with offset.")]
        void SetSqlBytes(int ordinal, SqlBytes value);

        // Method introduced as of SMI V2
        void SetSqlBytes(int ordinal, SqlBytes value, int offset);

        void SetSqlXml(int ordinal, SqlXml value);
    }
}
