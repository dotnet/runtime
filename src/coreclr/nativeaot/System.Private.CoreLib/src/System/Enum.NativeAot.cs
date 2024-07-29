// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Reflection.Augments;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using CorElementType = System.Reflection.CorElementType;
using EETypeElementType = Internal.Runtime.EETypeElementType;

namespace System
{
    public abstract partial class Enum : ValueType, IComparable, IFormattable, IConvertible
    {
#pragma warning disable IDE0060
        internal static unsafe EnumInfo GetEnumInfo(RuntimeType enumType, bool getNames = true)
        {
            Debug.Assert(enumType != null);
            Debug.Assert(enumType.IsEnum);

            return enumType.TypeHandle.ToMethodTable()->ElementType switch
            {
                EETypeElementType.SByte or EETypeElementType.Byte => GetEnumInfo<byte>(enumType),
                EETypeElementType.Int16 or EETypeElementType.UInt16 => GetEnumInfo<ushort>(enumType),
                EETypeElementType.Int32 or EETypeElementType.UInt32 => GetEnumInfo<uint>(enumType),
                EETypeElementType.Int64 or EETypeElementType.UInt64 => GetEnumInfo<ulong>(enumType),
                _ => throw new NotSupportedException(),
            };
        }

        internal static EnumInfo<TStorage> GetEnumInfo<TStorage>(RuntimeType enumType, bool getNames = true)
            where TStorage : struct, INumber<TStorage>
        {
            Debug.Assert(enumType != null);
            Debug.Assert(enumType.IsEnum);
            Debug.Assert(
                typeof(TStorage) == typeof(byte) ||
                typeof(TStorage) == typeof(ushort) ||
                typeof(TStorage) == typeof(uint) ||
                typeof(TStorage) == typeof(ulong));

            return (EnumInfo<TStorage>)ReflectionAugments.ReflectionCoreCallbacks.GetEnumInfo(enumType,
                static (underlyingType, names, valuesAsObject, isFlags) =>
                {
                    // Only after we've sorted, create the underlying array.
                    var values = new TStorage[valuesAsObject.Length];
                    for (int i = 0; i < valuesAsObject.Length; i++)
                    {
                        values[i] = (TStorage)valuesAsObject[i];
                    }
                    return new EnumInfo<TStorage>(underlyingType, values, names, isFlags);
                });
        }
#pragma warning restore

        internal static unsafe object ToObject(MethodTable* mt, long value)
            => InternalBoxEnum(new RuntimeTypeHandle(mt), value);

        private static unsafe CorElementType InternalGetCorElementType(RuntimeType rt)
        {
            Debug.Assert(rt.IsActualEnum);
            return new EETypePtr(rt.TypeHandle.ToMethodTable()).CorElementType;
        }

        private unsafe CorElementType InternalGetCorElementType()
        {
            return new EETypePtr(this.GetMethodTable()).CorElementType;
        }

        //
        // Note: This works on both Enum's and underlying integer values.
        //
        //
        // This returns the underlying enum values as "ulong" regardless of the actual underlying type. Signed integral
        // types get sign-extended into the 64-bit value, unsigned types get zero-extended.
        //
        // The return value is "bool" if "value" is not an enum or an "integer type" as defined by the BCL Enum apis.
        //
        internal static unsafe bool TryGetUnboxedValueOfEnumOrInteger(object value, out ulong result)
        {
            MethodTable* eeType = value.GetMethodTable();
            // For now, this check is required to flush out pointers.
            if (!eeType->IsDefType)
            {
                result = 0;
                return false;
            }
            EETypeElementType elementType = eeType->ElementType;

            ref byte pValue = ref value.GetRawData();

            switch (elementType)
            {
                case EETypeElementType.Char:
                    result = (ulong)(long)Unsafe.As<byte, char>(ref pValue);
                    return true;

                case EETypeElementType.SByte:
                    result = (ulong)(long)Unsafe.As<byte, sbyte>(ref pValue);
                    return true;

                case EETypeElementType.Byte:
                    result = (ulong)(long)Unsafe.As<byte, byte>(ref pValue);
                    return true;

                case EETypeElementType.Int16:
                    result = (ulong)(long)Unsafe.As<byte, short>(ref pValue);
                    return true;

                case EETypeElementType.UInt16:
                    result = (ulong)(long)Unsafe.As<byte, ushort>(ref pValue);
                    return true;

                case EETypeElementType.Int32:
                    result = (ulong)(long)Unsafe.As<byte, int>(ref pValue);
                    return true;

                case EETypeElementType.UInt32:
                    result = (ulong)(long)Unsafe.As<byte, uint>(ref pValue);
                    return true;

                case EETypeElementType.Int64:
                    result = (ulong)(long)Unsafe.As<byte, long>(ref pValue);
                    return true;

                case EETypeElementType.UInt64:
                    result = (ulong)(long)Unsafe.As<byte, ulong>(ref pValue);
                    return true;

                default:
                    result = 0;
                    return false;
            }
        }

        internal static Type InternalGetUnderlyingType(RuntimeType enumType)
        {
            Debug.Assert(enumType is RuntimeType);
            Debug.Assert(enumType.IsEnum);

            return GetEnumInfo(enumType).UnderlyingType;
        }
    }
}
