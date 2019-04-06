// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    internal static class DefaultConverters
    {
        internal static object Create(Type type)
        {
            if (type.IsEnum)
            {
                return Activator.CreateInstance(
                    typeof(JsonValueConverterEnum<>).MakeGenericType(type),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    new object[] { false },
                    culture: null);
            }
            TypeCode typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return new JsonValueConverterBoolean();
                case TypeCode.Char:
                    return new JsonValueConverterChar();
                case TypeCode.SByte:
                    return new JsonValueConverterSByte();
                case TypeCode.Byte:
                    return new JsonValueConverterByte();
                case TypeCode.Int16:
                    return new JsonValueConverterInt16();
                case TypeCode.UInt16:
                    return new JsonValueConverterUInt16();
                case TypeCode.Int32:
                    return new JsonValueConverterInt32();
                case TypeCode.UInt32:
                    return new JsonValueConverterUInt32();
                case TypeCode.Int64:
                    return new JsonValueConverterInt64();
                case TypeCode.UInt64:
                    return new JsonValueConverterUInt64();
                case TypeCode.Single:
                    return new JsonValueConverterSingle();
                case TypeCode.Double:
                    return new JsonValueConverterDouble();
                case TypeCode.Decimal:
                    return new JsonValueConverterDecimal();
                case TypeCode.DateTime:
                    return new JsonValueConverterDateTime();
                case TypeCode.String:
                    return new JsonValueConverterString();
            }

            if (type == typeof(DateTimeOffset))
            {
                return new JsonValueConverterDateTimeOffset();
            }
            
            return null;
        }
    }
}
