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
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Enum_GetValuesAndNames")]
        private static partial void GetEnumValuesAndNames(QCallTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, Interop.BOOL getNames);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe CorElementType InternalGetCorElementType(MethodTable* pMT);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe CorElementType InternalGetCorElementType(RuntimeType rt)
        {
            Debug.Assert(rt.IsActualEnum);
            CorElementType elementType = InternalGetCorElementType((MethodTable*)rt.GetUnderlyingNativeHandle());
            GC.KeepAlive(rt);
            return elementType;
        }

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
        private static EnumInfo<TStorage> GetEnumInfo<TStorage>(RuntimeType enumType, bool getNames = true)
            where TStorage : struct, INumber<TStorage>
        {
            Debug.Assert(
                typeof(TStorage) == typeof(byte) || typeof(TStorage) == typeof(ushort) || typeof(TStorage) == typeof(uint) || typeof(TStorage) == typeof(ulong) ||
                typeof(TStorage) == typeof(nuint) || typeof(TStorage) == typeof(float) || typeof(TStorage) == typeof(double) || typeof(TStorage) == typeof(char),
                $"Unexpected {nameof(TStorage)} == {typeof(TStorage)}");

            return enumType.FindCacheEntry<EnumInfo<TStorage>>() is {} info && (!getNames || info.Names is not null) ?
                info :
                InitializeEnumInfo(enumType, getNames);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static EnumInfo<TStorage> InitializeEnumInfo(RuntimeType enumType, bool getNames)
            {
                // If we're asked to get the cache with names,
                // force that copy into the cache even if we already have a cache entry without names
                // so we don't have to recompute the names if asked again.
                return getNames
                    ? enumType.OverwriteCacheEntry(EnumInfo<TStorage>.CreateWithNames(enumType))
                    : enumType.GetOrCreateCacheEntry<EnumInfo<TStorage>>();
            }
        }

        internal sealed partial class EnumInfo<TStorage> : RuntimeType.IGenericCacheEntry<EnumInfo<TStorage>>
        {
            static RuntimeType.IGenericCacheEntry.GenericCacheKind RuntimeType.IGenericCacheEntry.Kind => RuntimeType.IGenericCacheEntry.GenericCacheKind.EnumInfo;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static EnumInfo<TStorage> CreateWithNames(RuntimeType type)
            {
                TStorage[]? values = null;
                string[]? names = null;

                GetEnumValuesAndNames(
                    new QCallTypeHandle(ref type),
                    ObjectHandleOnStack.Create(ref values),
                    ObjectHandleOnStack.Create(ref names),
                    Interop.BOOL.TRUE);

                Debug.Assert(values!.GetType() == typeof(TStorage[]));

                bool hasFlagsAttribute = type.IsDefined(typeof(FlagsAttribute), inherit: false);

                return new EnumInfo<TStorage>(hasFlagsAttribute, values, names!);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static EnumInfo<TStorage> Create(RuntimeType type)
            {
                TStorage[]? values = null;
                string[]? names = null;

                GetEnumValuesAndNames(
                    new QCallTypeHandle(ref type),
                    ObjectHandleOnStack.Create(ref values),
                    ObjectHandleOnStack.Create(ref names),
                    Interop.BOOL.FALSE);

                Debug.Assert(values!.GetType() == typeof(TStorage[]));

                bool hasFlagsAttribute = type.IsDefined(typeof(FlagsAttribute), inherit: false);

                return new EnumInfo<TStorage>(hasFlagsAttribute, values, null!);
            }
        }
    }
}
