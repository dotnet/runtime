// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal static class DefaultConverters
    {
        private const int MaxTypeCode = 18;

        private static readonly object[] s_Converters = new object[MaxTypeCode + 1] {
            null,   // Empty 
            null,   // Object
            null,   // DBNull
            new JsonValueConverterBoolean(),
            new JsonValueConverterChar(),
            new JsonValueConverterSByte(),
            new JsonValueConverterByte(),
            new JsonValueConverterInt16(),
            new JsonValueConverterUInt16(),
            new JsonValueConverterInt32(),
            new JsonValueConverterUInt32(),
            new JsonValueConverterInt64(),
            new JsonValueConverterUInt64(),
            new JsonValueConverterSingle(),
            new JsonValueConverterDouble(),
            new JsonValueConverterDecimal(),
            new JsonValueConverterDateTime(),
            null,   // (not a value)
            new JsonValueConverterString()
        };

        private static readonly object[] s_NullableConverters = new object[MaxTypeCode + 1]
        {
            null,
            null,
            null,
            new JsonValueConverterBooleanNullable(),
            new JsonValueConverterCharNullable(),
            new JsonValueConverterSByteNullable(),
            new JsonValueConverterByteNullable(),
            new JsonValueConverterInt16Nullable(),
            new JsonValueConverterUInt16Nullable(),
            new JsonValueConverterInt32Nullable(),
            new JsonValueConverterUInt32Nullable(),
            new JsonValueConverterInt64Nullable(),
            new JsonValueConverterUInt64Nullable(),
            new JsonValueConverterSingleNullable(),
            new JsonValueConverterDoubleNullable(),
            new JsonValueConverterDecimalNullable(),
            new JsonValueConverterDateTimeNullable(),
            null,
            new JsonValueConverterString()
        };

        internal static object GetDefaultPropertyValueConverter(Type propertyType, bool isNullable)
        {
            object converter = null;

            int typeCode = (int)Type.GetTypeCode(propertyType);
            if (typeCode <= MaxTypeCode)
            {
                if (isNullable)
                {
                    converter = s_NullableConverters[typeCode];
                }
                else
                {
                    converter = s_Converters[typeCode];
                }
            }

            return converter;
        }
    }
}
