// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Microsoft.SqlServer.Server
{
    // Utilities for manipulating values with the Smi interface.
    //
    //  THIS CLASS IS BUILT ON TOP OF THE SMI INTERFACE -- SMI SHOULD NOT DEPEND ON IT!
    //
    //  These are all based off of knowing the clr type of the value
    //  as an ExtendedClrTypeCode enum for rapid access (lookup in static array is best, if possible).
    internal static class ValueUtilsSmi
    {
        private const int __maxByteChunkSize = TdsEnums.MAXSIZE;
        private const int __maxCharChunkSize = TdsEnums.MAXSIZE / sizeof(char);
        private const int NoLengthLimit = (int)SmiMetaData.UnlimitedMaxLengthIndicator;  // make sure we use the same constant

        // Constants
        private const int constBinBufferSize = 4096;  // Size of the buffer used to read input parameter of type Stream
        private const int constTextBufferSize = 4096; // Size of the buffer (in chars) user to read input parameter of type TextReader       

        //
        //  User-visible semantics-laden Getter/Setter support methods
        //      These methods implement common semantics for getters & setters
        //      All access to underlying Smi getters/setters must validate parameters
        //      in these methods
        //  

        //  The idea for the getters is that there are two types associated with the field/column,
        //  the one the user asks for (implicitly via a strongly-typed getter) and the one the data 
        //  is stored in (SmiMetaData).
        //  When a strong getter is invoked, we try one of two ways to get the value
        //      1) go directly to the source for the requested type if possible
        //      2) instantiate the value based on the stored type (GetValue), then ask the Clr 
        //          to convert.
        internal static bool IsDBNull(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            return IsDBNull_Unchecked(sink, getters, ordinal);
        }

        internal static bool GetBoolean(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.Boolean))
            {
                return GetBoolean_Unchecked(sink, getters, ordinal);
            }

            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (Boolean)result;
        }

        internal static byte GetByte(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.Byte))
            {
                return GetByte_Unchecked(sink, getters, ordinal);
            }
            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (Byte)result;
        }

        private static long GetBytesConversion(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData, long fieldOffset, byte[] buffer, int bufferOffset, int length, bool throwOnNull)
        {
            object obj = GetSqlValue(sink, getters, ordinal, metaData);
            if (null == obj)
            {
                throw ADP.InvalidCast();
            }
            SqlBinary value = (SqlBinary)obj;

            if (value.IsNull)
            {
                if (throwOnNull)
                {
                    throw SQL.SqlNullValue();
                }
                else
                {
                    // return zero length in any case
                    return 0;
                }
            }

            if (null == buffer)
            {
                return value.Length;
            }

            length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength * sizeof(char), value.Length,
                        fieldOffset, buffer.Length, bufferOffset, length);
            Array.Copy(value.Value, checked((int)fieldOffset), buffer, bufferOffset, length);
            return length;
        }

        internal static long GetBytes(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiExtendedMetaData metaData, long fieldOffset, byte[] buffer, int bufferOffset, int length, bool throwOnNull)
        {
            // Additional exclusions not caught by GetBytesInternal
            if ((SmiMetaData.UnlimitedMaxLengthIndicator != metaData.MaxLength &&
                    (SqlDbType.VarChar == metaData.SqlDbType ||
                     SqlDbType.NVarChar == metaData.SqlDbType ||
                     SqlDbType.Char == metaData.SqlDbType ||
                     SqlDbType.NChar == metaData.SqlDbType)) ||
                    SqlDbType.Xml == metaData.SqlDbType)
            {
                throw SQL.NonBlobColumn(metaData.Name);
            }
            else
            {
                return GetBytesInternal(sink, getters, ordinal, metaData, fieldOffset, buffer, bufferOffset, length, throwOnNull);
            }
        }

        internal static long GetBytesInternal(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData, long fieldOffset, byte[] buffer, int bufferOffset, int length, bool throwOnNull)
        {
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.ByteArray))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    if (throwOnNull)
                    {
                        throw SQL.SqlNullValue();
                    }
                    else
                    {
                        // check user's parameters for validity against a zero-length value
                        CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, 0, fieldOffset, buffer.Length, bufferOffset, length);

                        // return zero length in any case
                        return 0;
                    }
                }
                long actualLength = GetBytesLength_Unchecked(sink, getters, ordinal);
                if (null == buffer)
                {
                    return actualLength;
                }
                if (MetaDataUtilsSmi.IsCharOrXmlType(metaData.SqlDbType))
                {
                    length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength * sizeof(char), actualLength,
                                fieldOffset, buffer.Length, bufferOffset, length);
                }
                else
                {
                    length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, actualLength, fieldOffset, buffer.Length, bufferOffset, length);
                }
                Debug.Assert(length >= 0, "Invalid CheckXetParameters return length!");
                if (length > 0)
                {
                    length = GetBytes_Unchecked(sink, getters, ordinal, fieldOffset, buffer, bufferOffset, length);
                }
                return length;
            }

            return GetBytesConversion(sink, getters, ordinal, metaData, fieldOffset, buffer, bufferOffset, length, throwOnNull);
        }

        internal static long GetChars(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.CharArray))
            {
                long actualLength = GetCharsLength_Unchecked(sink, getters, ordinal);
                if (null == buffer)
                {
                    return actualLength;
                }
                length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, actualLength, fieldOffset, buffer.Length, bufferOffset, length);
                Debug.Assert(length >= 0, "Buffer.Length was invalid!");
                if (length > 0)
                {
                    length = GetChars_Unchecked(sink, getters, ordinal, fieldOffset, buffer, bufferOffset, length);
                }
                return length;
            }

            String value = ((String)GetValue(sink, getters, ordinal, metaData));
            if (null == value)
            {
                throw ADP.InvalidCast();
            }
            if (null == buffer)
            {
                return value.Length;
            }
            length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength * sizeof(char), value.Length,
                        fieldOffset, buffer.Length, bufferOffset, length);
            value.CopyTo(checked((int)fieldOffset), buffer, bufferOffset, length);
            return length;
        }

        internal static DateTime GetDateTime(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.DateTime))
            {
                return GetDateTime_Unchecked(sink, getters, ordinal);
            }
            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (DateTime)result;
        }

        // calling GetDateTimeOffset on possibly v100 SMI
        internal static DateTimeOffset GetDateTimeOffset(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData, bool gettersSupportKatmaiDateTime)
        {
            if (gettersSupportKatmaiDateTime)
            {
                return GetDateTimeOffset(sink, (SmiTypedGetterSetter)getters, ordinal, metaData);
            }
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (DateTimeOffset)result;
        }

        // dealing with v200 SMI
        internal static DateTimeOffset GetDateTimeOffset(SmiEventSink_Default sink, SmiTypedGetterSetter getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.DateTimeOffset))
            {
                return GetDateTimeOffset_Unchecked(sink, getters, ordinal);
            }
            return (DateTimeOffset)GetValue200(sink, getters, ordinal, metaData);
        }

        internal static Decimal GetDecimal(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.Decimal))
            {
                return GetDecimal_PossiblyMoney(sink, getters, ordinal, metaData);
            }
            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (Decimal)result;
        }

        internal static Double GetDouble(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.Double))
            {
                return GetDouble_Unchecked(sink, getters, ordinal);
            }
            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (Double)result;
        }

        internal static Guid GetGuid(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.Guid))
            {
                return GetGuid_Unchecked(sink, getters, ordinal);
            }
            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (Guid)result;
        }

        internal static Int16 GetInt16(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.Int16))
            {
                return GetInt16_Unchecked(sink, getters, ordinal);
            }
            object obj = GetValue(sink, getters, ordinal, metaData);
            if (null == obj)
            {
                throw ADP.InvalidCast();
            }
            return (Int16)obj;
        }

        internal static Int32 GetInt32(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.Int32))
            {
                return GetInt32_Unchecked(sink, getters, ordinal);
            }
            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (Int32)result;
        }

        internal static Int64 GetInt64(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.Int64))
            {
                return GetInt64_Unchecked(sink, getters, ordinal);
            }
            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (Int64)result;
        }

        internal static Single GetSingle(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.Single))
            {
                return GetSingle_Unchecked(sink, getters, ordinal);
            }
            object result = GetValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (Single)result;
        }

        internal static SqlBinary GetSqlBinary(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlBinary))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    return SqlBinary.Null;
                }
                return GetSqlBinary_Unchecked(sink, getters, ordinal);
            }
            object result = GetSqlValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (SqlBinary)result;
        }

        internal static SqlBoolean GetSqlBoolean(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlBoolean))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    return SqlBoolean.Null;
                }
                return new SqlBoolean(GetBoolean_Unchecked(sink, getters, ordinal));
            }
            object result = GetSqlValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (SqlBoolean)result;
        }

        internal static SqlByte GetSqlByte(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlByte))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    return SqlByte.Null;
                }
                return new SqlByte(GetByte_Unchecked(sink, getters, ordinal));
            }
            object result = GetSqlValue(sink, getters, ordinal, metaData);
            if (null == result)
            {
                throw ADP.InvalidCast();
            }
            return (SqlByte)result;
        }

        internal static SqlBytes GetSqlBytes(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlBytes result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlBytes))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlBytes.Null;
                }
                else
                {
                    long length = GetBytesLength_Unchecked(sink, getters, ordinal);
                    if (0 <= length && length < __maxByteChunkSize)
                    {
                        byte[] byteBuffer = GetByteArray_Unchecked(sink, getters, ordinal);
                        result = new SqlBytes(byteBuffer);
                    }
                    else
                    {
                        Stream s = new SmiGettersStream(sink, getters, ordinal, metaData);
                        s = CopyIntoNewSmiScratchStream(s, sink);
                        result = new SqlBytes(s);
                    }
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                SqlBinary binaryVal = (SqlBinary)obj;
                if (binaryVal.IsNull)
                {
                    result = SqlBytes.Null;
                }
                else
                {
                    result = new SqlBytes(binaryVal.Value);
                }
            }

            return result;
        }

        internal static SqlChars GetSqlChars(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlChars result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlChars))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlChars.Null;
                }
                else
                {
                    char[] charBuffer = GetCharArray_Unchecked(sink, getters, ordinal);
                    result = new SqlChars(charBuffer);
                }
            }
            else
            {
                SqlString stringValue;
                if (SqlDbType.Xml == metaData.SqlDbType)
                {
                    SqlXml xmlValue = GetSqlXml_Unchecked(sink, getters, ordinal);

                    if (xmlValue.IsNull)
                    {
                        result = SqlChars.Null;
                    }
                    else
                    {
                        result = new SqlChars(xmlValue.Value.ToCharArray());
                    }
                }
                else
                {
                    object obj = GetSqlValue(sink, getters, ordinal, metaData);
                    if (null == obj)
                    {
                        throw ADP.InvalidCast();
                    }
                    stringValue = (SqlString)obj;

                    if (stringValue.IsNull)
                    {
                        result = SqlChars.Null;
                    }
                    else
                    {
                        result = new SqlChars(stringValue.Value.ToCharArray());
                    }
                }
            }

            return result;
        }

        internal static SqlDateTime GetSqlDateTime(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlDateTime result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlDateTime))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlDateTime.Null;
                }
                else
                {
                    DateTime temp = GetDateTime_Unchecked(sink, getters, ordinal);
                    result = new SqlDateTime(temp);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlDateTime)obj;
            }

            return result;
        }

        internal static SqlDecimal GetSqlDecimal(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlDecimal result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlDecimal))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlDecimal.Null;
                }
                else
                {
                    result = GetSqlDecimal_Unchecked(sink, getters, ordinal);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlDecimal)obj;
            }

            return result;
        }

        internal static SqlDouble GetSqlDouble(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlDouble result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlDouble))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlDouble.Null;
                }
                else
                {
                    Double temp = GetDouble_Unchecked(sink, getters, ordinal);
                    result = new SqlDouble(temp);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlDouble)obj;
            }

            return result;
        }

        internal static SqlGuid GetSqlGuid(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlGuid result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlGuid))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlGuid.Null;
                }
                else
                {
                    Guid temp = GetGuid_Unchecked(sink, getters, ordinal);
                    result = new SqlGuid(temp);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlGuid)obj;
            }

            return result;
        }

        internal static SqlInt16 GetSqlInt16(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlInt16 result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlInt16))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlInt16.Null;
                }
                else
                {
                    Int16 temp = GetInt16_Unchecked(sink, getters, ordinal);
                    result = new SqlInt16(temp);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlInt16)obj;
            }

            return result;
        }

        internal static SqlInt32 GetSqlInt32(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlInt32 result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlInt32))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlInt32.Null;
                }
                else
                {
                    Int32 temp = GetInt32_Unchecked(sink, getters, ordinal);
                    result = new SqlInt32(temp);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlInt32)obj;
            }
            return result;
        }

        internal static SqlInt64 GetSqlInt64(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlInt64 result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlInt64))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlInt64.Null;
                }
                else
                {
                    Int64 temp = GetInt64_Unchecked(sink, getters, ordinal);
                    result = new SqlInt64(temp);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlInt64)obj;
            }

            return result;
        }

        internal static SqlMoney GetSqlMoney(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlMoney result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlMoney))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlMoney.Null;
                }
                else
                {
                    result = GetSqlMoney_Unchecked(sink, getters, ordinal);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlMoney)obj;
            }

            return result;
        }

        internal static SqlSingle GetSqlSingle(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlSingle result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlSingle))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlSingle.Null;
                }
                else
                {
                    Single temp = GetSingle_Unchecked(sink, getters, ordinal);
                    result = new SqlSingle(temp);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlSingle)obj;
            }

            return result;
        }

        internal static SqlString GetSqlString(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlString result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlString))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlString.Null;
                }
                else
                {
                    String temp = GetString_Unchecked(sink, getters, ordinal);
                    result = new SqlString(temp);
                }
            }
            else if (SqlDbType.Xml == metaData.SqlDbType)
            {
                SqlXml xmlValue = GetSqlXml_Unchecked(sink, getters, ordinal);

                if (xmlValue.IsNull)
                {
                    result = SqlString.Null;
                }
                else
                {
                    result = new SqlString(xmlValue.Value);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlString)obj;
            }

            return result;
        }

        internal static SqlXml GetSqlXml(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            SqlXml result;
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.SqlXml))
            {
                if (IsDBNull_Unchecked(sink, getters, ordinal))
                {
                    result = SqlXml.Null;
                }
                else
                {
                    result = GetSqlXml_Unchecked(sink, getters, ordinal);
                }
            }
            else
            {
                object obj = GetSqlValue(sink, getters, ordinal, metaData);
                if (null == obj)
                {
                    throw ADP.InvalidCast();
                }
                result = (SqlXml)obj;
            }

            return result;
        }

        internal static String GetString(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.String))
            {
                return GetString_Unchecked(sink, getters, ordinal);
            }
            object obj = GetValue(sink, getters, ordinal, metaData);
            if (null == obj)
            {
                throw ADP.InvalidCast();
            }
            return (String)obj;
        }


        // dealing with v200 SMI
        internal static TimeSpan GetTimeSpan(SmiEventSink_Default sink, SmiTypedGetterSetter getters, int ordinal, SmiMetaData metaData)
        {
            ThrowIfITypedGettersIsNull(sink, getters, ordinal);
            if (CanAccessGetterDirectly(metaData, ExtendedClrTypeCode.TimeSpan))
            {
                return GetTimeSpan_Unchecked(sink, getters, ordinal);
            }
            return (TimeSpan)GetValue200(sink, getters, ordinal, metaData);
        }

        // GetValue() for v200 SMI (new Katmai Date/Time types)
        internal static object GetValue200(
            SmiEventSink_Default sink,
            SmiTypedGetterSetter getters,
            int ordinal,
            SmiMetaData metaData
            )
        {
            object result = null;
            if (IsDBNull_Unchecked(sink, getters, ordinal))
            {
                result = DBNull.Value;
            }
            else
            {
                switch (metaData.SqlDbType)
                {
                    case SqlDbType.Variant:  // Handle variants specifically for v200, since they could contain v200 types
                        metaData = getters.GetVariantType(sink, ordinal);
                        sink.ProcessMessagesAndThrow();
                        Debug.Assert(SqlDbType.Variant != metaData.SqlDbType, "Variant-within-variant causes endless recursion!");
                        result = GetValue200(sink, getters, ordinal, metaData);
                        break;
                    case SqlDbType.Date:
                    case SqlDbType.DateTime2:
                        result = GetDateTime_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Time:
                        result = GetTimeSpan_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.DateTimeOffset:
                        result = GetDateTimeOffset_Unchecked(sink, getters, ordinal);
                        break;
                    default:
                        result = GetValue(sink, getters, ordinal, metaData);
                        break;
                }
            }

            return result;
        }

        //  implements SqlClient 1.1-compatible GetValue() semantics for everything except output parameters
        internal static object GetValue(
            SmiEventSink_Default sink,
            ITypedGettersV3 getters,
            int ordinal,
            SmiMetaData metaData
            )
        {
            object result = null;
            if (IsDBNull_Unchecked(sink, getters, ordinal))
            {
                result = DBNull.Value;
            }
            else
            {
                switch (metaData.SqlDbType)
                {
                    case SqlDbType.BigInt:
                        result = GetInt64_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Binary:
                        result = GetByteArray_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Bit:
                        result = GetBoolean_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Char:
                        result = GetString_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.DateTime:
                        result = GetDateTime_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Decimal:
                        result = GetSqlDecimal_Unchecked(sink, getters, ordinal).Value;
                        break;
                    case SqlDbType.Float:
                        result = GetDouble_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Image:
                        result = GetByteArray_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Int:
                        result = GetInt32_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Money:
                        result = GetSqlMoney_Unchecked(sink, getters, ordinal).Value;
                        break;
                    case SqlDbType.NChar:
                        result = GetString_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.NText:
                        result = GetString_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.NVarChar:
                        result = GetString_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Real:
                        result = GetSingle_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.UniqueIdentifier:
                        result = GetGuid_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.SmallDateTime:
                        result = GetDateTime_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.SmallInt:
                        result = GetInt16_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.SmallMoney:
                        result = GetSqlMoney_Unchecked(sink, getters, ordinal).Value;
                        break;
                    case SqlDbType.Text:
                        result = GetString_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Timestamp:
                        result = GetByteArray_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.TinyInt:
                        result = GetByte_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.VarBinary:
                        result = GetByteArray_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.VarChar:
                        result = GetString_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Variant:
                        metaData = getters.GetVariantType(sink, ordinal);
                        sink.ProcessMessagesAndThrow();
                        Debug.Assert(SqlDbType.Variant != metaData.SqlDbType, "Variant-within-variant causes endless recursion!");
                        result = GetValue(sink, getters, ordinal, metaData);
                        break;
                    case SqlDbType.Xml:
                        result = GetSqlXml_Unchecked(sink, getters, ordinal).Value;
                        break;
                    case SqlDbType.Udt:
                        result = GetUdt_LengthChecked(sink, getters, ordinal, metaData);
                        break;
                }
            }

            return result;
        }

        // dealing with v200 SMI
        internal static object GetSqlValue200(
            SmiEventSink_Default sink,
            SmiTypedGetterSetter getters,
            int ordinal,
            SmiMetaData metaData
            )
        {
            object result = null;
            if (IsDBNull_Unchecked(sink, getters, ordinal))
            {
                if (SqlDbType.Udt == metaData.SqlDbType)
                {
                    result = NullUdtInstance(metaData);
                }
                else
                {
                    result = s_typeSpecificNullForSqlValue[(int)metaData.SqlDbType];
                }
            }
            else
            {
                switch (metaData.SqlDbType)
                {
                    case SqlDbType.Variant: // Handle variants specifically for v200, since they could contain v200 types
                        metaData = getters.GetVariantType(sink, ordinal);
                        sink.ProcessMessagesAndThrow();
                        Debug.Assert(SqlDbType.Variant != metaData.SqlDbType, "Variant-within-variant causes endless recursion!");
                        result = GetSqlValue200(sink, getters, ordinal, metaData);
                        break;
                    case SqlDbType.Date:
                    case SqlDbType.DateTime2:
                        result = GetDateTime_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Time:
                        result = GetTimeSpan_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.DateTimeOffset:
                        result = GetDateTimeOffset_Unchecked(sink, getters, ordinal);
                        break;
                    default:
                        result = GetSqlValue(sink, getters, ordinal, metaData);
                        break;
                }
            }

            return result;
        }

        //  implements SqlClient 1.1-compatible GetSqlValue() semantics for everything except output parameters
        internal static object GetSqlValue(
            SmiEventSink_Default sink,
            ITypedGettersV3 getters,
            int ordinal,
            SmiMetaData metaData
            )
        {
            object result = null;
            if (IsDBNull_Unchecked(sink, getters, ordinal))
            {
                if (SqlDbType.Udt == metaData.SqlDbType)
                {
                    result = NullUdtInstance(metaData);
                }
                else
                {
                    result = s_typeSpecificNullForSqlValue[(int)metaData.SqlDbType];
                }
            }
            else
            {
                switch (metaData.SqlDbType)
                {
                    case SqlDbType.BigInt:
                        result = new SqlInt64(GetInt64_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.Binary:
                        result = GetSqlBinary_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Bit:
                        result = new SqlBoolean(GetBoolean_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.Char:
                        result = new SqlString(GetString_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.DateTime:
                        result = new SqlDateTime(GetDateTime_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.Decimal:
                        result = GetSqlDecimal_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Float:
                        result = new SqlDouble(GetDouble_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.Image:
                        result = GetSqlBinary_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Int:
                        result = new SqlInt32(GetInt32_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.Money:
                        result = GetSqlMoney_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.NChar:
                        result = new SqlString(GetString_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.NText:
                        result = new SqlString(GetString_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.NVarChar:
                        result = new SqlString(GetString_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.Real:
                        result = new SqlSingle(GetSingle_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.UniqueIdentifier:
                        result = new SqlGuid(GetGuid_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.SmallDateTime:
                        result = new SqlDateTime(GetDateTime_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.SmallInt:
                        result = new SqlInt16(GetInt16_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.SmallMoney:
                        result = GetSqlMoney_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Text:
                        result = new SqlString(GetString_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.Timestamp:
                        result = GetSqlBinary_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.TinyInt:
                        result = new SqlByte(GetByte_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.VarBinary:
                        result = GetSqlBinary_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.VarChar:
                        result = new SqlString(GetString_Unchecked(sink, getters, ordinal));
                        break;
                    case SqlDbType.Variant:
                        metaData = getters.GetVariantType(sink, ordinal);
                        sink.ProcessMessagesAndThrow();
                        Debug.Assert(SqlDbType.Variant != metaData.SqlDbType, "Variant-within-variant causes endless recursion!");
                        result = GetSqlValue(sink, getters, ordinal, metaData);
                        break;
                    case SqlDbType.Xml:
                        result = GetSqlXml_Unchecked(sink, getters, ordinal);
                        break;
                    case SqlDbType.Udt:
                        result = GetUdt_LengthChecked(sink, getters, ordinal, metaData);
                        break;
                }
            }

            return result;
        }

        // null return values for SqlClient 1.1-compatible GetSqlValue()
        private static object[] s_typeSpecificNullForSqlValue = {
            SqlInt64.Null,      // SqlDbType.BigInt
            SqlBinary.Null,     // SqlDbType.Binary
            SqlBoolean.Null,    // SqlDbType.Bit
            SqlString.Null,     // SqlDbType.Char
            SqlDateTime.Null,   // SqlDbType.DateTime
            SqlDecimal.Null,    // SqlDbType.Decimal
            SqlDouble.Null,     // SqlDbType.Float
            SqlBinary.Null,     // SqlDbType.Image
            SqlInt32.Null,      // SqlDbType.Int
            SqlMoney.Null,      // SqlDbType.Money
            SqlString.Null,     // SqlDbType.NChar
            SqlString.Null,     // SqlDbType.NText
            SqlString.Null,     // SqlDbType.NVarChar
            SqlSingle.Null,     // SqlDbType.Real
            SqlGuid.Null,       // SqlDbType.UniqueIdentifier
            SqlDateTime.Null,   // SqlDbType.SmallDateTime
            SqlInt16.Null,      // SqlDbType.SmallInt
            SqlMoney.Null,      // SqlDbType.SmallMoney
            SqlString.Null,     // SqlDbType.Text
            SqlBinary.Null,     // SqlDbType.Timestamp
            SqlByte.Null,       // SqlDbType.TinyInt
            SqlBinary.Null,     // SqlDbType.VarBinary
            SqlString.Null,     // SqlDbType.VarChar
            DBNull.Value,       // SqlDbType.Variant
            null,               // 24
            SqlXml.Null,        // SqlDbType.Xml
            null,               // 26
            null,               // 27
            null,               // 28
            null,               // SqlDbType.Udt -- requires instantiating udt-specific type
            null,               // SqlDbType.Structured
            DBNull.Value,       // SqlDbType.Date
            DBNull.Value,       // SqlDbType.Time
            DBNull.Value,       // SqlDbType.DateTime2
            DBNull.Value,       // SqlDbType.DateTimeOffset
        };

        internal static object NullUdtInstance(SmiMetaData metaData)
        {
            Type t = metaData.Type;
            Debug.Assert(t != null, "Unexpected null of Udt type on NullUdtInstance!");
            return t.InvokeMember("Null", BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Static, null, null, new Object[] { }, CultureInfo.InvariantCulture);
        }

        // Strongly-typed setters are a bit simpler than their corresponding getters.
        //      1) check to make sure the type is compatible (exception if not)
        //      2) push the data
        internal static void SetDBNull(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Boolean value)
        {
            SetDBNull_Unchecked(sink, setters, ordinal);
        }

        internal static void SetBoolean(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Boolean value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.Boolean);

            SetBoolean_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetByte(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Byte value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.Byte);

            SetByte_Unchecked(sink, setters, ordinal, value);
        }

        internal static long SetBytes(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.ByteArray);
            if (null == buffer)
            {
                throw ADP.ArgumentNull(nameof(buffer));
            }
            length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, fieldOffset, buffer.Length, bufferOffset, length);
            Debug.Assert(length >= 0, "Buffer.Length was invalid!");
            if (0 == length)
            {
                // Front end semantics says to ignore fieldOffset and bufferOffset
                //  if not doing any actual work.
                // Back end semantics says they must be valid, even for no work.
                // Compensate by setting offsets to zero here (valid for any scenario)
                fieldOffset = 0;
                bufferOffset = 0;
            }
            return SetBytes_Unchecked(sink, setters, ordinal, fieldOffset, buffer, bufferOffset, length);
        }

        internal static long SetBytesLength(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, long length)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.ByteArray);

            if (length < 0)
            {
                throw ADP.InvalidDataLength(length);
            }

            if (metaData.MaxLength >= 0 && length > metaData.MaxLength)
            {
                length = metaData.MaxLength;
            }

            setters.SetBytesLength(sink, ordinal, length);
            sink.ProcessMessagesAndThrow();

            return length;
        }

        internal static long SetChars(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.CharArray);
            if (null == buffer)
            {
                throw ADP.ArgumentNull(nameof(buffer));
            }
            length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, fieldOffset, buffer.Length, bufferOffset, length);
            Debug.Assert(length >= 0, "Buffer.Length was invalid!");
            if (0 == length)
            {
                // Front end semantics says to ignore fieldOffset and bufferOffset
                //  if not doing any actual work.
                // Back end semantics says they must be valid, even for no work.
                // Compensate by setting offsets to zero here (valid for any scenario)
                fieldOffset = 0;
                bufferOffset = 0;
            }
            return SetChars_Unchecked(sink, setters, ordinal, fieldOffset, buffer, bufferOffset, length);
        }

        internal static void SetDateTime(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, DateTime value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.DateTime);

            SetDateTime_Checked(sink, setters, ordinal, metaData, value);
        }

        internal static void SetDateTimeOffset(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, DateTimeOffset value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.DateTimeOffset);
            SetDateTimeOffset_Unchecked(sink, (SmiTypedGetterSetter)setters, ordinal, value);
        }

        internal static void SetDecimal(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Decimal value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.Decimal);

            SetDecimal_PossiblyMoney(sink, setters, ordinal, metaData, value);
        }

        internal static void SetDouble(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Double value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.Double);

            SetDouble_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetGuid(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Guid value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.Guid);

            SetGuid_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetInt16(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Int16 value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.Int16);

            SetInt16_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetInt32(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Int32 value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.Int32);

            SetInt32_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetInt64(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Int64 value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.Int64);

            SetInt64_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSingle(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Single value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.Single);

            SetSingle_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlBinary(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlBinary value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlBinary);
            SetSqlBinary_LengthChecked(sink, setters, ordinal, metaData, value, 0);
        }

        internal static void SetSqlBoolean(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlBoolean value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlBoolean);

            SetSqlBoolean_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlByte(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlByte value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlByte);

            SetSqlByte_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlBytes(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlBytes value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlBytes);

            SetSqlBytes_LengthChecked(sink, setters, ordinal, metaData, value, 0);
        }

        internal static void SetSqlChars(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlChars value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlChars);
            SetSqlChars_LengthChecked(sink, setters, ordinal, metaData, value, 0);
        }

        internal static void SetSqlDateTime(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlDateTime value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlDateTime);

            SetSqlDateTime_Checked(sink, setters, ordinal, metaData, value);
        }

        internal static void SetSqlDecimal(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlDecimal value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlDecimal);

            SetSqlDecimal_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlDouble(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlDouble value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlDouble);

            SetSqlDouble_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlGuid(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlGuid value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlGuid);

            SetSqlGuid_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlInt16(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlInt16 value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlInt16);

            SetSqlInt16_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlInt32(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlInt32 value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlInt32);

            SetSqlInt32_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlInt64(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlInt64 value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlInt64);

            SetSqlInt64_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlMoney(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlMoney value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlMoney);

            SetSqlMoney_Checked(sink, setters, ordinal, metaData, value);
        }

        internal static void SetSqlSingle(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlSingle value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlSingle);

            SetSqlSingle_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetSqlString(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlString value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlString);
            SetSqlString_LengthChecked(sink, setters, ordinal, metaData, value, 0);
        }

        internal static void SetSqlXml(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlXml value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.SqlXml);

            SetSqlXml_Unchecked(sink, setters, ordinal, value);
        }

        internal static void SetString(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, String value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.String);

            SetString_LengthChecked(sink, setters, ordinal, metaData, value, 0);
        }

        internal static void SetTimeSpan(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, TimeSpan value)
        {
            ThrowIfInvalidSetterAccess(metaData, ExtendedClrTypeCode.TimeSpan);
            SetTimeSpan_Checked(sink, (SmiTypedGetterSetter)setters, ordinal, metaData, value);
        }

        //  Implements SqlClient 2.0-compatible SetValue() semantics
        //      Assumes caller already validated type against the metadata, other than trimming lengths
        internal static void SetCompatibleValue(
            SmiEventSink_Default sink,
            ITypedSettersV3 setters,
            int ordinal,
            SmiMetaData metaData,       // metadata for target setter column
            object value,
            ExtendedClrTypeCode typeCode,
            int offset
            )
        {
            // Ensure either an invalid type, or caller validated compatibility
            // SqlDbType.Variant and have special handling
            Debug.Assert(typeCode == ExtendedClrTypeCode.Invalid ||
                            typeCode == ExtendedClrTypeCode.SByte ||
                            typeCode == ExtendedClrTypeCode.UInt16 ||
                            typeCode == ExtendedClrTypeCode.UInt32 ||
                            typeCode == ExtendedClrTypeCode.UInt64 ||
                            typeCode == ExtendedClrTypeCode.DBNull ||
                            typeCode == ExtendedClrTypeCode.Empty ||
                            CanAccessSetterDirectly(metaData, typeCode) ||
                            value is DataFeed /* already validated */);

            switch (typeCode)
            {
                case ExtendedClrTypeCode.Invalid: throw ADP.UnknownDataType(value.GetType());
                case ExtendedClrTypeCode.Boolean: SetBoolean_Unchecked(sink, setters, ordinal, (Boolean)value); break;
                case ExtendedClrTypeCode.Byte: SetByte_Unchecked(sink, setters, ordinal, (Byte)value); break;
                case ExtendedClrTypeCode.Char:
                    {
                        char[] charsValue = new char[] { (char)value };
                        // recur with array type
                        SetCompatibleValue(sink, setters, ordinal, metaData, charsValue, ExtendedClrTypeCode.CharArray, 0);
                        break;
                    }
                case ExtendedClrTypeCode.DateTime: SetDateTime_Checked(sink, setters, ordinal, metaData, (DateTime)value); break;
                case ExtendedClrTypeCode.DBNull: SetDBNull_Unchecked(sink, setters, ordinal); break;
                case ExtendedClrTypeCode.Decimal: SetDecimal_PossiblyMoney(sink, setters, ordinal, metaData, (Decimal)value); break;
                case ExtendedClrTypeCode.Double: SetDouble_Unchecked(sink, setters, ordinal, (Double)value); break;
                case ExtendedClrTypeCode.Empty: SetDBNull_Unchecked(sink, setters, ordinal); break;
                case ExtendedClrTypeCode.Int16: SetInt16_Unchecked(sink, setters, ordinal, (Int16)value); break;
                case ExtendedClrTypeCode.Int32: SetInt32_Unchecked(sink, setters, ordinal, (Int32)value); break;
                case ExtendedClrTypeCode.Int64: SetInt64_Unchecked(sink, setters, ordinal, (Int64)value); break;
                case ExtendedClrTypeCode.SByte: throw ADP.InvalidCast();
                case ExtendedClrTypeCode.Single: SetSingle_Unchecked(sink, setters, ordinal, (Single)value); break;
                case ExtendedClrTypeCode.String: SetString_LengthChecked(sink, setters, ordinal, metaData, (string)value, offset); break;
                case ExtendedClrTypeCode.UInt16: throw ADP.InvalidCast();
                case ExtendedClrTypeCode.UInt32: throw ADP.InvalidCast();
                case ExtendedClrTypeCode.UInt64: throw ADP.InvalidCast();
                case ExtendedClrTypeCode.Object: SetUdt_LengthChecked(sink, setters, ordinal, metaData, value); break;
                case ExtendedClrTypeCode.ByteArray: SetByteArray_LengthChecked(sink, setters, ordinal, metaData, (byte[])value, offset); break;
                case ExtendedClrTypeCode.CharArray: SetCharArray_LengthChecked(sink, setters, ordinal, metaData, (char[])value, offset); break;
                case ExtendedClrTypeCode.Guid: SetGuid_Unchecked(sink, setters, ordinal, (Guid)value); break;
                case ExtendedClrTypeCode.SqlBinary: SetSqlBinary_LengthChecked(sink, setters, ordinal, metaData, (SqlBinary)value, offset); break;
                case ExtendedClrTypeCode.SqlBoolean: SetSqlBoolean_Unchecked(sink, setters, ordinal, (SqlBoolean)value); break;
                case ExtendedClrTypeCode.SqlByte: SetSqlByte_Unchecked(sink, setters, ordinal, (SqlByte)value); break;
                case ExtendedClrTypeCode.SqlDateTime: SetSqlDateTime_Checked(sink, setters, ordinal, metaData, (SqlDateTime)value); break;
                case ExtendedClrTypeCode.SqlDouble: SetSqlDouble_Unchecked(sink, setters, ordinal, (SqlDouble)value); break;
                case ExtendedClrTypeCode.SqlGuid: SetSqlGuid_Unchecked(sink, setters, ordinal, (SqlGuid)value); break;
                case ExtendedClrTypeCode.SqlInt16: SetSqlInt16_Unchecked(sink, setters, ordinal, (SqlInt16)value); break;
                case ExtendedClrTypeCode.SqlInt32: SetSqlInt32_Unchecked(sink, setters, ordinal, (SqlInt32)value); break;
                case ExtendedClrTypeCode.SqlInt64: SetSqlInt64_Unchecked(sink, setters, ordinal, (SqlInt64)value); break;
                case ExtendedClrTypeCode.SqlMoney: SetSqlMoney_Checked(sink, setters, ordinal, metaData, (SqlMoney)value); break;
                case ExtendedClrTypeCode.SqlDecimal: SetSqlDecimal_Unchecked(sink, setters, ordinal, (SqlDecimal)value); break;
                case ExtendedClrTypeCode.SqlSingle: SetSqlSingle_Unchecked(sink, setters, ordinal, (SqlSingle)value); break;
                case ExtendedClrTypeCode.SqlString: SetSqlString_LengthChecked(sink, setters, ordinal, metaData, (SqlString)value, offset); break;
                case ExtendedClrTypeCode.SqlChars: SetSqlChars_LengthChecked(sink, setters, ordinal, metaData, (SqlChars)value, offset); break;
                case ExtendedClrTypeCode.SqlBytes: SetSqlBytes_LengthChecked(sink, setters, ordinal, metaData, (SqlBytes)value, offset); break;
                case ExtendedClrTypeCode.SqlXml: SetSqlXml_Unchecked(sink, setters, ordinal, (SqlXml)value); break;
                case ExtendedClrTypeCode.Stream: SetStream_Unchecked(sink, setters, ordinal, metaData, (StreamDataFeed)value); break;
                case ExtendedClrTypeCode.TextReader: SetTextReader_Unchecked(sink, setters, ordinal, metaData, (TextDataFeed)value); break;
                case ExtendedClrTypeCode.XmlReader: SetXmlReader_Unchecked(sink, setters, ordinal, ((XmlDataFeed)value)._source); break;
                default:
                    Debug.Assert(false, "Unvalidated extendedtypecode: " + typeCode);
                    break;
            }
        }

        // VSTFDevDiv#479681 - Data corruption when sending Katmai Date types to the server via TVP
        // Ensures proper handling on DateTime2 sub type for Sql_Variants and TVPs.
        internal static void SetCompatibleValueV200(
            SmiEventSink_Default sink,
            SmiTypedGetterSetter setters,
            int ordinal,
            SmiMetaData metaData,
            object value,
            ExtendedClrTypeCode typeCode,
            int offset,
            int length,
            ParameterPeekAheadValue peekAhead,
            SqlBuffer.StorageType storageType
            )
        {
            // Ensure caller validated compatibility for types handled directly in this method
            Debug.Assert((ExtendedClrTypeCode.DataTable != typeCode &&
                            ExtendedClrTypeCode.DbDataReader != typeCode &&
                            ExtendedClrTypeCode.IEnumerableOfSqlDataRecord != typeCode) ||
                        CanAccessSetterDirectly(metaData, typeCode), "Un-validated type '" + typeCode + "' for metaData: " + metaData.SqlDbType);

            if (typeCode == ExtendedClrTypeCode.DateTime)
            {
                if (storageType == SqlBuffer.StorageType.DateTime2)
                    SetDateTime2_Checked(sink, setters, ordinal, metaData, (DateTime)value);
                else if (storageType == SqlBuffer.StorageType.Date)
                    SetDate_Checked(sink, setters, ordinal, metaData, (DateTime)value);
                else
                    SetDateTime_Checked(sink, setters, ordinal, metaData, (DateTime)value);
            }
            else
            {
                SetCompatibleValueV200(sink, setters, ordinal, metaData, value, typeCode, offset, length, peekAhead);
            }
        }

        //  Implements SqlClient 2.0-compatible SetValue() semantics + Orcas extensions
        //      Assumes caller already validated basic type against the metadata, other than trimming lengths and 
        //      checking individual field values (TVPs)
        internal static void SetCompatibleValueV200(
            SmiEventSink_Default sink,
            SmiTypedGetterSetter setters,
            int ordinal,
            SmiMetaData metaData,
            object value,
            ExtendedClrTypeCode typeCode,
            int offset,
            int length,
            ParameterPeekAheadValue peekAhead
            )
        {
            // Ensure caller validated compatibility for types handled directly in this method
            Debug.Assert((ExtendedClrTypeCode.DataTable != typeCode &&
                            ExtendedClrTypeCode.DbDataReader != typeCode &&
                            ExtendedClrTypeCode.IEnumerableOfSqlDataRecord != typeCode) ||
                        CanAccessSetterDirectly(metaData, typeCode), "Un-validated type '" + typeCode + "' for metaData: " + metaData.SqlDbType);

            switch (typeCode)
            {
                case ExtendedClrTypeCode.DataTable:
                    SetDataTable_Unchecked(sink, setters, ordinal, metaData, (DataTable)value);
                    break;
                case ExtendedClrTypeCode.DbDataReader:
                    SetDbDataReader_Unchecked(sink, setters, ordinal, metaData, (DbDataReader)value);
                    break;
                case ExtendedClrTypeCode.IEnumerableOfSqlDataRecord:
                    SetIEnumerableOfSqlDataRecord_Unchecked(sink, setters, ordinal, metaData, (IEnumerable<SqlDataRecord>)value, peekAhead);
                    break;
                case ExtendedClrTypeCode.TimeSpan:
                    SetTimeSpan_Checked(sink, setters, ordinal, metaData, (TimeSpan)value);
                    break;
                case ExtendedClrTypeCode.DateTimeOffset:
                    SetDateTimeOffset_Unchecked(sink, setters, ordinal, (DateTimeOffset)value);
                    break;
                default:
                    SetCompatibleValue(sink, setters, ordinal, metaData, value, typeCode, offset);
                    break;
            }
        }

        private static void SetDataTable_Unchecked(
                SmiEventSink_Default sink,
                SmiTypedGetterSetter setters,
                int ordinal,
                SmiMetaData metaData,
                DataTable value
            )
        {
            // Get the target gettersetter
            setters = setters.GetTypedGetterSetter(sink, ordinal);
            sink.ProcessMessagesAndThrow();

            // iterate over all records
            //  if first record was obtained earlier, use it prior to pulling more
            ExtendedClrTypeCode[] cellTypes = new ExtendedClrTypeCode[metaData.FieldMetaData.Count];
            for (int i = 0; i < metaData.FieldMetaData.Count; i++)
            {
                cellTypes[i] = ExtendedClrTypeCode.Invalid;
            }
            foreach (DataRow row in value.Rows)
            {
                setters.NewElement(sink);
                sink.ProcessMessagesAndThrow();

                // Set all columns in the record
                for (int i = 0; i < metaData.FieldMetaData.Count; i++)
                {
                    SmiMetaData fieldMetaData = metaData.FieldMetaData[i];
                    if (row.IsNull(i))
                    {
                        SetDBNull_Unchecked(sink, setters, i);
                    }
                    else
                    {
                        object cellValue = row[i];

                        // Only determine cell types for first row, to save expensive 
                        if (ExtendedClrTypeCode.Invalid == cellTypes[i])
                        {
                            cellTypes[i] = MetaDataUtilsSmi.DetermineExtendedTypeCodeForUseWithSqlDbType(
                                    fieldMetaData.SqlDbType, fieldMetaData.IsMultiValued, cellValue, fieldMetaData.Type);
                        }
                        SetCompatibleValueV200(sink, setters, i, fieldMetaData, cellValue, cellTypes[i], 0, NoLengthLimit, null);
                    }
                }
            }

            setters.EndElements(sink);
            sink.ProcessMessagesAndThrow();
        }

        // Copy multiple fields from reader to ITypedSettersV3
        //  Assumes caller enforces that reader and setter metadata are compatible
        internal static void FillCompatibleITypedSettersFromReader(SmiEventSink_Default sink, ITypedSettersV3 setters, SmiMetaData[] metaData, SqlDataReader reader)
        {
            for (int i = 0; i < metaData.Length; i++)
            {
                if (reader.IsDBNull(i))
                {
                    ValueUtilsSmi.SetDBNull_Unchecked(sink, setters, i);
                }
                else
                {
                    switch (metaData[i].SqlDbType)
                    {
                        case SqlDbType.BigInt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Int64));
                            ValueUtilsSmi.SetInt64_Unchecked(sink, setters, i, reader.GetInt64(i));
                            break;
                        case SqlDbType.Binary:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            ValueUtilsSmi.SetSqlBytes_LengthChecked(sink, setters, i, metaData[i], reader.GetSqlBytes(i), 0);
                            break;
                        case SqlDbType.Bit:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Boolean));
                            SetBoolean_Unchecked(sink, setters, i, reader.GetBoolean(i));
                            break;
                        case SqlDbType.Char:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlChars));
                            SetSqlChars_LengthChecked(sink, setters, i, metaData[i], reader.GetSqlChars(i), 0);
                            break;
                        case SqlDbType.DateTime:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTime));
                            SetDateTime_Checked(sink, setters, i, metaData[i], reader.GetDateTime(i));
                            break;
                        case SqlDbType.Decimal:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlDecimal));
                            SetSqlDecimal_Unchecked(sink, setters, i, reader.GetSqlDecimal(i));
                            break;
                        case SqlDbType.Float:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Double));
                            SetDouble_Unchecked(sink, setters, i, reader.GetDouble(i));
                            break;
                        case SqlDbType.Image:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            SetSqlBytes_LengthChecked(sink, setters, i, metaData[i], reader.GetSqlBytes(i), 0);
                            break;
                        case SqlDbType.Int:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Int32));
                            SetInt32_Unchecked(sink, setters, i, reader.GetInt32(i));
                            break;
                        case SqlDbType.Money:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlMoney));
                            SetSqlMoney_Unchecked(sink, setters, i, metaData[i], reader.GetSqlMoney(i));
                            break;
                        case SqlDbType.NChar:
                        case SqlDbType.NText:
                        case SqlDbType.NVarChar:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlChars));
                            SetSqlChars_LengthChecked(sink, setters, i, metaData[i], reader.GetSqlChars(i), 0);
                            break;
                        case SqlDbType.Real:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Single));
                            SetSingle_Unchecked(sink, setters, i, reader.GetFloat(i));
                            break;
                        case SqlDbType.UniqueIdentifier:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Guid));
                            SetGuid_Unchecked(sink, setters, i, reader.GetGuid(i));
                            break;
                        case SqlDbType.SmallDateTime:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTime));
                            SetDateTime_Checked(sink, setters, i, metaData[i], reader.GetDateTime(i));
                            break;
                        case SqlDbType.SmallInt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Int16));
                            SetInt16_Unchecked(sink, setters, i, reader.GetInt16(i));
                            break;
                        case SqlDbType.SmallMoney:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlMoney));
                            SetSqlMoney_Checked(sink, setters, i, metaData[i], reader.GetSqlMoney(i));
                            break;
                        case SqlDbType.Text:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlChars));
                            SetSqlChars_LengthChecked(sink, setters, i, metaData[i], reader.GetSqlChars(i), 0);
                            break;
                        case SqlDbType.Timestamp:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            SetSqlBytes_LengthChecked(sink, setters, i, metaData[i], reader.GetSqlBytes(i), 0);
                            break;
                        case SqlDbType.TinyInt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Byte));
                            SetByte_Unchecked(sink, setters, i, reader.GetByte(i));
                            break;
                        case SqlDbType.VarBinary:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            SetSqlBytes_LengthChecked(sink, setters, i, metaData[i], reader.GetSqlBytes(i), 0);
                            break;
                        case SqlDbType.VarChar:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.String));
                            SetSqlChars_LengthChecked(sink, setters, i, metaData[i], reader.GetSqlChars(i), 0);
                            break;
                        case SqlDbType.Xml:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlXml));
                            SetSqlXml_Unchecked(sink, setters, i, reader.GetSqlXml(i));
                            break;
                        case SqlDbType.Variant:
                            object o = reader.GetSqlValue(i);
                            ExtendedClrTypeCode typeCode = MetaDataUtilsSmi.DetermineExtendedTypeCode(o);
                            SetCompatibleValue(sink, setters, i, metaData[i], o, typeCode, 0);
                            break;

                        case SqlDbType.Udt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            SetSqlBytes_LengthChecked(sink, setters, i, metaData[i], reader.GetSqlBytes(i), 0);
                            break;

                        default:
                            // In order for us to get here we would have to have an 
                            // invalid instance of SqlDbType, or one would have to add 
                            // new member to SqlDbType without adding a case in this 
                            // switch, hence the assert.
                            Debug.Assert(false, "unsupported DbType:" + metaData[i].SqlDbType.ToString());
                            throw ADP.NotSupported();
                    }
                }
            }
        }

        // Copy multiple fields from reader to SmiTypedGetterSetter
        //  Supports V200 code path, without damaging backward compat for V100 code.
        //  Main differences are supporting DbDataReader, and for binary, character, decimal and Udt types.
        //  Assumes caller enforces that reader and setter metadata are compatible
        internal static void FillCompatibleSettersFromReader(SmiEventSink_Default sink, SmiTypedGetterSetter setters, IList<SmiExtendedMetaData> metaData, DbDataReader reader)
        {
            for (int i = 0; i < metaData.Count; i++)
            {
                if (reader.IsDBNull(i))
                {
                    ValueUtilsSmi.SetDBNull_Unchecked(sink, setters, i);
                }
                else
                {
                    switch (metaData[i].SqlDbType)
                    {
                        case SqlDbType.BigInt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Int64));
                            SetInt64_Unchecked(sink, setters, i, reader.GetInt64(i));
                            break;
                        case SqlDbType.Binary:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.ByteArray));
                            SetBytes_FromReader(sink, setters, i, metaData[i], reader, 0);
                            break;
                        case SqlDbType.Bit:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Boolean));
                            SetBoolean_Unchecked(sink, setters, i, reader.GetBoolean(i));
                            break;
                        case SqlDbType.Char:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.CharArray));
                            SetCharsOrString_FromReader(sink, setters, i, metaData[i], reader, 0);
                            break;
                        case SqlDbType.DateTime:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTime));
                            SetDateTime_Checked(sink, setters, i, metaData[i], reader.GetDateTime(i));
                            break;
                        case SqlDbType.Decimal:
                            { // block to scope sqlReader local to avoid conflicts
                                Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlDecimal));
                                // Support full fidelity for SqlDataReader
                                SqlDataReader sqlReader = reader as SqlDataReader;
                                if (null != sqlReader)
                                {
                                    SetSqlDecimal_Unchecked(sink, setters, i, sqlReader.GetSqlDecimal(i));
                                }
                                else
                                {
                                    SetSqlDecimal_Unchecked(sink, setters, i, new SqlDecimal(reader.GetDecimal(i)));
                                }
                            }
                            break;
                        case SqlDbType.Float:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Double));
                            SetDouble_Unchecked(sink, setters, i, reader.GetDouble(i));
                            break;
                        case SqlDbType.Image:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.ByteArray));
                            SetBytes_FromReader(sink, setters, i, metaData[i], reader, 0);
                            break;
                        case SqlDbType.Int:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Int32));
                            SetInt32_Unchecked(sink, setters, i, reader.GetInt32(i));
                            break;
                        case SqlDbType.Money:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlMoney));
                            SetSqlMoney_Checked(sink, setters, i, metaData[i], new SqlMoney(reader.GetDecimal(i)));
                            break;
                        case SqlDbType.NChar:
                        case SqlDbType.NText:
                        case SqlDbType.NVarChar:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.CharArray));
                            SetCharsOrString_FromReader(sink, setters, i, metaData[i], reader, 0);
                            break;
                        case SqlDbType.Real:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Single));
                            SetSingle_Unchecked(sink, setters, i, reader.GetFloat(i));
                            break;
                        case SqlDbType.UniqueIdentifier:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Guid));
                            SetGuid_Unchecked(sink, setters, i, reader.GetGuid(i));
                            break;
                        case SqlDbType.SmallDateTime:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTime));
                            SetDateTime_Checked(sink, setters, i, metaData[i], reader.GetDateTime(i));
                            break;
                        case SqlDbType.SmallInt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Int16));
                            SetInt16_Unchecked(sink, setters, i, reader.GetInt16(i));
                            break;
                        case SqlDbType.SmallMoney:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlMoney));
                            SetSqlMoney_Checked(sink, setters, i, metaData[i], new SqlMoney(reader.GetDecimal(i)));
                            break;
                        case SqlDbType.Text:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.CharArray));
                            SetCharsOrString_FromReader(sink, setters, i, metaData[i], reader, 0);
                            break;
                        case SqlDbType.Timestamp:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.ByteArray));
                            SetBytes_FromReader(sink, setters, i, metaData[i], reader, 0);
                            break;
                        case SqlDbType.TinyInt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Byte));
                            SetByte_Unchecked(sink, setters, i, reader.GetByte(i));
                            break;
                        case SqlDbType.VarBinary:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.ByteArray));
                            SetBytes_FromReader(sink, setters, i, metaData[i], reader, 0);
                            break;
                        case SqlDbType.VarChar:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.String));
                            SetCharsOrString_FromReader(sink, setters, i, metaData[i], reader, 0);
                            break;
                        case SqlDbType.Xml:
                            {
                                Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlXml));
                                SqlDataReader sqlReader = reader as SqlDataReader;
                                if (null != sqlReader)
                                {
                                    SetSqlXml_Unchecked(sink, setters, i, sqlReader.GetSqlXml(i));
                                }
                                else
                                {
                                    SetBytes_FromReader(sink, setters, i, metaData[i], reader, 0);
                                }
                            }
                            break;
                        case SqlDbType.Variant:
                            {  // block to scope sqlReader local and avoid conflicts
                                // Support better options for SqlDataReader
                                SqlDataReader sqlReader = reader as SqlDataReader;
                                SqlBuffer.StorageType storageType = SqlBuffer.StorageType.Empty;
                                object o;
                                if (null != sqlReader)
                                {
                                    o = sqlReader.GetSqlValue(i);
                                    storageType = sqlReader.GetVariantInternalStorageType(i);
                                }
                                else
                                {
                                    o = reader.GetValue(i);
                                }
                                ExtendedClrTypeCode typeCode = MetaDataUtilsSmi.DetermineExtendedTypeCodeForUseWithSqlDbType(metaData[i].SqlDbType, metaData[i].IsMultiValued, o, null);
                                if ((storageType == SqlBuffer.StorageType.DateTime2) || (storageType == SqlBuffer.StorageType.Date))
                                    SetCompatibleValueV200(sink, setters, i, metaData[i], o, typeCode, 0, 0, null, storageType);
                                else
                                    SetCompatibleValueV200(sink, setters, i, metaData[i], o, typeCode, 0, 0, null);
                            }
                            break;

                        case SqlDbType.Udt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.ByteArray));
                            // Skip serialization for Udt types.
                            SetBytes_FromReader(sink, setters, i, metaData[i], reader, 0);
                            break;

                        // SqlDbType.Structured should have been caught before this point for TVPs.  SUDTs will still need to implement.

                        case SqlDbType.Date:
                        case SqlDbType.DateTime2:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTime));
                            SetDateTime_Checked(sink, setters, i, metaData[i], reader.GetDateTime(i));
                            break;
                        case SqlDbType.Time:
                            { // block to scope sqlReader local and avoid conflicts
                                Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.TimeSpan));
                                SqlDataReader sqlReader = reader as SqlDataReader;
                                TimeSpan ts;
                                if (null != sqlReader)
                                {
                                    ts = sqlReader.GetTimeSpan(i);
                                }
                                else
                                {
                                    ts = (TimeSpan)reader.GetValue(i);
                                }
                                SetTimeSpan_Checked(sink, setters, i, metaData[i], ts);
                            }
                            break;
                        case SqlDbType.DateTimeOffset:
                            { // block to scope sqlReader local and avoid conflicts
                                Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTimeOffset));
                                SqlDataReader sqlReader = reader as SqlDataReader;
                                DateTimeOffset dto;
                                if (null != sqlReader)
                                {
                                    dto = sqlReader.GetDateTimeOffset(i);
                                }
                                else
                                {
                                    dto = (DateTimeOffset)reader.GetValue(i);
                                }
                                SetDateTimeOffset_Unchecked(sink, setters, i, dto);
                            }
                            break;

                        default:
                            // In order for us to get here we would have to have an 
                            // invalid instance of SqlDbType, or one would have to add 
                            // new member to SqlDbType without adding a case in this 
                            // switch, hence the assert.
                            Debug.Assert(false, "unsupported DbType:" + metaData[i].SqlDbType.ToString());
                            throw ADP.NotSupported();
                    }
                }
            }
        }


        internal static void FillCompatibleSettersFromRecord(SmiEventSink_Default sink, SmiTypedGetterSetter setters, SmiMetaData[] metaData, SqlDataRecord record, SmiDefaultFieldsProperty useDefaultValues)
        {
            for (int i = 0; i < metaData.Length; ++i)
            {
                if (null != useDefaultValues && useDefaultValues[i])
                {
                    continue;
                }
                if (record.IsDBNull(i))
                {
                    ValueUtilsSmi.SetDBNull_Unchecked(sink, setters, i);
                }
                else
                {
                    switch (metaData[i].SqlDbType)
                    {
                        case SqlDbType.BigInt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Int64));
                            SetInt64_Unchecked(sink, setters, i, record.GetInt64(i));
                            break;
                        case SqlDbType.Binary:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            SetBytes_FromRecord(sink, setters, i, metaData[i], record, 0);
                            break;
                        case SqlDbType.Bit:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Boolean));
                            SetBoolean_Unchecked(sink, setters, i, record.GetBoolean(i));
                            break;
                        case SqlDbType.Char:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlChars));
                            SetChars_FromRecord(sink, setters, i, metaData[i], record, 0);
                            break;
                        case SqlDbType.DateTime:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTime));
                            SetDateTime_Checked(sink, setters, i, metaData[i], record.GetDateTime(i));
                            break;
                        case SqlDbType.Decimal:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlDecimal));
                            SetSqlDecimal_Unchecked(sink, setters, i, record.GetSqlDecimal(i));
                            break;
                        case SqlDbType.Float:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Double));
                            SetDouble_Unchecked(sink, setters, i, record.GetDouble(i));
                            break;
                        case SqlDbType.Image:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            SetBytes_FromRecord(sink, setters, i, metaData[i], record, 0);
                            break;
                        case SqlDbType.Int:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Int32));
                            SetInt32_Unchecked(sink, setters, i, record.GetInt32(i));
                            break;
                        case SqlDbType.Money:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlMoney));
                            SetSqlMoney_Unchecked(sink, setters, i, metaData[i], record.GetSqlMoney(i));
                            break;
                        case SqlDbType.NChar:
                        case SqlDbType.NText:
                        case SqlDbType.NVarChar:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlChars));
                            SetChars_FromRecord(sink, setters, i, metaData[i], record, 0);
                            break;
                        case SqlDbType.Real:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Single));
                            SetSingle_Unchecked(sink, setters, i, record.GetFloat(i));
                            break;
                        case SqlDbType.UniqueIdentifier:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Guid));
                            SetGuid_Unchecked(sink, setters, i, record.GetGuid(i));
                            break;
                        case SqlDbType.SmallDateTime:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTime));
                            SetDateTime_Checked(sink, setters, i, metaData[i], record.GetDateTime(i));
                            break;
                        case SqlDbType.SmallInt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Int16));
                            SetInt16_Unchecked(sink, setters, i, record.GetInt16(i));
                            break;
                        case SqlDbType.SmallMoney:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlMoney));
                            SetSqlMoney_Checked(sink, setters, i, metaData[i], record.GetSqlMoney(i));
                            break;
                        case SqlDbType.Text:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlChars));
                            SetChars_FromRecord(sink, setters, i, metaData[i], record, 0);
                            break;
                        case SqlDbType.Timestamp:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            SetBytes_FromRecord(sink, setters, i, metaData[i], record, 0);
                            break;
                        case SqlDbType.TinyInt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.Byte));
                            SetByte_Unchecked(sink, setters, i, record.GetByte(i));
                            break;
                        case SqlDbType.VarBinary:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            SetBytes_FromRecord(sink, setters, i, metaData[i], record, 0);
                            break;
                        case SqlDbType.VarChar:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.String));
                            SetChars_FromRecord(sink, setters, i, metaData[i], record, 0);
                            break;
                        case SqlDbType.Xml:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlXml));
                            SetSqlXml_Unchecked(sink, setters, i, record.GetSqlXml(i));    // perf improvement?
                            break;
                        case SqlDbType.Variant:
                            object o = record.GetSqlValue(i);
                            ExtendedClrTypeCode typeCode = MetaDataUtilsSmi.DetermineExtendedTypeCode(o);
                            SetCompatibleValueV200(sink, setters, i, metaData[i], o, typeCode, 0, -1 /* no length restriction */, null /* no peekahead */);
                            break;
                        case SqlDbType.Udt:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.SqlBytes));
                            SetBytes_FromRecord(sink, setters, i, metaData[i], record, 0);
                            break;
                        case SqlDbType.Date:
                        case SqlDbType.DateTime2:
                            Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTime));
                            SetDateTime_Checked(sink, setters, i, metaData[i], record.GetDateTime(i));
                            break;
                        case SqlDbType.Time:
                            { // block to scope sqlReader local and avoid conflicts
                                Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.TimeSpan));
                                SqlDataRecord sqlRecord = record as SqlDataRecord;
                                TimeSpan ts;
                                if (null != sqlRecord)
                                {
                                    ts = sqlRecord.GetTimeSpan(i);
                                }
                                else
                                {
                                    ts = (TimeSpan)record.GetValue(i);
                                }
                                SetTimeSpan_Checked(sink, setters, i, metaData[i], ts);
                            }
                            break;
                        case SqlDbType.DateTimeOffset:
                            { // block to scope sqlReader local and avoid conflicts
                                Debug.Assert(CanAccessSetterDirectly(metaData[i], ExtendedClrTypeCode.DateTimeOffset));
                                SqlDataRecord sqlRecord = record as SqlDataRecord;
                                DateTimeOffset dto;
                                if (null != sqlRecord)
                                {
                                    dto = sqlRecord.GetDateTimeOffset(i);
                                }
                                else
                                {
                                    dto = (DateTimeOffset)record.GetValue(i);
                                }
                                SetDateTimeOffset_Unchecked(sink, setters, i, dto);
                            }
                            break;

                        default:
                            Debug.Assert(false, "unsupported DbType:" + metaData[i].SqlDbType.ToString());
                            throw ADP.NotSupported();
                    }
                }
            }
        }

        // spool a Stream into a scratch stream from the Smi interface and return it as a Stream
        internal static Stream CopyIntoNewSmiScratchStream(Stream source, SmiEventSink_Default sink)
        {
            Stream dest = new MemoryStream();

            int chunkSize;
            if (source.CanSeek && __maxByteChunkSize > source.Length)
            {
                chunkSize = unchecked((int)source.Length);  // unchecked cast is safe due to check on line above
            }
            else
            {
                chunkSize = __maxByteChunkSize;
            }

            byte[] copyBuffer = new byte[chunkSize];
            int bytesRead;
            while (0 != (bytesRead = source.Read(copyBuffer, 0, chunkSize)))
            {
                dest.Write(copyBuffer, 0, bytesRead);
            }
            dest.Flush();

            //  Need to re-wind scratch stream to beginning before returning
            dest.Seek(0, SeekOrigin.Begin);

            return dest;
        }

        //
        //  Common utility code to get lengths correct for trimming
        //
        private static object GetUdt_LengthChecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            object result;
            if (IsDBNull_Unchecked(sink, getters, ordinal))
            {
                Type t = metaData.Type;
                Debug.Assert(t != null, "Unexpected null of udtType on GetUdt_LengthChecked!");
                result = t.InvokeMember("Null", BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Static, null, null, new Object[] { }, CultureInfo.InvariantCulture);
                Debug.Assert(result != null);
            }
            else
            {
                // Note: do not need to copy getter stream, since it will not be used beyond
                //  deserialization (valid lifetime of getters is limited).
                Stream s = new SmiGettersStream(sink, getters, ordinal, metaData);
                result = SerializationHelperSql9.Deserialize(s, metaData.Type);
            }
            return result;
        }

        private static Decimal GetDecimal_PossiblyMoney(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            if (SqlDbType.Decimal == metaData.SqlDbType)
            {
                return GetSqlDecimal_Unchecked(sink, getters, ordinal).Value;
            }
            else
            {
                Debug.Assert(SqlDbType.Money == metaData.SqlDbType ||
                                SqlDbType.SmallMoney == metaData.SqlDbType,
                            "Unexpected sqldbtype=" + metaData.SqlDbType);
                return GetSqlMoney_Unchecked(sink, getters, ordinal).Value;
            }
        }

        private static void SetDecimal_PossiblyMoney(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, Decimal value)
        {
            if (SqlDbType.Decimal == metaData.SqlDbType || SqlDbType.Variant == metaData.SqlDbType)
            {
                SetDecimal_Unchecked(sink, setters, ordinal, value);
            }
            else
            {
                Debug.Assert(SqlDbType.Money == metaData.SqlDbType ||
                                SqlDbType.SmallMoney == metaData.SqlDbType,
                            "Unexpected sqldbtype=" + metaData.SqlDbType);
                SetSqlMoney_Checked(sink, setters, ordinal, metaData, new SqlMoney(value));
            }
        }

        // Hard coding smalldatetime limits...
        private static readonly DateTime s_dtSmallMax = new DateTime(2079, 06, 06, 23, 59, 29, 998);
        private static readonly DateTime s_dtSmallMin = new DateTime(1899, 12, 31, 23, 59, 29, 999);
        private static void VerifyDateTimeRange(SqlDbType dbType, DateTime value)
        {
            if (SqlDbType.SmallDateTime == dbType && (s_dtSmallMax < value || s_dtSmallMin > value))
            {
                throw ADP.InvalidMetaDataValue();
            }
        }

        private static readonly TimeSpan s_timeMin = TimeSpan.Zero;
        private static readonly TimeSpan s_timeMax = new TimeSpan(TimeSpan.TicksPerDay - 1);
        private static void VerifyTimeRange(SqlDbType dbType, TimeSpan value)
        {
            if (SqlDbType.Time == dbType && (s_timeMin > value || value > s_timeMax))
            {
                throw ADP.InvalidMetaDataValue();
            }
        }

        private static void SetDateTime_Checked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, DateTime value)
        {
            VerifyDateTimeRange(metaData.SqlDbType, value);
            SetDateTime_Unchecked(sink, setters, ordinal, ((SqlDbType.Date == metaData.SqlDbType) ? value.Date : value));
        }

        private static void SetTimeSpan_Checked(SmiEventSink_Default sink, SmiTypedGetterSetter setters, int ordinal, SmiMetaData metaData, TimeSpan value)
        {
            VerifyTimeRange(metaData.SqlDbType, value);
            SetTimeSpan_Unchecked(sink, setters, ordinal, value);
        }

        private static void SetSqlDateTime_Checked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlDateTime value)
        {
            if (!value.IsNull)
            {
                VerifyDateTimeRange(metaData.SqlDbType, value.Value);
            }
            SetSqlDateTime_Unchecked(sink, setters, ordinal, value);
        }

        private static void SetDateTime2_Checked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, DateTime value)
        {
            VerifyDateTimeRange(metaData.SqlDbType, value);
            SetDateTime2_Unchecked(sink, setters, ordinal, metaData, value);
        }

        private static void SetDate_Checked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, DateTime value)
        {
            VerifyDateTimeRange(metaData.SqlDbType, value);
            SetDate_Unchecked(sink, setters, ordinal, metaData, value);
        }

        private static void SetSqlMoney_Checked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlMoney value)
        {
            if (!value.IsNull && SqlDbType.SmallMoney == metaData.SqlDbType)
            {
                decimal decimalValue = value.Value;
                if (TdsEnums.SQL_SMALL_MONEY_MIN > decimalValue || TdsEnums.SQL_SMALL_MONEY_MAX < decimalValue)
                {
                    throw SQL.MoneyOverflow(decimalValue.ToString(CultureInfo.InvariantCulture));
                }
            }
            SetSqlMoney_Unchecked(sink, setters, ordinal, metaData, value);
        }

        private static void SetByteArray_LengthChecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, byte[] buffer, int offset)
        {
            int length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, buffer.Length, offset, buffer.Length - offset);
            Debug.Assert(length >= 0, "buffer.Length was invalid!");
            SetByteArray_Unchecked(sink, setters, ordinal, buffer, offset, length);
        }

        private static void SetCharArray_LengthChecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, char[] buffer, int offset)
        {
            int length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, buffer.Length, offset, buffer.Length - offset);
            Debug.Assert(length >= 0, "buffer.Length was invalid!");
            SetCharArray_Unchecked(sink, setters, ordinal, buffer, offset, length);
        }

        private static void SetSqlBinary_LengthChecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlBinary value, int offset)
        {
            int length = 0;
            if (!value.IsNull)
            {
                length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, value.Length, offset, value.Length - offset);
                Debug.Assert(length >= 0, "value.Length was invalid!");
            }
            SetSqlBinary_Unchecked(sink, setters, ordinal, value, offset, length);
        }

        private static void SetBytes_FromRecord(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlDataRecord record, int offset)
        {
            int length = 0;

            // Deal with large values by sending bufferLength of NoLengthLimit (== assume 
            //  CheckXetParameters will ignore requested-length checks in this case
            long bufferLength = record.GetBytes(ordinal, 0, null, 0, 0);
            if (bufferLength > Int32.MaxValue)
            {
                bufferLength = NoLengthLimit;
            }
            length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, checked((int)bufferLength), offset, checked((int)bufferLength));

            int chunkSize;
            if (length > __maxByteChunkSize || length < 0)
            {
                chunkSize = __maxByteChunkSize;
            }
            else
            {
                chunkSize = checked((int)length);
            }

            byte[] buffer = new byte[chunkSize];
            long bytesRead;
            long bytesWritten = 1;  // prime value to get into write loop
            long currentOffset = offset;
            long lengthWritten = 0;

            while ((length < 0 || lengthWritten < length) &&
                    0 != (bytesRead = record.GetBytes(ordinal, currentOffset, buffer, 0, chunkSize)) &&
                    0 != bytesWritten)
            {
                bytesWritten = setters.SetBytes(sink, ordinal, currentOffset, buffer, 0, checked((int)bytesRead));
                sink.ProcessMessagesAndThrow();
                checked { currentOffset += bytesWritten; }
                checked { lengthWritten += bytesWritten; }
            }

            // Make sure to trim any left-over data
            setters.SetBytesLength(sink, ordinal, currentOffset);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetBytes_FromReader(SmiEventSink_Default sink, SmiTypedGetterSetter setters, int ordinal, SmiMetaData metaData, DbDataReader reader, int offset)
        {
            int length = 0;

            // Deal with large values by sending bufferLength of NoLengthLimit (== assume 
            //  CheckXetParameters will ignore requested-length checks in this case)
            length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, NoLengthLimit /* buffer length */, offset, NoLengthLimit /* requested length */ );

            // Use fixed chunk size for all cases to avoid inquiring from reader.
            int chunkSize = __maxByteChunkSize;

            byte[] buffer = new byte[chunkSize];
            long bytesRead;
            long bytesWritten = 1;  // prime value to get into write loop
            long currentOffset = offset;
            long lengthWritten = 0;

            while ((length < 0 || lengthWritten < length) &&
                    0 != (bytesRead = reader.GetBytes(ordinal, currentOffset, buffer, 0, chunkSize)) &&
                    0 != bytesWritten)
            {
                bytesWritten = setters.SetBytes(sink, ordinal, currentOffset, buffer, 0, checked((int)bytesRead));
                sink.ProcessMessagesAndThrow();
                checked { currentOffset += bytesWritten; }
                checked { lengthWritten += bytesWritten; }
            }

            // Make sure to trim any left-over data (remember to trim at end of offset, not just the amount written
            setters.SetBytesLength(sink, ordinal, currentOffset);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlBytes_LengthChecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlBytes value, int offset)
        {
            int length = 0;
            if (!value.IsNull)
            {
                // Deal with large values by sending bufferLength of NoLengthLimit (== assume 
                //  CheckXetParameters will ignore requested-length checks in this case
                long bufferLength = value.Length;
                if (bufferLength > Int32.MaxValue)
                {
                    bufferLength = NoLengthLimit;
                }
                length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, checked((int)bufferLength), offset, checked((int)bufferLength));
            }
            SetSqlBytes_Unchecked(sink, setters, ordinal, value, 0, length);
        }

        private static void SetChars_FromRecord(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlDataRecord record, int offset)
        {
            int length = 0;

            // Deal with large values by sending bufferLength of NoLengthLimit
            //  CheckXetParameters will ignore length checks in this case
            long bufferLength = record.GetChars(ordinal, 0, null, 0, 0);
            if (bufferLength > Int32.MaxValue)
            {
                bufferLength = NoLengthLimit;
            }
            length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, checked((int)bufferLength), offset, checked((int)bufferLength - offset));

            int chunkSize;
            if (length > __maxCharChunkSize || length < 0)
            {
                if (MetaDataUtilsSmi.IsAnsiType(metaData.SqlDbType))
                {
                    chunkSize = __maxByteChunkSize;
                }
                else
                {
                    chunkSize = __maxCharChunkSize;
                }
            }
            else
            {
                chunkSize = checked((int)length);
            }

            char[] buffer = new char[chunkSize];
            long charsRead;
            long charsWritten = 1;  // prime value to get into write loop
            long currentOffset = offset;
            long lengthWritten = 0;

            while ((length < 0 || lengthWritten < length) &&
                    0 != (charsRead = record.GetChars(ordinal, currentOffset, buffer, 0, chunkSize)) &&
                    0 != charsWritten)
            {
                charsWritten = setters.SetChars(sink, ordinal, currentOffset, buffer, 0, checked((int)charsRead));
                sink.ProcessMessagesAndThrow();
                checked { currentOffset += charsWritten; }
                checked { lengthWritten += charsWritten; }
            }

            // Make sure to trim any left-over data
            setters.SetCharsLength(sink, ordinal, currentOffset);
            sink.ProcessMessagesAndThrow();
        }

        // Transfer a character value from a reader when we're not sure which GetXXX method the reader will support.
        //  Prefers to chunk data via GetChars, but falls back to GetString if that fails.
        //  Mainly put in place because DataTableReader doesn't support GetChars on string columns, but others could fail too...
        private static void SetCharsOrString_FromReader(SmiEventSink_Default sink, SmiTypedGetterSetter setters, int ordinal, SmiMetaData metaData, DbDataReader reader, int offset)
        {
            bool success = false;
            try
            {
                SetChars_FromReader(sink, setters, ordinal, metaData, reader, offset);
                success = true;
            }
            catch (Exception e)
            {
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }
            }

            if (!success)
            {
                SetString_FromReader(sink, setters, ordinal, metaData, reader, offset);
            }
        }

        // Use chunking via SetChars to transfer a value from a reader to a gettersetter
        private static void SetChars_FromReader(SmiEventSink_Default sink, SmiTypedGetterSetter setters, int ordinal, SmiMetaData metaData, DbDataReader reader, int offset)
        {
            int length = 0;

            // Deal with large values by sending bufferLength of NoLengthLimit (== assume 
            //  CheckXetParameters will ignore requested-length checks in this case)
            length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, NoLengthLimit /* buffer length */, offset, NoLengthLimit /* requested length */ );

            // Use fixed chunk size for all cases to avoid inquiring from reader.
            int chunkSize;
            if (MetaDataUtilsSmi.IsAnsiType(metaData.SqlDbType))
            {
                chunkSize = __maxByteChunkSize;
            }
            else
            {
                chunkSize = __maxCharChunkSize;
            }

            char[] buffer = new char[chunkSize];
            long charsRead;
            long charsWritten = 1;  // prime value to get into write loop
            long currentOffset = offset;
            long lengthWritten = 0;

            while ((length < 0 || lengthWritten < length) &&
                    0 != (charsRead = reader.GetChars(ordinal, currentOffset, buffer, 0, chunkSize)) &&
                    0 != charsWritten)
            {
                charsWritten = setters.SetChars(sink, ordinal, currentOffset, buffer, 0, checked((int)charsRead));
                sink.ProcessMessagesAndThrow();
                checked { currentOffset += charsWritten; }
                checked { lengthWritten += charsWritten; }
            }

            // Make sure to trim any left-over data (remember to trim at end of offset, not just the amount written
            setters.SetCharsLength(sink, ordinal, currentOffset);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetString_FromReader(SmiEventSink_Default sink, SmiTypedGetterSetter setters, int ordinal, SmiMetaData metaData, DbDataReader reader, int offset)
        {
            string value = reader.GetString(ordinal);
            int length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, value.Length, 0, NoLengthLimit /* buffer */, offset, NoLengthLimit /* request */);

            setters.SetString(sink, ordinal, value, offset, length);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlChars_LengthChecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlChars value, int offset)
        {
            int length = 0;
            if (!value.IsNull)
            {
                // Deal with large values by sending bufferLength of NoLengthLimit
                //  CheckXetParameters will ignore length checks in this case
                long bufferLength = value.Length;
                if (bufferLength > Int32.MaxValue)
                {
                    bufferLength = NoLengthLimit;
                }
                length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, checked((int)bufferLength), offset, checked((int)bufferLength - offset));
            }
            SetSqlChars_Unchecked(sink, setters, ordinal, value, 0, length);
        }

        private static void SetSqlString_LengthChecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlString value, int offset)
        {
            if (value.IsNull)
            {
                SetDBNull_Unchecked(sink, setters, ordinal);
            }
            else
            {
                string stringValue = value.Value;
                int length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, stringValue.Length, offset, stringValue.Length - offset);
                Debug.Assert(length >= 0, "value.Length was invalid!");
                SetSqlString_Unchecked(sink, setters, ordinal, metaData, value, offset, length);
            }
        }

        private static void SetString_LengthChecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, string value, int offset)
        {
            int length = CheckXetParameters(metaData.SqlDbType, metaData.MaxLength, NoLengthLimit /* actual */, 0, value.Length, offset, checked(value.Length - offset));
            Debug.Assert(length >= 0, "value.Length was invalid!");
            SetString_Unchecked(sink, setters, ordinal, value, offset, length);
        }

        private static void SetUdt_LengthChecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, object value)
        {
            if (ADP.IsNull(value))
            {
                setters.SetDBNull(sink, ordinal);
                sink.ProcessMessagesAndThrow();
            }
            else
            {
                Stream target = new SmiSettersStream(sink, setters, ordinal, metaData);
                SerializationHelperSql9.Serialize(target, value);
            }
        }


        //
        //  Semantics support routines
        //

        private static void ThrowIfInvalidSetterAccess(SmiMetaData metaData, ExtendedClrTypeCode setterTypeCode)
        {
            if (!CanAccessSetterDirectly(metaData, setterTypeCode))
            {
                throw ADP.InvalidCast();
            }
        }

        private static void ThrowIfITypedGettersIsNull(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            if (IsDBNull_Unchecked(sink, getters, ordinal))
            {
                throw SQL.SqlNullValue();
            }
        }

        private static bool CanAccessGetterDirectly(SmiMetaData metaData, ExtendedClrTypeCode setterTypeCode)
        {
            // Make sure no-one adds new ExtendedType, nor SqlDbType without updating this file!
            Debug.Assert(ExtendedClrTypeCode.First == 0 && (int)ExtendedClrTypeCode.Last == s_canAccessGetterDirectly.GetLength(0) - 1, "ExtendedClrTypeCodes does not match with __canAccessGetterDirectly");
            Debug.Assert(SqlDbType.BigInt == 0 && (int)SqlDbType.DateTimeOffset == s_canAccessGetterDirectly.GetLength(1) - 1, "SqlDbType does not match with __canAccessGetterDirectly");
            Debug.Assert(ExtendedClrTypeCode.First <= setterTypeCode && ExtendedClrTypeCode.Last >= setterTypeCode);
            Debug.Assert(SqlDbType.BigInt <= metaData.SqlDbType && SqlDbType.DateTimeOffset >= metaData.SqlDbType);

            bool returnValue = s_canAccessGetterDirectly[(int)setterTypeCode, (int)metaData.SqlDbType];

            // Additional restrictions to distinguish TVPs and Structured UDTs
            if (returnValue &&
                   (ExtendedClrTypeCode.DataTable == setterTypeCode ||
                    ExtendedClrTypeCode.DbDataReader == setterTypeCode ||
                    ExtendedClrTypeCode.IEnumerableOfSqlDataRecord == setterTypeCode))
            {
                returnValue = metaData.IsMultiValued;
            }

            return returnValue;
        }

        private static bool CanAccessSetterDirectly(SmiMetaData metaData, ExtendedClrTypeCode setterTypeCode)
        {
            // Make sure no-one adds new ExtendedType, nor SqlDbType without updating this file!
            Debug.Assert(ExtendedClrTypeCode.First == 0 && (int)ExtendedClrTypeCode.Last == s_canAccessSetterDirectly.GetLength(0) - 1, "ExtendedClrTypeCodes does not match with __canAccessSetterDirectly");
            Debug.Assert(SqlDbType.BigInt == 0 && (int)SqlDbType.DateTimeOffset == s_canAccessSetterDirectly.GetLength(1) - 1, "SqlDbType does not match with __canAccessSetterDirectly");
            Debug.Assert(ExtendedClrTypeCode.First <= setterTypeCode && ExtendedClrTypeCode.Last >= setterTypeCode);
            Debug.Assert(SqlDbType.BigInt <= metaData.SqlDbType && SqlDbType.DateTimeOffset >= metaData.SqlDbType);

            bool returnValue = s_canAccessSetterDirectly[(int)setterTypeCode, (int)metaData.SqlDbType];

            // Additional restrictions to distinguish TVPs and Structured UDTs
            if (returnValue &&
                   (ExtendedClrTypeCode.DataTable == setterTypeCode ||
                    ExtendedClrTypeCode.DbDataReader == setterTypeCode ||
                    ExtendedClrTypeCode.IEnumerableOfSqlDataRecord == setterTypeCode))
            {
                returnValue = metaData.IsMultiValued;
            }

            return returnValue;
        }

        private static long PositiveMin(long first, long second)
        {
            if (first < 0)
            {
                return second;
            }

            if (second < 0)
            {
                return first;
            }

            return Math.Min(first, second);
        }

        // Check Get Byte/Chars parameters, throw or adjust invalid values
        private static int CheckXetParameters(
                SqlDbType dbType,
                long maxLength,
                long actualLength,
                long fieldOffset,
                int bufferLength,
                int bufferOffset,
                int length)
        {
            if (0 > fieldOffset)
                throw ADP.NegativeParameter(nameof(fieldOffset));

            // if negative buffer index, throw
            if (bufferOffset < 0)
            {
                throw ADP.InvalidDestinationBufferIndex(bufferLength, bufferOffset, nameof(bufferOffset));
            }

            // skip further length checks for LOB buffer lengths
            if (bufferLength < 0)
            {
                length = checked((int)PositiveMin(length, PositiveMin(maxLength, actualLength)));
                if (length < NoLengthLimit)
                {
                    length = NoLengthLimit;
                }
                return length;
            }

            // if bad buffer index, throw
            if (bufferOffset > bufferLength)
            {
                throw ADP.InvalidDestinationBufferIndex(bufferLength, bufferOffset, nameof(bufferOffset));
            }

            // if there is not enough room in the buffer for data
            if (checked(length + bufferOffset) > bufferLength)
                throw ADP.InvalidBufferSizeOrIndex(length, bufferOffset);

            if (length < 0)
                throw ADP.InvalidDataLength(length);

            if (0 <= actualLength && actualLength <= fieldOffset)
            {
                return 0;
            }

            // trim length against both bufferLength and actual or max length
            //  (actual or max < 0 means don't trim against that value)
            //  Note that parameter UDTs don't know the correct maxlength, so the back end does
            //      the trimming.  Actual length should still be trimmed against here, though.
            length = Math.Min(length, bufferLength - bufferOffset);

            // special case for variants, since their maxLength is actually a bit bigger than
            // the actual data length allowed.
            if (SqlDbType.Variant == dbType)
            {
                length = Math.Min(length, TdsEnums.TYPE_SIZE_LIMIT);
            }

            Debug.Assert(0 > maxLength || 0 > actualLength ||
                    maxLength >= actualLength, "Actual = " + actualLength + ", max = " + maxLength + ", sqldbtype=" + dbType);

            if (0 <= actualLength)
            {
                // Length is guaranteed to be >= 0 coming in and actualLength >= fieldOffset, so this operation guarantees result >= 0
                length = (int)Math.Min((long)length, actualLength - fieldOffset);
                Debug.Assert(length >= 0, "result < 0, actualLength/fieldOffset problem?");
            }
            else if (SqlDbType.Udt != dbType && 0 <= maxLength)
            {
                length = (int)Math.Min((long)length, maxLength - fieldOffset);
                Debug.Assert(length >= 0, "Result < 0, maxlen/fieldoffset problem?");
            }

            if (length < 0)
            {
                return 0;
            }
            else
            {
                return length;
            }
        }

        //
        // These tables are formal encoding of the "unchecked" method access rules for various types
        //  The tables should only be accessed from the CanAccessXetterDirectly methods.
        //

        // A couple of private constants to increase the getter/setter access tables' constrast
        private const bool X = true;
        private const bool _ = false;

        private static bool[,] s_canAccessGetterDirectly = {
            // SqlDbTypes as columns (abbreviated, but in order)
            //  ExtendedClrTypeCodes as rows

            //     BI, Bin, Bit, Ch, DT, Dec, Fl, Im, Int, Mny, NCh, NTx, NVC, Rl, UI, SDT, SI, SMn, Txt, TS, TI, VBn, VCh, Var, 24, Xml, 26, 27, 28, Udt, St, Dat, Tm, DT2, DTO
            /*Bool*/
         { _ ,  _ ,  X , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Bool*/
                                                                                                                                                                            /*Byte*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , X , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Byte*/
                                                                                                                                                                            /*Char*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Char*/
                                                                                                                                                                            /*DTime*/
         { _ ,  _ ,  _ , _ , X , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , X  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  X  , _  , X  , _ , },/*DateTime*/
                                                                                                                                                                            /*DBNul*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*DBNull*/
                                                                                                                                                                            /*Decim*/
         { _ ,  _ ,  _ , _ , _ , X  , _ , _ , _  , X  , _  , _  , _  , _ , _ , _  , _ , X  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Decimal*/
                                                                                                                                                                            /*Doubl*/
         { _ ,  _ ,  _ , _ , _ , _  , X , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Double*/
                                                                                                                                                                            /*Empty*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Empty*/
                                                                                                                                                                            /*Int16*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , X , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Int16*/
                                                                                                                                                                            /*Int32*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , X  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Int32*/
                                                                                                                                                                            /*Int64*/
         { X ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Int64*/
                                                                                                                                                                            /*SByte*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SByte*/
                                                                                                                                                                            /*Singl*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , X , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Single*/
                                                                                                                                                                            /*Strng*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*String*/
                                                                                                                                                                            /*UIn16*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*UInt16*/
                                                                                                                                                                            /*UIn32*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*UInt32*/
                                                                                                                                                                            /*UIn64*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*UInt64*/
                                                                                                                                                                            /*Objct*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  X , _,  _  , _  , _  , _ , },/*Object*/
                                                                                                                                                                            /*BytAr*/
         { _ ,  X ,  _ , X , _ , _  , _ , X , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , X , _ , X  , X  , _  , _ , X  , _ , _ , _ ,  X , _,  _  , _  , _  , _ , },/*ByteArray*/
                                                                                                                                                                            /*ChrAr*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*CharArray*/
                                                                                                                                                                            /*Guid*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , X , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Guid*/
                                                                                                                                                                            /*SBin*/
         { _ ,  X ,  _ , _ , _ , _  , _ , X , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , X , _ , X  , _  , _  , _ , _  , _ , _ , _ ,  X , _,  _  , _  , _  , _ , },/*SqlBinary*/
                                                                                                                                                                            /*SBool*/
         { _ ,  _ ,  X , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlBoolean*/
                                                                                                                                                                            /*SByte*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , X , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlByte*/
                                                                                                                                                                            /*SDTme*/
         { _ ,  _ ,  _ , _ , X , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , X  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  X  , _  , X  , _ , },/*SqlDateTime*/
                                                                                                                                                                            /*SDubl*/
         { _ ,  _ ,  _ , _ , _ , _  , X , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlDouble*/
                                                                                                                                                                            /*SGuid*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , X , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlGuid*/
                                                                                                                                                                            /*SIn16*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , X , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlInt16*/
                                                                                                                                                                            /*SIn32*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , X  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlInt32*/
                                                                                                                                                                            /*SIn64*/
         { X ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlInt64*/
                                                                                                                                                                            /*SMony*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , X  , _  , _  , _  , _ , _ , _  , _ , X  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlMoney*/
                                                                                                                                                                            /*SDeci*/
         { _ ,  _ ,  _ , _ , _ , X  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlDecimal*/
                                                                                                                                                                            /*SSngl*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , X , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlSingle*/
                                                                                                                                                                            /*SStrn*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlString*/
                                                                                                                                                                            /*SChrs*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlChars*/
                                                                                                                                                                            /*SByts*/
         { _ ,  X ,  _ , _ , _ , _  , _ , X , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , X , _ , X  , _  , _  , _ , _  , _ , _ , _ ,  X , _,  _  , _  , _  , _ , },/*SqlBytes*/
                                                                                                                                                                            /*SXml*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , X  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlXml*/
                                                                                                                                                                            /*DTbl*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , X,  _  , _  , _  , _ , },/*DataTable*/
                                                                                                                                                                            /*Rdr */
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , X,  _  , _  , _  , _ , },/*DbDataReader*/
                                                                                                                                                                            /*EnSDR*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , X,  _  , _  , _  , _ , },/*IEnurerable<SqlDataRecord>*/
                                                                                                                                                                            /*TmSpn*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , X  , _  , _ , },/*TimeSpan*/
                                                                                                                                                                            /*DTOst*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , X , },/*DateTimeOffset*/
                                                                                                                                                                            /*Strm */
         { _ ,  X ,  _ , _ , _ , _  , _ , X , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , X  , _  , _  , _ , _  , _ , _ , _ ,  X , _,  _  , _  , _  , _ , },/*Stream*/
                                                                                                                                                                            /*TxRdr*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*TextReader*/
                                                                                                                                                                            /*XmlRd*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*XmlReader*/
                                                                                                                                                                            //     BI, Bin, Bit, Ch, DT, Dec, Fl, Im, Int, Mny, NCh, NTx, NVC, Rl, UI, SDT, SI, SMn, Txt, TS, TI, VBn, VCh, Var, 24, Xml, 26, 27, 28, Udt, St, Dat, Tm, DT2, DTO
        };

        private static bool[,] s_canAccessSetterDirectly = {
            // Setters as columns (labels are abbreviated from ExtendedClrTypeCode names)
            // SqlDbTypes as rows
            //     BI, Bin, Bit, Ch, DT, Dec, Fl, Im, Int, Mny, NCh, NTx, NVC, Rl, UI, SDT, SI, SMn, Txt, TS, TI, VBn, VCh, Var, 24, Xml, 26, 27, 28, Udt, St, Dat, Tm, DT2, DTO
            /*Bool*/
         { _ ,  _ ,  X , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Bool*/
                                                                                                                                                                            /*Byte*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , X , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Byte*/
                                                                                                                                                                            /*Char*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Char*/
                                                                                                                                                                            /*DTime*/
         { _ ,  _ ,  _ , _ , X , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , X  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  X  , _  , X  , _ , },/*DateTime*/
                                                                                                                                                                            /*DBNul*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*DBNull*/
                                                                                                                                                                            /*Decim*/
         { _ ,  _ ,  _ , _ , _ , X  , _ , _ , _  , X  , _  , _  , _  , _ , _ , _  , _ , X  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Decimal*/
                                                                                                                                                                            /*Doubl*/
         { _ ,  _ ,  _ , _ , _ , _  , X , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Double*/
                                                                                                                                                                            /*Empty*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Empty*/
                                                                                                                                                                            /*Int16*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , X , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Int16*/
                                                                                                                                                                            /*Int32*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , X  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Int32*/
                                                                                                                                                                            /*Int64*/
         { X ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Int64*/
                                                                                                                                                                            /*SByte*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SByte*/
                                                                                                                                                                            /*Singl*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , X , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Single*/
                                                                                                                                                                            /*Strng*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , X  , _ , X  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*String*/
                                                                                                                                                                            /*UIn16*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*UInt16*/
                                                                                                                                                                            /*UIn32*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*UInt32*/
                                                                                                                                                                            /*UIn64*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*UInt64*/
                                                                                                                                                                            /*Objct*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  X , _,  _  , _  , _  , _ , },/*Object*/
                                                                                                                                                                            /*BytAr*/
         { _ ,  X ,  _ , _ , _ , _  , _ , X , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , X , _ , X  , _  , X  , _ , X  , _ , _ , _ ,  X , _,  _  , _  , _  , _ , },/*ByteArray*/
                                                                                                                                                                            /*ChrAr*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*CharArray*/
                                                                                                                                                                            /*Guid*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , X , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Guid*/
                                                                                                                                                                            /*SBin*/
         { _ ,  X ,  _ , _ , _ , _  , _ , X , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , X , _ , X  , _  , X  , _ , _  , _ , _ , _ ,  X , _,  _  , _  , _  , _ , },/*SqlBinary*/
                                                                                                                                                                            /*SBool*/
         { _ ,  _ ,  X , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlBoolean*/
                                                                                                                                                                            /*SByte*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , X , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlByte*/
                                                                                                                                                                            /*SDTme*/
         { _ ,  _ ,  _ , _ , X , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , X  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  X  , _  , X  , _ , },/*SqlDateTime*/
                                                                                                                                                                            /*SDubl*/
         { _ ,  _ ,  _ , _ , _ , _  , X , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlDouble*/
                                                                                                                                                                            /*SGuid*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , X , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlGuid*/
                                                                                                                                                                            /*SIn16*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , X , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlInt16*/
                                                                                                                                                                            /*SIn32*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , X  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlInt32*/
                                                                                                                                                                            /*SIn64*/
         { X ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlInt64*/
                                                                                                                                                                            /*SMony*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , X  , _  , _  , _  , _ , _ , _  , _ , X  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlMoney*/
                                                                                                                                                                            /*SDeci*/
         { _ ,  _ ,  _ , _ , _ , X  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlDecimal*/
                                                                                                                                                                            /*SSngl*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , X , _ , _  , _ , _  , _  , _ , _ , _  , _  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlSingle*/
                                                                                                                                                                            /*SStrn*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , X  , _ , X  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlString*/
                                                                                                                                                                            /*SChrs*/
         { _ ,  _ ,  _ , X , _ , _  , _ , _ , _  , _  , X  , X  , X  , _ , _ , _  , _ , _  , X  , _ , _ , _  , X  , X  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlChars*/
                                                                                                                                                                            /*SByts*/
         { _ ,  X ,  _ , _ , _ , _  , _ , X , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , X , _ , X  , _  , X  , _ , _  , _ , _ , _ ,  X , _,  _  , _  , _  , _ , },/*SqlBytes*/
                                                                                                                                                                            /*SXml*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , X  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*SqlXml*/
                                                                                                                                                                            /*DTbl*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , X,  _  , _  , _  , _ , },/*DataTable*/
                                                                                                                                                                            /*Rdr */
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , X,  _  , _  , _  , _ , },/*DbDataReader*/
                                                                                                                                                                            /*EnSDR*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , X,  _  , _  , _  , _ , },/*IEnurerable<SqlDataRecord>*/
                                                                                                                                                                            /*TmSpn*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , X  , _  , _ , },/*TimeSpan*/
                                                                                                                                                                            /*DTOst*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , X , },/*DateTimeOffset*/
                                                                                                                                                                            /*Strm */
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*Stream*/
                                                                                                                                                                            /*TxRdr*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*TextReader*/
                                                                                                                                                                            /*XmlRd*/
         { _ ,  _ ,  _ , _ , _ , _  , _ , _ , _  , _  , _  , _  , _  , _ , _ , _  , _ , _  , _  , _ , _ , _  , _  , _  , _ , _  , _ , _ , _ ,  _ , _,  _  , _  , _  , _ , },/*XmlReader*/
                                                                                                                                                                            //     BI, Bin, Bit, Ch, DT, Dec, Fl, Im, Int, Mny, NCh, NTx, NVC, Rl, UI, SDT, SI, SMn, Txt, TS, TI, VBn, VCh, Var, 24, Xml, 26, 27, 28, Udt, St, Dat, Tm, DT2, DTO
        };


        //
        //  Private implementation of common mappings from a given type to corresponding Smi Getter/Setter
        //      These classes do type validation, parameter limit validation, nor coercions
        //
        private static bool IsDBNull_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            bool result = getters.IsDBNull(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static bool GetBoolean_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            bool result = getters.GetBoolean(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static byte GetByte_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            byte result = getters.GetByte(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static byte[] GetByteArray_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            long length = getters.GetBytesLength(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            int len = checked((int)length);

            byte[] buffer = new byte[len];
            getters.GetBytes(sink, ordinal, 0, buffer, 0, len);
            sink.ProcessMessagesAndThrow();
            return buffer;
        }

        internal static int GetBytes_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));
            Debug.Assert(ordinal >= 0, string.Format("Invalid ordinal: {0}", ordinal));
            Debug.Assert(sink != null, "Null SmiEventSink");
            Debug.Assert(getters != null, "Null getters");
            Debug.Assert(fieldOffset >= 0, string.Format("Invalid field offset: {0}", fieldOffset));
            Debug.Assert(buffer != null, "Null buffer");
            Debug.Assert(bufferOffset >= 0 && length >= 0 && bufferOffset + length <= buffer.Length, string.Format("Bad offset or length. bufferOffset: {0}, length: {1}, buffer.Length{2}", bufferOffset, length, buffer.Length));

            int result = getters.GetBytes(sink, ordinal, fieldOffset, buffer, bufferOffset, length);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static long GetBytesLength_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            long result = getters.GetBytesLength(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }


        private static char[] GetCharArray_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            long length = getters.GetCharsLength(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            int len = checked((int)length);

            char[] buffer = new char[len];
            getters.GetChars(sink, ordinal, 0, buffer, 0, len);
            sink.ProcessMessagesAndThrow();
            return buffer;
        }

        internal static int GetChars_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));
            Debug.Assert(ordinal >= 0, string.Format("Invalid ordinal: {0}", ordinal));
            Debug.Assert(sink != null, "Null SmiEventSink");
            Debug.Assert(getters != null, "Null getters");
            Debug.Assert(fieldOffset >= 0, string.Format("Invalid field offset: {0}", fieldOffset));
            Debug.Assert(buffer != null, "Null buffer");
            Debug.Assert(bufferOffset >= 0 && length >= 0 && bufferOffset + length <= buffer.Length, string.Format("Bad offset or length. bufferOffset: {0}, length: {1}, buffer.Length{2}", bufferOffset, length, buffer.Length));

            int result = getters.GetChars(sink, ordinal, fieldOffset, buffer, bufferOffset, length);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static long GetCharsLength_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            long result = getters.GetCharsLength(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static DateTime GetDateTime_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            DateTime result = getters.GetDateTime(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static DateTimeOffset GetDateTimeOffset_Unchecked(SmiEventSink_Default sink, SmiTypedGetterSetter getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            DateTimeOffset result = getters.GetDateTimeOffset(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static Double GetDouble_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            Double result = getters.GetDouble(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static Guid GetGuid_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            Guid result = getters.GetGuid(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static Int16 GetInt16_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            Int16 result = getters.GetInt16(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static Int32 GetInt32_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            Int32 result = getters.GetInt32(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static Int64 GetInt64_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            Int64 result = getters.GetInt64(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static Single GetSingle_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            Single result = getters.GetSingle(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static SqlBinary GetSqlBinary_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            byte[] buffer = GetByteArray_Unchecked(sink, getters, ordinal);
            return new SqlBinary(buffer);
        }

        private static SqlDecimal GetSqlDecimal_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            SqlDecimal result = getters.GetSqlDecimal(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static SqlMoney GetSqlMoney_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            Int64 temp = getters.GetInt64(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return SqlTypeWorkarounds.SqlMoneyCtor(temp, 1 /* ignored */ );
        }

        private static SqlXml GetSqlXml_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));


            // Note: must make a copy of getter stream, since it will be used beyond
            //  this method (valid lifetime of getters is limited).
            Stream s = new SmiGettersStream(sink, getters, ordinal, SmiMetaData.DefaultXml);
            Stream copy = ValueUtilsSmi.CopyIntoNewSmiScratchStream(s, sink);
            SqlXml result = new SqlXml(copy);
            return result;
        }

        private static String GetString_Unchecked(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            // Note: depending on different getters, the result string maybe truncated, e.g. for 
            // Inproc process, the getter is InProcRecordBuffer (implemented in SqlAcess), string will be
            // truncated to 4000 (if length is more than 4000). If MemoryRecordBuffer getter is used, data 
            // is not truncated. Please refer VSDD 479655 for more detailed information regarding the string length.
            String result = getters.GetString(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static TimeSpan GetTimeSpan_Unchecked(SmiEventSink_Default sink, SmiTypedGetterSetter getters, int ordinal)
        {
            Debug.Assert(!IsDBNull_Unchecked(sink, getters, ordinal));

            TimeSpan result = getters.GetTimeSpan(sink, ordinal);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static void SetBoolean_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Boolean value)
        {
            setters.SetBoolean(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetByteArray_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, byte[] buffer, int bufferOffset, int length)
        {
            if (length > 0)
            {
                setters.SetBytes(sink, ordinal, 0, buffer, bufferOffset, length);
                sink.ProcessMessagesAndThrow();
            }
            setters.SetBytesLength(sink, ordinal, length);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetStream_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metadata, StreamDataFeed feed)
        {
            long len = metadata.MaxLength;
            byte[] buff = new byte[constBinBufferSize];
            int nWritten = 0;
            do
            {
                int nRead = 0;
                int readSize = constBinBufferSize;
                if (len > 0 && nWritten + readSize > len)
                {
                    readSize = (int)(len - nWritten);
                }

                Debug.Assert(readSize >= 0);

                nRead = feed._source.Read(buff, 0, readSize);

                if (nRead == 0)
                {
                    break;
                }

                setters.SetBytes(sink, ordinal, nWritten, buff, 0, nRead);
                sink.ProcessMessagesAndThrow();

                nWritten += nRead;
            } while (len <= 0 || nWritten < len);

            setters.SetBytesLength(sink, ordinal, nWritten);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetTextReader_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metadata, TextDataFeed feed)
        {
            long len = metadata.MaxLength;
            char[] buff = new char[constTextBufferSize];
            int nWritten = 0;
            do
            {
                int nRead = 0;
                int readSize = constTextBufferSize;
                if (len > 0 && nWritten + readSize > len)
                {
                    readSize = (int)(len - nWritten);
                }

                Debug.Assert(readSize >= 0);

                nRead = feed._source.Read(buff, 0, readSize);

                if (nRead == 0)
                {
                    break;
                }

                setters.SetChars(sink, ordinal, nWritten, buff, 0, nRead);
                sink.ProcessMessagesAndThrow();

                nWritten += nRead;
            } while (len <= 0 || nWritten < len);

            setters.SetCharsLength(sink, ordinal, nWritten);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetByte_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Byte value)
        {
            setters.SetByte(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static int SetBytes_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            int result = setters.SetBytes(sink, ordinal, fieldOffset, buffer, bufferOffset, length);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static void SetCharArray_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, char[] buffer, int bufferOffset, int length)
        {
            if (length > 0)
            {
                setters.SetChars(sink, ordinal, 0, buffer, bufferOffset, length);
                sink.ProcessMessagesAndThrow();
            }
            setters.SetCharsLength(sink, ordinal, length);
            sink.ProcessMessagesAndThrow();
        }

        private static int SetChars_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            int result = setters.SetChars(sink, ordinal, fieldOffset, buffer, bufferOffset, length);
            sink.ProcessMessagesAndThrow();
            return result;
        }

        private static void SetDBNull_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal)
        {
            setters.SetDBNull(sink, ordinal);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetDecimal_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Decimal value)
        {
            setters.SetSqlDecimal(sink, ordinal, new SqlDecimal(value));
            sink.ProcessMessagesAndThrow();
        }

        private static void SetDateTime_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, DateTime value)
        {
            setters.SetDateTime(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetDateTime2_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, DateTime value)
        {
            Debug.Assert(SqlDbType.Variant == metaData.SqlDbType, "Invalid type. This should be called only when the type is variant.");
            setters.SetVariantMetaData(sink, ordinal, SmiMetaData.DefaultDateTime2);
            setters.SetDateTime(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetDate_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, DateTime value)
        {
            Debug.Assert(SqlDbType.Variant == metaData.SqlDbType, "Invalid type. This should be called only when the type is variant.");
            setters.SetVariantMetaData(sink, ordinal, SmiMetaData.DefaultDate);
            setters.SetDateTime(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetTimeSpan_Unchecked(SmiEventSink_Default sink, SmiTypedGetterSetter setters, int ordinal, TimeSpan value)
        {
            setters.SetTimeSpan(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetDateTimeOffset_Unchecked(SmiEventSink_Default sink, SmiTypedGetterSetter setters, int ordinal, DateTimeOffset value)
        {
            setters.SetDateTimeOffset(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetDouble_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Double value)
        {
            setters.SetDouble(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetGuid_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Guid value)
        {
            setters.SetGuid(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetInt16_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Int16 value)
        {
            setters.SetInt16(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetInt32_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Int32 value)
        {
            setters.SetInt32(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetInt64_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Int64 value)
        {
            setters.SetInt64(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSingle_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, Single value)
        {
            setters.SetSingle(sink, ordinal, value);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlBinary_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlBinary value, int offset, int length)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                SetByteArray_Unchecked(sink, setters, ordinal, value.Value, offset, length);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlBoolean_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlBoolean value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetBoolean(sink, ordinal, value.Value);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlByte_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlByte value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetByte(sink, ordinal, value.Value);
            }
            sink.ProcessMessagesAndThrow();
        }

        // note: length < 0 indicates write everything
        private static void SetSqlBytes_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlBytes value, int offset, long length)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
                sink.ProcessMessagesAndThrow();
            }
            else
            {
                int chunkSize;
                if (length > __maxByteChunkSize || length < 0)
                {
                    chunkSize = __maxByteChunkSize;
                }
                else
                {
                    chunkSize = checked((int)length);
                }

                byte[] buffer = new byte[chunkSize];
                long bytesRead;
                long bytesWritten = 1;  // prime value to get into write loop
                long currentOffset = offset;
                long lengthWritten = 0;

                while ((length < 0 || lengthWritten < length) &&
                        0 != (bytesRead = value.Read(currentOffset, buffer, 0, chunkSize)) &&
                        0 != bytesWritten)
                {
                    bytesWritten = setters.SetBytes(sink, ordinal, currentOffset, buffer, 0, checked((int)bytesRead));
                    sink.ProcessMessagesAndThrow();
                    checked { currentOffset += bytesWritten; }
                    checked { lengthWritten += bytesWritten; }
                }

                // Make sure to trim any left-over data
                setters.SetBytesLength(sink, ordinal, currentOffset);
                sink.ProcessMessagesAndThrow();
            }
        }

        private static void SetSqlChars_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlChars value, int offset, int length)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
                sink.ProcessMessagesAndThrow();
            }
            else
            {
                int chunkSize;
                if (length > __maxCharChunkSize || length < 0)
                {
                    chunkSize = __maxCharChunkSize;
                }
                else
                {
                    chunkSize = checked((int)length);
                }

                char[] buffer = new char[chunkSize];
                long charsRead;
                long charsWritten = 1;  // prime value to get into write loop
                long currentOffset = offset;
                long lengthWritten = 0;

                while ((length < 0 || lengthWritten < length) &&
                        0 != (charsRead = value.Read(currentOffset, buffer, 0, chunkSize)) &&
                        0 != charsWritten)
                {
                    charsWritten = setters.SetChars(sink, ordinal, currentOffset, buffer, 0, checked((int)charsRead));
                    sink.ProcessMessagesAndThrow();
                    checked { currentOffset += charsWritten; }
                    checked { lengthWritten += charsWritten; }
                }

                // Make sure to trim any left-over data
                setters.SetCharsLength(sink, ordinal, currentOffset);
                sink.ProcessMessagesAndThrow();
            }
        }

        private static void SetSqlDateTime_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlDateTime value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetDateTime(sink, ordinal, value.Value);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlDecimal_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlDecimal value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetSqlDecimal(sink, ordinal, value);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlDouble_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlDouble value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetDouble(sink, ordinal, value.Value);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlGuid_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlGuid value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetGuid(sink, ordinal, value.Value);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlInt16_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlInt16 value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetInt16(sink, ordinal, value.Value);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlInt32_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlInt32 value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetInt32(sink, ordinal, value.Value);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlInt64_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlInt64 value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetInt64(sink, ordinal, value.Value);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlMoney_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlMoney value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                if (SqlDbType.Variant == metaData.SqlDbType)
                {
                    setters.SetVariantMetaData(sink, ordinal, SmiMetaData.DefaultMoney);
                    sink.ProcessMessagesAndThrow();
                }

                setters.SetInt64(sink, ordinal, SqlTypeWorkarounds.SqlMoneyToSqlInternalRepresentation(value));
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlSingle_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlSingle value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
            }
            else
            {
                setters.SetSingle(sink, ordinal, value.Value);
            }
            sink.ProcessMessagesAndThrow();
        }

        private static void SetSqlString_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData, SqlString value, int offset, int length)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
                sink.ProcessMessagesAndThrow();
            }
            else
            {
                if (SqlDbType.Variant == metaData.SqlDbType)
                {
                    // Set up a NVarChar metadata with correct LCID/Collation
                    metaData = new SmiMetaData(
                            SqlDbType.NVarChar,
                            SmiMetaData.MaxUnicodeCharacters,
                            0,
                            0,
                            value.LCID,
                            value.SqlCompareOptions,
                            null);
                    setters.SetVariantMetaData(sink, ordinal, metaData);
                    sink.ProcessMessagesAndThrow();
                }
                SetString_Unchecked(sink, setters, ordinal, value.Value, offset, length);
            }
        }

        private static void SetSqlXml_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SqlXml value)
        {
            if (value.IsNull)
            {
                setters.SetDBNull(sink, ordinal);
                sink.ProcessMessagesAndThrow();
            }
            else
            {
                SetXmlReader_Unchecked(sink, setters, ordinal, value.CreateReader());
            }
        }

        private static void SetXmlReader_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, XmlReader xmlReader)
        {
            // set up writer
            XmlWriterSettings WriterSettings = new XmlWriterSettings();
            WriterSettings.CloseOutput = false;		// don't close the memory stream
            WriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
            WriterSettings.Encoding = System.Text.Encoding.Unicode;
            WriterSettings.OmitXmlDeclaration = true;

            System.IO.Stream target = new SmiSettersStream(sink, setters, ordinal, SmiMetaData.DefaultXml);

            XmlWriter xmlWriter = XmlWriter.Create(target, WriterSettings);

            // now spool the data into the writer (WriteNode will call Read())
            xmlReader.Read();
            while (!xmlReader.EOF)
            {
                xmlWriter.WriteNode(xmlReader, true);
            }
            xmlWriter.Flush();
            sink.ProcessMessagesAndThrow();
        }

        private static void SetString_Unchecked(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, String value, int offset, int length)
        {
            setters.SetString(sink, ordinal, value, offset, length);
            sink.ProcessMessagesAndThrow();
        }


        // Set a DbDataReader to a Structured+MultiValued setter (table type)
        //  Assumes metaData correctly describes the reader's shape, and consumes only the current resultset
        private static void SetDbDataReader_Unchecked(
            SmiEventSink_Default sink,
            SmiTypedGetterSetter setters,
            int ordinal,
            SmiMetaData metaData,
            DbDataReader value
            )
        {
            // Get the target gettersetter
            setters = setters.GetTypedGetterSetter(sink, ordinal);
            sink.ProcessMessagesAndThrow();

            // Iterate over all rows in the current set of results
            while (value.Read())
            {
                setters.NewElement(sink);
                sink.ProcessMessagesAndThrow();

                FillCompatibleSettersFromReader(sink, setters, metaData.FieldMetaData, value);
            }

            setters.EndElements(sink);
            sink.ProcessMessagesAndThrow();
        }

        private static void SetIEnumerableOfSqlDataRecord_Unchecked(
            SmiEventSink_Default sink,
            SmiTypedGetterSetter setters,
            int ordinal,
            SmiMetaData metaData,
            IEnumerable<SqlDataRecord> value,
            ParameterPeekAheadValue peekAhead
            )
        {
            // Get target gettersetter
            setters = setters.GetTypedGetterSetter(sink, ordinal);
            sink.ProcessMessagesAndThrow();

            IEnumerator<SqlDataRecord> enumerator = null;
            try
            {
                // Need to copy field metadata to an array to call FillCompatibleITypeSettersFromRecord
                SmiExtendedMetaData[] mdFields = new SmiExtendedMetaData[metaData.FieldMetaData.Count];
                metaData.FieldMetaData.CopyTo(mdFields, 0);

                SmiDefaultFieldsProperty defaults = (SmiDefaultFieldsProperty)metaData.ExtendedProperties[SmiPropertySelector.DefaultFields];

                int recordNumber = 1;   // used only for reporting position when there are errors.

                // obtain enumerator and handle any peekahead values
                if (null != peekAhead && null != peekAhead.FirstRecord)
                {
                    // hook up to enumerator
                    enumerator = peekAhead.Enumerator;

                    // send the first record that was obtained earlier
                    setters.NewElement(sink);
                    sink.ProcessMessagesAndThrow();
                    FillCompatibleSettersFromRecord(sink, setters, mdFields, peekAhead.FirstRecord, defaults);
                    recordNumber++;
                }
                else
                {
                    enumerator = value.GetEnumerator();
                }

                using (enumerator)
                {
                    while (enumerator.MoveNext())
                    {
                        setters.NewElement(sink);
                        sink.ProcessMessagesAndThrow();

                        SqlDataRecord record = enumerator.Current;

                        if (record.FieldCount != mdFields.Length)
                        {
                            throw SQL.EnumeratedRecordFieldCountChanged(recordNumber);
                        }

                        for (int i = 0; i < record.FieldCount; i++)
                        {
                            if (!MetaDataUtilsSmi.IsCompatible(metaData.FieldMetaData[i], record.GetSqlMetaData(i)))
                            {
                                throw SQL.EnumeratedRecordMetaDataChanged(record.GetName(i), recordNumber);
                            }
                        }

                        FillCompatibleSettersFromRecord(sink, setters, mdFields, record, defaults);
                        recordNumber++;
                    }

                    setters.EndElements(sink);
                    sink.ProcessMessagesAndThrow();
                }
            }
            finally
            {
                // Clean up!
                IDisposable disposable = enumerator as IDisposable;
                if (null != disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}

