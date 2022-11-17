// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public abstract partial class Enum
    {
        // This returns 0 for all values for float/double/nint/nuint.
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Enum_GetValuesAndNames")]
        private static partial void GetEnumValuesAndNames(QCallTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, Interop.BOOL getNames);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object InternalBoxEnum(RuntimeType enumType, long value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe CorElementType InternalGetCorElementType(MethodTable* pMT);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe CorElementType InternalGetCorElementType()
        {
            CorElementType elementType = InternalGetCorElementType(RuntimeHelpers.GetMethodTable(this));
            GC.KeepAlive(this);
            return elementType;
        }

        // Indexed by CorElementType
        private static readonly RuntimeType?[] s_underlyingTypes =
        {
            null,
            null,
            (RuntimeType)typeof(bool),
            (RuntimeType)typeof(char),
            (RuntimeType)typeof(sbyte),
            (RuntimeType)typeof(byte),
            (RuntimeType)typeof(short),
            (RuntimeType)typeof(ushort),
            (RuntimeType)typeof(int),
            (RuntimeType)typeof(uint),
            (RuntimeType)typeof(long),
            (RuntimeType)typeof(ulong),
            (RuntimeType)typeof(float),
            (RuntimeType)typeof(double),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            (RuntimeType)typeof(nint),
            (RuntimeType)typeof(nuint)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe RuntimeType InternalGetUnderlyingType(RuntimeType enumType)
        {
            // Sanity check the last element in the table
            Debug.Assert(s_underlyingTypes[(int)CorElementType.ELEMENT_TYPE_U] == typeof(nuint));

            RuntimeType? underlyingType = s_underlyingTypes[(int)InternalGetCorElementType((MethodTable*)enumType.GetUnderlyingNativeHandle())];
            GC.KeepAlive(enumType);

            Debug.Assert(underlyingType != null);
            return underlyingType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static EnumInfo<TUnderlyingValue> GetEnumInfo<TUnderlyingValue>(RuntimeType enumType, bool getNames = true)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            return enumType.GenericCache is EnumInfo<TUnderlyingValue> info && (!getNames || info.Names is not null) ?
                info :
                InitializeEnumInfo(enumType, getNames);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static EnumInfo<TUnderlyingValue> InitializeEnumInfo(RuntimeType enumType, bool getNames)
            {
                ulong[]? uint64Values = null;
                string[]? names = null;
                RuntimeTypeHandle enumTypeHandle = enumType.TypeHandle;
                GetEnumValuesAndNames(
                    new QCallTypeHandle(ref enumTypeHandle),
                    ObjectHandleOnStack.Create(ref uint64Values),
                    ObjectHandleOnStack.Create(ref names),
                    getNames ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
                bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

                TUnderlyingValue[] values = ToUnderlyingValues<TUnderlyingValue>(uint64Values!);

                var entry = new EnumInfo<TUnderlyingValue>(hasFlagsAttribute, values, names!);
                enumType.GenericCache = entry;
                return entry;
            }
        }
    }
}
