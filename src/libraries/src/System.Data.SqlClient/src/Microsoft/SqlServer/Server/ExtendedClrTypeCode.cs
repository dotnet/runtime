// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

namespace Microsoft.SqlServer.Server
{
    internal enum ExtendedClrTypeCode
    {
        Invalid = -1,
        Boolean,                    // System.Boolean
        Byte,                       // System.Byte
        Char,                       // System.Char
        DateTime,                   // System.DateTime
        DBNull,                     // System.DBNull
        Decimal,                    // System.Decimal
        Double,                     // System.Double
        Empty,                      // null reference
        Int16,                      // System.Int16
        Int32,                      // System.Int32
        Int64,                      // System.Int64
        SByte,                      // System.SByte
        Single,                     // System.Single
        String,                     // System.String
        UInt16,                     // System.UInt16
        UInt32,                     // System.UInt32
        UInt64,                     // System.UInt64
        Object,                     // System.Object
        ByteArray,                  // System.ByteArray
        CharArray,                  // System.CharArray
        Guid,                       // System.Guid
        SqlBinary,                  // System.Data.SqlTypes.SqlBinary
        SqlBoolean,                 // System.Data.SqlTypes.SqlBoolean
        SqlByte,                    // System.Data.SqlTypes.SqlByte
        SqlDateTime,                // System.Data.SqlTypes.SqlDateTime
        SqlDouble,                  // System.Data.SqlTypes.SqlDouble
        SqlGuid,                    // System.Data.SqlTypes.SqlGuid
        SqlInt16,                   // System.Data.SqlTypes.SqlInt16
        SqlInt32,                   // System.Data.SqlTypes.SqlInt32
        SqlInt64,                   // System.Data.SqlTypes.SqlInt64
        SqlMoney,                   // System.Data.SqlTypes.SqlMoney
        SqlDecimal,                 // System.Data.SqlTypes.SqlDecimal
        SqlSingle,                  // System.Data.SqlTypes.SqlSingle
        SqlString,                  // System.Data.SqlTypes.SqlString
        SqlChars,                   // System.Data.SqlTypes.SqlChars
        SqlBytes,                   // System.Data.SqlTypes.SqlBytes
        SqlXml,                     // System.Data.SqlTypes.SqlXml
        DataTable,                  // System.Data.DataTable
        DbDataReader,               // System.Data.DbDataReader (SqlDataReader falls under this category)
        IEnumerableOfSqlDataRecord, // System.Collections.Generic.IEnumerable<Microsoft.SqlServer.Server.SqlDataRecord>
        TimeSpan,                   // System.TimeSpan
        DateTimeOffset,             // System.DateTimeOffset
        Stream,                     // System.IO.Stream
        TextReader,                 // System.IO.TextReader
        XmlReader,                  // System.Xml.XmlReader
        Last = XmlReader,           // end-of-enum marker
        First = Boolean,            // beginning-of-enum marker
    };
}

