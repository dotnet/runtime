// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.Reflection.Augments;

using CorElementType = System.Reflection.CorElementType;
using EETypeElementType = Internal.Runtime.EETypeElementType;

namespace System
{
    public abstract partial class Enum : ValueType, IComparable, IFormattable, IConvertible
    {
#pragma warning disable IDE0060
        internal static EnumInfo GetEnumInfo(Type enumType, bool getNames = true)
        {
            Debug.Assert(enumType != null);
            Debug.Assert(enumType is RuntimeType);
            Debug.Assert(enumType.IsEnum);

            RuntimeType rt = (RuntimeType)enumType;
            return Type.GetTypeCode(RuntimeAugments.GetEnumUnderlyingType(rt.TypeHandle)) switch
            {
                TypeCode.SByte or TypeCode.Byte => GetEnumInfo<byte>(rt),
                TypeCode.Int16 or TypeCode.UInt16 => GetEnumInfo<ushort>(rt),
                TypeCode.Int32 or TypeCode.UInt32 => GetEnumInfo<uint>(rt),
                TypeCode.Int64 or TypeCode.UInt64 => GetEnumInfo<ulong>(rt),
                _ => throw new NotSupportedException(),
            };
        }

        internal static EnumInfo<TStorage> GetEnumInfo<TStorage>(Type enumType, bool getNames = true)
            where TStorage : struct, INumber<TStorage>
        {
            Debug.Assert(enumType != null);
            Debug.Assert(enumType is RuntimeType);
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

        private static object InternalBoxEnum(Type enumType, long value)
        {
            return ToObject(enumType.TypeHandle.ToEETypePtr(), value);
        }

        private static CorElementType InternalGetCorElementType(RuntimeType rt)
        {
            Debug.Assert(rt.IsActualEnum);
            return rt.TypeHandle.ToEETypePtr().CorElementType;
        }

        private CorElementType InternalGetCorElementType()
        {
            return this.GetEETypePtr().CorElementType;
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
        internal static bool TryGetUnboxedValueOfEnumOrInteger(object value, out ulong result)
        {
            EETypePtr eeType = value.GetEETypePtr();
            // For now, this check is required to flush out pointers.
            if (!eeType.IsDefType)
            {
                result = 0;
                return false;
            }
            EETypeElementType elementType = eeType.ElementType;

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

        //
        // Checks if value.GetType() matches enumType exactly.
        //
        internal static bool ValueTypeMatchesEnumType(Type enumType, object value)
        {
            EETypePtr enumEEType;
            if (!enumType.TryGetEEType(out enumEEType))
                return false;
            if (!(enumEEType == value.GetEETypePtr()))
                return false;
            return true;
        }

        [Conditional("BIGENDIAN")]
        private static unsafe void AdjustForEndianness(ref byte* pValue, EETypePtr enumEEType)
        {
            // On Debug builds, include the big-endian code to help deter bitrot (the "Conditional("BIGENDIAN")" will prevent it from executing on little-endian).
            // On Release builds, exclude code to deter IL bloat and toolchain work.
#if BIGENDIAN || DEBUG
            EETypeElementType elementType = enumEEType.ElementType;
            switch (elementType)
            {
                case EETypeElementType.SByte:
                case EETypeElementType.Byte:
                    pValue += sizeof(long) - sizeof(byte);
                    break;

                case EETypeElementType.Int16:
                case EETypeElementType.UInt16:
                    pValue += sizeof(long) - sizeof(short);
                    break;

                case EETypeElementType.Int32:
                case EETypeElementType.UInt32:
                    pValue += sizeof(long) - sizeof(int);
                    break;

                case EETypeElementType.Int64:
                case EETypeElementType.UInt64:
                    break;

                default:
                    throw new NotSupportedException();
            }
#endif //BIGENDIAN || DEBUG
        }

        #region ToObject

        internal static unsafe object ToObject(EETypePtr enumEEType, long value)
        {
            Debug.Assert(enumEEType.IsEnum);

            byte* pValue = (byte*)&value;
            AdjustForEndianness(ref pValue, enumEEType);
            return RuntimeImports.RhBox(enumEEType, ref *pValue);
        }
        #endregion
    }
}
